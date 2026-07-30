[CmdletBinding()]
param(
    [string]$ResourceGroupName = 'career-assistant-private',
    [Parameter(Mandatory)]
    [string]$ExpectedSubscriptionId = $env:CAREER_ASSISTANT_AZURE_SUBSCRIPTION_ID,
    [Parameter(Mandatory)]
    [string]$ExpectedTenantId = $env:CAREER_ASSISTANT_AUTHENTICATION_TENANT_ID,
    [ValidateRange(1, 100000)]
    [int]$MinimumFreeVCoreSeconds = 10000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-AzureJson {
    param([string[]]$Arguments, [string]$FailureMessage)

    $output = (& az @Arguments --only-show-errors --output json 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
    if ([string]::IsNullOrWhiteSpace($output)) { return $null }

    $parsed = $output | ConvertFrom-Json
    if ($parsed -is [System.Collections.IEnumerable] -and $parsed -isnot [string]) {
        foreach ($item in $parsed) { Write-Output $item }
    }
    else {
        Write-Output $parsed
    }
}

if ($null -eq (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Azure CLI is required.'
}

if ([string]::IsNullOrWhiteSpace($ExpectedSubscriptionId) -or
    [string]::IsNullOrWhiteSpace($ExpectedTenantId)) {
    throw 'Expected subscription and tenant values are required through the private operator environment.'
}

$account = Invoke-AzureJson -Arguments @('account', 'show') -FailureMessage 'Azure account lookup failed.'
if ($account.state -ne 'Enabled' -or
    $account.id -cne $ExpectedSubscriptionId -or
    $account.tenantId -cne $ExpectedTenantId) {
    throw 'The selected Azure account is not the expected enabled subscription and tenant.'
}

$apps = @(Invoke-AzureJson -Arguments @(
    'containerapp', 'list',
    '--resource-group', $ResourceGroupName
) -FailureMessage 'Container App inventory failed.' | ForEach-Object { $_ })

if ($apps.Count -ne 1) {
    throw 'Expected exactly one Container App in the private resource group.'
}

$app = $apps[0]
$ingress = $app.properties.configuration.ingress
$externalIngressEnabled =
    $null -ne $ingress -and
    $null -ne $ingress.PSObject.Properties['external'] -and
    [bool]$ingress.external

if ($externalIngressEnabled -or $app.properties.configuration.activeRevisionsMode -ne 'Single') {
    throw 'The Container App is not in the required fail-closed Single revision state.'
}

$activeRevisions = @(Invoke-AzureJson -Arguments @(
    'containerapp', 'revision', 'list',
    '--name', [string]$app.name,
    '--resource-group', $ResourceGroupName,
    '--all'
) -FailureMessage 'Container App revision inventory failed.' |
    ForEach-Object { $_ } |
    Where-Object { $_.properties.active })

if ($activeRevisions.Count -ne 1 -or $activeRevisions[0].properties.healthState -ne 'Healthy') {
    throw 'Expected exactly one healthy active Container App revision.'
}

$servers = @(Invoke-AzureJson -Arguments @(
    'sql', 'server', 'list',
    '--resource-group', $ResourceGroupName
) -FailureMessage 'Azure SQL server inventory failed.' | ForEach-Object { $_ })

if ($servers.Count -ne 1) {
    throw 'Expected exactly one Azure SQL server in the private resource group.'
}

$serverName = [string]$servers[0].name
$databases = @(Invoke-AzureJson -Arguments @(
    'sql', 'db', 'list',
    '--resource-group', $ResourceGroupName,
    '--server', $serverName
) -FailureMessage 'Azure SQL database inventory failed.' |
    ForEach-Object { $_ } |
    Where-Object { ([string]$_.name).Trim() -ne 'master' })

if ($databases.Count -ne 1) {
    throw 'Expected exactly one application Azure SQL database.'
}

$databaseName = ([string]$databases[0].name).Trim()
$database = Invoke-AzureJson -Arguments @(
    'sql', 'db', 'show',
    '--resource-group', $ResourceGroupName,
    '--server', $serverName,
    '--name', $databaseName
) -FailureMessage 'Azure SQL database lookup failed.'

if ($database.sku.tier -ne 'GeneralPurpose' -or
    [int]$database.autoPauseDelay -ne 60 -or
    $database.freeLimitExhaustionBehavior -ne 'AutoPause' -or
    -not [bool]$database.useFreeLimit) {
    throw 'The database does not have the expected cost-controlled serverless configuration.'
}

$now = [DateTime]::UtcNow
$monthStart = [DateTime]::new($now.Year, $now.Month, 1, 0, 0, 0, [DateTimeKind]::Utc)
$metricResult = Invoke-AzureJson -Arguments @(
    'monitor', 'metrics', 'list',
    '--resource', [string]$database.id,
    '--metric', 'free_amount_remaining',
    '--interval', 'PT1H',
    '--start-time', $monthStart.ToString('o'),
    '--end-time', $now.ToString('o'),
    '--aggregation', 'Average'
) -FailureMessage 'Azure SQL free-allowance metric lookup failed.'

$freeSamples = @(
    $metricResult.value |
        ForEach-Object { $_.timeseries } |
        ForEach-Object { $_.data } |
        ForEach-Object { $_ } |
        Where-Object {
            $null -ne $_.PSObject.Properties['average'] -and
            $null -ne $_.average
        }
)

if ($freeSamples.Count -eq 0) {
    throw 'Azure SQL free-allowance telemetry is unavailable.'
}

$freeVCoreSeconds = [double]($freeSamples | Select-Object -Last 1).average
if ($freeVCoreSeconds -lt $MinimumFreeVCoreSeconds) {
    throw 'Azure SQL free compute is below the required verification threshold.'
}

$initialStatus = [string]$database.status
if ($initialStatus -notin @('Online', 'Paused')) {
    throw 'Azure SQL is neither Online nor safely Paused.'
}

[pscustomobject]@{
    Status                    = 'Passed'
    AccountValidated          = $true
    IngressDisabled           = $true
    SingleRevisionMode        = $true
    OneHealthyActiveRevision  = $true
    CostControlsUnchanged     = $true
    FreeAllowanceSufficient   = $true
    DatabaseOnline            = $initialStatus -eq 'Online'
    AuthenticatedWakeRequired = $initialStatus -eq 'Paused'
} | ConvertTo-Json -Compress
