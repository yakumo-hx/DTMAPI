if ($ValidateSaveTestModeOnly -or $qaRoutingProjectionOnly) {
    throw 'A validation-only request must end before session preparation, build, deployment or launch.'
}
$launchViaSteam = [bool]$UseSteam -or -not [bool]$DirectExe
$existingGameProcess = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
if ($existingGameProcess) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'DolocTown.exe already running before smoke launch.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
    Write-SmokeBlockedResult -EvidencePath $evidence -Reason $blockedReason -RunStatus 'Blocked'
    Write-Error "DolocTown.exe is already running. Close the existing game/window before launching smoke. Evidence: $evidence"
    $SmokeRunnerExitCode = [int](1); return
}
$existingFatalWindow = Test-FatalInstanceWindow
if ($existingFatalWindow) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'Fatal instance popup already visible before smoke launch.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
    Write-SmokeBlockedResult -EvidencePath $evidence -Reason $blockedReason -RunStatus 'Aborted'
    Write-Error "Fatal instance popup is already visible. Close the dialog before launching smoke. Evidence: $evidence"
    $SmokeRunnerExitCode = [int](1); return
}
if (($batch5GcLadderAutoFishing -or ($AutoExerciseAutoFishingPhase -and -not $autoFishingNoDemandFrameProfile)) -and
    ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 5)) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'AutoFishing phase and Batch 5 AutoFishing smoke require the real fifth save fixture. Re-run with explicit -SaveSlot 5.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason`nSaveSlot=$SaveSlot`nSaveSlotExplicit=$($SmokeRunnerBoundParameters.ContainsKey('SaveSlot'))`nAutoExerciseAutoFishingPhase=$AutoExerciseAutoFishingPhase`nAutoFishingScenario=$autoFishingScenarioEffective`nAutoFishingCastChargeRatio=$AutoFishingCastChargeRatio`nAutoFishingSoakLoops=$AutoFishingSoakLoops`nAutoFishingToggleKey=$autoFishingToggleKeyEffective" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Write-SmokeJsonObject -Path (Join-Path $evidence 'result.json') -Value @{
        SchemaVersion = 2
        RunStatus = 'Blocked'
        RunStatusReason = $blockedReason
        StartupLog = 'Blocked'
        GameLaunched = 'Blocked'
        HookProbe = 'Skipped'
        SaveLoaded = 'Blocked'
        AutoFishingHotkey = 'Blocked'
        AutoFishingMovementCancel = 'Skipped'
        AutoFishingPhase = 'Blocked'
        AutoFishingMonoGate = Get-SmokeStatus -Requested ([bool]$AutoFishingMonoGate) -Passed $false -Blocked ([bool]$AutoFishingMonoGate)
        AutoFishingScenario = $autoFishingScenarioEffective
        AutoFishingInstantBite = Get-SmokeStatus -Requested ([bool]$autoFishingScenarioRequestsInstantBite) -Passed $false -Blocked $autoFishingScenarioRequestsInstantBite
        AutoFishingMiniGameSkip = Get-SmokeStatus -Requested ([bool]$autoFishingScenarioRequestsSkip) -Passed $false -Blocked $autoFishingScenarioRequestsSkip
        AutoFishingMiniGameComplete = Get-SmokeStatus -Requested ([bool]$autoFishingScenarioRequestsComplete) -Passed $false -Blocked $autoFishingScenarioRequestsComplete
        AutoFishingAnimationSpeed = Get-SmokeStatus -Requested ([bool]$autoFishingScenarioRequestsAnimationSpeed) -Passed $false -Blocked $autoFishingScenarioRequestsAnimationSpeed
        AutoFishingCastCharge = Get-SmokeStatus -Requested ([bool]$autoFishingScenarioRequestsCastCharge) -Passed $false -Blocked $autoFishingScenarioRequestsCastCharge
        AutoFishingLifecycle = 'Blocked'
        AutoFishingLifecycleSummary = ''
        AutoFishingSoak = Get-SmokeStatus -Requested ($AutoFishingSoakLoops -gt 0) -Passed $false -Blocked ($AutoFishingSoakLoops -gt 0)
        AutoFishingReportExport = 'Blocked'
        ModOwnerLifecycle = 'Skipped'
        ModOwnerLifecycleSummary = ''
        ModLoadTransaction = 'Skipped'
        OwnerBoundInput = 'Skipped'
        EventHandlerCleanup = 'Skipped'
        ConfigPreviewAudit = 'Skipped'
        FailedModRollback = 'Skipped'
        ContentRegistry = 'Skipped'
        ManifestRegistry = 'Skipped'
        DependencyCompatibility = 'Skipped'
        ContentPackOwnership = 'Skipped'
        RegistryDiffs = 'Skipped'
        GameBridgeFeatureContracts = 'Skipped'
        GameBridgeFinalHealthSnapshot = 'Skipped'
        GameBridgeFinalHealthSummary = ''
        SaveLoadRequestCoordinator = 'Skipped'
        SaveLoadRequestSummary = ''
        DuplicateLoadRequests = 'Skipped'
        SaveLoadBoundary = 'Skipped'
        TitleReturnBoundaryLedger = 'Skipped'
        TitleReturnBoundarySummary = ''
        HookScheduler = 'Skipped'
        CoreHookReadiness = 'Skipped'
        FeatureHookReadiness = 'Skipped'
        HookStatusQueue = 'Skipped'
        OffThreadHookRequests = 'Skipped'
        AssemblyLoadSubscription = 'Skipped'
        RetryTimerAlive = 'Skipped'
        NoFatalInstanceWindow = 'Passed'
        ProcessExited = 'Skipped'
        ForcedClose = 'Skipped'
        Completed = Get-Date -Format o
    }
    Write-Error "$blockedReason Evidence: $evidence"
    $SmokeRunnerExitCode = [int](1); return
}
if ($AutoExerciseVehicle) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'Vehicle custom motor smoke is archived after 2026-06-15 manual QA found severe texture pollution in the SecondMotor sample. IMotorVehicleApi and MotorVehicle GameBridge hooks are retired/removed from active runtime code; no active SecondMotor package is installed or validated.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason`nSaveSlot=$SaveSlot`nSaveSlotExplicit=$($SmokeRunnerBoundParameters.ContainsKey('SaveSlot'))`nAutoExerciseVehicle=$AutoExerciseVehicle" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Write-SmokeJsonObject -Path (Join-Path $evidence 'result.json') -Value @{
        SchemaVersion = 2
        RunStatus = 'Blocked'
        RunStatusReason = $blockedReason
        StartupLog = 'Blocked'
        GameLaunched = 'Blocked'
        HookProbe = 'Skipped'
        SaveLoaded = 'Blocked'
        VehicleSecondMotor = 'Blocked'
        ModOwnerLifecycle = 'Skipped'
        ModOwnerLifecycleSummary = ''
        ModLoadTransaction = 'Skipped'
        OwnerBoundInput = 'Skipped'
        EventHandlerCleanup = 'Skipped'
        ConfigPreviewAudit = 'Skipped'
        FailedModRollback = 'Skipped'
        ContentRegistry = 'Skipped'
        ManifestRegistry = 'Skipped'
        DependencyCompatibility = 'Skipped'
        ContentPackOwnership = 'Skipped'
        RegistryDiffs = 'Skipped'
        GameBridgeFeatureContracts = 'Skipped'
        GameBridgeFinalHealthSnapshot = 'Skipped'
        GameBridgeFinalHealthSummary = ''
        SaveLoadRequestCoordinator = 'Skipped'
        SaveLoadRequestSummary = ''
        DuplicateLoadRequests = 'Skipped'
        SaveLoadBoundary = 'Skipped'
        TitleReturnBoundaryLedger = 'Skipped'
        TitleReturnBoundarySummary = ''
        HookScheduler = 'Skipped'
        CoreHookReadiness = 'Skipped'
        FeatureHookReadiness = 'Skipped'
        HookStatusQueue = 'Skipped'
        OffThreadHookRequests = 'Skipped'
        AssemblyLoadSubscription = 'Skipped'
        RetryTimerAlive = 'Skipped'
        NoFatalInstanceWindow = 'Passed'
        ProcessExited = 'Skipped'
        ForcedClose = 'Skipped'
        Completed = Get-Date -Format o
    }
    Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
    Write-Error "$blockedReason Evidence: $evidence"
    $SmokeRunnerExitCode = [int](1); return
}
if ($AutoExerciseAudioReplacement -and ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or (($SaveSlot -ne 8) -and ($SaveSlot -ne 10)))) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'Audio replacement cardboard smoke requires a real paper-box fixture save. Re-run with explicit -SaveSlot 8 or -SaveSlot 10.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason`nSaveSlot=$SaveSlot`nSaveSlotExplicit=$($SmokeRunnerBoundParameters.ContainsKey('SaveSlot'))`nAutoExerciseAudioReplacement=$AutoExerciseAudioReplacement" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Write-SmokeJsonObject -Path (Join-Path $evidence 'result.json') -Value @{
        SchemaVersion = 2
        RunStatus = 'Blocked'
        RunStatusReason = $blockedReason
        StartupLog = 'Blocked'
        GameLaunched = 'Blocked'
        HookProbe = 'Skipped'
        SaveLoaded = 'Blocked'
        AudioReplacement = 'Blocked'
        ModOwnerLifecycle = 'Skipped'
        ModOwnerLifecycleSummary = ''
        ModLoadTransaction = 'Skipped'
        OwnerBoundInput = 'Skipped'
        EventHandlerCleanup = 'Skipped'
        ConfigPreviewAudit = 'Skipped'
        FailedModRollback = 'Skipped'
        ContentRegistry = 'Skipped'
        ManifestRegistry = 'Skipped'
        DependencyCompatibility = 'Skipped'
        ContentPackOwnership = 'Skipped'
        RegistryDiffs = 'Skipped'
        GameBridgeFeatureContracts = 'Skipped'
        GameBridgeFinalHealthSnapshot = 'Skipped'
        GameBridgeFinalHealthSummary = ''
        SaveLoadRequestCoordinator = 'Skipped'
        SaveLoadRequestSummary = ''
        DuplicateLoadRequests = 'Skipped'
        SaveLoadBoundary = 'Skipped'
        TitleReturnBoundaryLedger = 'Skipped'
        TitleReturnBoundarySummary = ''
        HookScheduler = 'Skipped'
        CoreHookReadiness = 'Skipped'
        FeatureHookReadiness = 'Skipped'
        HookStatusQueue = 'Skipped'
        OffThreadHookRequests = 'Skipped'
        AssemblyLoadSubscription = 'Skipped'
        RetryTimerAlive = 'Skipped'
        NoFatalInstanceWindow = 'Passed'
        ProcessExited = 'Skipped'
        ForcedClose = 'Skipped'
        Completed = Get-Date -Format o
    }
    Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
    Write-Error "$blockedReason Evidence: $evidence"
    $SmokeRunnerExitCode = [int](1); return
}
if ($AutoExerciseHatchAnimalVoice -and ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 7)) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'Hatch AnimalVoice smoke requires the real seventh save fixture. Re-run with explicit -SaveSlot 7.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason`nSaveSlot=$SaveSlot`nSaveSlotExplicit=$($SmokeRunnerBoundParameters.ContainsKey('SaveSlot'))`nAutoExerciseHatchAnimalVoice=$AutoExerciseHatchAnimalVoice" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Write-SmokeJsonObject -Path (Join-Path $evidence 'result.json') -Value @{
        SchemaVersion = 2
        RunStatus = 'Blocked'
        RunStatusReason = $blockedReason
        StartupLog = 'Blocked'
        GameLaunched = 'Blocked'
        HookProbe = 'Skipped'
        SaveLoaded = 'Blocked'
        HatchAnimalVoice = 'Blocked'
        ModOwnerLifecycle = 'Skipped'
        ModOwnerLifecycleSummary = ''
        ModLoadTransaction = 'Skipped'
        OwnerBoundInput = 'Skipped'
        EventHandlerCleanup = 'Skipped'
        ConfigPreviewAudit = 'Skipped'
        FailedModRollback = 'Skipped'
        GameBridgeFeatureContracts = 'Skipped'
        GameBridgeFinalHealthSnapshot = 'Skipped'
        GameBridgeFinalHealthSummary = ''
        SaveLoadRequestCoordinator = 'Skipped'
        SaveLoadRequestSummary = ''
        DuplicateLoadRequests = 'Skipped'
        SaveLoadBoundary = 'Skipped'
        TitleReturnBoundaryLedger = 'Skipped'
        TitleReturnBoundarySummary = ''
        HookScheduler = 'Skipped'
        CoreHookReadiness = 'Skipped'
        FeatureHookReadiness = 'Skipped'
        HookStatusQueue = 'Skipped'
        OffThreadHookRequests = 'Skipped'
        AssemblyLoadSubscription = 'Skipped'
        RetryTimerAlive = 'Skipped'
        NoFatalInstanceWindow = 'Passed'
        ProcessExited = 'Skipped'
        ForcedClose = 'Skipped'
        Completed = Get-Date -Format o
    }
    Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
    Write-Error "$blockedReason Evidence: $evidence"
    $SmokeRunnerExitCode = [int](1); return
}

