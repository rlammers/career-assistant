[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$ResourceGroupName = 'career-assistant-private',
    [Parameter(Mandatory)]
    [string]$ExpectedSubscriptionId = $env:CAREER_ASSISTANT_AZURE_SUBSCRIPTION_ID,
    [Parameter(Mandatory)]
    [string]$ExpectedTenantId = $env:CAREER_ASSISTANT_AUTHENTICATION_TENANT_ID,
    [Parameter(Mandatory)]
    [uri]$ExpectedOrigin = $env:CAREER_ASSISTANT_AZURE_APPLICATION_ORIGIN,
    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9a-f]{64}$')]
    [string]$ExpectedJavaScriptSha256,
    [ValidateRange(30, 180)]
    [int]$TimeoutSeconds = 90,
    [string]$ChromePath = "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    [switch]$ActivateForDiagnostic
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-AzureText {
    param([string[]]$Arguments, [string]$FailureMessage)

    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = (& az @Arguments --only-show-errors 2>&1 | Out-String)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    if ($exitCode -ne 0) {
        throw $FailureMessage
    }

    $output
}

function Invoke-AzureJson {
    param([string[]]$Arguments, [string]$FailureMessage)

    $output = Invoke-AzureText -Arguments ($Arguments + @('--output', 'json')) -FailureMessage $FailureMessage
    if ([string]::IsNullOrWhiteSpace($output)) {
        return $null
    }

    $output | ConvertFrom-Json
}

function Send-CdpCommand {
    param(
        [Net.WebSockets.ClientWebSocket]$Socket,
        [int]$Id,
        [string]$Method,
        [hashtable]$Parameters
    )

    $payload = @{
        id = $Id
        method = $Method
        params = $Parameters
    } | ConvertTo-Json -Depth 8 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($payload)
    $segment = [ArraySegment[byte]]::new($bytes)
    [void]$Socket.SendAsync(
        $segment,
        [Net.WebSockets.WebSocketMessageType]::Text,
        $true,
        [Threading.CancellationToken]::None
    ).GetAwaiter().GetResult()
}

function Receive-CdpMessage {
    param(
        [Net.WebSockets.ClientWebSocket]$Socket,
        [Threading.CancellationToken]$CancellationToken
    )

    $buffer = New-Object byte[] 65536
    $stream = [IO.MemoryStream]::new()
    try {
        do {
            $segment = [ArraySegment[byte]]::new($buffer)
            $received = $Socket.ReceiveAsync($segment, $CancellationToken).GetAwaiter().GetResult()
            if ($received.MessageType -eq [Net.WebSockets.WebSocketMessageType]::Close) {
                return $null
            }

            $stream.Write($buffer, 0, $received.Count)
        } while (-not $received.EndOfMessage)

        [Text.Encoding]::UTF8.GetString($stream.ToArray()) | ConvertFrom-Json
    }
    finally {
        $stream.Dispose()
    }
}

function Get-RedirectUriQueryValue {
    param([uri]$AuthorizationUri)

    $pair = @(
        $AuthorizationUri.Query.TrimStart('?').Split('&') |
            Where-Object { $_.StartsWith('redirect_uri=', [StringComparison]::Ordinal) } |
            Select-Object -First 1
    )
    if ($pair.Count -ne 1) {
        return $null
    }

    [uri][Uri]::UnescapeDataString($pair[0].Substring('redirect_uri='.Length))
}

function Test-ExternalIngressEnabled {
    param($App)

    $ingress = $App.properties.configuration.ingress
    $null -ne $ingress -and
        $null -ne $ingress.PSObject.Properties['external'] -and
        [bool]$ingress.external
}

