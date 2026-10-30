function Get-DtmApiReleaseTestSelection {
    param([string] $Stage = '', [string] $Focus = '')
    $scripts = @{
        ScriptContracts = [ordered]@{
            retention = 'test-runtime-evidence-retention.ps1'
            'reverse-path' = 'test-reverse-baseline-path-safety.ps1'
            'steam-identity' = 'test-steam-appmanifest-identity.ps1'
            'portable-reverse-path' = 'test-portable-reverse-capture-path-safety.ps1'
            'workshop-markers' = 'test-workshop-download-markers.ps1'
            routing = 'test-release-routing.ps1'
        }
        QaLifecycle = [ordered]@{
            boundary = 'check-batch4-qa-semantic-boundary.ps1'
            inventory = 'test-batch4-qa-semantic-inventory.ps1'
            'gc-ladder' = 'test-batch5-gc-ladder.ps1'
            'auto-fishing-gc' = 'test-batch6-autofishing-gc-ladder.ps1'
            'auto-fishing-behavior' = 'test-batch6-autofishing-behavior-matrix.ps1'
            'auto-fishing-lifecycle' = 'test-batch6-autofishing-manager-lifecycle.ps1'
            'no-demand' = 'test-batch5-no-demand-profile.ps1'
            'noqa-deadline' = 'test-noqa-deadline.ps1'
        }
        InstallerMatrices = [ordered]@{
            ownership = 'test-player-runtime-only-uninstall.ps1'
            'invalid-target' = 'test-installer-invalid-target-failure.ps1'
            upgrade = 'test-runtime-upgrade-transaction.ps1'
            'local-install' = 'test-developer-official-local-install-transaction.ps1'
        }
        Governance = [ordered]@{
            documents = 'check-doc-governance.ps1'
            artifacts = 'check-test-artifact-governance.ps1'
        }
    }
    $sdkTargets = [ordered]@{ 'optional-tools' = @() }
    $stages = @('BuildAndUnit', 'AuthorSdk', 'ProductContracts', 'RuntimeInstaller',
        'ScriptContracts', 'QaLifecycle', 'InstallerMatrices', 'Governance')
    if (-not $Stage) {
        if ($Focus) { throw '-Focus requires -Stage.' }
        return [pscustomobject]@{
            Stages = $stages
            Focuses = $scripts
            AuthorSdkFocuses = @($sdkTargets.Keys)
            Note = 'Choose one affected scope. BuildAndUnit requires a unit focus; AuthorSdk requires an SDK focus; ProductContracts requires CatalogId; RuntimeInstaller requires PackageRoot. All, StartAt and pre-0.6 compatibility gates are retired.'
        }
    }
    if ($Stage -notin $stages) { throw "Unknown or retired stage '$Stage'. Use -List; All/RetainedAbi are no longer validation scopes." }
    $scriptName = $null
    $targets = @()
    if ($scripts.ContainsKey($Stage)) {
        if (-not $Focus -or -not $scripts[$Stage].Contains($Focus)) { throw "Select a $Stage focus: $($scripts[$Stage].Keys -join ', ')." }
        $scriptName = $scripts[$Stage][$Focus]
    }
    elseif ($Stage -eq 'AuthorSdk') {
        if (-not $Focus -or -not $sdkTargets.Contains($Focus)) { throw "Unknown or retired SDK focus '$Focus'. Use -List for current checks; pre-0.6 SDK compatibility checks are retired." }
        $targets = @($sdkTargets[$Focus])
    }
    elseif ($Stage -eq 'BuildAndUnit') {
        if ([string]::IsNullOrWhiteSpace($Focus)) { throw 'BuildAndUnit requires an explicit unit -Focus; use test-unit.ps1 -List.' }
    }
    elseif ($Focus) { throw "-Focus does not apply to $Stage." }
    [pscustomobject]@{
        Stage = $Stage
        Focus = $Focus
        Script = $scriptName
        ApiTargets = $targets
        UsesConfiguration = ($Stage -eq 'InstallerMatrices' -or ($Stage -eq 'QaLifecycle' -and $Focus -in @('boundary', 'inventory')))
    }
}
function Assert-DtmApiFullTestEnvironment {
    $processEnvironment = [Environment]::GetEnvironmentVariables('Process')
    $filters = @($processEnvironment.Keys | Where-Object {
        $_ -like 'DTMAPI_*_TEST_FOCUS' -and
        -not [string]::IsNullOrEmpty([string]$processEnvironment[$_])
    } | Sort-Object)
    if ($filters.Count -gt 0) {
        throw "This explicitly broad source-test entry does not accept focus filters: $($filters -join ', '). Use the selected test entry with its explicit focus."
    }
}
