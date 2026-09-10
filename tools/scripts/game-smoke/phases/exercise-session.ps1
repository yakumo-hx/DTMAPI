$launchCommandStartedAt = Get-Date
if ($moreSavesFixed12AcceptanceRequested) {
    $SmokeRunnerScenarioDeadline = $launchCommandStartedAt.AddSeconds($TimeoutSeconds)
    "ScenarioDeadline=$($SmokeRunnerScenarioDeadline.ToString('o'));scope=MoreSavesFixed12LaunchThroughExit" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
}
$SmokeRunnerLaunchAttempted = $true
if ($AssertNoQaUiEvidence) {
    $noQaRunnerDeadline = $launchCommandStartedAt.AddSeconds($noQaRunnerBudgetSeconds)
    $noQaUiDeadline = $noQaRunnerDeadline
    "NoQaRunnerDeadline=$($noQaRunnerDeadline.ToString('o'));startedAt=$($launchCommandStartedAt.ToString('o'))" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
}
if ($launchViaSteam) {
    Start-Process 'steam://rungameid/2285550'
}
else {
    $exe = Join-Path $gameDir 'DolocTown.exe'
    if (-not (Test-Path $exe)) {
        throw "DolocTown.exe not found at $exe"
    }
    $oldSteamAppId = $env:SteamAppId
    $oldSteamGameId = $env:SteamGameId
    try {
        $env:SteamAppId = '2285550'
        $env:SteamGameId = '2285550'
        "DirectExeSteamEnv=SteamAppId=2285550;SteamGameId=2285550" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        Start-Process -FilePath $exe -WorkingDirectory $gameDir
    }
    finally {
        if ($null -eq $oldSteamAppId) { Remove-Item Env:\SteamAppId -ErrorAction SilentlyContinue } else { $env:SteamAppId = $oldSteamAppId }
        if ($null -eq $oldSteamGameId) { Remove-Item Env:\SteamGameId -ErrorAction SilentlyContinue } else { $env:SteamGameId = $oldSteamGameId }
    }
}
$launchCommandFinishedAt = Get-Date
"LaunchCommandStarted=$($launchCommandStartedAt.ToString('o'))`nLaunchCommandFinished=$($launchCommandFinishedAt.ToString('o'))" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')

$logPath = $freshLogPath
$startupTimeoutSeconds = if ($AssertNoQaUiEvidence) {
    Get-SmokeRemainingBudgetSeconds -Deadline $noQaRunnerDeadline -MaximumSeconds $TimeoutSeconds
}
else {
    $TimeoutSeconds
}
$launchModeLabel = if ($launchViaSteam) { 'Steam' } else { 'DirectExe' }
$startupAbsoluteDeadline = if ($AssertNoQaUiEvidence) { $noQaRunnerDeadline } else { $SmokeRunnerScenarioDeadline }
$startupOk = $startupTimeoutSeconds -gt 0 -and (Wait-ForStartupLogWithTimeline -LogPath $logPath -Pattern 'DTMAPI runtime starting.' -TimeoutSeconds $startupTimeoutSeconds -EvidenceDir $evidence -LaunchCommandStartedAt $launchCommandStartedAt -LaunchCommandFinishedAt $launchCommandFinishedAt -LaunchMode $launchModeLabel -AbsoluteDeadline $startupAbsoluteDeadline)
$gameLaunchedOk = $false
$saveLoadedRequested = (($SaveSlot -gt 0) -and ([bool]$IncludeHookProbe -or [bool]$AutoOpenAnimalPanel -or [bool]$AutoExerciseExperimentalHooks -or [bool]$AutoExerciseFishRoeTooltip -or [bool]$AutoExerciseDiagnosticsSnapshot -or [bool]$QaObserveSaveLoaded -or [bool]$QaObserveSaveSaved -or [bool]$AutoExerciseActionSpeedTool -or [bool]$AutoExerciseActionSpeedConfigApply -or [bool]$AutoExerciseActionSpeedInteraction -or [bool]$AutoExerciseOneActionResourceHit -or [bool]$AutoExerciseOneActionWrongTool -or [bool]$AutoExerciseOneActionFuelFeed -or [bool]$AutoExerciseOneActionVegetation -or [bool]$AutoPressOneActionMenuKey -or [bool]$AutoPressAutoFishingHotkey -or [bool]$AutoExerciseAutoFishingPhase -or [bool]$autoFishingNoDemandFrameProfile -or [bool]$AutoExerciseLegacyFishingCompatibility -or [bool]$AutoExerciseAutoFishingMovementCancel -or [bool]$AutoExercisePauseMenuLayout -or [bool]$AutoExerciseTitleButtonLifecycle -or [bool]$AutoExerciseModOwnerLifetime -or [bool]$AutoExerciseSaveLoadCycle -or [bool]$AutoExerciseSaveLoadCyclePendingPressure -or [bool]$AutoExerciseInstantSave -or [bool]$debugConsoleSaveAcceptanceRequested -or $requiresDebugConsoleKeySmoke -or [bool]$AutoExerciseDebugInventory -or [bool]$AutoExerciseDebugWeather -or [bool]$AutoExerciseDebugTeleport -or [bool]$AutoExerciseDebugTime -or [bool]$AutoExerciseDebugMovement -or [bool]$AutoExerciseAdvancedDebug -or [bool]$AutoExerciseNewContentApis -or [bool]$QaObserveContentMetadata -or [bool]$QaObserveEquipmentSlotsUi -or [bool]$AutoExerciseMineContentApis -or [bool]$AutoExerciseZoom -or [bool]$AutoExerciseZoomProductNative -or [bool]$AutoExerciseChestLocatorEnhancer -or [bool]$AutoExerciseMoreSavesOfficialSaveUi -or $MoreSavesFixed12AcceptancePhase -in @('EnabledLifecycle','ReenabledCold') -or [bool]$AutoExerciseStrongPlantingGun -or [bool]$AutoExerciseCropHarvestingApi -or [bool]$AutoExerciseCustomEntityApis -or [bool]$AutoExerciseAudioReplacement -or [bool]$AutoExerciseHatchAnimalVoice -or [bool]$batch5GcLadderEnabled -or [bool]$Batch6AutoFishingPilot -or [bool]$Batch6AutoFishingManagerLifecycle -or [bool]$moreEquipmentSlotsTransitionRequested -or $TitleIdleBeforeSaveSeconds -gt 0))
if ($SaveSlot -gt 0 -and
    ($AutoExerciseMoreEquipmentSlots -or
     $SetupMoreEquipmentSlotsCommittedShield -or
     $DamageMoreEquipmentSlotsCommittedShieldNoNativeSave -or
     $BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave -or
     $AutoExerciseMoreEquipmentSlotsNoNativeSave -or
     $ObserveMoreEquipmentSlotsNoNativeSaveCold)) {
    $saveLoadedRequested = $true
}
$probeOk = -not [bool]$IncludeHookProbe
$saveLoadedOk = -not $saveLoadedRequested
$titleLifecycleOk = -not [bool]$AutoExerciseTitleButtonLifecycle
$ownerLifetimeOk = -not [bool]$AutoExerciseModOwnerLifetime
$ownerLifetimeLine = $null
$saveLoadCycleOk = -not [bool]$AutoExerciseSaveLoadCycle
$saveLoadCycleSummary = ''
$preLoadGcProbeRequested = [bool]$AutoExercisePreLoadGcProbe
$preLoadGcProbeOk = -not $preLoadGcProbeRequested
$preLoadGcProbeSummary = ''
$saveLoadCyclePendingPressureOk = -not [bool]$AutoExerciseSaveLoadCyclePendingPressure
$saveLoadCyclePendingPressureSummary = ''
$titleButtonOk = -not $titleSettingsRequested
$titleButtonScreenshotOk = -not $titleSettingsRequested
$titleMenuOk = -not $titleSettingsRequested
$titleMenuScreenshotOk = -not $titleSettingsRequested
$managerStatusPageOk = -not $managerStatusRequested
$managerStatusPageScreenshotOk = -not $managerStatusRequested
$managerStatusPageScreenshotFileOk = -not $managerStatusRequested
$managerStatusSummaryTextOk = -not $managerStatusRequested
$managerStatusSummaryCopyOk = -not $managerMvpRequested
$managerModsPageOk = -not $managerMvpRequested
$managerModsPageScreenshotFileOk = -not $qaG4ManagerMvpUiEnabled
$managerErrorsPageOk = -not $managerMvpRequested
$managerHooksPageOk = -not $managerMvpRequested
$managerFeaturesPageOk = -not $managerMvpRequested
$managerAdvancedPageScreenshotFileOk = -not $qaG4ManagerMvpUiEnabled
$managerPlayerInteractionsOk = -not $qaG4ManagerMvpUiEnabled
$managerLogsPageOk = -not $managerMvpRequested
$managerLogsExportButtonOk = -not $managerMvpRequested
$managerLogsExportStateTextOk = -not $managerMvpRequested
$managerLogsPageScreenshotOk = -not $managerMvpRequested
$managerLogsPageScreenshotFileOk = -not $managerMvpRequested
$officialModUiOk = -not [bool]$AutoOpenOfficialModUi
$animalViewerUiOk = -not [bool]$AutoOpenAnimalPanel
$noQaAnimalViewerUiOk = -not [bool]$AssertNoQaUiEvidence
$noQaAnimalViewerCloseOk = -not [bool]$AssertNoQaUiEvidence
$noQaAnimalViewerNativeCloseOk = -not [bool]$AssertNoQaUiEvidence
$noQaAnimalViewerOverlayClearOk = -not [bool]$AssertNoQaUiEvidence
$noQaAnimalViewerCloseOrderOk = -not [bool]$AssertNoQaUiEvidence
$noQaAnimalViewerRenderReceiptCount = 0
$noQaAnimalViewerAutoDriveProvenanceOk = -not [bool]$AutoDriveNoQaAnimalViewer
$noQaAnimalViewerInputProvenanceOk = -not [bool]$AssertNoQaUiEvidence
$noQaEquipmentSlotsRegisterOk = -not [bool]$AssertNoQaUiEvidence
$noQaEquipmentSlotsRenderOk = -not [bool]$AssertNoQaUiEvidence
$noQaEquipmentSlotsInteractionOk = -not [bool]$AssertNoQaUiEvidence
$noQaEquipmentSlotsCloseInputOk = -not [bool]$AssertNoQaUiEvidence
$noQaEquipmentSlotsDownstreamInteractionOk = -not [bool]$AssertNoQaUiEvidence
$noQaEquipmentSlotsBoundaryCleanupOk = -not [bool]$AssertNoQaUiEvidence
$noQaEquipmentSlotsCloseOk = -not [bool]$AssertNoQaUiEvidence
$noQaEquipmentSlotsRecoveryOk = -not [bool]$AssertNoQaUiEvidence
$equipmentSlotsUiObservationOk = -not [bool]$qaG4EquipmentSlotsObservationEnabled
$equipmentSlotsUiEvidenceCapturedOk = -not [bool]$qaG4EquipmentSlotsObservationEnabled
$equipmentSlotsUiHoverOk = -not [bool]$qaG4EquipmentSlotsObservationEnabled
$equipmentSlotsUiCloseInputOk = -not [bool]$qaG4EquipmentSlotsObservationEnabled
$noQaPublishedOwnerSetOk = -not [bool]$AssertNoQaUiEvidence
$experimentalHooksOk = -not [bool]$AutoExerciseExperimentalHooks
$actionSpeedToolOk = -not [bool]$AutoExerciseActionSpeedTool
$actionSpeedConfigApplyOk = -not [bool]$AutoExerciseActionSpeedConfigApply
$actionSpeedInteractionOk = -not [bool]$AutoExerciseActionSpeedInteraction
$oneActionResourceHitOk = -not [bool]$AutoExerciseOneActionResourceHit
$oneActionPartialEnergyOk = -not [bool]$AutoExerciseOneActionResourceHit
$oneActionConfigReloadOk = -not [bool]$AutoExerciseOneActionResourceHit
$oneActionWrongToolOk = -not [bool]$AutoExerciseOneActionWrongTool
$oneActionFuelFeedOk = -not [bool]$AutoExerciseOneActionFuelFeed
$oneActionVegetationOk = -not [bool]$AutoExerciseOneActionVegetation
$advancedProductOwnerDeactivationOk = -not [bool]$AssertAdvancedProductOwnerDeactivation
$oneActionMenuKeyOk = -not [bool]$AutoPressOneActionMenuKey
$oneActionMenuInputEvidence = $null
$autoFishingInputLogOk = -not [bool]$AutoPressAutoFishingHotkey
$autoFishingHotkeyOk = -not ([bool]$AutoPressAutoFishingHotkey -or [bool]$AutoExerciseAutoFishingPhase)
$autoFishingMovementCancelOk = -not $autoFishingMovementRequested
$legacyFishingCompatibilityOk = -not [bool]$AutoExerciseLegacyFishingCompatibility
$autoFishingPhaseOk = -not [bool]$AutoExerciseAutoFishingPhase
$autoFishingPerformanceCounterBlocked = $false
$autoFishingMonoGateOk = -not [bool]$AutoFishingMonoGate
$autoFishingInstantBiteOk = -not ([bool]$AutoExerciseAutoFishingPhase -and $autoFishingScenarioRequestsInstantBite)
$autoFishingMiniGameSkipOk = -not ([bool]$AutoExerciseAutoFishingPhase -and $autoFishingScenarioRequestsSkip)
$autoFishingMiniGameCompleteOk = -not ([bool]$AutoExerciseAutoFishingPhase -and $autoFishingScenarioRequestsComplete)
$autoFishingAnimationSpeedOk = -not ([bool]$AutoExerciseAutoFishingPhase -and $autoFishingScenarioRequestsAnimationSpeed)
$autoFishingCastChargeOk = -not ([bool]$AutoExerciseAutoFishingPhase -and $autoFishingScenarioRequestsCastCharge)
$autoFishingLifecycleOk = -not [bool]$AutoExerciseAutoFishingPhase
$autoFishingLifecycleSummary = ''
$autoFishingSoakOk = -not ([bool]$AutoExerciseAutoFishingPhase -and $AutoFishingSoakLoops -gt 0)
$autoFishingReportExportOk = (-not [bool]$AutoExerciseAutoFishingPhase) -or [bool]$AutoFishingMonoGate
$batch6AutoFishingHandshakeOk = -not [bool]$Batch6AutoFishingPilot -or $Batch6AutoFishingLevel -eq 'L0'
$batch6AutoFishingInputOk = -not [bool]$Batch6AutoFishingPilot -or $Batch6AutoFishingLevel -eq 'L0'
$batch6AutoFishingPilotOk = -not [bool]$Batch6AutoFishingPilot
$batch6AutoFishingCleanupOk = -not [bool]$Batch6AutoFishingPilot
$batch6AutoFishingHandshakeReceipts = New-Object System.Collections.ArrayList
$batch6AutoFishingInputReceipts = New-Object System.Collections.ArrayList
$batch6AutoFishingManagerHandshakeOk = -not [bool]$Batch6AutoFishingManagerLifecycle -or $Batch6AutoFishingManagerMode -eq 'ColdDisabled'
$batch6AutoFishingManagerActionOk = -not [bool]$Batch6AutoFishingManagerLifecycle -or $Batch6AutoFishingManagerMode -eq 'ColdDisabled'
$batch6AutoFishingManagerLifecycleOk = -not [bool]$Batch6AutoFishingManagerLifecycle
$batch6AutoFishingManagerCleanupOk = -not [bool]$Batch6AutoFishingManagerLifecycle
$batch6AutoFishingManagerHandshakeReceipts = New-Object System.Collections.ArrayList
$batch6AutoFishingManagerInputReceipts = New-Object System.Collections.ArrayList
$batch6AutoFishingManagerMarkerActionReceipts = New-Object System.Collections.ArrayList
$pauseMenuLayoutOk = -not [bool]$AutoExercisePauseMenuLayout
$diagnosticsReportExportScenarios = @()
if ($AutoExerciseZoom -and -not $qaG4CameraPlayableEnabled) {
    $diagnosticsReportExportScenarios += 'Camera'
}
if ($AutoExerciseActionSpeedInteraction) {
    $diagnosticsReportExportScenarios += 'ActionSpeed'
}
if ($AutoExerciseAutoFishingPhase -and -not $AutoFishingMonoGate -and -not $autoFishingPerformanceBaseline) {
    $diagnosticsReportExportScenarios += 'AutoFishing'
}
$diagnosticsReportExportRequested = $diagnosticsReportExportScenarios.Count -gt 0
$diagnosticsReportExportOk = -not $diagnosticsReportExportRequested
$diagnosticsReportExportPassedScenarios = @()
$instantSaveOk = -not [bool]$AutoExerciseInstantSave
$debugConsoleSaveAcceptanceOk =
    -not [bool]$debugConsoleSaveAcceptanceRequested
$debugConsoleOpenY1Ok = -not $requiresDebugConsoleKeySmoke
$debugConsoleMouseGiveOk = -not [bool]$AutoExerciseDebugConsoleMouseGive
$debugConsoleCloseEscapeOk = -not $requiresDebugConsoleKeySmoke
$debugConsoleOpenY2Ok = -not $requiresDebugConsoleKeySmoke
$debugConsoleCloseYOk = -not $requiresDebugConsoleKeySmoke
$debugConsoleTenYShortTapsOk = -not $requiresDebugConsoleKeySmoke
$debugConsoleHoldYNoFlickerOk = -not $requiresDebugConsoleKeySmoke
$qaG4DebugEvidenceReadyOk = -not $qaG4DebugConsoleEnabled
$debugConsolePostHoldCloseOk = -not ([bool]$RequireExternalPlayerInputGate -or $qaG4DebugConsoleEnabled -or [bool]$AssertNoQaUiEvidence)
$debugConsoleTitleUiCleanupRequested = [bool]$AutoExerciseDebugConsole
$debugConsoleTitleUiCleanupOk = -not $debugConsoleTitleUiCleanupRequested
$debugConsoleTitleUiCleanupSummary = ''
$externalPlayerInputReadyOk = -not [bool]$RequireExternalPlayerInputGate
$externalPlayerInputMarkerWrittenOk = -not [bool]$RequireExternalPlayerInputGate
$externalPlayerInputCleanupOk = -not [bool]$RequireExternalPlayerInputGate
$externalPlayerInputNativeMenuLeakDetected = $false
$debugInventoryOk = -not [bool]$AutoExerciseDebugInventory
$debugWeatherOk = -not [bool]$AutoExerciseDebugWeather
# CSV export was retired from the current teleport surface. Keep the legacy
# result field non-requested for report-schema compatibility, but do not wait
# for a log that the current QA/product correctly cannot emit.
$debugTeleportCsvOk = $true
$debugTeleportOk = -not [bool]$AutoExerciseDebugTeleport
$debugTimeOk = -not [bool]$AutoExerciseDebugTime
$debugMovementOk = -not [bool]$AutoExerciseDebugMovement
$advancedDebugOk = -not [bool]$AutoExerciseAdvancedDebug
$vehicleSecondMotorOk = $true
$positiveNewContentRequested = [bool]$AutoExerciseNewContentApis -and -not [bool]$ExpectOilAbsent
$fullNewContentRequested = $positiveNewContentRequested -and -not [bool]$OilOnly
$newContentApisOk = -not [bool]$AutoExerciseNewContentApis
$newContentMineApisOk = -not [bool]$AutoExerciseMineContentApis
$newContentOilAbsentOk = -not [bool]$ExpectOilAbsent
$newContentOilOnlyOk = -not [bool]$OilOnly
$newContentOilItemMetadataOk = -not $positiveNewContentRequested
$newContentOilCoalDropOk = -not $positiveNewContentRequested
$newContentMineOfficialJsonOk = -not ($fullNewContentRequested -or [bool]$AutoExerciseMineContentApis)
$newContentMineOfficialTechTreeUiOk = -not [bool]$AutoExerciseMineContentApis
$newContentMineProductionOk = -not ($fullNewContentRequested -or [bool]$AutoExerciseMineContentApis)
$newContentEquipmentSlotsOk = -not $fullNewContentRequested
$zoomOk = -not [bool]$AutoExerciseZoom
$zoomProductNativeOk = -not [bool]$AutoExerciseZoomProductNative
$chestLocatorEnhancerOk = -not [bool]$AutoExerciseChestLocatorEnhancer
$moreEquipmentSlotsOk = -not [bool]$AutoExerciseMoreEquipmentSlots
$moreEquipmentSlotsProtectedTransactionOk = -not [bool]$AutoExerciseMoreEquipmentSlots
$moreEquipmentSlotsCommittedShieldSetupOk =
    -not [bool]$SetupMoreEquipmentSlotsCommittedShield