if ($null -eq (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Azure CLI is required.'
}
if (-not (Test-Path -LiteralPath $ChromePath)) {
    throw 'Google Chrome is required for the isolated browser capture.'
}
if ([string]::IsNullOrWhiteSpace($ExpectedSubscriptionId) -or
    [string]::IsNullOrWhiteSpace($ExpectedTenantId) -or
    $ExpectedOrigin.Scheme -ne 'https') {
    throw 'The expected subscription, tenant, and HTTPS application origin are required.'
}

$account = Invoke-AzureJson -Arguments @('account', 'show') -FailureMessage 'Azure account lookup failed.'
if ($account.state -ne 'Enabled' -or
    $account.id -cne $ExpectedSubscriptionId -or
    $account.tenantId -cne $ExpectedTenantId) {
    throw 'The selected Azure account is not the expected enabled subscription and tenant.'
}

$apps = @(
    Invoke-AzureJson -Arguments @('containerapp', 'list', '--resource-group', $ResourceGroupName) `
        -FailureMessage 'Container App inventory failed.' |
        ForEach-Object { $_ }
)
if ($apps.Count -ne 1) {
    throw 'Expected exactly one Container App in the target resource group.'
}

$app = $apps[0]
$appName = [string]$app.name
$externalIngressEnabled = Test-ExternalIngressEnabled -App $app
if ($externalIngressEnabled) {
    throw 'The application is not in the required fail-closed ingress state.'
}

$baseline = [ordered]@{
    AccountValidated = $true
    IngressDisabled = $true
    ExpectedOriginIsHttps = $true
    ChromeAvailable = $true
}

if (-not $ActivateForDiagnostic) {
    [pscustomobject]@{ Baseline = $baseline; DiagnosticRun = $false } | ConvertTo-Json -Compress
    return
}

if (-not $PSCmdlet.ShouldProcess(
    'the retained Container App revision',
    'Enable ingress for a bounded isolated-browser authentication redirect diagnostic'
)) {
    throw 'Authentication redirect diagnostic activation was not approved.'
}

$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$profilePath = [IO.Path]::GetFullPath(
    (Join-Path $tempRoot ('career-auth-browser-' + [Guid]::NewGuid().ToString('N')))
)
if (-not $profilePath.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Browser profile path validation failed.'
}

$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$debugPort = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
$listener.Stop()

$chrome = $null
$socket = $null
$cleanupVerified = $false
$result = [ordered]@{
    Status = 'Inconclusive'
    OutboundAuthorizationRequestCaptured = $false
    OutboundRedirectUsesLocalhost = $false
    OutboundRedirectUsesDeployedOrigin = $false
    ServedAssetMatchesActiveImage = $false
    CleanupVerified = $false
}

try {
    Invoke-AzureText -Arguments @(
        'containerapp', 'ingress', 'enable',
        '--name', $appName,
        '--resource-group', $ResourceGroupName,
        '--type', 'external',
        '--target-port', '8080',
        '--transport', 'auto',
        '--output', 'none'
    ) -FailureMessage 'External ingress could not be enabled.' | Out-Null

    $enabledApp = Invoke-AzureJson -Arguments @(
        'containerapp', 'show',
        '--name', $appName,
        '--resource-group', $ResourceGroupName
    ) -FailureMessage 'Enabled ingress state lookup failed.'
    if (-not (Test-ExternalIngressEnabled -App $enabledApp)) {
        throw 'External ingress could not be verified as enabled.'
    }

    $rootResponse = $null
    $readinessDeadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        try {
            $rootResponse = Invoke-WebRequest -Uri $ExpectedOrigin -UseBasicParsing -TimeoutSec 15 -Headers @{
                'Cache-Control' = 'no-cache'
                Pragma = 'no-cache'
            }
            if ($rootResponse.StatusCode -eq 200) {
                break
            }
        }
        catch {
            $rootResponse = $null
        }
        Start-Sleep -Seconds 3
    } while ([DateTime]::UtcNow -lt $readinessDeadline)

    if ($null -eq $rootResponse -or $rootResponse.StatusCode -ne 200) {
        throw 'The deployed frontend did not become reachable.'
    }

    $assetMatch = [regex]::Match(
        $rootResponse.Content,
        'assets/(?<file>index-[^"'']+\.js)'
    )
    if (-not $assetMatch.Success) {
        throw 'The served JavaScript asset was not found.'
    }

    $assetUri = [uri]::new($ExpectedOrigin, '/assets/' + $assetMatch.Groups['file'].Value)
    $webClient = New-Object Net.WebClient
    $webClient.Headers.Add('Cache-Control', 'no-cache')
    try {
        $servedBytes = $webClient.DownloadData($assetUri)
    }
    finally {
        $webClient.Dispose()
    }

    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        $servedHash = ([BitConverter]::ToString($sha256.ComputeHash($servedBytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
    $result.ServedAssetMatchesActiveImage = $servedHash -ceq $ExpectedJavaScriptSha256

    New-Item -ItemType Directory -Path $profilePath | Out-Null
    $chromeArguments = @(
        '--headless=new',
        "--remote-debugging-port=$debugPort",
        '--remote-allow-origins=*',
        "--user-data-dir=$profilePath",
        '--incognito',
        '--disable-extensions',
        '--disable-background-networking',
        '--no-first-run',
        '--no-default-browser-check',
        '--disk-cache-size=1',
        'about:blank'
    )
    $chrome = Start-Process -FilePath $ChromePath -ArgumentList $chromeArguments -WindowStyle Hidden -PassThru

    $targets = $null
    $debugDeadline = [DateTime]::UtcNow.AddSeconds(20)
    do {
        try {
            $targets = Invoke-RestMethod -Uri "http://127.0.0.1:$debugPort/json/list" -TimeoutSec 2
            if ($targets) {
                break
            }
        }
        catch {
            $targets = $null
        }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $debugDeadline)

    $page = @($targets | Where-Object { $_.type -eq 'page' } | Select-Object -First 1)
    if ($page.Count -ne 1) {
        throw 'The isolated browser debugging target was unavailable.'
    }

    $socket = [Net.WebSockets.ClientWebSocket]::new()
    [void]$socket.ConnectAsync(
        [uri]$page[0].webSocketDebuggerUrl,
        [Threading.CancellationToken]::None
    ).GetAwaiter().GetResult()

    Send-CdpCommand -Socket $socket -Id 1 -Method 'Network.enable' -Parameters @{}
    Send-CdpCommand -Socket $socket -Id 2 -Method 'Page.enable' -Parameters @{}
    Send-CdpCommand -Socket $socket -Id 3 -Method 'Page.navigate' -Parameters @{
        url = $ExpectedOrigin.AbsoluteUri
    }
    Start-Sleep -Seconds 3
    Send-CdpCommand -Socket $socket -Id 4 -Method 'Runtime.evaluate' -Parameters @{
        expression = '(()=>{window.__careerDiag=setInterval(()=>{const b=document.querySelector("button.auth-button");if(b){clearInterval(window.__careerDiag);b.click()}},100);return true})()'
        returnByValue = $true
    }

    $captureCancellation = [Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(30))
    try {
        while (-not $captureCancellation.IsCancellationRequested) {
            $message = Receive-CdpMessage -Socket $socket -CancellationToken $captureCancellation.Token
            if ($null -eq $message) {
                break
            }
            if ($null -eq $message.PSObject.Properties['method'] -or
                $message.method -ne 'Network.requestWillBeSent') {
                continue
            }

            $requestUri = [uri][string]$message.params.request.url
            if ($requestUri.Host -ne 'login.microsoftonline.com' -or
                $requestUri.AbsolutePath -notmatch '/oauth2/v2\.0/authorize$') {
                continue
            }

            $redirectUri = Get-RedirectUriQueryValue -AuthorizationUri $requestUri
            if ($null -eq $redirectUri) {
                continue
            }

            $result.OutboundAuthorizationRequestCaptured = $true
            $result.OutboundRedirectUsesLocalhost =
                $redirectUri.Host -eq 'localhost' -and $redirectUri.Port -eq 5173
            $result.OutboundRedirectUsesDeployedOrigin =
                $redirectUri.GetLeftPart([UriPartial]::Authority).TrimEnd('/') -ceq
                $ExpectedOrigin.GetLeftPart([UriPartial]::Authority).TrimEnd('/')
            break
        }
    }
    finally {
        $captureCancellation.Dispose()
    }

    if (-not $result.OutboundAuthorizationRequestCaptured) {
        throw 'The outbound authorization request was not captured.'
    }
    if (-not $result.ServedAssetMatchesActiveImage) {
        throw 'The externally served JavaScript does not match the expected image asset.'
    }
    if ($result.OutboundRedirectUsesLocalhost -or -not $result.OutboundRedirectUsesDeployedOrigin) {
        throw 'The outbound authorization request does not use the deployed application origin.'
    }

    $result.Status = 'Passed'
}
finally {
    if ($null -ne $socket) {
        try {
            $socket.Dispose()
        }
        catch {
        }
    }
    if ($null -ne $chrome) {
        Stop-Process -Id $chrome.Id -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path -LiteralPath $profilePath) {
        $browserProcesses = @(
            Get-CimInstance Win32_Process |
                Where-Object { $_.CommandLine -and $_.CommandLine.Contains($profilePath) }
        )
        foreach ($browserProcess in $browserProcesses) {
            Stop-Process -Id $browserProcess.ProcessId -Force -ErrorAction SilentlyContinue
        }
        Start-Sleep -Milliseconds 500
    }

    $currentApp = $null
    try {
        $currentApp = Invoke-AzureJson -Arguments @(
            'containerapp', 'show',
            '--name', $appName,
            '--resource-group', $ResourceGroupName
        ) -FailureMessage 'Ingress cleanup state lookup failed.'
    }
    catch {
    }

    if ($null -ne $currentApp -and (Test-ExternalIngressEnabled -App $currentApp)) {
        try {
            Invoke-AzureText -Arguments @(
                'containerapp', 'ingress', 'disable',
                '--name', $appName,
                '--resource-group', $ResourceGroupName,
                '--output', 'none'
            ) -FailureMessage 'External ingress cleanup failed.' | Out-Null
        }
        catch {
        }
    }

    try {
        $finalApp = Invoke-AzureJson -Arguments @(
            'containerapp', 'show',
            '--name', $appName,
            '--resource-group', $ResourceGroupName
        ) -FailureMessage 'Final ingress state lookup failed.'
        $cleanupVerified = -not (Test-ExternalIngressEnabled -App $finalApp)
    }
    catch {
        $cleanupVerified = $false
    }
    $result.CleanupVerified = $cleanupVerified

    if ((Test-Path -LiteralPath $profilePath) -and
        $profilePath.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $profilePath -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if (-not $cleanupVerified) {
    throw 'External ingress cleanup could not be verified.'
}

[pscustomobject]$result | ConvertTo-Json -Compress
