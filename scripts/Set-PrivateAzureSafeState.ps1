[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = "High")]
param(
    [string]$ResourceGroupName = "career-assistant-private",
    [string]$ExpectedSubscriptionId = $env:CAREER_ASSISTANT_AZURE_SUBSCRIPTION_ID,
    [string]$ExpectedTenantId = $env:CAREER_ASSISTANT_AUTHENTICATION_TENANT_ID,
    [ValidateRange(30, 900)]
    [int]$TimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-AzureJson {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,
        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    $output = (& az @Arguments --only-show-errors --output json 2>&1 | Out-String)

    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }

    if ([string]::IsNullOrWhiteSpace($output)) {
        return $null
    }

    return $output | ConvertFrom-Json
}

function Invoke-AzureMutation {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,
        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    $null = (& az @Arguments --only-show-errors --output none 2>&1 | Out-String)

    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

function Get-DeploymentState {
    param(
        [Parameter(Mandatory)]
        [string]$ContainerAppName
    )

    $application = Invoke-AzureJson `
        -Arguments @("containerapp", "show", "--name", $ContainerAppName, "--resource-group", $ResourceGroupName) `
        -FailureMessage "Container App state lookup failed."
    $revisions = @(
        Invoke-AzureJson `
            -Arguments @("containerapp", "revision", "list", "--name", $ContainerAppName, "--resource-group", $ResourceGroupName, "--all") `
            -FailureMessage "Container App revision inventory failed."
    )
    $activeRevisions = @(
        $revisions |
            Where-Object {
                $null -ne $_.PSObject.Properties["properties"] `
                    -and [bool]$_.properties.active
            }
    )
    $liveContainerStateCount = 0

    foreach ($revision in $revisions) {
        $replicas = @(
            Invoke-AzureJson `
                -Arguments @("containerapp", "replica", "list", "--name", $ContainerAppName, "--resource-group", $ResourceGroupName, "--revision", $revision.name) `
                -FailureMessage "Container App replica inventory failed."
        )

        foreach ($replica in $replicas) {
            $replicaProperties = $replica.PSObject.Properties["properties"]

            if ($null -eq $replicaProperties) {
                continue
            }

            $containers = $replicaProperties.Value.PSObject.Properties["containers"]

            if ($null -eq $containers) {
                continue
            }

            foreach ($container in @($containers.Value)) {
                $runningState = $container.PSObject.Properties["runningState"]
                $started = $container.PSObject.Properties["started"]
                $ready = $container.PSObject.Properties["ready"]

                if (($null -ne $runningState -and $runningState.Value) `
                    -or ($null -ne $started -and $started.Value) `
                    -or ($null -ne $ready -and $ready.Value)) {
                    $liveContainerStateCount++
                }
            }
        }
    }

    $ingress = $application.properties.configuration.ingress

    return [pscustomobject]@{
        IngressDisabled        = $null -eq $ingress -or $ingress.external -eq $false
        RevisionCount          = $revisions.Count
        ActiveRevisionCount    = $activeRevisions.Count
        LiveContainerStateCount = $liveContainerStateCount
        ActiveRevisions        = $activeRevisions
    }
}

if ($null -eq (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI is required."
}

if ([string]::IsNullOrWhiteSpace($ExpectedTenantId)) {
    throw "ExpectedTenantId or CAREER_ASSISTANT_AUTHENTICATION_TENANT_ID is required."
}

if ([string]::IsNullOrWhiteSpace($ExpectedSubscriptionId)) {
    throw "ExpectedSubscriptionId or CAREER_ASSISTANT_AZURE_SUBSCRIPTION_ID is required."
}

$account = Invoke-AzureJson `
    -Arguments @("account", "show") `
    -FailureMessage "Azure account lookup failed."

if ($account.state -ne "Enabled" `
    -or $account.tenantId -cne $ExpectedTenantId `
    -or $account.id -cne $ExpectedSubscriptionId) {
    throw "The selected Azure account is not the expected enabled subscription and tenant."
}

$resourceGroup = Invoke-AzureJson `
    -Arguments @("group", "show", "--name", $ResourceGroupName) `
    -FailureMessage "Target resource group lookup failed."

if ($resourceGroup.properties.provisioningState -ne "Succeeded") {
    throw "The target resource group is not in a succeeded state."
}

$applications = @(
    Invoke-AzureJson `
        -Arguments @("containerapp", "list", "--resource-group", $ResourceGroupName) `
        -FailureMessage "Container App inventory failed."
)

if ($applications.Count -gt 1) {
    throw "The target resource group contains more than one Container App."
}

$ingressDisableRequested = $false
$revisionDeactivationCount = 0
$deploymentState = $null

if ($applications.Count -eq 1) {
    $applicationName = [string]$applications[0].name
    $deploymentState = Get-DeploymentState -ContainerAppName $applicationName

    if (-not $deploymentState.IngressDisabled) {
        $ingressDisableRequested = $true

        if ($PSCmdlet.ShouldProcess("the sole Container App", "Disable external ingress")) {
            Invoke-AzureMutation `
                -Arguments @("containerapp", "ingress", "disable", "--name", $applicationName, "--resource-group", $ResourceGroupName) `
                -FailureMessage "External ingress could not be disabled."
        }
    }

    foreach ($revision in $deploymentState.ActiveRevisions) {
        $revisionDeactivationCount++

        if ($PSCmdlet.ShouldProcess("an active Container App revision", "Deactivate")) {
            Invoke-AzureMutation `
                -Arguments @("containerapp", "revision", "deactivate", "--name", $applicationName, "--resource-group", $ResourceGroupName, "--revision", $revision.name) `
                -FailureMessage "An active revision could not be deactivated."
        }
    }

    if (-not $WhatIfPreference) {
        $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)

        do {
            $deploymentState = Get-DeploymentState -ContainerAppName $applicationName
            $safe = $deploymentState.IngressDisabled `
                -and $deploymentState.ActiveRevisionCount -eq 0 `
                -and $deploymentState.LiveContainerStateCount -eq 0

            if ($safe) {
                break
            }

            Start-Sleep -Seconds 5
        } while ([DateTime]::UtcNow -lt $deadline)

        if (-not $safe) {
            throw "The Container App did not converge to the required safe state."
        }
    }
}

$safeStateVerified = $applications.Count -eq 0 -or (
    $deploymentState.IngressDisabled `
        -and $deploymentState.ActiveRevisionCount -eq 0 `
        -and $deploymentState.LiveContainerStateCount -eq 0
)

[pscustomobject]@{
    WhatIf                    = [bool]$WhatIfPreference
    ApplicationCount         = $applications.Count
    IngressDisableRequested  = $ingressDisableRequested
    RevisionDeactivationCount = $revisionDeactivationCount
    SafeStateVerified        = $safeStateVerified
    ActiveRevisionCount      = if ($null -eq $deploymentState) { 0 } else { $deploymentState.ActiveRevisionCount }
    LiveContainerStateCount  = if ($null -eq $deploymentState) { 0 } else { $deploymentState.LiveContainerStateCount }
} | ConvertTo-Json -Compress
