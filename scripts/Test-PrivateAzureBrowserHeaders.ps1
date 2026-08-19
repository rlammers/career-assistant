[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$ResourceGroupName = 'career-assistant-private',
    [Parameter(Mandatory)]
    [string]$ExpectedSubscriptionId = $env:CAREER_ASSISTANT_AZURE_SUBSCRIPTION_ID,
    [Parameter(Mandatory)]
    [string]$ExpectedTenantId = $env:CAREER_ASSISTANT_AUTHENTICATION_TENANT_ID,
    [ValidateRange(30, 900)]
    [int]$TimeoutSeconds = 180,
    [switch]$ActivateForDiagnostic
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$requiredHeaders = @('Content-Security-Policy', 'Strict-Transport-Security', 'X-Content-Type-Options', 'X-Frame-Options', 'Referrer-Policy', 'Permissions-Policy')

function Invoke-AzureJson {
    param([string[]]$Arguments, [string]$FailureMessage)
    $output = (& az @Arguments --only-show-errors --output json 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
    if ([string]::IsNullOrWhiteSpace($output)) { return $null }
    $parsed = $output | ConvertFrom-Json
    if ($parsed -is [System.Collections.IEnumerable] -and $parsed -isnot [string]) {
        foreach ($item in $parsed) { Write-Output $item }
    }
    else { Write-Output $parsed }
}

function Invoke-AzureText {
    param([string[]]$Arguments, [string]$FailureMessage)
    $output = (& az @Arguments --only-show-errors 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
    $output
}

function Test-RequiredHeaders {
    param($Headers)
    @($requiredHeaders | Where-Object { @($Headers.Keys) -notcontains $_ }).Count -eq 0
}

function Get-ExternalHeaderResult {
    param([uri]$Uri, [ValidateSet('Get', 'Head')][string]$Method)
    $response = Invoke-WebRequest -Uri $Uri -Method $Method -MaximumRedirection 0 -SkipHttpErrorCheck -Headers @{ 'Cache-Control' = 'no-cache'; Pragma = 'no-cache' } -TimeoutSec 30
    [pscustomobject]@{
        StatusCode = [int]$response.StatusCode
        Protocol = [string]$response.BaseResponse.Version
        RequiredHeadersPresent = Test-RequiredHeaders -Headers $response.Headers
    }
}

function Wait-ForHttpsEndpoint {
    param([uri]$Uri)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        try {
            return Get-ExternalHeaderResult -Uri $Uri -Method Get
        }
        catch {
            Start-Sleep -Seconds 5
        }
    } while ([DateTime]::UtcNow -lt $deadline)
    throw 'The HTTPS endpoint did not become reachable before the timeout.'
}

if ($null -eq (Get-Command az -ErrorAction SilentlyContinue)) { throw 'Azure CLI is required.' }
if ([string]::IsNullOrWhiteSpace($ExpectedSubscriptionId) -or [string]::IsNullOrWhiteSpace($ExpectedTenantId)) { throw 'ExpectedSubscriptionId and ExpectedTenantId are required; supply them through the approved private operator environment.' }

$account = Invoke-AzureJson -Arguments @('account', 'show') -FailureMessage 'Azure account lookup failed.'
if ($account.state -ne 'Enabled' -or $account.id -cne $ExpectedSubscriptionId -or $account.tenantId -cne $ExpectedTenantId) { throw 'The selected Azure account is not the expected enabled subscription and tenant.' }

$apps = @(Invoke-AzureJson -Arguments @('containerapp', 'list', '--resource-group', $ResourceGroupName) -FailureMessage 'Container App inventory failed.' | ForEach-Object { $_ })
if ($apps.Count -ne 1) { throw 'Expected exactly one Container App in the target resource group.' }
$app = $apps[0]
$appName = [string]$app.name
$revisions = @(Invoke-AzureJson -Arguments @('containerapp', 'revision', 'list', '--name', $appName, '--resource-group', $ResourceGroupName, '--all') -FailureMessage 'Container App revision inventory failed.' | ForEach-Object { $_ })
$activeRevisions = @($revisions | Where-Object { $_.properties.active })
$ingress = $app.properties.configuration.ingress
$externalIngressEnabled = $null -ne $ingress -and $null -ne $ingress.PSObject.Properties['external'] -and [bool]$ingress.external
if ($activeRevisions.Count -ne 1 -or $externalIngressEnabled) { throw 'The application is not in the required Single revision fail-closed baseline state.' }

$revision = Invoke-AzureJson -Arguments @('containerapp', 'revision', 'show', '--name', $appName, '--resource-group', $ResourceGroupName, '--revision', $activeRevisions[0].name) -FailureMessage 'Active revision detail lookup failed.'
$frontend = @($revision.properties.template.containers | Where-Object { $_.name -eq 'frontend' })
$backend = @($revision.properties.template.containers | Where-Object { $_.name -eq 'backend' })
if ($frontend.Count -ne 1 -or $backend.Count -ne 1) { throw 'The diagnostic revision does not contain exactly the expected frontend and backend containers.' }
$backendEnvironmentNames = @($backend[0].env | ForEach-Object { $_.name })
$baseline = [ordered]@{
    AccountValidated = $true
    IngressDisabled = -not $externalIngressEnabled
    SingleRevisionMode = $app.properties.configuration.activeRevisionsMode -eq 'Single'
    OneActiveRevision = $activeRevisions.Count -eq 1
    OneReplica = $revision.properties.template.scale.minReplicas -eq 1 -and $revision.properties.template.scale.maxReplicas -eq 1
    FrontendDigestQualified = $frontend[0].image -match '@sha256:[0-9a-f]{64}$'
    BackendDigestQualified = $backend[0].image -match '@sha256:[0-9a-f]{64}$'
    MockProvider = @($backend[0].env | Where-Object { $_.name -eq 'AI__Provider' -and $_.value -eq 'Mock' }).Count -eq 1
    PaidProviderKeyEnvironmentAbsent = @($backendEnvironmentNames | Where-Object { $_ -match '(?i)(openai.*key|anthropic.*key|azure.*openai.*key)' }).Count -eq 0
}

if (@($baseline.Values | Where-Object { $_ -ne $true }).Count -gt 0) {
    throw 'The diagnostic baseline does not satisfy the required deployment controls.'
}

if (-not $ActivateForDiagnostic) {
    [pscustomobject]@{ Baseline = $baseline; DiagnosticRun = $false } | ConvertTo-Json -Compress
    return
}

$replicas = @(Invoke-AzureJson -Arguments @('containerapp', 'replica', 'list', '--name', $appName, '--resource-group', $ResourceGroupName, '--revision', $revision.name) -FailureMessage 'Active revision replica inventory failed.' | ForEach-Object { $_ })
$replicaState = [ordered]@{
    ActiveRevisionRunningState = [string]$revision.properties.runningState
    ActiveRevisionHealthState = [string]$revision.properties.healthState
    ReplicaCount = $replicas.Count
    ReadyReplicaCount = @($replicas | Where-Object { @($_.properties.containers | Where-Object { $_.ready }).Count -eq 2 }).Count
}

$env:CAREER_ASSISTANT_AZURE_SUBSCRIPTION_ID = $ExpectedSubscriptionId
$env:CAREER_ASSISTANT_AUTHENTICATION_TENANT_ID = $ExpectedTenantId
$safeStateScript = Join-Path $PSScriptRoot 'Set-PrivateAzureSafeState.ps1'
$artifactDirectory = Join-Path $PSScriptRoot '..\artifacts'
$resultPath = Join-Path $artifactDirectory 'browser-header-diagnostic.json'
$result = [ordered]@{ Status = 'Inconclusive'; InternalHeaders = $false; ExternalHeaders = $false; MissingHeaders = @(); HttpStatus = $null; FailureReason = $null; CleanupSucceeded = $false }

try {
    if (-not $PSCmdlet.ShouldProcess('the retained Container App revision', 'Enable ingress for a bounded browser-header diagnostic')) {
        throw 'Browser-header diagnostic activation was not approved.'
    }
    Invoke-AzureText -Arguments @('containerapp', 'ingress', 'enable', '--name', $appName, '--resource-group', $ResourceGroupName, '--type', 'external', '--target-port', '8080', '--transport', 'auto') -FailureMessage 'External ingress could not be enabled for the diagnostic.'

    $runningApp = Invoke-AzureJson -Arguments @('containerapp', 'show', '--name', $appName, '--resource-group', $ResourceGroupName) -FailureMessage 'Container App ingress lookup failed.'
    $origin = [uri]('https://' + $runningApp.properties.configuration.ingress.fqdn + '/')
    $reachableRoot = Wait-ForHttpsEndpoint -Uri $origin
    $internalHeaders = Invoke-AzureText -Arguments @('containerapp', 'exec', '--name', $appName, '--resource-group', $ResourceGroupName, '--revision', $revision.name, '--container', 'frontend', '--command', "sh -c 'wget -q -S --spider http://127.0.0.1:8080/ 2>&1'") -FailureMessage 'Frontend-container header request failed.'
    $renderedNginx = Invoke-AzureText -Arguments @('containerapp', 'exec', '--name', $appName, '--resource-group', $ResourceGroupName, '--revision', $revision.name, '--container', 'frontend', '--command', "sh -c 'nginx -T 2>&1'") -FailureMessage 'Rendered nginx configuration inspection failed.'

    $internalPresent = @($requiredHeaders | Where-Object { $internalHeaders -notmatch [regex]::Escape($_) }).Count -eq 0
    $missingHeaders = if ($reachableRoot.RequiredHeadersPresent) { @() } else { $requiredHeaders }
    $result.InternalHeaders = $internalPresent
    $result.ExternalHeaders = [bool]$reachableRoot.RequiredHeadersPresent
    $result.MissingHeaders = $missingHeaders
    $result.HttpStatus = $reachableRoot.StatusCode
    $result.Status = if ($internalPresent -and $reachableRoot.RequiredHeadersPresent) { 'Passed' } else { 'Failed' }
}
catch {
    $result.Status = 'Inconclusive'
    $result.FailureReason = $_.Exception.Message
}
finally {
    $safeStateArguments = @{
        ResourceGroupName = $ResourceGroupName
        ExpectedSubscriptionId = $ExpectedSubscriptionId
        ExpectedTenantId = $ExpectedTenantId
        TimeoutSeconds = $TimeoutSeconds
    }
    if ($WhatIfPreference) { $safeStateArguments.WhatIf = $true }
    try {
        $safeState = & $safeStateScript @safeStateArguments | ConvertFrom-Json
        $safeStateExitCode = $LASTEXITCODE
        $result.CleanupSucceeded = $safeStateExitCode -eq 0 -and [bool]$safeState.SafeStateVerified
        if (-not $result.CleanupSucceeded) { $result.Status = 'Inconclusive'; $result.FailureReason = 'External ingress cleanup could not be verified.' }
    }
    catch { $result.Status = 'Inconclusive'; $result.FailureReason = 'External ingress cleanup failed.' }
}

$persistedResult = [ordered]@{
    Status = $result.Status
    HttpStatus = $result.HttpStatus
    MissingHeaders = @($result.MissingHeaders)
    FailureReason = $result.FailureReason
    CleanupStatus = if ($result.CleanupSucceeded) { 'Verified' } else { 'Unverified' }
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
}
New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
$resultJson = [pscustomobject]$persistedResult | ConvertTo-Json -Depth 5 -Compress
[System.IO.File]::WriteAllText($resultPath, $resultJson + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
[Console]::Out.WriteLine($resultJson)
exit $(if ($result.Status -eq 'Passed') { 0 } elseif ($result.Status -eq 'Failed') { 1 } else { 2 })