$managerStatusRequested = [bool]$AutoOpenTitleSettingsStatusPage -or [bool]$AutoOpenTitleSettingsManagerMvp
$managerMvpRequested = [bool]$AutoOpenTitleSettingsManagerMvp
$titleSettingsRequested = [bool]$AutoOpenTitleSettingsMenu -or $managerStatusRequested
$includeRuntimeTestMods = [bool]$IncludeHookProbe -or $titleSettingsRequested -or [bool]$AutoExerciseAudioReplacement
$includeDebugConsoleMod = [bool]$AutoExerciseDebugConsole -or [bool]$AutoExerciseDebugConsoleMouseGive -or [bool]$AutoExerciseAdvancedDebug -or [bool]$AssertNoQaUiEvidence
$requiresDebugConsoleKeySmoke = [bool]$AutoExerciseDebugConsole -or [bool]$AutoExerciseDebugConsoleMouseGive -or [bool]$AssertNoQaUiEvidence
$internalDebugConsoleSmokeEnabled = $requiresDebugConsoleKeySmoke -and -not [bool]$RequireExternalPlayerInputGate -and -not [bool]$AssertNoQaUiEvidence
if (-not $SkipInstall) {
    $installArguments = @{
        IncludeTestMods = $includeRuntimeTestMods
        IncludeDebugConsoleMod = $includeDebugConsoleMod
        IncludeHookProbe = [bool]$IncludeHookProbe
        InstallQaFixtures = [bool]$AssertProductOwnerRefresh
        SkipBuild = [bool]$SkipBuild
    }
    if (-not [string]::IsNullOrWhiteSpace($PackagePayloadRoot)) {
        $installArguments.PackagePayloadRoot = [System.IO.Path]::GetFullPath($PackagePayloadRoot)
    }
    & "$SmokeRunnerScriptsRoot\install-to-game.ps1" @installArguments
    $installExit = if (Get-Variable LASTEXITCODE -ErrorAction SilentlyContinue) { $LASTEXITCODE } else { 0 }
    if ($installExit -ne 0) {
        $SmokeRunnerExitCode = [int]($installExit); return
    }
}
else {
    Write-Host 'Skipping source/package installation; the smoke run will exercise the currently installed Runtime and existing official packages unchanged.'
}

