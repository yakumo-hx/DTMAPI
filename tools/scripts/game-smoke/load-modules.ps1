# Dot-sourced in the compatibility entry's scope. Helpers do not start a game.
function Get-SmokeSelectedScenarioModules {
    param([Parameter(Mandatory = $true)] [System.Collections.IDictionary] $Parameters)
    $selected = New-Object 'System.Collections.Generic.List[string]'
    $has = {
        param([string] $Name)
        return $Parameters.Contains($Name) -and [bool]$Parameters[$Name]
    }
    if ((& $has 'StageQaHost') -or (& $has 'ValidateQaG4EvidenceCleanupOnly')) {
        $selected.Add('core/qa-host.ps1')
    }
    if ((& $has 'AutoExerciseMoreSavesOfficialSaveUi') -or
        ($Parameters.Contains('MoreSavesFixed12AcceptancePhase') -and $Parameters['MoreSavesFixed12AcceptancePhase'] -ne 'None')) {
        $selected.Add('scenarios/more-saves.ps1')
    }
    if ($Parameters.Contains('MoreEquipmentSlotsTransitionPhase') -and
        $Parameters['MoreEquipmentSlotsTransitionPhase'] -in @('ColdPrepare','ColdCommit','ColdObserve')) {
        $selected.Add('scenarios/more-equipment.ps1')
    }
    if ((& $has 'Issue011Acceptance') -or (& $has 'ValidateNoQaUiEvidenceGateOnly')) {
        $selected.Add('scenarios/issue011.ps1')
    }
    $desktopInput = (& $has 'AssertNoQaUiEvidence') -or (& $has 'ValidateNoQaUiEvidenceGateOnly') -or (& $has 'RequireExternalPlayerInputGate') -or (& $has 'AutoDriveNoQaAnimalViewer')
    foreach ($name in $Parameters.Keys) {
        if ($name -match '^(AutoExercise|AutoOpen|AutoPress|Batch6AutoFishing)' -and $name -cne 'AutoExerciseMoreSavesOfficialSaveUi' -and [bool]$Parameters[$name]) {
            $desktopInput = $true
        }
    }
    if ($desktopInput) { $selected.Add('scenarios/desktop-input.ps1') }
    return $selected.ToArray()
}

$SmokeLoadedModules = @('core/evidence.ps1', 'core/save-policy.ps1', 'core/session.ps1', 'core/deployment.ps1', 'core/diagnostics.ps1')
$SmokeLoadedModules += @(Get-SmokeSelectedScenarioModules -Parameters $SmokeRunnerBoundParameters)
foreach ($modulePath in $SmokeLoadedModules) {
    . (Join-Path $SmokeRunnerModuleRoot $modulePath)
}