$moreEquipmentSlotsCommittedShieldDamageOk =
    -not [bool]$DamageMoreEquipmentSlotsCommittedShieldNoNativeSave
$moreEquipmentSlotsCommittedShieldBreakReplaceOk =
    -not [bool]$BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave
$moreEquipmentSlotsInterruptedCandidateRecoveryOk =
    -not [bool]$ObserveMoreEquipmentSlotsInterruptedCandidateRecovery
$moreEquipmentSlotsNoNativeSaveOk =
    -not [bool]$AutoExerciseMoreEquipmentSlotsNoNativeSave
$moreEquipmentSlotsNoNativeSaveColdOk =
    -not [bool]$ObserveMoreEquipmentSlotsNoNativeSaveCold
$moreEquipmentSlotsTransitionOk =
    -not [bool]$moreEquipmentSlotsTransitionRequested
$moreSavesFixed12LifecycleOk =
    -not [bool]$moreSavesFixed12AcceptanceRequested
$moreEquipmentSlotsTransitionSleepInput =
    [ordered]@{
        Requested = $false
        Sent = $false
        Key = ''
        ForegroundMatched = $false
        SendInputSucceeded = $false
        PostMessageFallbackUsed = $false
        Attempts = @()
    }
$moreEquipmentSlotsTransitionDeadline = [datetime]::MaxValue
$moreEquipmentSlotsColdRecoveryDemandOk =
    $MoreEquipmentSlotsTransitionPhase -ne 'ColdCommit'
$moreEquipmentSlotsColdRecoveryHostOk =
    $MoreEquipmentSlotsTransitionPhase -ne 'ColdCommit'
$moreEquipmentSlotsColdRecoveryDestinationOk =
    $MoreEquipmentSlotsTransitionPhase -ne 'ColdCommit'
$moreEquipmentSlotsColdRecoveryOk =
    -not [bool]$moreEquipmentSlotsColdRecoveryTransitionRequested
$moreSavesOfficialSaveUiOk = -not [bool]$AutoExerciseMoreSavesOfficialSaveUi
$moreSavesOfficialSaveUiEvidenceOk = -not [bool]$AutoExerciseMoreSavesOfficialSaveUi
$strongPlantingGunOk = -not [bool]$AutoExerciseStrongPlantingGun
$cropHarvestingApiOk = -not [bool]$AutoExerciseCropHarvestingApi
$cropHarvestingApiEvidenceOk = -not [bool]$AutoExerciseCropHarvestingApi
$customEntityApisOk = -not [bool]$AutoExerciseCustomEntityApis
$fishRoeTooltipOk = -not $qaFishRoeTooltipObservationEnabled
$itemDisplayNameLifecycleRequested = $qaFishRoeTooltipObservationEnabled -and
    [bool]$AutoExerciseSaveLoadCycle -and $saveLoadCycleCountEffective -ge 2 -and
    $qaG4AnimalObservationEnabled
$itemDisplayNameLifecycleOk = -not $itemDisplayNameLifecycleRequested
$qaDiagnosticsSnapshotOk = -not $qaDiagnosticsSnapshotEnabled
$qaSaveLoadedObservationOk = -not [bool]$QaObserveSaveLoaded
$qaSaveSavedObservationOk = -not [bool]$QaObserveSaveSaved
$qaWorkshopReloadObservationOk = -not [bool]$QaObserveWorkshopReloadCompleted
$saveFixtureIsolationRequested = $disposableSaveFixtureRequested
$saveFixtureIsolationOk = -not $saveFixtureIsolationRequested
$saveFixtureIsolationCleanupOk = -not $saveFixtureIsolationRequested
$audioReplacementOk = -not [bool]$AutoExerciseAudioReplacement
$hatchAnimalVoiceOk = -not [bool]$AutoExerciseHatchAnimalVoice
$contentMetadataObservationOk = -not $qaG4ContentMetadataObservationEnabled