$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$dtmapiDir = Resolve-DtmApiStateDir -GameDir $gameDir
New-Item -ItemType Directory -Force -Path $dtmapiDir | Out-Null
$evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
$smokeRunStartedAt = Get-Date
$issue011LastGiveStatePath = [System.IO.Path]::GetFullPath((Join-Path $dtmapiDir 'debug-console-last-give.txt'))
$issue011LastGiveBaseline = if ($Issue011Acceptance) {
    Get-SmokeIssue011LastGiveBaseline -Path $issue011LastGiveStatePath
}
else { $null }
$issue011InteractiveEvidenceObserved = -not [bool]$Issue011Acceptance
$issue011AcceptanceProjection = $null
$issue011AcceptanceOk = -not [bool]$Issue011Acceptance
$issue011AcceptanceReceiptPath = if ($Issue011Acceptance) {
    [System.IO.Path]::GetFullPath((Join-Path $evidence 'issue-011-acceptance.json'))
}
else { '' }
Write-SmokeCrashBaseline -Path (Join-Path $evidence 'crash-baseline.txt') -RunStartedAt $smokeRunStartedAt
Write-SmokeJsonObject -Path (Join-Path $evidence 'save-test-mode.json') -Value ([ordered]@{
    SaveTestMode = $SaveTestMode
    SaveSlot = $SaveSlot
    DisposableSaveFixtureRoot = $disposableSaveFixtureRootResolved
    DisposableSaveFixtureSaveRoot = if ($disposableSaveFixtureRequested) { [System.IO.Path]::GetFullPath((Join-Path $disposableSaveFixtureRootResolved 'SAVE')) } else { '' }
    RequireDisposableSaveRedirect = $disposableSaveFixtureRequested
    QaIsolationOwner = if ($disposableSaveFixtureRequested) { 'qa-only pre-Runtime LocalSave.cloudDirPath exact Harmony owner plus native path probe' } else { 'none' }
    SteamAutoCloudIsolated = if ($null -eq $disposableSaveFixtureMarker) { $null } else { [bool]$disposableSaveFixtureMarker.steamAutoCloudIsolated }
    RoutinePlayerSaveByteBackupCreated = $false
    PlayerArchiveWritebackAllowed = $false
})
$batch6ManagerMarkerPath = ''
$batch6ManagerMarkerBackupPath = ''
$batch6ManagerMarkerAnyPathBefore = $false
$batch6ManagerMarkerExistedBefore = $false
$batch6ManagerMarkerLengthBefore = [int64]0
$batch6ManagerMarkerSha256Before = ''
$batch6ManagerMarkerRestoreOk = -not [bool]$Batch6AutoFishingManagerLifecycle
if ($Batch6AutoFishingManagerLifecycle) {
    $Batch6AutoFishingManagerProductRoot = [System.IO.Path]::GetFullPath($Batch6AutoFishingManagerProductRoot).TrimEnd([char]92, [char]47)
    $expectedManagerProductRoot = [System.IO.Path]::GetFullPath((Join-Path $gameDir 'Mods\Yuuka.DTMAPI.AutoFishing')).TrimEnd([char]92, [char]47)
    if (-not [string]::Equals($Batch6AutoFishingManagerProductRoot, $expectedManagerProductRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Batch 6 AutoFishing Manager lifecycle product root is outside the exact managed game Mods destination.'
    }
    $batch6ManagerMarkerPath = Join-Path $Batch6AutoFishingManagerProductRoot 'dtmapi.disabled'
    $batch6ManagerMarkerAnyPathBefore = Test-Path -LiteralPath $batch6ManagerMarkerPath
    $batch6ManagerMarkerExistedBefore = Test-Path -LiteralPath $batch6ManagerMarkerPath -PathType Leaf
    if ($batch6ManagerMarkerAnyPathBefore -and -not $batch6ManagerMarkerExistedBefore) {
        throw 'Batch 6 AutoFishing Manager lifecycle refuses a non-file dtmapi.disabled path.'
    }
    if ($batch6ManagerMarkerExistedBefore) {
        $batch6ManagerMarkerLengthBefore = [int64](Get-Item -LiteralPath $batch6ManagerMarkerPath).Length
        $batch6ManagerMarkerSha256Before = Get-SmokeFileSha256 -Path $batch6ManagerMarkerPath
        $batch6ManagerMarkerBackupPath = Join-Path $evidence 'dtmapi.disabled.before'
        Copy-Item -LiteralPath $batch6ManagerMarkerPath -Destination $batch6ManagerMarkerBackupPath
    }
    if ($Batch6AutoFishingManagerMode -eq 'SameProcessDisable' -and $batch6ManagerMarkerAnyPathBefore) {
        throw 'SameProcessDisable requires dtmapi.disabled to be absent at process start.'
    }
    if ($Batch6AutoFishingManagerMode -eq 'ColdDisabled' -and
        (-not $batch6ManagerMarkerExistedBefore -or -not [string]::Equals($batch6ManagerMarkerSha256Before, $Batch6AutoFishingManagerMarkerSha256, [System.StringComparison]::OrdinalIgnoreCase))) {
        throw 'ColdDisabled requires the exact receipt-bound dtmapi.disabled marker at process start.'
    }
    Write-SmokeJsonObject -Path (Join-Path $evidence 'batch6-autofishing-manager-marker-baseline.json') -Value ([ordered]@{
        Path = $batch6ManagerMarkerPath
        Existed = $batch6ManagerMarkerExistedBefore
        Length = $batch6ManagerMarkerLengthBefore
        Sha256 = $batch6ManagerMarkerSha256Before
        Mode = $Batch6AutoFishingManagerMode
    })
}
$noQaRuntimePreflight = $null
$noQaRuntimePostflight = $null
$noQaLegacyEvidenceBefore = @()
$noQaLegacyEvidenceComparisons = @()
$noQaLegacyEvidenceUnchangedOk = -not [bool]$AssertNoQaUiEvidence
$noQaRuntimePreflightOk = -not [bool]$AssertNoQaUiEvidence
$noQaRuntimePostflightOk = -not [bool]$AssertNoQaUiEvidence
if ($AssertNoQaUiEvidence) {
    $noQaRuntimePreflight = Get-SmokeNoQaRuntimeReceipt -GameDir $gameDir -StateDir $dtmapiDir
    $noQaRuntimePreflightOk = [bool]$noQaRuntimePreflight.Passed
    $noQaLegacyEvidenceBefore = @('DEBUG-CONSOLE-UI','ANIMAL-001','EQUIPMENT-SLOTS-UI' | ForEach-Object {
        Get-SmokeDirectoryReceipt -Path (Join-Path $dtmapiDir ('evidence\' + $_))
    })
    Write-SmokeJsonObject -Path (Join-Path $evidence 'no-qa-ui-preflight.json') -Value ([ordered]@{
        Runtime = $noQaRuntimePreflight
        LegacyEvidence = @($noQaLegacyEvidenceBefore)
        Passed = $noQaRuntimePreflightOk
    })
    if (-not $noQaRuntimePreflightOk) {
        throw "No-QA UI preflight found a QA receipt/root/DLL or a non-five-DLL Runtime shape. Evidence: $evidence"
    }
}
$publishedProductArtifactSnapshots = @()
$publishedProductPostExitArtifactSnapshots = @()
$publishedProductArtifactsUnchangedDuringRun = $true
$publishedProductArtifactsOk = $true
$publishedProductArtifactGateRequested = [bool]$AssertPublishedProductCombination -or [bool]$AssertPublishedProductsDisabled -or
    ([bool]$AssertNoQaUiEvidence -and $OfficialModProfile -eq 'Published11')
$externalPlayerInputHandshakeToken = if ($RequireExternalPlayerInputGate) { [Guid]::NewGuid().ToString('N') } else { '' }
$externalPlayerInputCompletionMarkerPath = if ($RequireExternalPlayerInputGate) { [System.IO.Path]::GetFullPath((Join-Path $evidence 'external-player-input-complete.signal')) } else { '' }
$externalWorkshopContentRoot = [System.IO.Path]::GetFullPath((Join-Path $gameDir '..\..\workshop\content\2285550'))
$expectedYConsoleWorkshopRoot = ''
$expectedYConsoleWorkshopDll = ''
$expectedYConsoleWorkshopDllSha256 = ''
$actualYConsoleWorkshopDllSha256 = ''
$externalPlayerInputWorkshopArtifactSnapshot = $null
$externalPlayerInputWorkshopDllExists = $false
$externalPlayerInputWorkshopDllLength = [int64]0
$externalPlayerInputWorkshopDllNonEmpty = $false
$externalPlayerInputWorkshopDllLengthOk = $false
$expectedYConsoleWorkshopDllLength = [int64]0
$externalPlayerInputWorkshopDllHashOk = $false
$externalPlayerInputArtifactOk = -not [bool]$RequireExternalPlayerInputGate
$externalPlayerInputDuplicateSelectionEvidenceAvailable = $false
$externalPlayerInputDuplicateSelectionCount = 0
$externalPlayerInputDuplicateSelectionOk = -not [bool]$RequireExternalPlayerInputGate
$externalPlayerInputDuplicateSelectionLines = @()
$externalPlayerInputLoadSourceCount = 0
$externalPlayerInputLoadSourceOk = -not [bool]$RequireExternalPlayerInputGate
$externalPlayerInputLoadSourceLines = @()
$moreEquipmentSlotsRetainedWorkshopArtifactSnapshot = $null
$moreEquipmentSlotsRetainedWorkshopPreflightOk =
    $MoreEquipmentSlotsTransitionPhase -ne 'U1'
if ($MoreEquipmentSlotsTransitionPhase -eq 'U1') {
    $moreEquipmentSlotsRetainedProducts = @(
        Get-SmokePublishedProductDefinitions |
            Where-Object {
                [string]$_.catalogId -eq 'more-equipment-slots' -and
                [string]$_.uniqueId -eq 'DTMAPI.MoreEquipmentSlotsMod' -and
                [string]$_.workshopId -eq '3744059735'
            })
    if ($moreEquipmentSlotsRetainedProducts.Count -ne 1) {
        throw "MoreEquipmentSlots U1 retained Workshop preflight requires exactly one Catalog more-equipment-slots / DTMAPI.MoreEquipmentSlotsMod / 3744059735 row; found $($moreEquipmentSlotsRetainedProducts.Count)."
    }

    $moreEquipmentSlotsRetainedProduct =
        $moreEquipmentSlotsRetainedProducts[0]
    $moreEquipmentSlotsRetainedArtifact =
        $moreEquipmentSlotsRetainedProduct.retainedArtifact
    if ($null -eq $moreEquipmentSlotsRetainedArtifact -or
        [int]$moreEquipmentSlotsRetainedArtifact.fileCount -ne 9 -or
        [int64]$moreEquipmentSlotsRetainedArtifact.bytes -ne 539565 -or
        -not [string]::Equals(
            [string]$moreEquipmentSlotsRetainedArtifact.treeSha256,
            'e0854cee94969d98b916a3f6085fd03773c67bcd35c7bc83dc2894f8156e0ca6',
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'MoreEquipmentSlots U1 retained Workshop preflight found Catalog drift from the frozen 9-file / 539565-byte / e0854cee... authority.'
    }

    $moreEquipmentSlotsRetainedWorkshopRoot =
        [System.IO.Path]::GetFullPath(
            (Join-Path $externalWorkshopContentRoot '3744059735'))
    $moreEquipmentSlotsRetainedWorkshopArtifactSnapshot =
        Get-SmokeWorkshopArtifactSnapshot `
            -Path $moreEquipmentSlotsRetainedWorkshopRoot `
            -Product $moreEquipmentSlotsRetainedProduct `
            -ArtifactBoundary Retained
    $moreEquipmentSlotsRetainedWorkshopPreflightOk =
        [bool]$moreEquipmentSlotsRetainedWorkshopArtifactSnapshot.Passed
    Write-SmokeJsonObject `
        -Path (
            Join-Path `
                $evidence `
                'more-equipment-slots-retained-workshop-preflight.json') `
        -Value ([ordered]@{
            ReadOnly = $true
            WorkshopContentRoot = $externalWorkshopContentRoot
            Snapshot =
                $moreEquipmentSlotsRetainedWorkshopArtifactSnapshot
            Normalization =
                'For each file: lowercase SHA-256, two spaces, forward-slash relative path; sort by relative path; join with LF; UTF-8 SHA-256.'
            Passed =
                $moreEquipmentSlotsRetainedWorkshopPreflightOk
        })
    if (-not $moreEquipmentSlotsRetainedWorkshopPreflightOk) {
        throw "MoreEquipmentSlots U1 retained Workshop artifact preflight failed before profile mutation or game launch. Steam-managed bytes were read only. Evidence: $evidence"
    }
}
if ($RequireExternalPlayerInputGate) {
    $externalPlayerInputProducts = @(Get-SmokePublishedProductDefinitions | Where-Object {
        [string]$_.catalogId -eq 'y-console' -and
        [string]$_.uniqueId -eq 'DTMAPI.DebugConsoleMod' -and
        [string]$_.workshopId -eq '3742714442'
    })
    if ($externalPlayerInputProducts.Count -ne 1) {
        throw "External player-input Workshop artifact preflight requires exactly one Catalog y-console / DTMAPI.DebugConsoleMod / 3742714442 row; found $($externalPlayerInputProducts.Count)."
    }

    $externalPlayerInputProduct = $externalPlayerInputProducts[0]
    $externalPlayerInputCurrentArtifactProperty =
        $externalPlayerInputProduct.PSObject.Properties['currentPublishedArtifact']
    $externalPlayerInputCurrentArtifact = if ($null -ne $externalPlayerInputCurrentArtifactProperty) {
        $externalPlayerInputCurrentArtifactProperty.Value
    }
    else {
        $null
    }
    if ($null -eq $externalPlayerInputCurrentArtifact -or
        [string]$externalPlayerInputProduct.packageDll -ne 'DTMAPI.DebugConsole.dll' -or
        [string]$externalPlayerInputCurrentArtifact.releaseVersion -ne '1.0.0' -or
        [string]$externalPlayerInputCurrentArtifact.treeDigestAlgorithm -ne 'DTMAPI-Published-SHA256SUMS-v1' -or
        [string]$externalPlayerInputCurrentArtifact.entryDllSha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
        [int64]$externalPlayerInputCurrentArtifact.entryDllBytes -le 0 -or
        [int]$externalPlayerInputCurrentArtifact.steamDeliveredFileCount -le 0 -or
        [int64]$externalPlayerInputCurrentArtifact.steamDeliveredBytes -le 0 -or
        [string]$externalPlayerInputCurrentArtifact.steamDeliveredTreeSha256 -notmatch '^[A-Fa-f0-9]{64}$') {
        throw 'External player-input Workshop artifact preflight requires the Catalog y-console currentPublishedArtifact to freeze the current entry DLL, its SHA-256, and the exact published file/byte/tree baseline.'
    }

    $expectedYConsoleWorkshopRoot = [System.IO.Path]::GetFullPath((Join-Path $externalWorkshopContentRoot '3742714442'))
    $expectedYConsoleWorkshopDll = Join-Path $expectedYConsoleWorkshopRoot ('Content\DTMAPI\' + [string]$externalPlayerInputProduct.packageDll)
    $externalPlayerInputWorkshopArtifactSnapshot = Get-SmokeWorkshopArtifactSnapshot -Path $expectedYConsoleWorkshopRoot -Product $externalPlayerInputProduct -ArtifactBoundary CurrentPublished
    $expectedYConsoleWorkshopDllSha256 = ([string]$externalPlayerInputCurrentArtifact.entryDllSha256).ToUpperInvariant()
    $expectedYConsoleWorkshopDllLength = [int64]$externalPlayerInputCurrentArtifact.entryDllBytes
    $externalPlayerInputWorkshopDllExists = Test-Path -LiteralPath $expectedYConsoleWorkshopDll -PathType Leaf
    if ($externalPlayerInputWorkshopDllExists) {
        $externalPlayerInputWorkshopDllLength = [int64](Get-Item -LiteralPath $expectedYConsoleWorkshopDll).Length
        $externalPlayerInputWorkshopDllNonEmpty = $externalPlayerInputWorkshopDllLength -gt 0
        $externalPlayerInputWorkshopDllLengthOk =
            $externalPlayerInputWorkshopDllLength -eq $expectedYConsoleWorkshopDllLength
        $actualYConsoleWorkshopDllSha256 = (Get-SmokeFileSha256 -Path $expectedYConsoleWorkshopDll).ToUpperInvariant()
        $externalPlayerInputWorkshopDllHashOk = [string]::Equals(
            $actualYConsoleWorkshopDllSha256,
            $expectedYConsoleWorkshopDllSha256,
            [System.StringComparison]::Ordinal)
    }
    $externalPlayerInputArtifactOk = [bool]$externalPlayerInputWorkshopArtifactSnapshot.Passed -and
        $externalPlayerInputWorkshopDllExists -and
        $externalPlayerInputWorkshopDllNonEmpty -and
        $externalPlayerInputWorkshopDllLengthOk -and
        $externalPlayerInputWorkshopDllHashOk
    Write-SmokeJsonObject -Path (Join-Path $evidence 'external-input-artifact-preflight.json') -Value ([ordered]@{
        CatalogId = [string]$externalPlayerInputProduct.catalogId
        UniqueId = [string]$externalPlayerInputProduct.uniqueId
        WorkshopId = [string]$externalPlayerInputProduct.workshopId
        PackageDll = [string]$externalPlayerInputProduct.packageDll
        ArtifactBoundary = 'CurrentPublished'
        WorkshopArtifactSnapshot = $externalPlayerInputWorkshopArtifactSnapshot
        WorkshopDllPath = $expectedYConsoleWorkshopDll
        WorkshopDllExists = $externalPlayerInputWorkshopDllExists
        ExpectedWorkshopDllLength = $expectedYConsoleWorkshopDllLength
        WorkshopDllLength = $externalPlayerInputWorkshopDllLength
        WorkshopDllNonEmpty = $externalPlayerInputWorkshopDllNonEmpty
        WorkshopDllLengthPassed = $externalPlayerInputWorkshopDllLengthOk
        ExpectedWorkshopDllSha256 = $expectedYConsoleWorkshopDllSha256
        ActualWorkshopDllSha256 = $actualYConsoleWorkshopDllSha256
        WorkshopDllHashPassed = $externalPlayerInputWorkshopDllHashOk
        Passed = $externalPlayerInputArtifactOk
    })
    if (-not $externalPlayerInputArtifactOk) {
        throw "External player-input Workshop artifact preflight failed for Catalog product y-console / Workshop 3742714442. Evidence: $evidence"
    }
}
if ($publishedProductArtifactGateRequested) {
    $workshopContentRoot = [System.IO.Path]::GetFullPath((Join-Path $gameDir '..\..\workshop\content\2285550'))
    $publishedProductArtifactSnapshots = @(Get-SmokePublishedProductDefinitions | ForEach-Object {
        Get-SmokeWorkshopArtifactSnapshot -Path (Join-Path $workshopContentRoot ([string]$_.workshopId)) -Product $_
    })
    $publishedProductArtifactsOk = @($publishedProductArtifactSnapshots | Where-Object { -not [bool]$_.Passed }).Count -eq 0
    Write-SmokeJsonObject -Path (Join-Path $evidence 'public-product-artifact-gate.json') -Value ([ordered]@{
        WorkshopContentRoot = $workshopContentRoot
        ExpectedProductCount = 11
        PreLaunch = [ordered]@{
            ActualProductCount = $publishedProductArtifactSnapshots.Count
            Snapshots = @($publishedProductArtifactSnapshots)
            Passed = $publishedProductArtifactsOk
        }
        PostExit = $null
        UnchangedDuringRun = $null
        Passed = $publishedProductArtifactsOk
        Normalization = 'CurrentPublished: DTMAPI-Published-SHA256SUMS-v1 Ordinal forward-slash paths, excluding Content/.tools/bepinex/extract/**, LF join without final LF. Retained: frozen DTMAPI-Retained-SHA256SUMS-v1 OrdinalIgnoreCase forward-slash paths, LF join without final LF.'
    })
    if (-not $publishedProductArtifactsOk) {
        throw "Published-product subscription artifact gate failed before launch. Classify the changed Steam-managed tree before updating the retained baseline. Evidence: $evidence"
    }
}
$preservePlayerSaveFiles = $false
$playerSaveSnapshots = @()
$playerSaveRestoreOk = $true
$playerSaveUnchangedBeforeCleanupOk = $true
$committedSidecarSnapshots = @()
$committedSidecarsUnchangedBeforeCleanupOk = $true
$g5CommittedSidecarUnchangedOk = $true
$noNativeSaveMetadataRequested = $SaveTestMode -eq 'NoNativeSave' -and $SaveSlot -gt 0
if ($noNativeSaveMetadataRequested) {
    $saveRoot = if ($disposableSaveFixtureRequested) {
        [System.IO.Path]::GetFullPath(
            (Join-Path $disposableSaveFixtureRootResolved 'SAVE'))
    }
    else {
        Join-Path (Get-DolocTownLivePersistentRootForSmoke) 'SAVE'
    }
    $archiveIndex = Get-SmokeNativeArchiveIndex -UiSaveSlot $SaveSlot
    $archiveIndexes = @($archiveIndex)
    foreach ($currentArchiveIndex in $archiveIndexes) {
        $playerSaveSnapshots += Get-DtmApiCurrentSaveArchiveFamilySnapshot `
            -SaveRoot $saveRoot `
            -ArchiveIndex $currentArchiveIndex
    }
    Write-SmokeJsonObject -Path (Join-Path $evidence 'player-save-baseline.json') -Value ([ordered]@{
        SaveTestMode = $SaveTestMode
        SaveSlot = $SaveSlot
        ArchiveIndex = $archiveIndex
        ArchiveIndexes = @($archiveIndexes)
        MetadataOnly = $true
        RoutineByteBackupCreated = $false
        PlayerArchiveWritebackAllowed = $false
        CurrentArchiveFamily = '<current>.data + <current>.data.prev[0-9]+ + <current>.data.bak'
        Families = @($playerSaveSnapshots)
        Files = @($playerSaveSnapshots | ForEach-Object { @($_.Files) })
    })

    $committedSidecarPaths = @(Get-SmokeCommittedSidecarPaths -RepoRoot $repo -DtmApiStateDir $dtmapiDir -ArchiveIndex $archiveIndex)
    $committedSidecarSnapshots = @($committedSidecarPaths | ForEach-Object {
        Get-SmokeFileMetadataSnapshot -Kind 'CommittedSidecar' -Path $_
    })
    Write-SmokeJsonObject -Path (Join-Path $evidence 'committed-sidecar-baseline.json') -Value ([ordered]@{
        SaveTestMode = $SaveTestMode
        SaveSlot = $SaveSlot
        ArchiveIndex = $archiveIndex
        MetadataOnly = $true
        RoutineByteBackupCreated = $false
        Files = @($committedSidecarSnapshots)
    })
}
$officialModProfileSummary = $null
$officialModProfileRestored = $false
$local11AuthorSourceSummary = $null
$localAuthorSourceRequested = $false
$localAuthorSourceRequestedIds = @()
$local11AuthorSourceAppliedOk = $true
$local11AuthorSourceRestored = $true
$oneActionConfigPath = Join-Path $dtmapiDir 'config\Yuuka.DTMAPI.OneActionComplete.json'
$oneActionConfigBackup = Join-Path $evidence 'Yuuka.DTMAPI.OneActionComplete.before.json'
$oneActionConfigHadOriginal = $false
$usesOneActionConfigSmoke = $false
$actionSpeedConfigPath = Join-Path $dtmapiDir 'config\Yuuka.DTMAPI.ActionSpeed.json'
$actionSpeedConfigBackup = Join-Path $evidence 'Yuuka.DTMAPI.ActionSpeed.before.json'
$actionSpeedConfigHadOriginal = $false
$usesActionSpeedConfigSmoke = $false
$autoFishingConfigPath = Join-Path $dtmapiDir 'config\Yuuka.DTMAPI.AutoFishing.json'
$autoFishingConfigBackup = Join-Path $evidence 'Yuuka.DTMAPI.AutoFishing.before.json'
$autoFishingConfigHadOriginal = $false
$usesAutoFishingConfigSmoke = $false
$qaHostStage = $null
$qaHostCleanup = $null
$moreEquipmentSlotsColdOfficialPackagePreflightOk =
    -not [bool]$moreEquipmentSlotsColdRecoveryTransitionRequested
$moreEquipmentSlotsColdOfficialDisabledOk =
    -not [bool]$moreEquipmentSlotsColdRecoveryTransitionRequested
$g5ConfigDirectoryBaseline = $null
$g5ConfigDirectoryRestored = $true
$strongPlantingGunConfigPath = ''
$strongPlantingGunConfigBackup = ''
$strongPlantingGunConfigHadOriginal = $false
if ($qaExternalStateProtectionRequested) {
    Write-SmokeJsonObject -Path (Join-Path $evidence 'g5-external-state-baseline.json') -Value ([ordered]@{
        Cases = @($qaG5WorldMutationCases.ToArray())
        SaveSlot = $SaveSlot
        SaveFiles = @($playerSaveSnapshots)
        ConfigDirectoryTransaction = 'forbidden'
        ConfigCleanup = 'case-local exact files only'
        CapturedBeforeQaStage = $true
        CapturedBeforeOfficialProfile = $true
        CapturedBeforeProductConfigWrite = $true
    })
}
$smokeBodyFailure = ''