if ($startupOk) {
    if ($saveFixtureIsolationRequested) {
        $saveFixtureIsolationOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
            -LogPath $logPath `
            -Pattern 'Smoke\.SaveFixtureIsolation = verified' `
            -TimeoutSeconds $TimeoutSeconds `
            -Regex `
            -AbortOnFatalInstanceWindow
    }
    $gameLaunchedWaitSeconds = if ($AssertNoQaUiEvidence) {
        Get-SmokeRemainingBudgetSeconds -Deadline $noQaRunnerDeadline -MaximumSeconds 60
    }
    else {
        60
    }
    $gameLaunchedOk = if ($AssertNoQaUiEvidence) {
        $gameLaunchedDeadline = Get-SmokeCappedDeadline -Deadline $noQaRunnerDeadline -MaximumMilliseconds 60000
        (Test-SmokeDeadlineHasBudget -Deadline $gameLaunchedDeadline) -and
            (Wait-ForLogLineAfterOffsetUntilDeadline -LogPath $logPath -Offset 0 -Pattern 'GameLaunched dispatched.' -Deadline $gameLaunchedDeadline -AbortOnFatalInstanceWindow)
    }
    else {
        $gameLaunchedWaitSeconds -gt 0 -and (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'GameLaunched dispatched.' -TimeoutSeconds $gameLaunchedWaitSeconds -AbortOnFatalInstanceWindow)
    }
    if ($gameLaunchedOk -and $AssertNoQaUiEvidence) {
        $noQaSaveLoadedWaitSeconds = Get-SmokeRemainingBudgetSeconds -Deadline $noQaRunnerDeadline
        $noQaOrdinaryPlayerHandshakeShown = $true
        $noQaTitleInstruction = if ($Issue011Acceptance) {
            'on the title screen open the DTMAPI settings button once, close that menu, then select save slot 10'
        }
        else {
            'from the title screen select save slot 10'
        }
        Write-Host ("NO-QA ORDINARY-PLAYER HANDSHAKE: no internal QA auto-load or ReturnHome navigation is active. Use normal player controls now: {0}. Waiting for SaveLoaded with {1}s remaining; shared runner deadline={2}." -f $noQaTitleInstruction, $noQaSaveLoadedWaitSeconds, $noQaRunnerDeadline.ToString('o')) -ForegroundColor Yellow
        "NoQaOrdinaryPlayerHandshake=$(Get-Date -Format o);shown=True;instruction=$noQaTitleInstruction;internalQaAutoLoad=False;internalQaReturnHome=False;remainingSeconds=$noQaSaveLoadedWaitSeconds;deadline=$($noQaRunnerDeadline.ToString('o'))" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    }
    if ($gameLaunchedOk -and $qaG4AnyTitleUiEnabled) {
        # The optional QA case owns the complete selected title-UI observation.
        # Its verified terminal is emitted only after the selected QA screenshots exist, so
        # do not wait on unrelated button/menu screenshot breadcrumbs that this
        # ownership route deliberately disables.
        $titleMenuOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.TitleSettingsMenu = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $titleButtonOk = $titleMenuOk
        $titleButtonScreenshotOk = $titleMenuOk
        $titleMenuScreenshotOk = $titleMenuOk
        if ($qaG4ManagerStatusUiEnabled -or $qaG4ManagerMvpUiEnabled) {
            $managerStatusPageOk = $titleMenuOk
            $managerStatusSummaryTextOk = $titleMenuOk
            $managerStatusPageScreenshotOk = $titleMenuOk
        }
        if ($qaG4ManagerMvpUiEnabled) {
            $managerStatusSummaryCopyOk = $titleMenuOk
            $managerModsPageOk = $titleMenuOk
            $managerPlayerInteractionsOk = $titleMenuOk
            $managerErrorsPageOk = $titleMenuOk
            $managerHooksPageOk = $titleMenuOk
            $managerFeaturesPageOk = $titleMenuOk
            $managerLogsPageOk = $titleMenuOk
            $managerLogsExportButtonOk = $titleMenuOk
            $managerLogsExportStateTextOk = $titleMenuOk
            $managerLogsPageScreenshotOk = $titleMenuOk
        }
    }
    elseif ($gameLaunchedOk -and $titleSettingsRequested) {
        $titleButtonOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'DTMAPI title settings button visible on HomePageUiState.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        if ($titleButtonOk) {
            $titleButtonScreenshotOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Title settings button screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        if ($titleButtonScreenshotOk) {
            $titleMenuOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI title settings menu.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        if ($titleMenuOk) {
            $titleMenuScreenshotOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Title settings menu screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
    }
    if ($titleMenuOk -and $managerStatusRequested -and -not $qaG4AnyTitleUiEnabled) {
        $managerStatusPageOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Status page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($managerStatusPageOk -and $managerStatusRequested -and -not $qaG4AnyTitleUiEnabled) {
        $managerStatusSummaryTextOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Manager Status summary text OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($managerStatusSummaryTextOk -and $managerStatusRequested -and -not $qaG4AnyTitleUiEnabled) {
        if ($managerMvpRequested) {
            $managerStatusSummaryCopyOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Manager Status summary copy OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        $managerStatusPageScreenshotOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Manager Status page screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($managerStatusPageScreenshotOk -and $managerMvpRequested -and -not $qaG4AnyTitleUiEnabled) {
        $managerModsPageOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Mods page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $managerErrorsPageOk = $managerModsPageOk -and (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Errors page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerHooksPageOk = $managerErrorsPageOk -and (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Hooks page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerFeaturesPageOk = $managerHooksPageOk -and (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Features page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsPageOk = $managerFeaturesPageOk -and (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Logs page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsExportButtonOk = $managerLogsPageOk -and (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Manager Logs export button OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsExportStateTextOk = $managerLogsExportButtonOk -and (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Manager Logs export state OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsPageScreenshotOk = $managerLogsExportStateTextOk -and (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Manager Logs page screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
    }
    if ($gameLaunchedOk -and $AutoOpenOfficialModUi) {
        $officialModUiOk = if ($qaG4OfficialModUiEnabled) {
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.OfficialModUi = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        else {
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Official Mod UI evidence OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
    }
    if ($gameLaunchedOk -and $IncludeHookProbe) {
        $probeOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'HookProbe GameLaunched OK' -TimeoutSeconds 60 -AbortOnFatalInstanceWindow
    }
    if ($probeOk -and $IncludeHookProbe -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'HookProbe SaveLoaded OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $qaG3SaveLoadedBranchRequested) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseTitleButtonLifecycle -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseSaveLoadCycle -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseSaveLoadCyclePendingPressure -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and
        $MoreSavesFixed12AcceptancePhase -in @('EnabledLifecycle', 'ReenabledCold') -and
        $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoOpenAnimalPanel -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseExperimentalHooks -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseActionSpeedTool -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseActionSpeedConfigApply -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseActionSpeedInteraction -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionResourceHit -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionWrongTool -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionFuelFeed -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionVegetation -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoPressOneActionMenuKey -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and ($AutoPressAutoFishingHotkey -or $AutoExerciseAutoFishingMovementCancel) -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and ($AutoExerciseAutoFishingPhase -or $AutoExerciseLegacyFishingCompatibility) -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $autoFishingNoDemandFrameProfile -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExercisePauseMenuLayout -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and
        ($AutoExerciseInstantSave -or
         $debugConsoleSaveAcceptanceRequested) -and
        $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and ($Batch6AutoFishingPilot -or $Batch6AutoFishingManagerLifecycle) -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $batch5GcLadderEnabled -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AssertNoQaUiEvidence -and $SaveSlot -gt 0) {
        $saveLoadedOk = (Test-SmokeDeadlineHasBudget -Deadline $noQaRunnerDeadline) -and
            (Wait-ForLogLineAfterOffsetUntilDeadline -LogPath $logPath -Offset 0 -Pattern 'SaveLoaded hook dispatched.' -Deadline $noQaRunnerDeadline -AbortOnFatalInstanceWindow)
        $noQaSaveLoadedDeadlineExpired = -not $saveLoadedOk -and -not (Test-SmokeDeadlineHasBudget -Deadline $noQaRunnerDeadline)
        "NoQaSaveLoadedWait=$(Get-Date -Format o);ok=$saveLoadedOk;waitSeconds=$noQaSaveLoadedWaitSeconds;deadlineExpired=$noQaSaveLoadedDeadlineExpired" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        if ($noQaSaveLoadedDeadlineExpired) {
            Write-Warning 'NO-QA ORDINARY-PLAYER HANDSHAKE timed out before SaveLoaded. The shared deadline is exhausted; proceeding directly to bounded evidence collection and process cleanup.'
        }
    }
    elseif ($probeOk -and ($requiresDebugConsoleKeySmoke -or $AutoExerciseDebugInventory -or $AutoExerciseDebugWeather -or $AutoExerciseDebugTeleport -or $AutoExerciseDebugTime -or $AutoExerciseDebugMovement -or $AutoExerciseAdvancedDebug -or $AutoExerciseNewContentApis -or $QaObserveContentMetadata -or $QaObserveEquipmentSlotsUi -or $AutoExerciseMineContentApis -or $AutoExerciseZoom -or $AutoExerciseZoomProductNative -or $AutoExerciseChestLocatorEnhancer -or $AutoExerciseMoreEquipmentSlots -or $ObserveMoreEquipmentSlotsNoNativeSaveCold -or $AutoExerciseMoreSavesOfficialSaveUi -or $AutoExerciseStrongPlantingGun -or $AutoExerciseCropHarvestingApi -or $AutoExerciseCustomEntityApis -or $AutoExerciseAudioReplacement -or $AutoExerciseHatchAnimalVoice) -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and ($AutoExerciseModOwnerLifetime -or $TitleIdleBeforeSaveSeconds -gt 0) -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $Batch6AutoFishingPilot) {
        $batch6AutoFishingHandshakeOk = $true
        $batch6AutoFishingInputOk = $true
        $batch6AutoFishingHandshakeOffset = [int64]0
        $batch6AutoFishingDirectNeutralStabilityMilliseconds = 1000
        if ($Batch6AutoFishingLevel -ne 'L0') {
            $initialEnable = Send-Batch6AutoFishingF6AfterHandshake -LogPath $logPath -Offset $batch6AutoFishingHandshakeOffset `
                -State 'awaiting-initial-enable-toggle' -Key $autoFishingToggleKeyEffective -TimeoutSeconds $TimeoutSeconds `
                -HandshakeReceipts $batch6AutoFishingHandshakeReceipts -InputReceipts $batch6AutoFishingInputReceipts
            $batch6AutoFishingHandshakeOk = $batch6AutoFishingHandshakeOk -and [bool]$initialEnable.HandshakeObserved
            $batch6AutoFishingInputOk = $batch6AutoFishingInputOk -and [bool]$initialEnable.ProvenancePassed
            $batch6AutoFishingHandshakeOffset = [int64]$initialEnable.InputLogOffset

            if ($batch6AutoFishingHandshakeOk -and $batch6AutoFishingInputOk -and $Batch6AutoFishingManualMovementCancel) {
                $movementPhaseSlugs = @('ready','cast','wait','bite-ready','minigame')
                for ($phaseIndex = 0; $phaseIndex -lt $movementPhaseSlugs.Count; $phaseIndex++) {
                    $phaseSlug = $movementPhaseSlugs[$phaseIndex]
                    $movementCancel = Send-Batch6AutoFishingF6AfterHandshake -LogPath $logPath -Offset $batch6AutoFishingHandshakeOffset `
                        -State ("awaiting-manual-movement-$phaseSlug-a") -Key 'A' -TimeoutSeconds $TimeoutSeconds `
                        -HandshakeReceipts $batch6AutoFishingHandshakeReceipts -InputReceipts $batch6AutoFishingInputReceipts
                    $batch6AutoFishingHandshakeOk = $batch6AutoFishingHandshakeOk -and [bool]$movementCancel.HandshakeObserved
                    $batch6AutoFishingInputOk = $batch6AutoFishingInputOk -and [bool]$movementCancel.ProvenancePassed
                    $batch6AutoFishingHandshakeOffset = [int64]$movementCancel.InputLogOffset
                    if (-not $batch6AutoFishingHandshakeOk -or -not $batch6AutoFishingInputOk) { break }
                    if ($phaseIndex + 1 -lt $movementPhaseSlugs.Count) {
                        $nextPhaseSlug = $movementPhaseSlugs[$phaseIndex + 1]
                        $reenable = Send-Batch6AutoFishingF6AfterHandshake -LogPath $logPath -Offset $batch6AutoFishingHandshakeOffset `
                            -State ("awaiting-manual-movement-$nextPhaseSlug-reenable") -Key $autoFishingToggleKeyEffective -TimeoutSeconds $TimeoutSeconds `
                            -HandshakeReceipts $batch6AutoFishingHandshakeReceipts -InputReceipts $batch6AutoFishingInputReceipts
                        $batch6AutoFishingHandshakeOk = $batch6AutoFishingHandshakeOk -and [bool]$reenable.HandshakeObserved
                        $batch6AutoFishingInputOk = $batch6AutoFishingInputOk -and [bool]$reenable.ProvenancePassed
                        $batch6AutoFishingHandshakeOffset = [int64]$reenable.InputLogOffset
                        if (-not $batch6AutoFishingHandshakeOk -or -not $batch6AutoFishingInputOk) { break }
                    }
                }
            }
            elseif ($batch6AutoFishingHandshakeOk -and $batch6AutoFishingInputOk) {
                $initialDisable = Send-Batch6AutoFishingF6AfterHandshake -LogPath $logPath -Offset $batch6AutoFishingHandshakeOffset `
                    -State 'awaiting-disable-toggle' -Key $autoFishingToggleKeyEffective -TimeoutSeconds $TimeoutSeconds `
                    -HandshakeReceipts $batch6AutoFishingHandshakeReceipts -InputReceipts $batch6AutoFishingInputReceipts
                $batch6AutoFishingHandshakeOk = $batch6AutoFishingHandshakeOk -and [bool]$initialDisable.HandshakeObserved
                $batch6AutoFishingInputOk = $batch6AutoFishingInputOk -and [bool]$initialDisable.ProvenancePassed
                $batch6AutoFishingHandshakeOffset = [int64]$initialDisable.InputLogOffset
            }

            if ($batch6AutoFishingHandshakeOk -and $batch6AutoFishingInputOk -and $Batch6AutoFishingLevel -eq 'L4') {
                $l4Recovery = Wait-Batch6AutoFishingHandshakeState -LogPath $logPath -Offset $batch6AutoFishingHandshakeOffset `
                    -State 'l4-qa-recovery-active' -TimeoutSeconds $TimeoutSeconds -Receipts $batch6AutoFishingHandshakeReceipts
                $batch6AutoFishingHandshakeOk = $batch6AutoFishingHandshakeOk -and [bool]$l4Recovery.Observed
            }
            elseif ($batch6AutoFishingHandshakeOk -and $batch6AutoFishingInputOk -and $Batch6AutoFishingLevel -eq 'L5') {
                foreach ($state in @('awaiting-title','awaiting-fifth-save-reentry')) {
                    $waitReceipt = Wait-Batch6AutoFishingHandshakeState -LogPath $logPath -Offset $batch6AutoFishingHandshakeOffset `
                        -State $state -TimeoutSeconds $TimeoutSeconds -Receipts $batch6AutoFishingHandshakeReceipts
                    $batch6AutoFishingHandshakeOk = $batch6AutoFishingHandshakeOk -and [bool]$waitReceipt.Observed
                    if (-not $batch6AutoFishingHandshakeOk) { break }
                }
                if ($batch6AutoFishingHandshakeOk -and $batch6AutoFishingInputOk) {
                    $reentryEnable = Send-Batch6AutoFishingF6AfterHandshake -LogPath $logPath -Offset $batch6AutoFishingHandshakeOffset `
                        -State 'awaiting-reentry-enable-toggle' -Key $autoFishingToggleKeyEffective -TimeoutSeconds $TimeoutSeconds `
                        -HandshakeReceipts $batch6AutoFishingHandshakeReceipts -InputReceipts $batch6AutoFishingInputReceipts
                    $batch6AutoFishingHandshakeOk = $batch6AutoFishingHandshakeOk -and [bool]$reentryEnable.HandshakeObserved
                    $batch6AutoFishingInputOk = $batch6AutoFishingInputOk -and [bool]$reentryEnable.ProvenancePassed
                    $batch6AutoFishingHandshakeOffset = [int64]$reentryEnable.InputLogOffset
                }
                if ($batch6AutoFishingHandshakeOk -and $batch6AutoFishingInputOk) {
                    $finalDisable = Send-Batch6AutoFishingF6AfterHandshake -LogPath $logPath -Offset $batch6AutoFishingHandshakeOffset `
                        -State 'awaiting-final-disable-toggle' -Key $autoFishingToggleKeyEffective -TimeoutSeconds $TimeoutSeconds `
                        -HandshakeReceipts $batch6AutoFishingHandshakeReceipts -InputReceipts $batch6AutoFishingInputReceipts
                    $batch6AutoFishingHandshakeOk = $batch6AutoFishingHandshakeOk -and [bool]$finalDisable.HandshakeObserved
                    $batch6AutoFishingInputOk = $batch6AutoFishingInputOk -and [bool]$finalDisable.ProvenancePassed
                    $batch6AutoFishingHandshakeOffset = [int64]$finalDisable.InputLogOffset
                }
            }
        }

        $expectedBatch6HandshakeStates = if ($Batch6AutoFishingManualMovementCancel) {
            @(
                'awaiting-initial-enable-toggle',
                'awaiting-manual-movement-ready-a',
                'awaiting-manual-movement-cast-reenable',
                'awaiting-manual-movement-cast-a',
                'awaiting-manual-movement-wait-reenable',
                'awaiting-manual-movement-wait-a',
                'awaiting-manual-movement-bite-ready-reenable',
                'awaiting-manual-movement-bite-ready-a',
                'awaiting-manual-movement-minigame-reenable',
                'awaiting-manual-movement-minigame-a')
        }
        else { switch ($Batch6AutoFishingLevel) {
            'L0' { @() }
            'L4' { @('awaiting-initial-enable-toggle','awaiting-disable-toggle','l4-qa-recovery-active') }
            'L5' { @('awaiting-initial-enable-toggle','awaiting-disable-toggle','awaiting-title','awaiting-fifth-save-reentry','awaiting-reentry-enable-toggle','awaiting-final-disable-toggle') }
            default { @('awaiting-initial-enable-toggle','awaiting-disable-toggle') }
        } }
        $expectedBatch6InputStates = if ($Batch6AutoFishingManualMovementCancel) {
            @($expectedBatch6HandshakeStates)
        }
        else { switch ($Batch6AutoFishingLevel) {
            'L0' { @() }
            'L5' { @('awaiting-initial-enable-toggle','awaiting-disable-toggle','awaiting-reentry-enable-toggle','awaiting-final-disable-toggle') }
            default { @('awaiting-initial-enable-toggle','awaiting-disable-toggle') }
        } }
        $actualBatch6HandshakeStates = @($batch6AutoFishingHandshakeReceipts | ForEach-Object { [string]$_.State })
        $actualBatch6InputStates = @($batch6AutoFishingInputReceipts | ForEach-Object { [string]$_.HandshakeState })
        $batch6AutoFishingHandshakeOk = $batch6AutoFishingHandshakeOk -and
            (($actualBatch6HandshakeStates -join '|') -ceq ($expectedBatch6HandshakeStates -join '|'))
        $batch6AutoFishingInputOk = $batch6AutoFishingInputOk -and
            (($actualBatch6InputStates -join '|') -ceq ($expectedBatch6InputStates -join '|')) -and
            @($batch6AutoFishingInputReceipts | Where-Object { -not [bool]$_.ProvenancePassed }).Count -eq 0
        $preflightInputs = @($batch6AutoFishingInputReceipts | Where-Object {
            [string]$_.Purpose -in @('FacingRightPreflight','NativeNeutralReset') -or [string]$_.Key -in @('D','Escape')
        })
        $batch6AutoFishingInputOk = $batch6AutoFishingInputOk -and
            $preflightInputs.Count -eq 0
        if ($Batch6AutoFishingManualMovementCancel) {
            $actualKeys = @($batch6AutoFishingInputReceipts | ForEach-Object { [string]$_.Key })
            $batch6AutoFishingInputOk = $batch6AutoFishingInputOk -and
                (($actualKeys -join '|') -ceq 'F7|A|F7|A|F7|A|F7|A|F7|A')
        }
        else {
            $configuredToggleInputs = @($batch6AutoFishingInputReceipts | Where-Object { [string]$_.Purpose -ceq 'ConfiguredToggle' })
            $batch6AutoFishingInputOk = $batch6AutoFishingInputOk -and
                @($configuredToggleInputs | Where-Object { [string]$_.Key -cne $autoFishingToggleKeyEffective }).Count -eq 0
        }
        $preflightChronologyOk = $true
        foreach ($enableState in @('awaiting-initial-enable-toggle','awaiting-reentry-enable-toggle')) {
            $enableInputsForState = @($batch6AutoFishingInputReceipts | Where-Object { [string]$_.HandshakeState -ceq $enableState })
            $enableHandshakesForState = @($batch6AutoFishingHandshakeReceipts | Where-Object { [string]$_.State -ceq $enableState })
            if ($enableInputsForState.Count -eq 0 -and $enableHandshakesForState.Count -eq 0) { continue }
            $preflightChronologyOk = $preflightChronologyOk -and
                $enableInputsForState.Count -eq 1 -and $enableHandshakesForState.Count -eq 1 -and
                [string]$enableInputsForState[0].Purpose -ceq 'ConfiguredToggle' -and [bool]$enableHandshakesForState[0].Observed -and
                ([datetimeoffset]$enableHandshakesForState[0].CompletedAtUtc) -le ([datetimeoffset]$enableInputsForState[0].KeyDownAt)
        }
        $batch6AutoFishingInputOk = $batch6AutoFishingInputOk -and $preflightChronologyOk

        $batch6AutoFishingTerminalPattern = 'Hook status: Smoke.Batch6.AutoFishingPilot = verified.'
        $batch6AutoFishingPilotOk = $batch6AutoFishingHandshakeOk -and $batch6AutoFishingInputOk -and
            (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern $batch6AutoFishingTerminalPattern -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $batch6AutoFishingCleanupOk = $batch6AutoFishingPilotOk -and
            (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Hook status: Smoke.Batch6.AutoFishingPilot.Cleanup = verified.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        Write-SmokeJsonObject -Path (Join-Path $evidence 'batch6-autofishing-runner-input.json') -Value ([ordered]@{
            SchemaVersion = 1
            Level = $Batch6AutoFishingLevel
            FormalContract = $Batch6AutoFishingFormalContract
            Scenario = $Batch6AutoFishingScenario
            CastChargeRatio = $Batch6AutoFishingCastChargeRatio
            ManualMovementCancel = [bool]$Batch6AutoFishingManualMovementCancel
            ToggleKey = $autoFishingToggleKeyEffective
            PreflightMode = 'DirectReadOnlyNoInput'
            PreflightInputCount = [int]$preflightInputs.Count
            DirectNeutralStabilityMilliseconds = $batch6AutoFishingDirectNeutralStabilityMilliseconds
            HandshakePassed = [bool]$batch6AutoFishingHandshakeOk
            InputPassed = [bool]$batch6AutoFishingInputOk
            Handshakes = @($batch6AutoFishingHandshakeReceipts.ToArray())
            Inputs = @($batch6AutoFishingInputReceipts.ToArray())
        })
        "Batch6AutoFishingRunner=$(Get-Date -Format o);level=$Batch6AutoFishingLevel;handshakes=$($actualBatch6HandshakeStates -join '|');inputs=$($actualBatch6InputStates -join '|');handshakePassed=$batch6AutoFishingHandshakeOk;inputPassed=$batch6AutoFishingInputOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    }
    if ($saveLoadedOk -and $Batch6AutoFishingManagerLifecycle) {
        $batch6ManagerHandshakeOffset = [int64]0
        if ($Batch6AutoFishingManagerMode -eq 'SameProcessDisable') {
            $batch6AutoFishingManagerHandshakeOk = $true
            $batch6AutoFishingManagerActionOk = $true
            $initialEnable = Send-Batch6AutoFishingF6AfterHandshake -LogPath $logPath -Offset $batch6ManagerHandshakeOffset `
                -State 'awaiting-initial-enable-f6' -TimeoutSeconds $TimeoutSeconds `
                -HandshakeReceipts $batch6AutoFishingManagerHandshakeReceipts -InputReceipts $batch6AutoFishingManagerInputReceipts `
                -HookId 'Smoke.Batch6.AutoFishingManagerLifecycle.Handshake'
            $batch6AutoFishingManagerHandshakeOk = [bool]$initialEnable.HandshakeObserved
            $batch6AutoFishingManagerActionOk = [bool]$initialEnable.ProvenancePassed
            $batch6ManagerHandshakeOffset = [int64]$initialEnable.InputLogOffset

            if ($batch6AutoFishingManagerHandshakeOk -and $batch6AutoFishingManagerActionOk) {
                $createHandshake = Wait-Batch6AutoFishingHandshakeState -LogPath $logPath -Offset $batch6ManagerHandshakeOffset `
                    -State 'awaiting-create-disabled-marker' -TimeoutSeconds $TimeoutSeconds `
                    -Receipts $batch6AutoFishingManagerHandshakeReceipts -HookId 'Smoke.Batch6.AutoFishingManagerLifecycle.Handshake'
                $batch6AutoFishingManagerHandshakeOk = [bool]$createHandshake.Observed
                $created = $false
                $actualCreateSha = ''
                if ($batch6AutoFishingManagerHandshakeOk -and -not (Test-Path -LiteralPath $batch6ManagerMarkerPath)) {
                    [System.IO.File]::WriteAllBytes($batch6ManagerMarkerPath, $batch6ManagerMarkerBytes)
                    $actualCreateSha = Get-SmokeFileSha256 -Path $batch6ManagerMarkerPath
                    $created = [string]::Equals($actualCreateSha, $Batch6AutoFishingManagerMarkerSha256, [System.StringComparison]::OrdinalIgnoreCase)
                }
                $batch6AutoFishingManagerActionOk = $batch6AutoFishingManagerActionOk -and $created
                [void]$batch6AutoFishingManagerMarkerActionReceipts.Add([ordered]@{
                    Sequence = 1; Action = 'CreateExactDisabledMarker'; HandshakeObserved = [bool]$createHandshake.Observed
                    Path = $batch6ManagerMarkerPath; ExpectedSha256 = $Batch6AutoFishingManagerMarkerSha256
                    ActualSha256 = $actualCreateSha; Passed = $created
                })
            }

            if ($batch6AutoFishingManagerHandshakeOk -and $batch6AutoFishingManagerActionOk) {
                $removeHandshake = Wait-Batch6AutoFishingHandshakeState -LogPath $logPath -Offset $batch6ManagerHandshakeOffset `
                    -State 'awaiting-remove-disabled-marker' -TimeoutSeconds $TimeoutSeconds `
                    -Receipts $batch6AutoFishingManagerHandshakeReceipts -HookId 'Smoke.Batch6.AutoFishingManagerLifecycle.Handshake'
                $batch6AutoFishingManagerHandshakeOk = [bool]$removeHandshake.Observed
                $removed = $false
                if ($batch6AutoFishingManagerHandshakeOk -and (Test-Path -LiteralPath $batch6ManagerMarkerPath -PathType Leaf)) {
                    $preRemoveSha = Get-SmokeFileSha256 -Path $batch6ManagerMarkerPath
                    if ([string]::Equals($preRemoveSha, $Batch6AutoFishingManagerMarkerSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
                        Remove-Item -Force -LiteralPath $batch6ManagerMarkerPath
                        $removed = -not (Test-Path -LiteralPath $batch6ManagerMarkerPath)
                    }
                }
                $batch6AutoFishingManagerActionOk = $batch6AutoFishingManagerActionOk -and $removed
                [void]$batch6AutoFishingManagerMarkerActionReceipts.Add([ordered]@{
                    Sequence = 2; Action = 'RemoveExactDisabledMarker'; HandshakeObserved = [bool]$removeHandshake.Observed
                    Path = $batch6ManagerMarkerPath; ExpectedSha256 = $Batch6AutoFishingManagerMarkerSha256
                    Passed = $removed
                })
            }

            $actualManagerHandshakeStates = @($batch6AutoFishingManagerHandshakeReceipts | ForEach-Object { [string]$_.State })
            $batch6AutoFishingManagerHandshakeOk = $batch6AutoFishingManagerHandshakeOk -and
                (($actualManagerHandshakeStates -join '|') -ceq 'awaiting-initial-enable-f6|awaiting-create-disabled-marker|awaiting-remove-disabled-marker')
            $batch6AutoFishingManagerActionOk = $batch6AutoFishingManagerActionOk -and
                @($batch6AutoFishingManagerInputReceipts).Count -eq 1 -and
                [string]$batch6AutoFishingManagerInputReceipts[0].Key -ceq 'F6' -and
                [bool]$batch6AutoFishingManagerInputReceipts[0].ProvenancePassed -and
                @($batch6AutoFishingManagerMarkerActionReceipts | Where-Object { -not [bool]$_.Passed }).Count -eq 0
        }

        $managerTerminalPattern = 'Hook status: Smoke.Batch6.AutoFishingManagerLifecycle = verified.'
        $batch6AutoFishingManagerLifecycleOk = $batch6AutoFishingManagerHandshakeOk -and $batch6AutoFishingManagerActionOk -and
            (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern $managerTerminalPattern -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $batch6AutoFishingManagerCleanupOk = $batch6AutoFishingManagerLifecycleOk -and
            (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Hook status: Smoke.Batch6.AutoFishingManagerLifecycle.Cleanup = verified.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        Write-SmokeJsonObject -Path (Join-Path $evidence 'batch6-autofishing-manager-runner-actions.json') -Value ([ordered]@{
            SchemaVersion = 1
            Mode = $Batch6AutoFishingManagerMode
            ProductRoot = $Batch6AutoFishingManagerProductRoot
            MarkerPath = $batch6ManagerMarkerPath
            MarkerSha256 = $Batch6AutoFishingManagerMarkerSha256
            HandshakePassed = [bool]$batch6AutoFishingManagerHandshakeOk
            ActionPassed = [bool]$batch6AutoFishingManagerActionOk
            Handshakes = @($batch6AutoFishingManagerHandshakeReceipts.ToArray())
            Inputs = @($batch6AutoFishingManagerInputReceipts.ToArray())
            MarkerActions = @($batch6AutoFishingManagerMarkerActionReceipts.ToArray())
        })
        "Batch6AutoFishingManagerRunner=$(Get-Date -Format o);mode=$Batch6AutoFishingManagerMode;handshakePassed=$batch6AutoFishingManagerHandshakeOk;actionPassed=$batch6AutoFishingManagerActionOk;lifecyclePassed=$batch6AutoFishingManagerLifecycleOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    }
    if ($saveLoadedOk -and $qaG4EquipmentSlotsObservationEnabled) {
        $equipmentLogOffset = if (Test-Path -LiteralPath $logPath -PathType Leaf) { [int64](Get-Item -LiteralPath $logPath).Length } else { [int64]0 }
        $equipmentInputStartedAt = (Get-Date).ToUniversalTime().ToString('o')
        $equipmentOpenSent = Send-DolocTownKey -VirtualKey 0x42 -Name 'EquipmentSlotsB' -HoldMilliseconds $ExternalPlayerInputHoldMilliseconds -InputLogPath $logPath
        $equipmentOpenEvidence = $script:lastDolocTownInputEvidence
        $equipmentOpenProvenanceOk = $equipmentOpenSent -and [bool]$equipmentOpenEvidence.ForegroundMatchedAtSend -and
            [bool]$equipmentOpenEvidence.SendInputSucceeded -and -not [bool]$equipmentOpenEvidence.PostMessageFallbackUsed
        $equipmentSlotsPlayerInputAttempts.Add([ordered]@{
            Label = 'EquipmentSlotsB'
            AttemptStartedAt = $equipmentInputStartedAt
            Sent = [bool]$equipmentOpenSent
            ForegroundMatched = [bool]$equipmentOpenEvidence.ForegroundMatchedAtSend
            SendInputSucceeded = [bool]$equipmentOpenEvidence.SendInputSucceeded
            PostMessageFallbackUsed = [bool]$equipmentOpenEvidence.PostMessageFallbackUsed
            LogOffset = $equipmentLogOffset
        }) | Out-Null
        $equipmentObservationDeadline = (Get-Date).AddSeconds($TimeoutSeconds)
        $equipmentEvidenceCaptured = $false
        $equipmentHoverOk = $false
        if ($equipmentOpenProvenanceOk -and $moreEquipmentSlots100UiAcceptanceRequested) {
            $equipmentEvidenceCaptured = Wait-ForLogLineAfterOffsetUntilDeadline `
                -LogPath $logPath `
                -Offset $equipmentLogOffset `
                -Pattern 'EVIDENCE_CAPTURED_WAITING_ESCAPE' `
                -Deadline $equipmentObservationDeadline `
                -AbortOnFatalInstanceWindow
            $equipmentHoverReceipt = if ($equipmentEvidenceCaptured -and (Get-Date) -lt $equipmentObservationDeadline) {
                Invoke-DolocTownHoverSweep `
                    -LogPath $logPath `
                    -LogOffset $equipmentLogOffset `
                    -Pattern 'MoreEquipmentSlots UI hover slot=' `
                    -Deadline $equipmentObservationDeadline
            }
            else {
                [PSCustomObject]@{ DolocTownPid = 0; ForegroundMatched = $false; MoveOnly = $true; MouseClickSent = $false; PointsVisited = 0; MatchedExpectedLog = $false }
            }
            $equipmentHoverOk = [bool]$equipmentHoverReceipt.ForegroundMatched -and
                [bool]$equipmentHoverReceipt.MoveOnly -and
                -not [bool]$equipmentHoverReceipt.MouseClickSent -and
                [bool]$equipmentHoverReceipt.MatchedExpectedLog
            $equipmentSlotsPlayerInputAttempts.Add([ordered]@{
                Label = 'MoreEquipmentSlotsDynamicRowMoveOnlyHoverSweep'
                ForegroundMatched = [bool]$equipmentHoverReceipt.ForegroundMatched
                MoveOnly = [bool]$equipmentHoverReceipt.MoveOnly
                MouseClickSent = [bool]$equipmentHoverReceipt.MouseClickSent
                PointsVisited = [int]$equipmentHoverReceipt.PointsVisited
                MatchedExpectedLog = [bool]$equipmentHoverReceipt.MatchedExpectedLog
                LogOffset = $equipmentLogOffset
            }) | Out-Null
        }
        elseif ($equipmentOpenProvenanceOk) {
            $equipmentEvidenceCaptured = Wait-ForLogLineAfterOffset -LogPath $logPath -Offset $equipmentLogOffset -Pattern 'EVIDENCE_CAPTURED_WAITING_ESCAPE' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $equipmentHoverReceipt = if ($equipmentEvidenceCaptured -and (Get-Date) -lt $equipmentObservationDeadline) {
                Invoke-DolocTownHoverSweep -LogPath $logPath -LogOffset $equipmentLogOffset -Pattern 'EquipmentSlots UI hover owner=' -Deadline $equipmentObservationDeadline
            }
            else {
                [PSCustomObject]@{ DolocTownPid = 0; ForegroundMatched = $false; MoveOnly = $true; MouseClickSent = $false; PointsVisited = 0; MatchedExpectedLog = $false }
            }
            $equipmentHoverOk = [bool]$equipmentHoverReceipt.ForegroundMatched -and [bool]$equipmentHoverReceipt.MoveOnly -and
                -not [bool]$equipmentHoverReceipt.MouseClickSent -and [bool]$equipmentHoverReceipt.MatchedExpectedLog
            $equipmentSlotsPlayerInputAttempts.Add([ordered]@{
                Label = 'EquipmentSlotsMoveOnlyHoverSweep'
                ForegroundMatched = [bool]$equipmentHoverReceipt.ForegroundMatched
                MoveOnly = [bool]$equipmentHoverReceipt.MoveOnly
                MouseClickSent = [bool]$equipmentHoverReceipt.MouseClickSent
                PointsVisited = [int]$equipmentHoverReceipt.PointsVisited
                MatchedExpectedLog = [bool]$equipmentHoverReceipt.MatchedExpectedLog
                LogOffset = $equipmentLogOffset
            }) | Out-Null
        }
        $equipmentSlotsUiEvidenceCapturedOk = [bool]$equipmentEvidenceCaptured
        $equipmentSlotsUiHoverOk = [bool]$equipmentHoverOk
        if ($equipmentEvidenceCaptured -and $equipmentHoverOk) {
            $equipmentCloseVirtualKey =
                if ($MoreEquipmentSlotsTransitionPhase -eq 'U1') {
                    0x42
                }
                else {
                    0x1B
                }
            $equipmentCloseLabel =
                if ($MoreEquipmentSlotsTransitionPhase -eq 'U1') {
                    'EquipmentSlotsCloseB'
                }
                else {
                    'EquipmentSlotsEscape'
                }
            $equipmentCloseSent = Send-DolocTownKey -VirtualKey $equipmentCloseVirtualKey -Name $equipmentCloseLabel -HoldMilliseconds $ExternalPlayerInputHoldMilliseconds -InputLogPath $logPath
            $equipmentCloseEvidence = $script:lastDolocTownInputEvidence
            $equipmentSlotsPlayerInputAttempts.Add([ordered]@{
                Label = $equipmentCloseLabel
                Sent = [bool]$equipmentCloseSent
                ForegroundMatched = [bool]$equipmentCloseEvidence.ForegroundMatchedAtSend
                SendInputSucceeded = [bool]$equipmentCloseEvidence.SendInputSucceeded
                PostMessageFallbackUsed = [bool]$equipmentCloseEvidence.PostMessageFallbackUsed
                LogOffset = [int64]$equipmentCloseEvidence.LogOffsetAtSend
            }) | Out-Null
            $equipmentCloseProvenanceOk = $equipmentCloseSent -and [bool]$equipmentCloseEvidence.ForegroundMatchedAtSend -and
                [bool]$equipmentCloseEvidence.SendInputSucceeded -and -not [bool]$equipmentCloseEvidence.PostMessageFallbackUsed
            $equipmentSlotsUiCloseInputOk = $equipmentCloseProvenanceOk
            if ($equipmentCloseProvenanceOk -and $moreEquipmentSlots100UiAcceptanceRequested) {
                $reopenReady = Wait-ForLogLineAfterOffsetUntilDeadline `
                    -LogPath $logPath `
                    -Offset ([int64]$equipmentCloseEvidence.LogOffsetAtSend) `
                    -Pattern 'MORE_EQUIPMENT_100_WAITING_REOPEN_B' `
                    -Deadline $equipmentObservationDeadline `
                    -AbortOnFatalInstanceWindow
                $reopenSent = $reopenReady -and (Send-DolocTownKey -VirtualKey 0x42 -Name 'MoreEquipmentSlotsReopenB' -HoldMilliseconds $ExternalPlayerInputHoldMilliseconds -InputLogPath $logPath)
                $reopenEvidence = if ($reopenSent) {
                    $script:lastDolocTownInputEvidence
                }
                else {
                    [ordered]@{
                        ForegroundMatchedAtSend = $false
                        SendInputSucceeded = $false
                        PostMessageFallbackUsed = $false
                        LogOffsetAtSend = [int64]0
                    }
                }
                $reopenOk = $reopenSent -and
                    [bool]$reopenEvidence.ForegroundMatchedAtSend -and
                    [bool]$reopenEvidence.SendInputSucceeded -and
                    -not [bool]$reopenEvidence.PostMessageFallbackUsed
                $equipmentSlotsPlayerInputAttempts.Add([ordered]@{
                    Label = 'MoreEquipmentSlotsReopenB'
                    MarkerObserved = [bool]$reopenReady
                    Sent = [bool]$reopenSent
                    ForegroundMatched = [bool]$reopenEvidence.ForegroundMatchedAtSend
                    SendInputSucceeded = [bool]$reopenEvidence.SendInputSucceeded
                    PostMessageFallbackUsed = [bool]$reopenEvidence.PostMessageFallbackUsed
                    LogOffset = [int64]$reopenEvidence.LogOffsetAtSend
                }) | Out-Null
                $reopenCaptured = $reopenOk -and (Wait-ForLogLineAfterOffsetUntilDeadline `
                    -LogPath $logPath `
                    -Offset ([int64]$reopenEvidence.LogOffsetAtSend) `
                    -Pattern 'MORE_EQUIPMENT_100_REOPEN_CAPTURED_WAITING_ESCAPE' `
                    -Deadline $equipmentObservationDeadline `
                    -AbortOnFatalInstanceWindow)
                $finalCloseSent = $reopenCaptured -and (Send-DolocTownKey -VirtualKey 0x1B -Name 'MoreEquipmentSlotsFinalEscape' -HoldMilliseconds $ExternalPlayerInputHoldMilliseconds -InputLogPath $logPath)
                $finalCloseEvidence = if ($finalCloseSent) {
                    $script:lastDolocTownInputEvidence
                }
                else {
                    [ordered]@{
                        ForegroundMatchedAtSend = $false
                        SendInputSucceeded = $false
                        PostMessageFallbackUsed = $false
                        LogOffsetAtSend = [int64]0
                    }
                }
                $finalCloseOk = $finalCloseSent -and
                    [bool]$finalCloseEvidence.ForegroundMatchedAtSend -and
                    [bool]$finalCloseEvidence.SendInputSucceeded -and
                    -not [bool]$finalCloseEvidence.PostMessageFallbackUsed
                $equipmentSlotsPlayerInputAttempts.Add([ordered]@{
                    Label = 'MoreEquipmentSlotsFinalEscape'
                    MarkerObserved = [bool]$reopenCaptured
                    Sent = [bool]$finalCloseSent
                    ForegroundMatched = [bool]$finalCloseEvidence.ForegroundMatchedAtSend
                    SendInputSucceeded = [bool]$finalCloseEvidence.SendInputSucceeded
                    PostMessageFallbackUsed = [bool]$finalCloseEvidence.PostMessageFallbackUsed
                    LogOffset = [int64]$finalCloseEvidence.LogOffsetAtSend
                }) | Out-Null
                $equipmentSlotsUiCloseInputOk = $equipmentSlotsUiCloseInputOk -and $reopenReady -and $reopenOk -and $reopenCaptured -and $finalCloseOk
                if ($equipmentSlotsUiCloseInputOk) {
                    $remaining = [Math]::Max(1, [int][Math]::Floor(($equipmentObservationDeadline - (Get-Date)).TotalSeconds))
                    $equipmentSlotsUiObservationOk = Wait-ForLogLineAfterOffset -LogPath $logPath -Offset ([int64]$finalCloseEvidence.LogOffsetAtSend) -Pattern 'Smoke.EquipmentSlotsUiObservation = verified' -TimeoutSeconds $remaining -AbortOnFatalInstanceWindow
                }
            }
            elseif ($equipmentCloseProvenanceOk) {
                $remaining = [Math]::Max(1, [int][Math]::Floor(($equipmentObservationDeadline - (Get-Date)).TotalSeconds))
                $equipmentSlotsUiObservationOk = Wait-ForLogLineAfterOffset -LogPath $logPath -Offset ([int64]$equipmentCloseEvidence.LogOffsetAtSend) -Pattern 'Smoke.EquipmentSlotsUiObservation = verified' -TimeoutSeconds $remaining -AbortOnFatalInstanceWindow
            }
        }
    }
    if ($saveLoadedOk -and
        $moreEquipmentSlotsTransitionRequested) {
        $moreEquipmentSlotsTransitionDeadline =
            (Get-Date).AddSeconds($TimeoutSeconds)
        if ($MoreEquipmentSlotsTransitionPhase -eq 'U1' -and
            -not $equipmentSlotsUiObservationOk) {
            $transitionSleepReady = $false
        }
        elseif ($MoreEquipmentSlotsTransitionPhase -notin @('U4','ColdObserve')) {
            $transitionConfirmKey = 'Enter'
            $transitionConfirmVirtualKey = 0x0D
            $transitionReadyWaitSeconds =
                Get-SmokeRemainingBudgetSeconds `
                    -Deadline $moreEquipmentSlotsTransitionDeadline
            $transitionSleepReady =
                $transitionReadyWaitSeconds -gt 0 -and
                (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
                    -LogPath $logPath `
                    -Pattern ("Smoke MoreEquipmentSlotsTransition native sleep menu ready phase={0}; selectedOption=0; inputOwner=runner-real-{1}." -f $MoreEquipmentSlotsTransitionPhase, $transitionConfirmKey) `
                    -TimeoutSeconds $transitionReadyWaitSeconds `
                    -AbortOnFatalInstanceWindow)
            $moreEquipmentSlotsTransitionSleepInput.Requested = $true
            $moreEquipmentSlotsTransitionSleepInput.Key =
                $transitionConfirmKey
            if ($transitionSleepReady) {
                Start-Sleep -Milliseconds 750
                $nativeSaveObservedAfterTransitionInput = $false
                # One foreground Enter is the complete causal action for this
                # process.  If the game does not emit SaveSaving after it, fail
                # this run instead of replaying Enter against a UI state that
                # may already have changed.
                for ($transitionAttemptIndex = 1;
                    $transitionAttemptIndex -le 1 -and
                    -not $nativeSaveObservedAfterTransitionInput;
                    $transitionAttemptIndex++) {
                    $transitionSleepSent =
                        Send-DolocTownKey `
                            -VirtualKey $transitionConfirmVirtualKey `
                            -Name ("MoreEquipmentSlots{0}Sleep{1}Attempt{2}" -f $MoreEquipmentSlotsTransitionPhase, $transitionConfirmKey, $transitionAttemptIndex) `
                            -HoldMilliseconds $ExternalPlayerInputHoldMilliseconds `
                            -InputLogPath $logPath `
                            -Deadline $moreEquipmentSlotsTransitionDeadline
                    $transitionSleepEvidence =
                        $script:lastDolocTownInputEvidence
                    $transitionAttempt = [ordered]@{
                        Sequence = $transitionAttemptIndex
                        Sent = [bool]$transitionSleepSent
                        ForegroundMatched =
                            [bool]$transitionSleepEvidence.ForegroundMatchedAtSend
                        SendInputSucceeded =
                            [bool]$transitionSleepEvidence.SendInputSucceeded
                        PostMessageFallbackUsed =
                            [bool]$transitionSleepEvidence.PostMessageFallbackUsed
                        LogOffset =
                            [int64]$transitionSleepEvidence.LogOffsetAtSend
                        NativeSaveObservedAfterInput = $false
                    }
                    $transitionAttemptProvenanceOk =
                        [bool]$transitionAttempt.Sent -and
                        [bool]$transitionAttempt.ForegroundMatched -and
                        [bool]$transitionAttempt.SendInputSucceeded -and
                        -not [bool]$transitionAttempt.PostMessageFallbackUsed
                    $transitionResponseWaitSeconds =
                        Get-SmokeRemainingBudgetSeconds `
                            -Deadline $moreEquipmentSlotsTransitionDeadline `
                            -MaximumSeconds 5
                    if ($transitionAttemptProvenanceOk -and
                        $transitionResponseWaitSeconds -gt 0) {
                        $nativeSaveObservedAfterTransitionInput =
                            Wait-ForLogLineAfterOffset `
                                -LogPath $logPath `
                                -Offset ([int64]$transitionAttempt.LogOffset) `
                                -Pattern 'SaveSaving hook dispatched. slot/index=2' `
                                -TimeoutSeconds $transitionResponseWaitSeconds `
                                -AbortOnFatalInstanceWindow
                    }
                    $transitionAttempt.NativeSaveObservedAfterInput =
                        [bool]$nativeSaveObservedAfterTransitionInput
                    $moreEquipmentSlotsTransitionSleepInput.Attempts +=
                        [pscustomobject]$transitionAttempt
                }
                $transitionAttempts =
                    @($moreEquipmentSlotsTransitionSleepInput.Attempts)
                $moreEquipmentSlotsTransitionSleepInput.Sent =
                    $transitionAttempts.Count -gt 0 -and
                    @($transitionAttempts | Where-Object {
                        -not [bool]$_.Sent
                    }).Count -eq 0
                $moreEquipmentSlotsTransitionSleepInput.ForegroundMatched =
                    $transitionAttempts.Count -gt 0 -and
                    @($transitionAttempts | Where-Object {
                        -not [bool]$_.ForegroundMatched
                    }).Count -eq 0
                $moreEquipmentSlotsTransitionSleepInput.SendInputSucceeded =
                    $transitionAttempts.Count -gt 0 -and
                    @($transitionAttempts | Where-Object {
                        -not [bool]$_.SendInputSucceeded
                    }).Count -eq 0
                $moreEquipmentSlotsTransitionSleepInput.PostMessageFallbackUsed =
                    @($transitionAttempts | Where-Object {
                        [bool]$_.PostMessageFallbackUsed
                    }).Count -gt 0
                if (-not $nativeSaveObservedAfterTransitionInput) {
                    $transitionSleepReady = $false
                }
            }
        }
        else {
            $transitionSleepReady = $true
        }
        $transitionTerminalWaitSeconds =
            Get-SmokeRemainingBudgetSeconds `
                -Deadline $moreEquipmentSlotsTransitionDeadline
        $moreEquipmentSlotsTransitionOk =
            $transitionSleepReady -and
            $transitionTerminalWaitSeconds -gt 0 -and
            (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
                -LogPath $logPath `
                -Pattern ("Smoke exercise MoreEquipmentSlotsTransition OK phase={0};" -f $MoreEquipmentSlotsTransitionPhase) `
                -TimeoutSeconds $transitionTerminalWaitSeconds `
                -AbortOnFatalInstanceWindow)
    }
    if ($moreSavesFixed12AcceptanceRequested) {
        $moreSavesFixed12LifecycleOk = Wait-MoreSavesFixed12Lifecycle `
            -Phase $MoreSavesFixed12AcceptancePhase -LogPath $logPath -Deadline $SmokeRunnerScenarioDeadline
    }
    if ($saveLoadedOk -and $AutoExerciseNewContentApis) {
        if ($ExpectOilAbsent) {
            $newContentOilAbsentOk = if ($qaG4ContentMetadataObservationEnabled) {
                Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.ContentMetadataObservation = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            }
            else {
                Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise NewContentOilAbsent OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            }
            $contentMetadataObservationOk = $newContentOilAbsentOk
            $newContentApisOk = $newContentOilAbsentOk
        }
        elseif ($OilOnly) {
            $newContentOilOnlyOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise NewContentOilOnly OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            if ($newContentOilOnlyOk) {
                $newContentOilItemMetadataOk = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Smoke exercise NewContentOilItemMetadata OK' -ErrorAction SilentlyContinue).Count -gt 0
                $newContentOilCoalDropOk = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Smoke exercise NewContentOilCoalDrop OK' -ErrorAction SilentlyContinue).Count -gt 0
            }
            $newContentApisOk = $newContentOilItemMetadataOk -and $newContentOilCoalDropOk -and $newContentOilOnlyOk
        }
        else {
            $mineOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Mine ProductNative activation complete owner=dtmapi.mod.dtmapi.minemod hooks=3 schedulerMode=session-derived' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $equipmentSlotsOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'MoreEquipmentSlots API register success=True reason=SaveLoaded' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentOilItemMetadataOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise NewContentOilItemMetadata OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentOilCoalDropOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise NewContentOilCoalDrop OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentMineOfficialJsonOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise NewContentMineOfficialJson OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentEquipmentSlotsOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise NewContentEquipmentSlots OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentMineProductionOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise NewContentMineProduction OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentApisOk = $mineOk -and $equipmentSlotsOk -and $newContentOilItemMetadataOk -and $newContentOilCoalDropOk -and $newContentMineOfficialJsonOk -and $newContentEquipmentSlotsOk -and $newContentMineProductionOk
        }
    }
    if ($saveLoadedOk -and $qaG4ContentMetadataObservationEnabled -and -not $AutoExerciseNewContentApis) {
        $contentMetadataObservationOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.ContentMetadataObservation = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        if ($ExpectOilAbsent) {
            $newContentOilAbsentOk = $contentMetadataObservationOk
        }
    }
    if ($saveLoadedOk -and $AutoExerciseMineContentApis) {
        $mineOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Mine ProductNative activation complete owner=dtmapi.mod.dtmapi.minemod hooks=3 schedulerMode=session-derived' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineOfficialJsonOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise NewContentMineOfficialJson OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineOfficialTechTreeUiOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Mine official tech tree UI evidence OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineProductionOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise NewContentMineProduction OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineApisOk = $mineOk -and $newContentMineOfficialJsonOk -and $newContentMineOfficialTechTreeUiOk -and $newContentMineProductionOk
    }
    if ($saveLoadedOk -and $AutoOpenAnimalPanel) {
        $animalViewerUiOk = if ($qaG4AnimalObservationEnabled) {
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.AnimalViewerRendering = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        else {
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Animal viewer UI evidence OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
    }
    if ($saveLoadedOk -and $AutoPressOneActionMenuKey) {
        $oneActionMenuVirtualKey = Resolve-DolocTownVirtualKey -Key 'F11'
        $oneActionMenuOpenSent = $null -ne $oneActionMenuVirtualKey -and (Send-DolocTownKey -VirtualKey $oneActionMenuVirtualKey -Name 'OneActionCompleteF11' -HoldMilliseconds 80 -InputLogPath $logPath)
        $oneActionMenuOpenEvidence = $script:lastDolocTownInputEvidence
        $oneActionMenuOpened = $oneActionMenuOpenSent -and (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'OneActionComplete hotkey OpenConfig OK key=F11' -TimeoutSeconds 20 -AbortOnFatalInstanceWindow)
        $oneActionMenuCloseSent = $false
        $oneActionMenuCloseEvidence = $null
        if ($oneActionMenuOpened) {
            Start-Sleep -Milliseconds 500
            $oneActionMenuCloseSent = Send-DolocTownKey -VirtualKey 0x1B -Name 'OneActionCompleteEscape' -HoldMilliseconds 80 -InputLogPath $logPath
            $oneActionMenuCloseEvidence = $script:lastDolocTownInputEvidence
        }
        $oneActionMenuKeyOk = $oneActionMenuOpened -and
            [bool]$oneActionMenuOpenEvidence.ForegroundMatchedAtSend -and [bool]$oneActionMenuOpenEvidence.SendInputSucceeded -and -not [bool]$oneActionMenuOpenEvidence.PostMessageFallbackUsed
        $oneActionMenuInputEvidence = [ordered]@{
            Requested = $true
            Opened = [bool]$oneActionMenuOpened
            ClosedWithEscape = [bool]$oneActionMenuCloseSent
            Passed = [bool]$oneActionMenuKeyOk
            Open = $oneActionMenuOpenEvidence
            Close = $oneActionMenuCloseEvidence
        }
        Write-SmokeJsonObject -Path (Join-Path $evidence 'one-action-menu-input.json') -Value $oneActionMenuInputEvidence
    }
    if ($saveLoadedOk -and $AutoExerciseExperimentalHooks) {
        $experimentalHooksOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.ExperimentalHookExercise = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedTool) {
        $actionSpeedToolOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedTool OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedConfigApply) {
        $actionSpeedConfigApplyOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedConfigApply OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedInteraction) {
        $actionSpeedInteractionOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedInteraction OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        if ($actionSpeedInteractionOk) {
            $actionSpeedDiagnosticsOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.DiagnosticsSnapshot = verified. scenario=ActionSpeed' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            if ($actionSpeedDiagnosticsOk) {
                $diagnosticsReportExportPassedScenarios += 'ActionSpeed'
            }
        }
        else {
            $diagnosticsReportExportOk = $false
        }
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionResourceHit) {
        $oneActionResourceHitOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise OneActionResourceHit OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $oneActionPartialEnergyOk = [bool](Select-String -Path $logPath -Pattern 'Smoke exercise OneActionPartialEnergy OK' | Select-Object -Last 1)
        $oneActionConfigReloadOk = [bool](Select-String -Path $logPath -Pattern 'Smoke exercise OneActionConfigReload OK' | Select-Object -Last 1)
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionWrongTool) {
        $oneActionWrongToolOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise OneActionWrongTool OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionFuelFeed) {
        $oneActionFuelFeedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise OneActionFuelFeed OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionVegetation) {
        $oneActionVegetationOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise OneActionVegetation OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseSaveLoadCycle) {
        $saveLoadCycleOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise SaveLoadCycle OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $saveLoadCycleSummaryMatch = Select-String -Path $logPath -Pattern 'Smoke exercise SaveLoadCycle OK' -ErrorAction SilentlyContinue | Select-Object -Last 1
        if ($saveLoadCycleSummaryMatch) {
            $saveLoadCycleSummary = $saveLoadCycleSummaryMatch.Line
        }
    }
    if ($saveLoadedOk -and $AutoExerciseSaveLoadCyclePendingPressure) {
        $saveLoadCyclePendingPressureOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise SaveLoadCyclePendingPressure OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $saveLoadCyclePendingPressureSummaryMatch = Select-String -Path $logPath -Pattern 'Smoke exercise SaveLoadCyclePendingPressure OK' -ErrorAction SilentlyContinue | Select-Object -Last 1
        if ($saveLoadCyclePendingPressureSummaryMatch) {
            $saveLoadCyclePendingPressureSummary = $saveLoadCyclePendingPressureSummaryMatch.Line
        }
    }
    if ($saveLoadedOk -and $AutoExerciseAudioReplacement) {
        $audioReadyOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'AudioReplacement local WAV ready owner=Yuuka.DTMAPI.ManboCardboardAudio' -TimeoutSeconds 20 -AbortOnFatalInstanceWindow
        if ($audioReadyOk) {
            Start-Sleep -Seconds 2
            $audioReplacementPattern = 'AudioReplacement event owner=Yuuka.DTMAPI.ManboCardboardAudio replacement=manbo-paper-box event=PLAY_RESOURCE_PAPER_BOX played=True suppressed=True'
            $audioPaperBoxPattern = 'AudioReplacement paper-box OnInteract owner=DungeonResourceModelPaperBox event=PLAY_RESOURCE_PAPER_BOX'
            for ($audioAttempt = 1; $audioAttempt -le 6 -and -not $audioReplacementOk; $audioAttempt++) {
                $audioLogOffset = 0
                if (Test-Path $logPath) {
                    $audioLogOffset = (Get-Item -LiteralPath $logPath).Length
                }
                $sentAudioReplacementInteract = Send-DolocTownNamedKey -Key 'E'
                "SentExternalAudioReplacementInteractAttempt${audioAttempt}=$(Get-Date -Format o);key=E;ok=$sentAudioReplacementInteract;logOffset=$audioLogOffset" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                if ($sentAudioReplacementInteract) {
                    $audioPaperBoxInteractOk = Wait-ForLogLineAfterOffset -LogPath $logPath -Pattern $audioPaperBoxPattern -Offset $audioLogOffset -TimeoutSeconds 5 -AbortOnFatalInstanceWindow
                    $audioReplacementEventOk = Wait-ForLogLineAfterOffset -LogPath $logPath -Pattern $audioReplacementPattern -Offset $audioLogOffset -TimeoutSeconds 5 -AbortOnFatalInstanceWindow
                    "AudioReplacementInteractAttempt${audioAttempt}Evidence=$(Get-Date -Format o);paperBoxOnInteract=$audioPaperBoxInteractOk;replacementEvent=$audioReplacementEventOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                    $audioReplacementOk = $audioPaperBoxInteractOk -and $audioReplacementEventOk
                }
                if (-not $audioReplacementOk) {
                    Start-Sleep -Milliseconds 700
                }
            }
            if (-not $audioReplacementOk) {
                $audioPaperBoxInteractOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern $audioPaperBoxPattern -TimeoutSeconds 2 -AbortOnFatalInstanceWindow
                $audioReplacementEventOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern $audioReplacementPattern -TimeoutSeconds 2 -AbortOnFatalInstanceWindow
                $audioReplacementOk = $audioPaperBoxInteractOk -and $audioReplacementEventOk
                "AudioReplacementRunLogEvidence=$(Get-Date -Format o);paperBoxOnInteract=$audioPaperBoxInteractOk;replacementEvent=$audioReplacementEventOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
            }
            $sentAudioReplacementInteract = [bool]$audioReplacementOk
        }
        else {
            $audioReplacementOk = $false
            "AudioReplacementReadyTimeout=$(Get-Date -Format o)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        }
        if ($qaG4AudioObservationEnabled) {
            $qaG4AudioObservationOk = $audioReplacementOk -and
                (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.AudioReplacement = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
            $audioReplacementOk = $audioReplacementOk -and $qaG4AudioObservationOk
        }
    }
    if ($saveLoadedOk -and $AutoExerciseHatchAnimalVoice) {
        $hatchVoiceReadyOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'AudioReplacement local WAV ready owner=DTMAPI.HatchAssets' -TimeoutSeconds 30 -AbortOnFatalInstanceWindow
        if ($hatchVoiceReadyOk) {
            $hatchVoiceSmokeOk = if ($qaG4HatchVoiceEnabled) {
                Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.HatchAnimalVoice = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            }
            else {
                Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise HatchAnimalVoice OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            }
            $hatchVoiceChildOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'AudioReplacement event owner=DTMAPI.HatchAssets replacement=hatch-pet-child event=PLAY_ANIMAL_PET_CHICKEN_CHILD played=True suppressed=True' -TimeoutSeconds 20 -AbortOnFatalInstanceWindow
            $hatchVoiceAdultOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'AudioReplacement event owner=DTMAPI.HatchAssets replacement=hatch-pet-adult event=PLAY_ANIMAL_PET_CHICKEN played=True suppressed=True' -TimeoutSeconds 20 -AbortOnFatalInstanceWindow
            $hatchAnimalVoiceOk = $hatchVoiceSmokeOk -and $hatchVoiceChildOk -and $hatchVoiceAdultOk
            "HatchAnimalVoiceEvidence=$(Get-Date -Format o);ready=$hatchVoiceReadyOk;smoke=$hatchVoiceSmokeOk;child=$hatchVoiceChildOk;adult=$hatchVoiceAdultOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        }
        else {
            $hatchAnimalVoiceOk = $false
            "HatchAnimalVoiceReadyTimeout=$(Get-Date -Format o)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        }
    }
    if ($saveLoadedOk -and $autoFishingMovementRequested) {
        $autoFishingInputLogOk = if ($autoFishingPerformanceBaseline -and $AutoFishingPerformanceProfile -eq 'EnabledNoRod') { $true } else { Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise AutoFishingHotkey OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow }
        $autoFishingHotkeyOk = $autoFishingInputLogOk
        $autoFishingMovementGateReady = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke exercise AutoFishingMovementCancel awaiting native input' -TimeoutSeconds $TimeoutSeconds
        $autoFishingMovementAlreadyComplete = [bool](Select-String -Path $logPath -Pattern 'Smoke exercise AutoFishingMovementCancel OK' -ErrorAction SilentlyContinue | Select-Object -Last 1)
        if ($autoFishingMovementGateReady -and -not $autoFishingMovementAlreadyComplete) {
            for ($movementAttempt = 1; $movementAttempt -le 3 -and -not $autoFishingMovementAlreadyComplete; $movementAttempt++) {
                $sentAutoFishingMovement = Send-DolocTownNamedKey -Key 'A' -HoldMilliseconds 800
                "SentExternalAutoFishingMovement=A $(Get-Date -Format o);attempt=$movementAttempt;ok=$sentAutoFishingMovement" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                if ($sentAutoFishingMovement) {
                    $autoFishingMovementAlreadyComplete = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke exercise AutoFishingMovementCancel OK' -TimeoutSeconds 5
                }
            }
        }
        $autoFishingMovementCancelOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke exercise AutoFishingMovementCancel OK' -TimeoutSeconds $TimeoutSeconds
    }
    elseif ($saveLoadedOk -and $AutoPressAutoFishingHotkey) {
        $autoFishingTogglePattern = 'AutoFishing automation enabled reason=hotkey ' + $autoFishingToggleKeyEffective
        for ($attempt = 1; $attempt -le 3 -and -not $autoFishingInputLogOk; $attempt++) {
            $autoFishingLogOffset = 0
            if (Test-Path $logPath) {
                $autoFishingLogOffset = (Get-Item -LiteralPath $logPath).Length
            }
            $sentAutoFishingToggle = Send-DolocTownNamedKey -Key $autoFishingToggleKeyEffective
            if ($sentAutoFishingToggle) {
                "SentExternalAutoFishingToggleAttempt$attempt=$autoFishingToggleKeyEffective $(Get-Date -Format o);logOffset=$autoFishingLogOffset" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                $autoFishingInputLogOk = Wait-ForLogLineAfterOffset -LogPath $logPath -Pattern $autoFishingTogglePattern -Offset $autoFishingLogOffset -TimeoutSeconds 8 -AbortOnFatalInstanceWindow
            }
            else {
                "SentExternalAutoFishingToggleAttempt${attempt}Failed=$autoFishingToggleKeyEffective $(Get-Date -Format o)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
            }
        }
        if ($autoFishingInputLogOk) {
            $autoFishingHotkeyOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern ('AutoFishing automation enabled reason=hotkey ' + $autoFishingToggleKeyEffective) -TimeoutSeconds 10
        }
    }
    if ($saveLoadedOk -and $AutoExerciseAutoFishingPhase) {
        if (-not $AutoPressAutoFishingHotkey -or $autoFishingInputLogOk) {
            if ($AutoFishingPerformance) {
                if ($AutoFishingPerformanceProfile -eq 'InactiveNoConsumer') {
                    $autoFishingHotkeyOk = $true
                }
                else {
                    $autoFishingHotkeyOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern ('AutoFishing automation enabled reason=hotkey ' + $autoFishingToggleKeyEffective) -TimeoutSeconds $TimeoutSeconds
                }
                $autoFishingPerformanceTerminalStatus = Wait-ForAutoFishingPerformanceTerminalStatus -LogPath $logPath -TimeoutSeconds $TimeoutSeconds
                $autoFishingPhaseOk = $autoFishingPerformanceTerminalStatus -eq 'verified'
                $autoFishingReportExportOk = $autoFishingPhaseOk
            }
            else {
                $autoFishingHotkeyOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern ('AutoFishing automation enabled reason=hotkey ' + $autoFishingToggleKeyEffective) -TimeoutSeconds $TimeoutSeconds
                $autoFishingPhaseOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke exercise AutoFishingLoop OK' -TimeoutSeconds $TimeoutSeconds
            }
            if ($AutoFishingMonoGate) {
                $autoFishingMonoGateOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke.AutoFishingMonoGate = verified' -TimeoutSeconds $TimeoutSeconds
            }
            if ($autoFishingScenarioRequestsInstantBite) {
                $autoFishingInstantBiteOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke.AutoFishingInstantBite = verified' -TimeoutSeconds $TimeoutSeconds
            }
            if ($autoFishingScenarioRequestsSkip) {
                $autoFishingMiniGameSkipOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke.AutoFishingMiniGameSkip = verified' -TimeoutSeconds $TimeoutSeconds
            }
            if ($autoFishingScenarioRequestsComplete) {
                $autoFishingMiniGameCompleteOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke.AutoFishingMiniGameComplete = verified' -TimeoutSeconds $TimeoutSeconds
            }
            if ($autoFishingScenarioRequestsAnimationSpeed) {
                $autoFishingAnimationSpeedOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke.AutoFishingAnimationSpeed = verified' -TimeoutSeconds $TimeoutSeconds
            }
            if ($autoFishingScenarioRequestsCastCharge) {
                $autoFishingCastChargePattern = 'Smoke.AutoFishingCastCharge = verified. owner=Yuuka.DTMAPI.AutoFishing, behavior=ReadyChargeTarget, source=FishingAnimationController typed Ready timer, target=' + ([double]$AutoFishingCastChargeRatio).ToString('0.###', [System.Globalization.CultureInfo]::InvariantCulture) + '.'
                $autoFishingCastChargeOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern $autoFishingCastChargePattern -TimeoutSeconds $TimeoutSeconds
            }
            if ($AutoFishingSoakLoops -gt 0) {
                $autoFishingSoakOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke.AutoFishingSoak = verified' -TimeoutSeconds $TimeoutSeconds
            }
            if (-not $AutoFishingMonoGate -and -not $AutoFishingPerformance) {
                $autoFishingReportExportOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke.DiagnosticsSnapshot = verified. scenario=AutoFishing' -TimeoutSeconds $TimeoutSeconds
                if ($autoFishingReportExportOk) {
                    $diagnosticsReportExportPassedScenarios += 'AutoFishing'
                }
            }
        }
        else {
            $autoFishingHotkeyOk = $false
            $autoFishingMovementCancelOk = $true
            $autoFishingPhaseOk = $false
            $autoFishingMonoGateOk = $false
            $autoFishingInstantBiteOk = $false
            $autoFishingMiniGameSkipOk = $false
            $autoFishingMiniGameCompleteOk = $false
            $autoFishingAnimationSpeedOk = $false
            $autoFishingCastChargeOk = $false
            $autoFishingLifecycleOk = $false
            $autoFishingSoakOk = $false
            $autoFishingReportExportOk = $false
            $diagnosticsReportExportOk = $false
        }
    }
    if ($saveLoadedOk -and $AutoExercisePauseMenuLayout) {
        $pauseMenuLayoutOk = if ($qaG4PauseMenuLayoutEnabled) {
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.PauseMenuLayout = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        else {
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise PauseMenuLayout OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
    }
    if ($saveLoadedOk -and $AutoExerciseInstantSave) {
        $instantSaveOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise InstantSave OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $debugConsoleSaveAcceptanceRequested) {
        $debugConsoleSaveAcceptanceOk =
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
                -LogPath $logPath `
                -Pattern (
                    'Smoke exercise DebugConsoleSaveAcceptance OK phase=' +
                    $DebugConsoleSaveAcceptancePhase) `
                -TimeoutSeconds $TimeoutSeconds `
                -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $requiresDebugConsoleKeySmoke) {
        $debugConsoleActionDeadline = if ($AssertNoQaUiEvidence) { $noQaUiDeadline } else { [datetime]::MaxValue }
        try {
            if ($RequireExternalPlayerInputGate) {
                $readyPattern = if ($qaG4ExternalPlayerInputObservationEnabled) {
                    'Smoke external player input READY token=' + $externalPlayerInputHandshakeToken + '.'
                }
                else {
                    'Smoke external player input READY token=' + $externalPlayerInputHandshakeToken + '.'
                }
                $externalPlayerInputReadyOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern $readyPattern -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
                "ExternalPlayerInputReady=$(Get-Date -Format o);token=$externalPlayerInputHandshakeToken;ok=$externalPlayerInputReadyOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
            }

            $debugConsoleInGameHotkeyOk = if ($RequireExternalPlayerInputGate -or $qaG4DebugConsoleEnabled -or $AssertNoQaUiEvidence) {
                $false
            }
            else {
                Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise DebugConsoleHotkey OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            }
            if ($debugConsoleInGameHotkeyOk) {
                $debugConsoleOpenY1Ok = $true
                $debugConsoleCloseEscapeOk = $true
                $debugConsoleOpenY2Ok = $true
                $debugConsoleCloseYOk = $true
                $debugConsoleTenYShortTapsOk = $true
                $debugConsoleHoldYNoFlickerOk = $true
                "InGameDebugConsoleHotkeySmoke=$(Get-Date -Format o);ok=True" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                if ($AutoExerciseDebugConsoleMouseGive) {
                    $debugConsoleMouseGiveOk = Invoke-DebugConsoleMouseGiveSmoke
                }
            }
            elseif (-not $RequireExternalPlayerInputGate -or $externalPlayerInputReadyOk) {
                $externalReason = if ($RequireExternalPlayerInputGate) { 'required-external-player-input-gate' } elseif ($AssertNoQaUiEvidence) { 'no-qa-ui-evidence-gate' } elseif ($qaG4DebugConsoleEnabled) { 'qa-observed-real-key-matrix' } else { 'fallback-external-key-injection' }
                "InGameDebugConsoleHotkeySmoke=$(Get-Date -Format o);ok=False;route=$externalReason;holdMilliseconds=$(if ($RequireExternalPlayerInputGate -or $AssertNoQaUiEvidence) { $ExternalPlayerInputHoldMilliseconds } else { 260 })" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                $debugConsoleInitialDelayOk = Start-SmokeCappedSleep -Deadline $debugConsoleActionDeadline -Milliseconds 1000
                $debugConsoleOpenY1Ok = $debugConsoleInitialDelayOk -and (Invoke-DebugConsoleSmokeKey -Label 'Y1' -VirtualKey 0x59 -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -MinimumCount 1 -Attempts 1 -WaitSeconds 5 -Deadline $debugConsoleActionDeadline)
                if ($debugConsoleOpenY1Ok) {
                    $y1Attempt = @($externalPlayerInputAttempts.ToArray() | Where-Object { [string]$_.Label -eq 'Y1' } | Select-Object -Last 1)
                    $debugConsoleOpenY1Ok = $y1Attempt.Count -eq 1 -and
                        (Wait-ForLogLineAfterOffsetUntilDeadline -LogPath $logPath -Offset ([int64]$y1Attempt[0].LogOffset) -Pattern 'Debug console Canvas visible as Y-key console.' -Deadline (Get-SmokeCappedDeadline -Deadline $debugConsoleActionDeadline -MaximumMilliseconds 5000) -AbortOnFatalInstanceWindow)
                }
                if ($debugConsoleOpenY1Ok) {
                    if ($AutoExerciseDebugConsoleMouseGive) {
                        $debugConsoleMouseGiveOk = Invoke-DebugConsoleMouseGiveSmoke
                    }
                    $debugConsoleCloseEscapeOk = Invoke-DebugConsoleSmokeKey -Label 'Escape' -VirtualKey 0x1B -Pattern 'Debug console closed reason=Escape owner=DTMAPI.DebugConsoleMod.' -MinimumCount 1 -Attempts 1 -WaitSeconds 5 -Deadline $debugConsoleActionDeadline
                    if (($RequireExternalPlayerInputGate -or $AssertNoQaUiEvidence) -and $debugConsoleCloseEscapeOk) {
                        $escapeDrainWithinDeadline = Start-SmokeCappedSleep -Deadline $debugConsoleActionDeadline -Milliseconds 750
                        if (-not $escapeDrainWithinDeadline) {
                            $debugConsoleCloseEscapeOk = $false
                        }
                        else {
                            $escapeAttempt = @($externalPlayerInputAttempts.ToArray() | Where-Object { [string]$_.Label -eq 'Escape' } | Select-Object -Last 1)
                            if ($escapeAttempt.Count -eq 1) {
                                $escapeTail = Get-LogTextAfterOffset -LogPath $logPath -Offset ([int64]$escapeAttempt[0].LogOffset)
                                $externalPlayerInputNativeMenuLeakDetected = $escapeTail.IndexOf('MainMenuPanel.OnStartShow', [System.StringComparison]::Ordinal) -ge 0 -or
                                    $escapeTail.IndexOf('runtimeContext=MainMenuUiState', [System.StringComparison]::Ordinal) -ge 0
                                if ($externalPlayerInputNativeMenuLeakDetected) {
                                    $debugConsoleCloseEscapeOk = $false
                                    "ExternalPlayerInputModalLeak=$(Get-Date -Format o);detected=True;reason=Escape reached native MainMenuUiState" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                                }
                            }
                        }
                    }
                }
                if ($debugConsoleCloseEscapeOk) {
                    $debugConsoleOpenY2Ok = Invoke-DebugConsoleSmokeKey -Label 'Y2' -VirtualKey 0x59 -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -MinimumCount 2 -Attempts 1 -WaitSeconds 5 -Deadline $debugConsoleActionDeadline
                }
                if ($debugConsoleOpenY2Ok) {
                    $debugConsoleCloseYOk = Invoke-DebugConsoleSmokeKey -Label 'Y3' -VirtualKey 0x59 -Pattern 'Debug console closed reason=hotkey Y owner=DTMAPI.DebugConsoleMod.' -MinimumCount 1 -Attempts 1 -WaitSeconds 5 -Deadline $debugConsoleActionDeadline
                }
                if ($debugConsoleCloseYOk) {
                    $debugConsoleTenYShortTapsOk = $true
                    for ($tap = 1; $tap -le 10; $tap++) {
                        if (-not (Test-SmokeDeadlineHasBudget -Deadline $debugConsoleActionDeadline)) {
                            $debugConsoleTenYShortTapsOk = $false
                            break
                        }
                        if (($tap % 2) -eq 1) {
                            $expectedOpenCount = 2 + [int](($tap + 1) / 2)
                            $ok = Invoke-DebugConsoleSmokeKey -Label "YShort$tap" -VirtualKey 0x59 -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -MinimumCount $expectedOpenCount -Attempts 1 -WaitSeconds 4 -Deadline $debugConsoleActionDeadline
                        }
                        else {
                            $expectedCloseCount = 1 + [int]($tap / 2)
                            $ok = Invoke-DebugConsoleSmokeKey -Label "YShort$tap" -VirtualKey 0x59 -Pattern 'Debug console closed reason=hotkey Y owner=DTMAPI.DebugConsoleMod.' -MinimumCount $expectedCloseCount -Attempts 1 -WaitSeconds 4 -Deadline $debugConsoleActionDeadline
                        }

                        if (-not $ok) {
                            $debugConsoleTenYShortTapsOk = $false
                            break
                        }
                    }

                    if ($debugConsoleTenYShortTapsOk) {
                        $openCountBeforeHold = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -ErrorAction SilentlyContinue).Count
                        $closeYCountBeforeHold = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console closed reason=hotkey Y owner=DTMAPI.DebugConsoleMod.' -ErrorAction SilentlyContinue).Count
                        $sentHoldY = Invoke-DebugConsoleSmokeKey -Label 'YHold' -VirtualKey 0x59 -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -MinimumCount ($openCountBeforeHold + 1) -Attempts 1 -WaitSeconds 5 -HoldMilliseconds 1800 -Deadline $debugConsoleActionDeadline
                        "SentExternalDebugConsoleYHold=$(Get-Date -Format o);ok=$sentHoldY;openBefore=$openCountBeforeHold;closeYBefore=$closeYCountBeforeHold" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                        if ($sentHoldY) {
                            $holdObservationWithinDeadline = Start-SmokeCappedSleep -Deadline $debugConsoleActionDeadline -Milliseconds 2000
                            $openCountAfterHold = if ($holdObservationWithinDeadline) { @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -ErrorAction SilentlyContinue).Count } else { -1 }
                            $closeYCountAfterHold = if ($holdObservationWithinDeadline) { @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console closed reason=hotkey Y owner=DTMAPI.DebugConsoleMod.' -ErrorAction SilentlyContinue).Count } else { -1 }
                            $debugConsoleHoldYNoFlickerOk = $holdObservationWithinDeadline -and ($openCountAfterHold -eq ($openCountBeforeHold + 1)) -and ($closeYCountAfterHold -eq $closeYCountBeforeHold)
                            "DebugConsoleYHoldResult=$(Get-Date -Format o);openAfter=$openCountAfterHold;closeYAfter=$closeYCountAfterHold;ok=$debugConsoleHoldYNoFlickerOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                            if ($qaG4DebugConsoleEnabled -and $debugConsoleHoldYNoFlickerOk) {
                                $holdAttempt = @($externalPlayerInputAttempts.ToArray() | Where-Object { [string]$_.Label -eq 'YHold' } | Select-Object -Last 1)
                                $qaG4DebugEvidenceReadyOk = $holdAttempt.Count -eq 1 -and
                                    (Wait-ForLogLineAfterOffset -LogPath $logPath -Offset ([int64]$holdAttempt[0].LogOffset) -Pattern 'EVIDENCE_CAPTURED_WAITING_ESCAPE' -TimeoutSeconds 10 -AbortOnFatalInstanceWindow)
                                "DebugConsoleQaEvidenceReady=$(Get-Date -Format o);ok=$qaG4DebugEvidenceReadyOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                            }
                            if (($RequireExternalPlayerInputGate -or $qaG4DebugConsoleEnabled -or $AssertNoQaUiEvidence) -and
                                $debugConsoleHoldYNoFlickerOk -and $qaG4DebugEvidenceReadyOk -and
                                -not [bool]$Issue011Acceptance) {
                                $escapeCountBeforeCleanup = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console closed reason=Escape owner=DTMAPI.DebugConsoleMod.' -ErrorAction SilentlyContinue).Count
                                $debugConsolePostHoldCloseOk = Invoke-DebugConsoleSmokeKey -Label 'YHoldCleanupEscape' -VirtualKey 0x1B -Pattern 'Debug console closed reason=Escape owner=DTMAPI.DebugConsoleMod.' -MinimumCount ($escapeCountBeforeCleanup + 1) -Attempts 1 -WaitSeconds 5 -Deadline $debugConsoleActionDeadline
                            }
                        }
                        else {
                            $debugConsoleHoldYNoFlickerOk = $false
                            "DebugConsoleYHoldResult=$(Get-Date -Format o);ok=False" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                        }
                    }
                }
            }
            if ($qaG4DebugConsoleEnabled) {
                $qaG4DebugObservationOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.DebugConsoleHotkey = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
                if (-not $qaG4DebugObservationOk) {
                    $debugConsoleHoldYNoFlickerOk = $false
                }
            }
        }
        finally {
            if ($RequireExternalPlayerInputGate) {
                $externalPlayerInputTerminalPassed = $externalPlayerInputReadyOk -and $debugConsoleOpenY1Ok -and $debugConsoleCloseEscapeOk -and $debugConsoleOpenY2Ok -and $debugConsoleCloseYOk -and $debugConsoleTenYShortTapsOk -and $debugConsoleHoldYNoFlickerOk -and $debugConsolePostHoldCloseOk
                $externalPlayerInputCompletionTempPath = $externalPlayerInputCompletionMarkerPath + '.tmp.' + [Guid]::NewGuid().ToString('N')
                try {
                    if (Test-Path -LiteralPath $externalPlayerInputCompletionMarkerPath) {
                        throw 'The external player-input terminal marker already exists.'
                    }
                    @(
                        "Token=$externalPlayerInputHandshakeToken",
                        'Terminal=True',
                        "Passed=$externalPlayerInputTerminalPassed",
                        "CompletedAt=$((Get-Date).ToString('o'))"
                    ) | Set-Content -LiteralPath $externalPlayerInputCompletionTempPath
                    Move-Item -LiteralPath $externalPlayerInputCompletionTempPath -Destination $externalPlayerInputCompletionMarkerPath -ErrorAction Stop
                    $externalPlayerInputMarkerWrittenOk = $true
                }
                catch {
                    $externalPlayerInputMarkerWrittenOk = $false
                    "ExternalPlayerInputMarkerWrite=$(Get-Date -Format o);ok=False;error=$([string]$_.Exception.Message)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                }
                finally {
                    if (Test-Path -LiteralPath $externalPlayerInputCompletionTempPath) {
                        Remove-Item -LiteralPath $externalPlayerInputCompletionTempPath -Force
                    }
                }
            }
        }
        if ($RequireExternalPlayerInputGate -and $externalPlayerInputMarkerWrittenOk) {
            $cleanupPattern = if ($qaG4ExternalPlayerInputObservationEnabled) {
                'Smoke exercise ExternalPlayerInputCleanup OK token=' + $externalPlayerInputHandshakeToken + '; continuousHomePage=true; owner=qa-observation.'
            }
            else {
                'Smoke exercise ExternalPlayerInputCleanup OK token=' + $externalPlayerInputHandshakeToken + ';'
            }
            $externalPlayerInputCleanupOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern $cleanupPattern -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
    }
    # DebugConsole QA owns the in-save real-input sequence. Its optional
    # participant requests ReturnHome only after that sequence reaches its
    # terminal state, so title/Loader cleanup must be awaited afterwards.
    if ($saveLoadedOk -and $AutoExerciseTitleButtonLifecycle) {
        if ($moreEquipmentSlotsTransitionRequested) {
            $titleLifecycleWaitSeconds =
                Get-SmokeRemainingBudgetSeconds `
                    -Deadline $moreEquipmentSlotsTransitionDeadline
            $titleLifecycleOk =
                $moreEquipmentSlotsTransitionOk -and
                $titleLifecycleWaitSeconds -gt 0 -and
                (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
                    -LogPath $logPath `
                    -Pattern 'Smoke exercise TitleButtonLifecycle OK' `
                    -TimeoutSeconds $titleLifecycleWaitSeconds `
                    -AbortOnFatalInstanceWindow)
        }
        else {
            $titleLifecycleOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise TitleButtonLifecycle OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
    }
    if ($saveLoadedOk -and $AssertAdvancedProductOwnerDeactivation) {
        $advancedProductOwnerDeactivationOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise AdvancedProductOwnerDeactivation OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AssertNoQaUiEvidence) {
        if ($Issue011Acceptance) {
            Write-Host 'ISSUE-011 ACCEPTANCE READY FOR INTERACTION: the foreground Y hold already opened the console. Enter and commit a non-empty item search, hover one filtered item until its tooltip appears, and give at least one item. If a tooltip appeared before the search was committed, close and reopen the console once so its retained non-empty search lifecycle is logged. Leave the console open so the runner can close it through the same foreground SendInput path.' -ForegroundColor Yellow
            $issue011TooltipObserved = $false
            $issue011SearchObserved = $false
            $issue011GiveObserved = $false
            while ((Test-SmokeDeadlineHasBudget -Deadline $noQaUiDeadline) -and
                -not ($issue011TooltipObserved -and $issue011SearchObserved -and $issue011GiveObserved)) {
                if (Test-Path -LiteralPath $logPath -PathType Leaf) {
                    $issue011TooltipObserved = [bool](Select-String `
                        -LiteralPath $logPath `
                        -Pattern 'DebugConsole status UI\.DebugConsoleItemTooltip=visible ' `
                        -ErrorAction SilentlyContinue |
                        Select-Object -Last 1)
                    $issue011TooltipSearchObserved = [bool](Select-String `
                        -LiteralPath $logPath `
                        -Pattern 'DebugConsole status UI\.DebugConsoleItemTooltip=visible .*searchText=(?!<empty>)[^\.\r\n]+\.' `
                        -ErrorAction SilentlyContinue |
                        Select-Object -Last 1)
                    $issue011LifecycleSearchObserved = [bool](Select-String `
                        -LiteralPath $logPath `
                        -Pattern 'Debug console open lifecycle state searchText=(?!<empty>).*? category=' `
                        -ErrorAction SilentlyContinue |
                        Select-Object -Last 1)
                    $issue011SearchObserved = $issue011TooltipSearchObserved -or $issue011LifecycleSearchObserved
                }
                if (Test-Path -LiteralPath $issue011LastGiveStatePath -PathType Leaf) {
                    try {
                        $issue011CurrentGiveText = (Get-Content -Raw -LiteralPath $issue011LastGiveStatePath -ErrorAction Stop).Trim()
                        $issue011CurrentGiveHash = (Get-SmokeFileSha256 -Path $issue011LastGiveStatePath).ToLowerInvariant()
                        $issue011CurrentGiveMatch = [regex]::Match($issue011CurrentGiveText, '^(?<Time>\S+)\s+status=verified\s+source=\S+\s+item=\S+\s+requested=(?<Requested>\d+)\s+given=(?<Given>\d+)\s+rightClick=(?:True|False)\s+failure=.*$')
                        $issue011CurrentGiveTime = [DateTimeOffset]::MinValue
                        $issue011GiveObserved = $issue011CurrentGiveMatch.Success -and
                            [DateTimeOffset]::TryParse($issue011CurrentGiveMatch.Groups['Time'].Value, [ref]$issue011CurrentGiveTime) -and
                            $issue011CurrentGiveTime.UtcDateTime -ge $smokeRunStartedAt.ToUniversalTime().AddSeconds(-5) -and
                            [int]$issue011CurrentGiveMatch.Groups['Requested'].Value -gt 0 -and
                            [int]$issue011CurrentGiveMatch.Groups['Given'].Value -gt 0 -and
                            (-not [bool]$issue011LastGiveBaseline.Existed -or
                                -not [string]::Equals([string]$issue011LastGiveBaseline.Sha256, $issue011CurrentGiveHash, [System.StringComparison]::OrdinalIgnoreCase))
                    }
                    catch {
                        $issue011GiveObserved = $false
                    }
                }
                if (-not ($issue011TooltipObserved -and $issue011SearchObserved -and $issue011GiveObserved)) {
                    if (Test-FatalInstanceWindow) { break }
                    if (-not (Start-SmokeCappedSleep -Deadline $noQaUiDeadline -Milliseconds 500)) { break }
                }
            }
            $issue011InteractiveEvidenceObserved = $issue011TooltipObserved -and $issue011SearchObserved -and $issue011GiveObserved
            "Issue011InteractiveEvidence=$(Get-Date -Format o);tooltip=$issue011TooltipObserved;search=$issue011SearchObserved;freshLastGive=$issue011GiveObserved;passed=$issue011InteractiveEvidenceObserved" |
                Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
            if ($issue011InteractiveEvidenceObserved -and $debugConsoleHoldYNoFlickerOk) {
                $issue011EscapeCountBeforeCleanup = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console closed reason=Escape owner=DTMAPI.DebugConsoleMod.' -ErrorAction SilentlyContinue).Count
                $debugConsolePostHoldCloseOk = Invoke-DebugConsoleSmokeKey `
                    -Label 'Issue011InteractiveCleanupEscape' `
                    -VirtualKey 0x1B `
                    -Pattern 'Debug console closed reason=Escape owner=DTMAPI.DebugConsoleMod.' `
                    -MinimumCount ($issue011EscapeCountBeforeCleanup + 1) `
                    -Attempts 1 `
                    -WaitSeconds 5 `
                    -Deadline $noQaUiDeadline
            }
            "Issue011InteractiveCleanup=$(Get-Date -Format o);ok=$debugConsolePostHoldCloseOk" |
                Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        }
        if ($AutoDriveNoQaAnimalViewer) {
            Write-Host 'NO-QA UI GATE: Y console input is complete. After the EquipmentSlots check, the explicit animal auto-drive will try foreground SendInput A,E; only if no viewer render appears will it try one more A,E, followed by one normalized second-row click.'
        }
        else {
            Write-Host 'NO-QA UI GATE: Y console input is complete. The runner will now open EquipmentSlots with one foreground SendInput B, perform a move-only hover sweep, and close it with Escape. Afterwards, use ordinary player controls in the tenth save to open a configured animal viewer and switch animals until two new progress-render receipts appear.'
        }

        $noQaEquipmentSlotsRegisterOk = [bool](Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'MoreEquipmentSlots API register success=True reason=SaveLoaded' -ErrorAction SilentlyContinue | Select-Object -First 1)
        $noQaEquipmentLogOffset = if (Test-Path -LiteralPath $logPath -PathType Leaf) { [int64](Get-Item -LiteralPath $logPath).Length } else { [int64]0 }
        $noQaEquipmentOpenSent = Send-DolocTownKey -VirtualKey 0x42 -Name 'NoQaEquipmentSlotsB' -HoldMilliseconds $ExternalPlayerInputHoldMilliseconds -InputLogPath $logPath -Deadline $noQaUiDeadline
        $noQaEquipmentOpenEvidence = $script:lastDolocTownInputEvidence
        $noQaEquipmentOpenProvenanceOk = $noQaEquipmentOpenSent -and [bool]$noQaEquipmentOpenEvidence.ForegroundMatchedAtSend -and
            [bool]$noQaEquipmentOpenEvidence.SendInputSucceeded -and [bool]$noQaEquipmentOpenEvidence.CompletedBeforeDeadline -and
            -not [bool]$noQaEquipmentOpenEvidence.PostMessageFallbackUsed
        $equipmentSlotsPlayerInputAttempts.Add([ordered]@{
            Label = 'NoQaEquipmentSlotsB'
            Sent = [bool]$noQaEquipmentOpenSent
            ForegroundMatched = [bool]$noQaEquipmentOpenEvidence.ForegroundMatchedAtSend
            SendInputSucceeded = [bool]$noQaEquipmentOpenEvidence.SendInputSucceeded
            CompletedBeforeDeadline = [bool]$noQaEquipmentOpenEvidence.CompletedBeforeDeadline
            PostMessageFallbackUsed = [bool]$noQaEquipmentOpenEvidence.PostMessageFallbackUsed
            LogOffset = $noQaEquipmentLogOffset
        }) | Out-Null

        if ($noQaEquipmentOpenProvenanceOk) {
            $noQaEquipmentSlotsRenderOk = Wait-ForLogLineAfterOffsetUntilDeadline `
                -LogPath $logPath `
                -Offset $noQaEquipmentLogOffset `
                -Pattern 'EquipmentSlots UI bind diagnostic' `
                -Deadline (Get-SmokeCappedDeadline -Deadline $noQaUiDeadline -MaximumMilliseconds 20000) `
                -AbortOnFatalInstanceWindow
        }
        $noQaEquipmentHoverReceipt = if ($noQaEquipmentSlotsRenderOk -and (Test-SmokeDeadlineHasBudget -Deadline $noQaUiDeadline)) {
            Invoke-DolocTownHoverSweep -LogPath $logPath -LogOffset $noQaEquipmentLogOffset -Pattern 'EquipmentSlots UI hover owner=' -Deadline $noQaUiDeadline
        }
        else {
            [PSCustomObject]@{ DolocTownPid = 0; ForegroundMatched = $false; MoveOnly = $true; MouseClickSent = $false; PointsVisited = 0; MatchedExpectedLog = $false }
        }
        $noQaEquipmentSlotsInteractionOk = [bool]$noQaEquipmentHoverReceipt.ForegroundMatched -and [bool]$noQaEquipmentHoverReceipt.MoveOnly -and
            -not [bool]$noQaEquipmentHoverReceipt.MouseClickSent -and [bool]$noQaEquipmentHoverReceipt.MatchedExpectedLog
        $equipmentSlotsPlayerInputAttempts.Add([ordered]@{
            Label = 'NoQaEquipmentSlotsMoveOnlyHoverSweep'
            ForegroundMatched = [bool]$noQaEquipmentHoverReceipt.ForegroundMatched
            MoveOnly = [bool]$noQaEquipmentHoverReceipt.MoveOnly
            MouseClickSent = [bool]$noQaEquipmentHoverReceipt.MouseClickSent
            PointsVisited = [int]$noQaEquipmentHoverReceipt.PointsVisited
            MatchedExpectedLog = [bool]$noQaEquipmentHoverReceipt.MatchedExpectedLog
            LogOffset = $noQaEquipmentLogOffset
        }) | Out-Null
        $noQaEquipmentCloseSent = Send-DolocTownKey -VirtualKey 0x1B -Name 'NoQaEquipmentSlotsEscape' -HoldMilliseconds $ExternalPlayerInputHoldMilliseconds -InputLogPath $logPath -Deadline $noQaUiDeadline
        $noQaEquipmentCloseEvidence = $script:lastDolocTownInputEvidence
        $noQaEquipmentCloseProvenanceOk = $noQaEquipmentCloseSent -and [bool]$noQaEquipmentCloseEvidence.ForegroundMatchedAtSend -and
            [bool]$noQaEquipmentCloseEvidence.SendInputSucceeded -and [bool]$noQaEquipmentCloseEvidence.CompletedBeforeDeadline -and
            -not [bool]$noQaEquipmentCloseEvidence.PostMessageFallbackUsed
        $equipmentSlotsPlayerInputAttempts.Add([ordered]@{
            Label = 'NoQaEquipmentSlotsEscape'
            Sent = [bool]$noQaEquipmentCloseSent
            ForegroundMatched = [bool]$noQaEquipmentCloseEvidence.ForegroundMatchedAtSend
            SendInputSucceeded = [bool]$noQaEquipmentCloseEvidence.SendInputSucceeded
            CompletedBeforeDeadline = [bool]$noQaEquipmentCloseEvidence.CompletedBeforeDeadline
            PostMessageFallbackUsed = [bool]$noQaEquipmentCloseEvidence.PostMessageFallbackUsed
            LogOffset = [int64]$noQaEquipmentCloseEvidence.LogOffsetAtSend
        }) | Out-Null
        $noQaEquipmentSlotsCloseInputOk = $noQaEquipmentCloseProvenanceOk

        $noQaAnimalLogOffset = if (Test-Path -LiteralPath $logPath -PathType Leaf) { [int64](Get-Item -LiteralPath $logPath).Length } else { [int64]0 }
        $noQaAnimalViewerReceiptPattern = 'AnimalHusbandryProgress product viewer active state=visible,.*receiptSequence=\d+, receipt=new-data,'
        $noQaAnimalViewerProductClosePattern = 'AnimalHusbandryProgress native panel closed; product-owned derived rows and clones are zero.'
        if ($AutoDriveNoQaAnimalViewer) {
            $noQaAnimalViewerAutoDriveProvenanceOk = $true
            for ($animalOpenAttempt = 1; $animalOpenAttempt -le 2 -and $noQaAnimalViewerRenderReceiptCount -lt 1 -and (Test-SmokeDeadlineHasBudget -Deadline $noQaUiDeadline); $animalOpenAttempt++) {
                foreach ($animalKeyStep in @(
                    [ordered]@{ Label = "NoQaAnimalApproachA$animalOpenAttempt"; VirtualKey = 0x41; PauseMilliseconds = 320 },
                    [ordered]@{ Label = "NoQaAnimalOpenE$animalOpenAttempt"; VirtualKey = 0x45; PauseMilliseconds = 500 }
                )) {
                    if (-not (Test-SmokeDeadlineHasBudget -Deadline $noQaUiDeadline)) {
                        $noQaAnimalViewerAutoDriveProvenanceOk = $false
                        break
                    }
                    $animalKeySent = Send-DolocTownKey -VirtualKey ([int]$animalKeyStep.VirtualKey) -Name ([string]$animalKeyStep.Label) -HoldMilliseconds 40 -InputLogPath $logPath -Deadline $noQaUiDeadline
                    $animalKeyEvidence = $script:lastDolocTownInputEvidence
                    $animalKeyProvenanceOk = $animalKeySent -and [bool]$animalKeyEvidence.ForegroundMatchedAtSend -and
                        [bool]$animalKeyEvidence.SendInputSucceeded -and [bool]$animalKeyEvidence.CompletedBeforeDeadline -and
                        -not [bool]$animalKeyEvidence.PostMessageFallbackUsed
                    $noQaAnimalViewerAutoDriveProvenanceOk = $noQaAnimalViewerAutoDriveProvenanceOk -and $animalKeyProvenanceOk
                    $animalViewerPlayerInputAttempts.Add([ordered]@{
                        Label = [string]$animalKeyStep.Label
                        Attempt = $animalOpenAttempt
                        InputKind = 'KeyboardSendInput'
                        Sent = [bool]$animalKeySent
                        ForegroundMatched = [bool]$animalKeyEvidence.ForegroundMatchedAtSend
                        SendInputSucceeded = [bool]$animalKeyEvidence.SendInputSucceeded
                        CompletedBeforeDeadline = [bool]$animalKeyEvidence.CompletedBeforeDeadline
                        PostMessageFallbackUsed = [bool]$animalKeyEvidence.PostMessageFallbackUsed
                        HoldMilliseconds = 40
                        LogOffset = [int64]$animalKeyEvidence.LogOffsetAtSend
                    }) | Out-Null
                    if (-not (Start-SmokeCappedSleep -Deadline $noQaUiDeadline -Milliseconds ([int]$animalKeyStep.PauseMilliseconds))) {
                        $noQaAnimalViewerAutoDriveProvenanceOk = $false
                        break
                    }
                }
                $animalOpenAttemptDeadline = (Get-Date).AddSeconds(3)
                if ($animalOpenAttemptDeadline -gt $noQaUiDeadline) { $animalOpenAttemptDeadline = $noQaUiDeadline }
                while ((Test-SmokeDeadlineHasBudget -Deadline $animalOpenAttemptDeadline) -and $noQaAnimalViewerRenderReceiptCount -lt 1) {
                    $animalTail = Get-LogTextAfterOffset -LogPath $logPath -Offset $noQaAnimalLogOffset
                    if (-not (Test-SmokeDeadlineHasBudget -Deadline $animalOpenAttemptDeadline)) {
                        break
                    }
                    $noQaAnimalViewerRenderReceiptCount = [regex]::Matches($animalTail, $noQaAnimalViewerReceiptPattern).Count
                    if ($noQaAnimalViewerRenderReceiptCount -lt 1) {
                        if (Test-FatalInstanceWindow) { break }
                        if (-not (Start-SmokeCappedSleep -Deadline $animalOpenAttemptDeadline -Milliseconds 250)) { break }
                    }
                }
            }

            $animalRowClickSent = $false
            $animalRowClickEvidence = [ordered]@{
                ForegroundMatchedAtSend = $false
                SendInputSucceeded = $false
                PostMessageFallbackUsed = $false
                SetCursorPosSucceeded = $false
                NormalizedX = 0.357
                NormalizedY = 0.427
                ClientX = -1
                ClientY = -1
                ClientWidth = 0
                ClientHeight = 0
                LogOffsetAtSend = [int64]0
                CompletedBeforeDeadline = $false
            }
            if ($noQaAnimalViewerRenderReceiptCount -ge 1 -and (Test-SmokeDeadlineHasBudget -Deadline $noQaUiDeadline)) {
                $animalRowClickSent = Send-DolocTownNormalizedMouseClick -Button Left -NormalizedX 0.357 -NormalizedY 0.427 -InputLogPath $logPath -Deadline $noQaUiDeadline
                $animalRowClickEvidence = $script:lastDolocTownMouseInputEvidence
            }
            $animalRowClickProvenanceOk = $animalRowClickSent -and [bool]$animalRowClickEvidence.ForegroundMatchedAtSend -and
                [bool]$animalRowClickEvidence.SendInputSucceeded -and [bool]$animalRowClickEvidence.CompletedBeforeDeadline -and
                [bool]$animalRowClickEvidence.SetCursorPosSucceeded -and
                -not [bool]$animalRowClickEvidence.PostMessageFallbackUsed
            $noQaAnimalViewerAutoDriveProvenanceOk = $noQaAnimalViewerAutoDriveProvenanceOk -and $animalRowClickProvenanceOk
            $animalViewerPlayerInputAttempts.Add([ordered]@{
                Label = 'NoQaAnimalSelectSecondRow'
                InputKind = 'NormalizedMouseSendInput'
                Sent = [bool]$animalRowClickSent
                ForegroundMatched = [bool]$animalRowClickEvidence.ForegroundMatchedAtSend
                SendInputSucceeded = [bool]$animalRowClickEvidence.SendInputSucceeded
                CompletedBeforeDeadline = [bool]$animalRowClickEvidence.CompletedBeforeDeadline
                PostMessageFallbackUsed = [bool]$animalRowClickEvidence.PostMessageFallbackUsed
                SetCursorPosSucceeded = [bool]$animalRowClickEvidence.SetCursorPosSucceeded
                NormalizedX = [double]$animalRowClickEvidence.NormalizedX
                NormalizedY = [double]$animalRowClickEvidence.NormalizedY
                ClientX = [int]$animalRowClickEvidence.ClientX
                ClientY = [int]$animalRowClickEvidence.ClientY
                ClientWidth = [int]$animalRowClickEvidence.ClientWidth
                ClientHeight = [int]$animalRowClickEvidence.ClientHeight
                LogOffset = [int64]$animalRowClickEvidence.LogOffsetAtSend
            }) | Out-Null
            Write-Host ("NO-QA UI GATE ANIMAL AUTO-DRIVE: foreground adaptive A,E attempts={0}; the first render was required before one normalized click at the second configured animal row; waiting for the second render receipt." -f @($animalViewerPlayerInputAttempts.ToArray() | Where-Object { [string]$_.Label -like 'NoQaAnimalOpenE*' }).Count)
        }
        else {
            Write-Host 'NO-QA UI GATE READY FOR ANIMAL INPUT: use only normal player controls to open a configured animal viewer and switch animals. Leave the second viewer open; the runner will close it with one foreground SendInput Escape after two causal progress-render receipts.'
        }
        while ((Test-SmokeDeadlineHasBudget -Deadline $noQaUiDeadline) -and -not $noQaAnimalViewerUiOk) {
            $animalTail = Get-LogTextAfterOffset -LogPath $logPath -Offset $noQaAnimalLogOffset
            if (-not (Test-SmokeDeadlineHasBudget -Deadline $noQaUiDeadline)) {
                break
            }
            $noQaAnimalViewerRenderReceiptCount = [regex]::Matches($animalTail, $noQaAnimalViewerReceiptPattern).Count
            $noQaAnimalViewerUiOk = $noQaAnimalViewerRenderReceiptCount -ge 2
            if (-not $noQaAnimalViewerUiOk) {
                if (Test-FatalInstanceWindow) { break }
                if (-not (Start-SmokeCappedSleep -Deadline $noQaUiDeadline -Milliseconds 500)) { break }
            }
        }
        if ($noQaAnimalViewerUiOk -and (Test-SmokeDeadlineHasBudget -Deadline $noQaUiDeadline)) {
            $noQaAnimalCloseSent = Send-DolocTownKey -VirtualKey 0x1B -Name 'NoQaAnimalViewerEscape' -HoldMilliseconds $ExternalPlayerInputHoldMilliseconds -InputLogPath $logPath -Deadline $noQaUiDeadline
            $noQaAnimalCloseEvidence = $script:lastDolocTownInputEvidence
            $noQaAnimalCloseProvenanceOk = $noQaAnimalCloseSent -and [bool]$noQaAnimalCloseEvidence.ForegroundMatchedAtSend -and
                [bool]$noQaAnimalCloseEvidence.SendInputSucceeded -and [bool]$noQaAnimalCloseEvidence.CompletedBeforeDeadline -and
                -not [bool]$noQaAnimalCloseEvidence.PostMessageFallbackUsed
                $animalViewerPlayerInputAttempts.Add([ordered]@{
                    Label = 'NoQaAnimalViewerEscape'
                    InputKind = 'KeyboardSendInput'
                    Sent = [bool]$noQaAnimalCloseSent
                ForegroundMatched = [bool]$noQaAnimalCloseEvidence.ForegroundMatchedAtSend
                SendInputSucceeded = [bool]$noQaAnimalCloseEvidence.SendInputSucceeded
                    CompletedBeforeDeadline = [bool]$noQaAnimalCloseEvidence.CompletedBeforeDeadline
                    PostMessageFallbackUsed = [bool]$noQaAnimalCloseEvidence.PostMessageFallbackUsed
                    HoldMilliseconds = $ExternalPlayerInputHoldMilliseconds
                    LogOffset = [int64]$noQaAnimalCloseEvidence.LogOffsetAtSend
            }) | Out-Null
            if ($noQaAnimalCloseProvenanceOk) {
                $noQaAnimalViewerNativeCloseOk = Wait-ForLogLineAfterOffsetUntilDeadline `
                    -LogPath $logPath `
                    -Offset ([int64]$noQaAnimalCloseEvidence.LogOffsetAtSend) `
                    -Pattern $noQaAnimalViewerProductClosePattern `
                    -Deadline (Get-SmokeCappedDeadline -Deadline $noQaUiDeadline -MaximumMilliseconds 20000) `
                    -AbortOnFatalInstanceWindow
                # The current Advanced product owns the native Hook and emits one atomic
                # close receipt after it has cleared both derived rows and cloned objects.
                $noQaAnimalViewerOverlayClearOk = $noQaAnimalViewerNativeCloseOk
                if ($noQaAnimalViewerNativeCloseOk -and $noQaAnimalViewerOverlayClearOk) {
                    $animalLifecycleTail = Get-LogTextAfterOffset -LogPath $logPath -Offset $noQaAnimalLogOffset
                    $animalRenderMatches = [regex]::Matches($animalLifecycleTail, $noQaAnimalViewerReceiptPattern)
                    $productCloseIndex = $animalLifecycleTail.IndexOf($noQaAnimalViewerProductClosePattern, [System.StringComparison]::Ordinal)
                    $lastRenderIndex = if ($animalRenderMatches.Count -gt 0) { $animalRenderMatches[$animalRenderMatches.Count - 1].Index } else { -1 }
                    $noQaAnimalViewerCloseOrderOk = $animalRenderMatches.Count -ge 2 -and $productCloseIndex -gt $lastRenderIndex
                }
                $noQaAnimalViewerCloseOk = $noQaAnimalViewerNativeCloseOk -and $noQaAnimalViewerOverlayClearOk -and $noQaAnimalViewerCloseOrderOk
            }
        }
        # Opening and using the animal viewer after the equipment Escape proves that
        # the native equipment modal released gameplay input. Its reusable clones are
        # required to be released separately at the ReturnedToTitle owner boundary.
        $noQaEquipmentSlotsDownstreamInteractionOk = $noQaAnimalViewerUiOk
        $noQaEquipmentSlotsCloseOk = $noQaEquipmentSlotsCloseInputOk -and $noQaEquipmentSlotsDownstreamInteractionOk
        $noQaEquipmentSlotsRecoveryOk = -not [bool](Select-String `
            -LiteralPath $logPath `
            -Pattern 'EquipmentSlots orphan recovery failed|Player\.EquipmentSlotsApi = orphan-recovery-failed' `
            -ErrorAction SilentlyContinue `
            | Select-Object -First 1)
        $noQaBehaviorCompletedAt = Get-Date
        $noQaBehaviorCompletedBeforeDeadlineOk = Test-SmokeDeadlineHasBudget -Deadline $noQaUiDeadline -ObservedAt $noQaBehaviorCompletedAt
        $noQaBehaviorRemainingMilliseconds = Get-SmokeRemainingBudgetMilliseconds -Deadline $noQaUiDeadline -ObservedAt $noQaBehaviorCompletedAt
        "NoQaBehaviorCompleted=$(Get-Date -Format o);completedAt=$($noQaBehaviorCompletedAt.ToString('o'));deadline=$($noQaUiDeadline.ToString('o'));completedBeforeDeadline=$noQaBehaviorCompletedBeforeDeadlineOk;remainingMilliseconds=$noQaBehaviorRemainingMilliseconds" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        Write-Host ("NO-QA UI GATE receipts: animal={0}; animalNativeClose={1}; animalOverlayClear={2}; animalCloseOrder={3}; animalClose={4}; animalAutoDrive={5}; equipmentRegister={6}; equipmentRender={7}; equipmentInteraction={8}; equipmentClose={9}; recoveryNoFailure={10}. Return to title and exit the game normally." -f `
            $noQaAnimalViewerUiOk, $noQaAnimalViewerNativeCloseOk, $noQaAnimalViewerOverlayClearOk, $noQaAnimalViewerCloseOrderOk, $noQaAnimalViewerCloseOk, $noQaAnimalViewerAutoDriveProvenanceOk, $noQaEquipmentSlotsRegisterOk, $noQaEquipmentSlotsRenderOk, $noQaEquipmentSlotsInteractionOk, $noQaEquipmentSlotsCloseOk, $noQaEquipmentSlotsRecoveryOk)
    }
    if ($saveLoadedOk -and $AutoExerciseDebugInventory) {
        $debugInventoryOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise DebugInventory OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugWeather) {
        $debugWeatherOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise DebugWeather OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugTeleport) {
        $debugTeleportOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise DebugTeleport OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugTime) {
        $debugTimeOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise DebugTime OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugMovement) {
        $debugMovementOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise DebugMovement OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseAdvancedDebug) {
        $advancedDebugOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise AdvancedDebug OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseZoom) {
        $zoomOk = if ($qaG4CameraPlayableEnabled) {
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.CameraPlayable = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        else {
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise CameraPlayable OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        if ($zoomOk -and -not $qaG4CameraPlayableEnabled) {
            $cameraDiagnosticsOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.DiagnosticsSnapshot = verified. scenario=Camera' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            if ($cameraDiagnosticsOk) {
                $diagnosticsReportExportPassedScenarios += 'Camera'
            }
        }
        elseif (-not $zoomOk) {
            $diagnosticsReportExportOk = $false
        }
    }
    if ($saveLoadedOk -and $AutoExerciseChestLocatorEnhancer) {
        $chestLocatorEnhancerOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise ChestLocatorEnhancer OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseZoomProductNative) {
        $zoomProductNativeOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise ZoomProductNative OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseMoreEquipmentSlots) {
        if ($ObserveMoreEquipmentSlotsInterruptedCandidateRecovery) {
            $moreEquipmentSlotsInterruptedCandidateRecoveryOk =
                Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
                    -LogPath $logPath `
                    -Pattern 'Smoke exercise MoreEquipmentSlotsInterruptedCandidateRecovery OK .*persistedCleanupWrites=1.*journal=false.*candidate=false.*beforeGiveItem=true.*beforeNativeSave=true' `
                    -TimeoutSeconds $TimeoutSeconds `
                    -Regex `
                    -AbortOnFatalInstanceWindow
        }
        $moreEquipmentSlotsReadyOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise MoreEquipmentSlots OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $moreEquipmentSlotsProtectedTransactionOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'MoreEquipmentSlots protected transaction committed destination=(Backpack|Mail)' -TimeoutSeconds $TimeoutSeconds -Regex -AbortOnFatalInstanceWindow
        $moreEquipmentSlotsOk = $moreEquipmentSlotsReadyOk -and $moreEquipmentSlotsProtectedTransactionOk
    }
    if ($saveLoadedOk -and
        $SetupMoreEquipmentSlotsCommittedShield) {
        $moreEquipmentSlotsCommittedShieldSetupOk =
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
                -LogPath $logPath `
                -Pattern 'Smoke exercise MoreEquipmentSlotsCommittedShieldSetup OK .*logicalShieldItems=1.*nativeShieldItems=0.*committedShieldItems=1.*journal=false.*candidate=false' `
                -TimeoutSeconds $TimeoutSeconds `
                -Regex `
                -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and
        $DamageMoreEquipmentSlotsCommittedShieldNoNativeSave) {
        $moreEquipmentSlotsCommittedShieldDamageOk =
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
                -LogPath $logPath `
                -Pattern 'Smoke exercise MoreEquipmentSlotsCommittedShieldDamageNoNativeSave OK .*workingShieldDamage=.*logicalCommittedShieldItems=1.*cleanupBeforeTitle=false.*nativeSaveRequested=false' `
                -TimeoutSeconds $TimeoutSeconds `
                -Regex `
                -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and
        $BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave) {
        $moreEquipmentSlotsCommittedShieldBreakReplaceOk =
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
                -LogPath $logPath `
                -Pattern 'Smoke exercise MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave OK .*workingShieldBroken=true.*replacement=true.*unequip=true.*logicalCommittedShieldItems=1.*cleanupBeforeTitle=false.*nativeSaveRequested=false' `
                -TimeoutSeconds $TimeoutSeconds `
                -Regex `
                -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and
        $AutoExerciseMoreEquipmentSlotsNoNativeSave) {
        $moreEquipmentSlotsNoNativeSaveOk =
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
                -LogPath $logPath `
                -Pattern 'Smoke exercise MoreEquipmentSlotsNoNativeSave OK .*nativeSaveRequested=false' `
                -TimeoutSeconds $TimeoutSeconds `
                -Regex `
                -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and
        $ObserveMoreEquipmentSlotsNoNativeSaveCold) {
        $expectedMoreEquipmentSlotsColdItemIds =
            @($expectedMoreEquipmentSlotsCommittedSlots -split '\|' |
                ForEach-Object {
                    $descriptionParts =
                        @($_ -split ':', 3)
                    if ($descriptionParts.Count -gt 1) {
                        [string]$descriptionParts[1]
                    }
                })
        $expectedMoreEquipmentSlotsColdButtonSidecar =
            @($expectedMoreEquipmentSlotsColdItemIds |
                Where-Object {
                    $_ -ceq 'grandmas_button'
                }).Count
        $expectedMoreEquipmentSlotsColdShieldSidecar =
            @($expectedMoreEquipmentSlotsColdItemIds |
                Where-Object {
                    $_ -ceq 'box_hat'
                }).Count
        $expectedMoreEquipmentSlotsColdButtonTotal =
            $ExpectedMoreEquipmentSlotsBackpackBaseline +
            $expectedMoreEquipmentSlotsColdButtonSidecar
        $expectedMoreEquipmentSlotsColdShieldTotal =
            $expectedMoreEquipmentSlotsColdShieldSidecar
        $expectedMoreEquipmentSlotsColdItemPattern =
            'Smoke exercise MoreEquipmentSlotsNoNativeSaveColdObserver OK ' +
            '.*boxHatBackpack=0.*boxHatMail=0' +
            ".*boxHatSidecar=$expectedMoreEquipmentSlotsColdShieldSidecar" +
            ".*boxHatExpectedTotal=$expectedMoreEquipmentSlotsColdShieldTotal" +
            ".*boxHatLogicalTotal=$expectedMoreEquipmentSlotsColdShieldTotal" +
            ".*grandmasButtonBackpack=$ExpectedMoreEquipmentSlotsBackpackBaseline" +
            '.*grandmasButtonMail=0' +
            ".*grandmasButtonSidecar=$expectedMoreEquipmentSlotsColdButtonSidecar" +
            ".*grandmasButtonExpectedTotal=$expectedMoreEquipmentSlotsColdButtonTotal" +
            ".*grandmasButtonLogicalTotal=$expectedMoreEquipmentSlotsColdButtonTotal" +
            '.*itemExpectationsMatch=true.*nativeSaveRequested=false'
        $moreEquipmentSlotsNoNativeSaveColdOk =
            Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
                -LogPath $logPath `
                -Pattern 'Smoke exercise MoreEquipmentSlotsNoNativeSaveColdObserver OK .*workingMatchesCommitted=true.*workingDirty=false.*journal=false.*candidate=false.*nativeSaveRequested=false' `
                -TimeoutSeconds $TimeoutSeconds `
                -Regex `
                -AbortOnFatalInstanceWindow
        if ($moreEquipmentSlotsNoNativeSaveColdOk) {
            $moreEquipmentSlotsNoNativeSaveColdOk =
                Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline `
                    -LogPath $logPath `
                    -Pattern $expectedMoreEquipmentSlotsColdItemPattern `
                    -TimeoutSeconds $TimeoutSeconds `
                    -Regex `
                    -AbortOnFatalInstanceWindow
        }
    }
    if ($saveLoadedOk -and
        $MoreEquipmentSlotsTransitionPhase -eq 'ColdCommit') {
        $moreEquipmentSlotsColdRecoveryDemandOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'EquipmentSlots cold compatibility demand detected source=(Sidecar|Journal)' -TimeoutSeconds $TimeoutSeconds -Regex -AbortOnFatalInstanceWindow
        $moreEquipmentSlotsColdRecoveryHostOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Hook status: Compatibility\.Host = resident\..*service=EquipmentSlots' -TimeoutSeconds $TimeoutSeconds -Regex -AbortOnFatalInstanceWindow
        $moreEquipmentSlotsColdRecoveryDestinationOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'EquipmentSlots orphan recovery OK owner=DTMAPI\.MoreEquipmentSlotsMod recovered=1 message=.*(native backpack|native item mail)' -TimeoutSeconds $TimeoutSeconds -Regex -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseMoreSavesOfficialSaveUi) {
        $moreSavesOfficialSaveUiOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.MoreSavesOfficialSaveUi = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $moreSavesOfficialSaveUiEvidenceOk = if ($qaG4SaveSlotsPagingEnabled) {
            $moreSavesOfficialSaveUiOk -and (
                -not $qaG4MoreSavesPostTitlePanelEnabled -or
                (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.MoreSavesPostTitleOfficialSaveUi = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
            )
        }
        else {
            $moreSavesOfficialSaveUiOk -and (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'MoreSaves official save UI evidence archiveFileCount=' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        }
    }
    if ($saveLoadedOk -and $AutoExerciseStrongPlantingGun) {
        $strongPlantingGunOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise StrongPlantingGun OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseCropHarvestingApi) {
        $cropHarvestingApiOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise CropHarvestingApi OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $cropHarvestingApiEvidenceOk = $cropHarvestingApiOk -and (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.CropHarvestingApi = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
    }
    if ($saveLoadedOk -and $AutoExerciseCustomEntityApis) {
        $customEntityApisOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke exercise CustomEntityApis OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $qaFishRoeTooltipObservationEnabled) {
        $fishRoeTooltipOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.FishRoeTooltip = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $itemDisplayNameLifecycleRequested) {
        $itemDisplayNameLifecycleOk =
            (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Shared item display-name consumer query succeeded owner=Yuuka.DTMAPI.FishBreedingAssistant ' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow) -and
            (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Shared item display-name consumer query succeeded owner=Yuuka.DTMAPI.AnimalHusbandryProgress ' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow) -and
            (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SharedNative.EnvironmentReset = experimental' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow) -and
            (Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.ItemDisplayNameLifecycle = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
    }
    if ($saveLoadedOk -and $qaDiagnosticsSnapshotEnabled) {
        $qaDiagnosticsSnapshotOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.DiagnosticsSnapshot = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $QaObserveSaveLoaded) {
        $qaSaveLoadedObservationOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.QaLifecycle.SaveLoaded = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($QaObserveSaveSaved) {
        $qaSaveSavedObservationOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.QaLifecycle.SaveSaved = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($QaObserveWorkshopReloadCompleted) {
        $qaWorkshopReloadObservationOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'Smoke.QaLifecycle.WorkshopReloadCompleted = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
}

$processWaitSeconds = if ($AssertNoQaUiEvidence) {
    if ($null -ne $noQaRunnerDeadline) {
        Get-SmokeRemainingBudgetSeconds -Deadline $noQaRunnerDeadline
    }
    else {
        0
    }
}
elseif ($startupOk) {
    $TimeoutSeconds
}
else {
    30
}
if ($AssertNoQaUiEvidence) {
    $noQaProcessWaitSeconds = $processWaitSeconds
    "NoQaProcessWaitSeconds=$noQaProcessWaitSeconds;sharedDeadline=$($noQaRunnerDeadline.ToString('o'));saveLoadedDeadlineExpired=$noQaSaveLoadedDeadlineExpired" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
}
$deadline = if ($AssertNoQaUiEvidence -and $null -ne $noQaRunnerDeadline) { $noQaRunnerDeadline } else { (Get-Date).AddSeconds($processWaitSeconds) }
if ($SmokeRunnerScenarioDeadline -lt $deadline) { $deadline = $SmokeRunnerScenarioDeadline }
$fatalWindows = @()
$fatalWindowDetectedAt = $null
$unityCrashFreshnessStatus = 'not-evaluated'
$unityCrashFreshnessSummary = ''
$unityCrashPostCloseFreshness = $null
$fatalProcessDumpStatus = 'Skipped'
$manualExitObservation = $null
if ($WaitForManualExit) {
    $manualExitObservation = Wait-SmokeManualExit -Deadline $deadline
    $fatalWindows = @($manualExitObservation.FatalWindows)
    $fatalWindowDetectedAt = $manualExitObservation.FatalDetectedAt
    Write-SmokeJsonObject -Path (Join-Path $evidence 'manual-exit.json') -Value $manualExitObservation
}
while (-not $WaitForManualExit -and (Test-SmokeDeadlineHasBudget -Deadline $deadline)) {
    $proc = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
    if (-not $proc) {
        break
    }
    $fatalWindows = @(Get-FatalInstanceWindows)
    if ($fatalWindows.Count -gt 0) {
        $fatalWindowDetectedAt = Get-Date
        break
    }
    if (-not (Start-SmokeCappedSleep -Deadline $deadline -Milliseconds 2000)) {
        break
    }
}

if ($fatalWindows.Count -gt 0) {
    $fatalLiveSummaryPath = Join-Path $evidence 'fatal-window-live-summary.txt'
    $fatalLiveCheckPath = Join-Path $evidence 'fatal-window-live-check.txt'
    $fatalProcessPath = Join-Path $evidence 'fatal-window-live-process.txt'
    $fatalLogPositionPath = Join-Path $evidence 'fatal-window-dtmapi-log-position.txt'

    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
    Write-FatalWindowCheck -Path $fatalLiveCheckPath
    "FatalWindowDetectedAt=$($fatalWindowDetectedAt.ToString('o'))`nFatalWindowCrashDumpGraceSeconds=$FatalWindowCrashDumpGraceSeconds`nPhase=detected-before-live-collect" | Set-Content -LiteralPath $fatalLiveSummaryPath
    Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue |
        ForEach-Object {
            "ProcessId=$($_.Id); MainWindowTitle=$($_.MainWindowTitle); MainWindowHandle=$($_.MainWindowHandle); HasExited=$($_.HasExited)"
        } |
        Set-Content -LiteralPath $fatalProcessPath

    if (Test-Path -LiteralPath $logPath) {
        $logItem = Get-Item -LiteralPath $logPath
        $lastLogLine = Get-Content -LiteralPath $logPath -Tail 1 -ErrorAction SilentlyContinue
        "Path=$logPath`nLength=$($logItem.Length)`nLastWriteTime=$($logItem.LastWriteTime.ToString('o'))`nLastLine=$lastLogLine" | Set-Content -LiteralPath $fatalLogPositionPath
    }
    else {
        "Path=$logPath`nMissing=True" | Set-Content -LiteralPath $fatalLogPositionPath
    }

    if ($FatalWindowCrashDumpGraceSeconds -gt 0) {
        "FatalWindowCrashDumpGraceStarted=$(Get-Date -Format o);seconds=$FatalWindowCrashDumpGraceSeconds" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        Start-Sleep -Seconds $FatalWindowCrashDumpGraceSeconds
        "FatalWindowCrashDumpGraceCompleted=$(Get-Date -Format o);processAlive=$([bool](Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue))" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    }

    $dumpTargetProcess = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Select-Object -First 1
    $fatalProcessDumpStatus = Invoke-SmokeFatalProcessDump -EvidencePath $evidence -Mode $FatalWindowProcessDumpMode -Process $dumpTargetProcess
    "FatalWindowProcessDumpStatus=$fatalProcessDumpStatus;mode=$FatalWindowProcessDumpMode" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
}

"UnityCrashEvidencePhase=live-collect-start $(Get-Date -Format o)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
& "$SmokeRunnerScriptsRoot\collect-logs.ps1" -CaseId 'GAME-SMOKE' -OutputDirectory $evidence -RuntimeEvidenceSinceUtc ($smokeRunStartedAt.ToUniversalTime()) | Tee-Object -FilePath (Join-Path $evidence 'collect-logs-output.txt')
$unityCrashSummaryPath = Join-Path $evidence 'Unity-Crashes\summary.txt'
$unityCrashLiveFreshness = Get-SmokeUnityCrashFreshness -SummaryPath $unityCrashSummaryPath -RunStartedAt $smokeRunStartedAt -FatalDetectedAt $fatalWindowDetectedAt -Phase 'live-collect'
Write-SmokeCrashFreshness -Path (Join-Path $evidence 'fatal-window-crash-freshness.txt') -Freshness $unityCrashLiveFreshness
$unityCrashLiveEvidence = if (Test-Path -LiteralPath $unityCrashSummaryPath) {
    $unityCrashLiveSummaryText = Get-Content -Raw -LiteralPath $unityCrashSummaryPath -ErrorAction SilentlyContinue
    if ($unityCrashLiveSummaryText -match 'FilesCopiedTotal=[1-9]|CopiedFile=') { 'present-live-collect' } else { 'summary-only-or-missing-files' }
}
else {
    'missing'
}
$unityCrashFreshnessStatus = [string]$unityCrashLiveFreshness.Status
$unityCrashFreshnessSummary = [string]$unityCrashLiveFreshness.Reason
"UnityCrashEvidencePhase=live-collect-complete $(Get-Date -Format o);unityCrashEvidence=$unityCrashLiveEvidence;freshness=$unityCrashFreshnessStatus" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
if ($fatalWindows.Count -gt 0) {
    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-post-live-collect-check.txt')
} else {
    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
}
if ($fatalWindows.Count -eq 0) {
    $fatalWindows = @(Get-FatalInstanceWindows)
}

$leftover = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
$forcedClose = $false
if ($leftover) {
    if($WaitForManualExit -and $null -ne $manualExitObservation -and [bool]$manualExitObservation.Passed) {
        $manualExitObservation.Passed=$false
        $manualExitObservation.Reason='ManualExitRequiredRunnerClose'
        Write-SmokeJsonObject -Path (Join-Path $evidence 'manual-exit.json') -Value $manualExitObservation
    }
    $null = $leftover.CloseMainWindow()
    Start-Sleep -Seconds 10
    $leftover = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
    if ($leftover) {
        $leftover | Stop-Process -Force
        $forcedClose = $true
        Start-Sleep -Seconds 2
        $leftover = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
    }
}
if ($saveFixtureIsolationRequested -and (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    $saveFixtureIsolationCleanupOk = [bool](Select-String `
        -LiteralPath $logPath `
        -Pattern 'Smoke save fixture isolation released owner=dtmapi\.qa\.save-fixture\.[A-Fa-f0-9]{32};reason=' `
        -ErrorAction SilentlyContinue |
        Select-Object -Last 1)
}
if ($fatalWindows.Count -gt 0) {
    Start-Sleep -Seconds $FatalWindowPostCloseCrashDumpWaitSeconds
    "UnityCrashEvidencePhase=post-close-collect-start $(Get-Date -Format o);processAlive=$([bool](Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue))" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    & "$SmokeRunnerScriptsRoot\collect-logs.ps1" -CaseId 'GAME-SMOKE' -OutputDirectory $evidence -RuntimeEvidenceSinceUtc ($smokeRunStartedAt.ToUniversalTime()) | Tee-Object -FilePath (Join-Path $evidence 'collect-logs-after-close-output.txt')
    $unityCrashPostCloseFreshness = Get-SmokeUnityCrashFreshness -SummaryPath $unityCrashSummaryPath -RunStartedAt $smokeRunStartedAt -FatalDetectedAt $fatalWindowDetectedAt -Phase 'post-close-collect'
    Write-SmokeCrashFreshness -Path (Join-Path $evidence 'fatal-window-crash-freshness.txt') -Freshness $unityCrashPostCloseFreshness
    $unityCrashPostCloseEvidence = if (Test-Path -LiteralPath $unityCrashSummaryPath) {
        $unityCrashSummaryText = Get-Content -Raw -LiteralPath $unityCrashSummaryPath -ErrorAction SilentlyContinue
        if ($unityCrashSummaryText -match 'FilesCopiedTotal=[1-9]|CopiedFile=') { 'present-post-close-collect' } else { 'summary-only-or-missing-files' }
    }
    else {
        'missing'
    }
    $unityCrashFinalSource = if ($unityCrashLiveEvidence -eq 'present-live-collect') {
        'live-collect'
    }
    elseif ($unityCrashPostCloseEvidence -eq 'present-post-close-collect') {
        'post-close-collect'
    }
    else {
        'missing'
    }
    $unityCrashFreshnessStatus = [string]$unityCrashPostCloseFreshness.Status
    $unityCrashFreshnessSummary = [string]$unityCrashPostCloseFreshness.Reason
    "UnityCrashEvidencePhase=post-close-collect-complete $(Get-Date -Format o);unityCrashEvidence=$unityCrashPostCloseEvidence;finalSource=$unityCrashFinalSource;freshness=$unityCrashFreshnessStatus" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
}
if ($publishedProductArtifactGateRequested) {
    $publishedProductPostExitArtifactSnapshots = @(Get-SmokePublishedProductDefinitions | ForEach-Object {
        Get-SmokeWorkshopArtifactSnapshot -Path (Join-Path $workshopContentRoot ([string]$_.workshopId)) -Product $_
    })
    $postExitBaselineOk = $publishedProductPostExitArtifactSnapshots.Count -eq 11 -and @($publishedProductPostExitArtifactSnapshots | Where-Object { -not [bool]$_.Passed }).Count -eq 0
    $publishedProductArtifactsUnchangedDuringRun = $publishedProductArtifactSnapshots.Count -eq $publishedProductPostExitArtifactSnapshots.Count
    foreach ($before in $publishedProductArtifactSnapshots) {
        $after = @($publishedProductPostExitArtifactSnapshots | Where-Object { [string]$_.WorkshopId -eq [string]$before.WorkshopId } | Select-Object -First 1)
        if ($after.Count -ne 1 -or
            [int]$after[0].ActualFileCount -ne [int]$before.ActualFileCount -or
            [int64]$after[0].ActualBytes -ne [int64]$before.ActualBytes -or
            -not [string]::Equals([string]$after[0].ActualTreeSha256, [string]$before.ActualTreeSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
            $publishedProductArtifactsUnchangedDuringRun = $false
        }
    }
    $publishedProductArtifactsOk = $publishedProductArtifactsOk -and $postExitBaselineOk -and $publishedProductArtifactsUnchangedDuringRun
    Write-SmokeJsonObject -Path (Join-Path $evidence 'public-product-artifact-gate.json') -Value ([ordered]@{
        WorkshopContentRoot = $workshopContentRoot
        ExpectedProductCount = 11
        PreLaunch = [ordered]@{
            ActualProductCount = $publishedProductArtifactSnapshots.Count
            Snapshots = @($publishedProductArtifactSnapshots)
            Passed = @($publishedProductArtifactSnapshots | Where-Object { -not [bool]$_.Passed }).Count -eq 0
        }
        PostExit = [ordered]@{
            ActualProductCount = $publishedProductPostExitArtifactSnapshots.Count
            Snapshots = @($publishedProductPostExitArtifactSnapshots)
            Passed = $postExitBaselineOk
        }
        UnchangedDuringRun = $publishedProductArtifactsUnchangedDuringRun
        Passed = $publishedProductArtifactsOk
        Normalization = 'CurrentPublished: DTMAPI-Published-SHA256SUMS-v1 Ordinal forward-slash paths, excluding Content/.tools/bepinex/extract/**, LF join without final LF. Retained: frozen DTMAPI-Retained-SHA256SUMS-v1 OrdinalIgnoreCase forward-slash paths, LF join without final LF.'
    })
}
