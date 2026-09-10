$autoFishingScenarioEffective = if (-not $SmokeRunnerBoundParameters.ContainsKey('AutoFishingScenario') -and $AutoExerciseAutoFishingMiniGameComplete) {
    'CombinedInstantComplete'
}
else {
    $AutoFishingScenario
}
if ($Batch6AutoFishingPilot) {
    $autoFishingScenarioEffective = $Batch6AutoFishingScenario
}
elseif ($Batch6AutoFishingManagerLifecycle -and $Batch6AutoFishingManagerMode -eq 'SameProcessDisable') {
    $autoFishingScenarioEffective = 'DefaultLoop'
}
$batch5GcLadderEnabled = $Batch5GcLadderDomain -ne 'None'
$batch5GcLadderActionSpeed = $Batch5GcLadderDomain -eq 'ActionSpeed'
$batch5GcLadderAutoFishing = $Batch5GcLadderDomain -eq 'AutoFishing'
if (-not $batch5GcLadderEnabled -and $Batch5GcLadderLevel -ne 'None') {
    throw '-Batch5GcLadderLevel requires -Batch5GcLadderDomain ActionSpeed or AutoFishing.'
}
if ($batch5GcLadderEnabled) {
    if (-not $StageQaHost) {
        throw 'Batch 5 GC ladder stages require -StageQaHost.'
    }
    if ($Batch5GcLadderLevel -eq 'None') {
        throw 'Batch 5 GC ladder stages require -Batch5GcLadderLevel L0 through L5.'
    }
    $batch5RequiredSaveSlot = if ($batch5GcLadderAutoFishing) { 5 } else { 3 }
    if ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne $batch5RequiredSaveSlot) {
        throw "Batch 5 GC ladder domain $Batch5GcLadderDomain requires explicit -SaveSlot $batch5RequiredSaveSlot."
    }
    if ([string]::IsNullOrWhiteSpace($Batch5GcLadderWorkload)) {
        throw 'Batch 5 GC ladder stages require a workload label.'
    }
    if ($batch5GcLadderActionSpeed -and $Batch5GcLadderWorkload -notin @('Tool','Interact','Eat','ContinuousUse')) {
        throw 'The ActionSpeed GC ladder automation requires workload=Tool, Interact, Eat, or ContinuousUse.'
    }
    if ($batch5GcLadderAutoFishing -and $Batch5GcLadderWorkload -ne 'FishLoop') {
        throw 'The AutoFishing GC ladder automation requires workload=FishLoop.'
    }
    if ($Batch5GcLadderSampleSeconds -gt $Batch5GcLadderMeasureSeconds) {
        throw 'Batch 5 GC ladder sample seconds cannot exceed measure seconds.'
    }
    if ($AutoExercisePreLoadGcProbe) {
        throw 'Batch 5 GC ladder stages forbid the pre-load forced-GC probe.'
    }
    if ($batch5GcLadderActionSpeed -and $AutoFishingPerformance) {
        throw 'ActionSpeed and AutoFishing GC measurements are independent runs.'
    }
    if ($batch5GcLadderAutoFishing -and (-not $AutoFishingPerformance -or $AutoFishingPerformanceProfile -ne 'FishLoop')) {
        throw 'AutoFishing GC ladder stages require -AutoFishingPerformance -AutoFishingPerformanceProfile FishLoop.'
    }
    if ($batch5GcLadderAutoFishing -and $AutoFishingPerformanceTargetFish -ne $Batch5GcLadderTargetUnits) {
        throw 'AutoFishing GC ladder target units must equal -AutoFishingPerformanceTargetFish.'
    }
}
if ($AutoFishingPerformance -and -not $AutoExerciseAutoFishingPhase -and -not $batch5GcLadderAutoFishing -and
    -not ($AutoFishingPerformanceProfile -ieq 'InactiveNoConsumer' -and $AutoFishingPerformanceTargetFrames -gt 0)) {
    throw '-AutoFishingPerformance requires -AutoExerciseAutoFishingPhase or the Batch 5 AutoFishing GC ladder route.'
}
if ($AutoFishingPerformance -and -not $StageQaHost) {
    throw '-AutoFishingPerformance requires -StageQaHost; the optional QA host is the sole performance measurement/result owner.'
}
if ($AutoFishingPerformance) {
    if ($AutoFishingPerformanceProfile -ieq 'FishLoop') {
        $AutoFishingPerformanceProfile = 'FishLoop'
    }
    elseif ($AutoFishingPerformanceProfile -ieq 'InactiveNoConsumer') {
        $AutoFishingPerformanceProfile = 'InactiveNoConsumer'
    }
    elseif ($AutoFishingPerformanceProfile -ieq 'EnabledNoRod') {
        $AutoFishingPerformanceProfile = 'EnabledNoRod'
    }
    if (@('FishLoop','InactiveNoConsumer','EnabledNoRod') -notcontains $AutoFishingPerformanceProfile) {
        throw '-AutoFishingPerformanceProfile must be FishLoop, InactiveNoConsumer, or EnabledNoRod.'
    }
    if ($AutoFishingPerformanceProfile -eq 'FishLoop' -and $AutoFishingPerformanceTargetFish -le 0) {
        throw 'The FishLoop QA performance profile requires -AutoFishingPerformanceTargetFish greater than zero.'
    }
    if ($AutoFishingPerformanceProfile -ne 'FishLoop' -and $AutoFishingPerformanceTargetFish -ne 0) {
        throw 'InactiveNoConsumer and EnabledNoRod QA performance profiles require -AutoFishingPerformanceTargetFish 0.'
    }
    if ($AutoFishingPerformanceWarmupFish -lt 0 -or $AutoFishingPerformanceZeroWarmupSeconds -lt 0 -or $AutoFishingPerformanceZeroMeasureSeconds -le 0) {
        throw 'QA performance warm-up values must be non-negative and zero-profile measurement seconds must be positive.'
    }
    if ($AutoFishingPerformanceTargetFrames -gt 0 -and $AutoFishingPerformanceProfile -ne 'InactiveNoConsumer') {
        throw '-AutoFishingPerformanceTargetFrames is valid only for InactiveNoConsumer.'
    }
    if ($AutoFishingPerformanceTargetFrames -gt 0 -and $AutoFishingPerformanceWarmupFrames -le 0) {
        throw 'InactiveNoConsumer frame-target performance requires -AutoFishingPerformanceWarmupFrames greater than zero.'
    }
}
$qaG4TitleSettingsUiEnabled = [bool]$StageQaHost -and
    [bool]$AutoOpenTitleSettingsMenu -and
    -not [bool]$AutoOpenTitleSettingsStatusPage -and
    -not [bool]$AutoOpenTitleSettingsManagerMvp
$qaG4ManagerStatusUiEnabled = [bool]$StageQaHost -and
    [bool]$AutoOpenTitleSettingsStatusPage -and
    -not [bool]$AutoOpenTitleSettingsManagerMvp
$qaG4ManagerMvpUiEnabled = [bool]$StageQaHost -and [bool]$AutoOpenTitleSettingsManagerMvp
$qaG4AnyTitleUiEnabled = $qaG4TitleSettingsUiEnabled -or $qaG4ManagerStatusUiEnabled -or $qaG4ManagerMvpUiEnabled
$qaG4OfficialModUiEnabled = [bool]$StageQaHost -and [bool]$AutoOpenOfficialModUi
$qaG4PauseMenuLayoutEnabled = [bool]$StageQaHost -and [bool]$AutoExercisePauseMenuLayout
# MouseGive remains a runner-owned real click mutation, while the optional QA
# participant observes the complete Y/Escape/short/hold lifecycle and captures
# the product-owned UI. Keep that observer active so the participant cannot
# terminate the process before the runner emits and verifies both click receipts.
$qaG4DebugConsoleEnabled = [bool]$StageQaHost -and [bool]$AutoExerciseDebugConsole
$qaG4SaveSlotsPagingEnabled = [bool]$StageQaHost -and [bool]$AutoExerciseMoreSavesOfficialSaveUi
$qaG4AnimalObservationEnabled = [bool]$StageQaHost -and [bool]$AutoOpenAnimalPanel
$qaG4EquipmentSlotsObservationEnabled = [bool]$StageQaHost -and [bool]$QaObserveEquipmentSlotsUi
$qaG4AudioObservationEnabled = [bool]$StageQaHost -and [bool]$AutoExerciseAudioReplacement
$qaG4HatchVoiceEnabled = [bool]$StageQaHost -and [bool]$AutoExerciseHatchAnimalVoice
$qaG4CameraPlayableEnabled = [bool]$StageQaHost -and [bool]$AutoExerciseZoom
$qaG4ContentMetadataObservationEnabled = [bool]$StageQaHost -and (
    [bool]$QaObserveContentMetadata -or
    ([bool]$AutoExerciseNewContentApis -and [bool]$ExpectOilAbsent))
$qaG4ExternalPlayerInputObservationEnabled = [bool]$StageQaHost -and [bool]$RequireExternalPlayerInputGate
$qaG4TitleLifecycleEnabled = [bool]$StageQaHost -and [bool]$AutoExerciseTitleButtonLifecycle
$qaG4ProductOwnerRefreshEnabled = [bool]$StageQaHost -and
    ([bool]$AutoReloadMods -or [bool]$AssertProductOwnerRefresh -or [bool]$AssertPublishedProductCombination)
$qaG4AdvancedProductOwnerDeactivationEnabled = [bool]$StageQaHost -and [bool]$AssertAdvancedProductOwnerDeactivation
$qaG4MoreSavesPostTitlePanelEnabled = $qaG4SaveSlotsPagingEnabled -and
    $qaG4AdvancedProductOwnerDeactivationEnabled -and
    @($AdvancedProductOwnerDeactivationOwnerIds | Where-Object { $_ -eq 'DTMAPI.MoreSavesMod' }).Count -gt 0
$qaG4AnyRequested = $qaG4AnyTitleUiEnabled -or $qaG4OfficialModUiEnabled -or
    $qaG4PauseMenuLayoutEnabled -or $qaG4DebugConsoleEnabled -or $qaG4SaveSlotsPagingEnabled -or
    $qaG4AnimalObservationEnabled -or $qaG4EquipmentSlotsObservationEnabled -or
    $qaG4AudioObservationEnabled -or $qaG4HatchVoiceEnabled -or
    $qaG4CameraPlayableEnabled -or $qaG4ContentMetadataObservationEnabled -or
    $qaG4ExternalPlayerInputObservationEnabled -or $qaG4TitleLifecycleEnabled -or $qaG4ProductOwnerRefreshEnabled -or
    $qaG4AdvancedProductOwnerDeactivationEnabled
$qaG4ContinuousHomePageTerminalEnabled = [bool]$StageQaHost -and (
    [bool]$RequireExternalPlayerInputGate -or
    $qaG4TitleLifecycleEnabled -or
    $qaG4ProductOwnerRefreshEnabled -or
    $qaG4AdvancedProductOwnerDeactivationEnabled)
$qaG4ContinuousHomePageRequiresSaveLoaded = $qaG4ContinuousHomePageTerminalEnabled
$qaG4ContinuousHomePageSeconds = 1.5
$qaG4ExpectedEvidencePaths = @(Get-SmokeQaG4ExpectedEvidencePaths `
    -TitleSettingsUiEnabled $qaG4TitleSettingsUiEnabled `
    -ManagerStatusUiEnabled $qaG4ManagerStatusUiEnabled `
    -ManagerMvpUiEnabled $qaG4ManagerMvpUiEnabled `
    -OfficialModUiEnabled $qaG4OfficialModUiEnabled `
    -PauseMenuLayoutEnabled $qaG4PauseMenuLayoutEnabled `
    -DebugConsoleEnabled $qaG4DebugConsoleEnabled `
    -SaveSlotsPagingEnabled $qaG4SaveSlotsPagingEnabled `
    -MoreSavesPostTitlePanelEnabled $qaG4MoreSavesPostTitlePanelEnabled `
    -AnimalObservationEnabled $qaG4AnimalObservationEnabled `
    -EquipmentSlotsObservationEnabled $qaG4EquipmentSlotsObservationEnabled `
    -MoreEquipmentSlots100UiAcceptanceEnabled $moreEquipmentSlots100UiAcceptanceRequested `
    -CameraPlayableEnabled $qaG4CameraPlayableEnabled)

$qaG5WorldMutationCases = New-Object 'System.Collections.Generic.List[string]'
if ($StageQaHost) {
    foreach ($entry in @(
        [ordered]@{ Requested = [bool]$AutoExerciseActionSpeedTool; Id = 'ActionSpeedTool' },
        [ordered]@{ Requested = [bool]$AutoExerciseActionSpeedConfigApply; Id = 'ActionSpeedConfigApply' },
        [ordered]@{ Requested = [bool]$AutoExerciseActionSpeedInteraction; Id = 'ActionSpeedInteraction' },
        [ordered]@{ Requested = [bool]$batch5GcLadderActionSpeed; Id = 'ActionSpeedGcLadder' },
        [ordered]@{ Requested = [bool]$AutoExerciseOneActionResourceHit; Id = 'OneActionResourceHit' },
        [ordered]@{ Requested = [bool]$AutoExerciseOneActionWrongTool; Id = 'OneActionWrongTool' },
        [ordered]@{ Requested = [bool]$AutoExerciseOneActionFuelFeed; Id = 'OneActionFuelFeed' },
        [ordered]@{ Requested = [bool]$AutoExerciseOneActionVegetation; Id = 'OneActionVegetation' },
        [ordered]@{ Requested = ([bool]$AutoExerciseNewContentApis -and -not [bool]$ExpectOilAbsent -and -not [bool]$OilOnly); Id = 'NewContent' },
        [ordered]@{ Requested = ([bool]$AutoExerciseNewContentApis -and [bool]$OilOnly); Id = 'OilOnly' },
        [ordered]@{ Requested = [bool]$AutoExerciseMineContentApis; Id = 'MineContent' },
        [ordered]@{ Requested = [bool]$AutoExerciseChestLocatorEnhancer; Id = 'ChestLocatorEnhancer' },
        [ordered]@{ Requested = [bool]$AutoExerciseZoomProductNative; Id = 'ZoomProductNative' },
        [ordered]@{ Requested = [bool]$AutoExerciseMoreEquipmentSlots; Id = 'MoreEquipmentSlots' },
        [ordered]@{ Requested = [bool]$SetupMoreEquipmentSlotsCommittedShield; Id = 'MoreEquipmentSlotsCommittedShieldSetup' },
        [ordered]@{ Requested = [bool]$DamageMoreEquipmentSlotsCommittedShieldNoNativeSave; Id = 'MoreEquipmentSlotsCommittedShieldDamageNoNativeSave' },
        [ordered]@{ Requested = [bool]$BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave; Id = 'MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave' },
        [ordered]@{ Requested = [bool]$AutoExerciseMoreEquipmentSlotsNoNativeSave; Id = 'MoreEquipmentSlotsNoNativeSave' },
        [ordered]@{ Requested = [bool]$ObserveMoreEquipmentSlotsNoNativeSaveCold; Id = 'MoreEquipmentSlotsNoNativeSaveColdObserver' },
        [ordered]@{ Requested = [bool]$moreEquipmentSlotsTransitionRequested; Id = 'MoreEquipmentSlotsTransition' },
        [ordered]@{ Requested = [bool]$AutoExerciseStrongPlantingGun; Id = 'StrongPlantingGun' },
        [ordered]@{ Requested = [bool]$AutoExerciseCropHarvestingApi; Id = 'CropHarvestingApi' },
        [ordered]@{ Requested = [bool]$AutoExerciseInstantSave; Id = 'InstantSave' },
        [ordered]@{ Requested = [bool]$debugConsoleSaveAcceptanceRequested; Id = 'DebugConsoleSaveAcceptance' },
        [ordered]@{ Requested = [bool]$AutoExerciseDebugInventory; Id = 'DebugInventory' },
        [ordered]@{ Requested = [bool]$AutoExerciseDebugWeather; Id = 'DebugWeather' },
        [ordered]@{ Requested = [bool]$AutoExerciseDebugTeleport; Id = 'DebugTeleport' },
        [ordered]@{ Requested = [bool]$AutoExerciseDebugTime; Id = 'DebugTime' },
        [ordered]@{ Requested = [bool]$AutoExerciseDebugMovement; Id = 'DebugMovement' },
        [ordered]@{ Requested = [bool]$AutoExerciseAdvancedDebug; Id = 'AdvancedDebug' }
    )) {
        if ([bool]$entry.Requested) {
            $qaG5WorldMutationCases.Add([string]$entry.Id) | Out-Null
        }
    }
}
$qaG5AnyRequested = $qaG5WorldMutationCases.Count -gt 0
$qaExternalStateProtectionRequested = [bool]$qaG5AnyRequested -or
    [bool]$AutoExerciseDebugConsoleMouseGive -or
    [bool]$AssertMoreEquipmentSlotsColdRecovery
if ($qaG5AnyRequested -and ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -le 0)) {
        throw 'G5 world-mutation cases require -StageQaHost and an explicit positive -SaveSlot so the external transaction can bind the selected save triple.'
}
if ($ValidateQaG5RoutingOnly) {
    if (-not $StageQaHost -or -not $qaG5AnyRequested) {
        throw '-ValidateQaG5RoutingOnly expected at least one explicitly staged G5 world-mutation case.'
    }
    [pscustomobject]@{
        ProtocolVersion = 7
        StageQaHost = [bool]$StageQaHost
        SaveSlot = $SaveSlot
        SaveSlotExplicit = [bool]$SmokeRunnerBoundParameters.ContainsKey('SaveSlot')
        Cases = @($qaG5WorldMutationCases.ToArray())
        ExclusiveQaG5Owner = $true
        SetupReceipt = 'explicit-g5-whitelist'
        CommitReceipt = 'per-case-existing-Smoke-status'
        CleanupReceipt = 'case-local-finally+runner-post-exit-save-config-profile-transaction'
        RealWorldFallbackAllowed = $false
    } | ConvertTo-Json -Depth 4
    $SmokeRunnerExitCode = [int](0); return
}

if ($QaObserveContentMetadata -and -not $StageQaHost) {
    throw '-QaObserveContentMetadata requires -StageQaHost; there is no production fallback lane.'
}
if ($QaObserveEquipmentSlotsUi -and -not $StageQaHost) {
    throw '-QaObserveEquipmentSlotsUi requires -StageQaHost; there is no production fallback lane.'
}
if ($StageQaHost -and $QaObserveContentMetadata -and $AutoExerciseNewContentApis -and -not $ExpectOilAbsent) {
    throw '-QaObserveContentMetadata cannot be combined with the positive -AutoExerciseNewContentApis world-mutation route. Use the QA observer alone, or use -AutoExerciseNewContentApis -ExpectOilAbsent for the legacy read-only Oil-absent route.'
}
if (($qaG4SaveSlotsPagingEnabled -or $qaG4CameraPlayableEnabled) -and $SmokeRootIsolationProfile -eq 'UiRuntime') {
    throw 'UiRuntime isolation disables the production SaveSlots/Camera roots and cannot be used as a QA ownership switch. Run G4 SaveSlots or CameraPlayable with -SmokeRootIsolationProfile None.'
}
$qaG4RequiresSaveLoaded = $qaG4PauseMenuLayoutEnabled -or $qaG4DebugConsoleEnabled -or
    $qaG4SaveSlotsPagingEnabled -or $qaG4AnimalObservationEnabled -or $qaG4EquipmentSlotsObservationEnabled -or $qaG4AudioObservationEnabled -or
    $qaG4HatchVoiceEnabled -or $qaG4CameraPlayableEnabled -or $qaG4ContentMetadataObservationEnabled -or
    $qaG4ExternalPlayerInputObservationEnabled -or $qaG4ContinuousHomePageTerminalEnabled
if (($qaG4ManagerStatusUiEnabled -or $qaG4ManagerMvpUiEnabled) -and $qaG4RequiresSaveLoaded) {
    throw 'Manager Status/MVP is a title-only G4 route and cannot be combined with save-bound G4, continuous HomePage, or external-input cases. Run the Manager title route separately.'
}
if ($qaG4RequiresSaveLoaded -and ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -le 0)) {
    throw 'The selected G4 QA cases require an explicit positive -SaveSlot; the default third slot is not a fixture selection receipt.'
}
if ($QaObserveEquipmentSlotsUi -and
    $MoreEquipmentSlotsTransitionPhase -ne 'U1') {
    $equipmentObservationAllowedParameters = @(
        'SaveSlot', 'TimeoutSeconds', 'UseSteam', 'SkipInstall', 'SkipBuild',
        'StageQaHost', 'QaObserveEquipmentSlotsUi', 'ValidateQaG4RoutingOnly',
        'OfficialModProfile', 'OfficialModProfileExtraEnabledIds', 'IsolateAllOfficialMods', 'AutoExitAfterSecondsOverride',
        'ExternalPlayerInputHoldMilliseconds', 'FatalWindowCrashDumpGraceSeconds',
        'FatalWindowProcessDumpMode', 'FatalWindowPostCloseCrashDumpWaitSeconds'
    )
    if ($moreEquipmentSlots100UiAcceptanceRequested) {
        $equipmentObservationAllowedParameters += @(
            'AutoExerciseMoreEquipmentSlotsNoNativeSave',
            'AutoExerciseTitleButtonLifecycle',
            'SaveTestMode'
        )
    }
    $equipmentObservationUnexpectedParameters = @($SmokeRunnerBoundParameters.Keys | Where-Object {
        $equipmentObservationAllowedParameters -notcontains [string]$_
    })
    if ($equipmentObservationUnexpectedParameters.Count -gt 0) {
        throw ('-QaObserveEquipmentSlotsUi accepts only its bounded observation route (or the exact 1.0 NoNativeSave combination) and cannot be combined with: ' +
            [string]::Join(', ', @($equipmentObservationUnexpectedParameters | Sort-Object)) + '.')
    }
    if (-not $UseSteam -or $DirectExe -or -not $SkipInstall) {
        throw '-QaObserveEquipmentSlotsUi requires explicit -UseSteam -SkipInstall and no -DirectExe.'
    }
    if ($OfficialModProfile -notin @('Published11','Local11') -or -not $IsolateAllOfficialMods -or
        @($OfficialModProfileExtraEnabledIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -ne 0 -or
        -not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot') -or $SaveSlot -ne 3) {
        throw '-QaObserveEquipmentSlotsUi requires the exact isolated Published11 or Local11 profile, no extra enabled products, and explicit third save slot 3.'
    }
    if ($moreEquipmentSlots100UiAcceptanceRequested -and
        ($OfficialModProfile -ne 'Local11' -or
         $SaveTestMode -ne 'NoNativeSave' -or
         -not $AutoExerciseTitleButtonLifecycle)) {
        throw 'MoreEquipmentSlots 1.0 UI acceptance requires the exact Local11 candidate, NoNativeSave, and title lifecycle in the same bounded third-save process.'
    }
}
if ($ValidateQaG4RoutingOnly) {
    [pscustomobject]@{
        ProtocolVersion = 7
        StageQaHost = [bool]$StageQaHost
        SaveSlotExplicit = [bool]$SmokeRunnerBoundParameters.ContainsKey('SaveSlot')
        UseSteam = [bool]$UseSteam
        DirectExe = [bool]$DirectExe
        SkipInstall = [bool]$SkipInstall
        IncludeHookProbe = [bool]$IncludeHookProbe
        TitleSettingsUiEnabled = $qaG4TitleSettingsUiEnabled
        ManagerStatusUiEnabled = $qaG4ManagerStatusUiEnabled
        ManagerMvpUiEnabled = $qaG4ManagerMvpUiEnabled
        TitleSettingsMode = if ($qaG4TitleSettingsUiEnabled) { 'Basic' } elseif ($qaG4ManagerStatusUiEnabled) { 'Status' } elseif ($qaG4ManagerMvpUiEnabled) { 'ManagerMvp' } else { 'NotRequested' }
        OfficialModUiEnabled = $qaG4OfficialModUiEnabled
        PauseMenuLayoutEnabled = $qaG4PauseMenuLayoutEnabled
        DebugConsoleEnabled = $qaG4DebugConsoleEnabled
        DebugConsoleBehaviorGate = if ($qaG4DebugConsoleEnabled) { 'runner-real-Y-Escape-short-hold-with-QA-observation' } else { 'not-requested' }
        DebugConsoleMouseGiveOwner = 'runner-real-click+qa-G4-observation'
        SaveSlotsPagingEnabled = $qaG4SaveSlotsPagingEnabled
        SaveSlot = $SaveSlot
        AnimalObservationEnabled = $qaG4AnimalObservationEnabled
        EquipmentSlotsObservationEnabled = $qaG4EquipmentSlotsObservationEnabled
        MoreEquipmentSlots100UiAcceptanceEnabled = [bool]$moreEquipmentSlots100UiAcceptanceRequested
        AudioObservationEnabled = $qaG4AudioObservationEnabled
        AudioBehaviorGate = if ($qaG4AudioObservationEnabled) { 'runner-real-E-then-paper-box-OnInteract-and-played-suppressed' } else { 'not-requested' }
        HatchVoiceEnabled = $qaG4HatchVoiceEnabled
        CameraPlayableEnabled = $qaG4CameraPlayableEnabled
        CameraBehaviorGate = if ($qaG4CameraPlayableEnabled) { 'bounded-playable-movement-telemetry-and-screenshots' } else { 'not-requested' }
        ContentMetadataObservationEnabled = $qaG4ContentMetadataObservationEnabled
        ContentMetadataExpectOilAbsent = [bool]$ExpectOilAbsent
        ExternalPlayerInputObservationEnabled = $qaG4ExternalPlayerInputObservationEnabled
        ExternalPlayerInputActionOwner = 'qa-participant-disposition'
        ContinuousHomePageTerminalEnabled = $qaG4ContinuousHomePageTerminalEnabled
        ContinuousHomePageRequiresSaveLoaded = $qaG4ContinuousHomePageRequiresSaveLoaded
        ContinuousHomePageSeconds = $qaG4ContinuousHomePageSeconds
        TitleLifecycleEnabled = $qaG4TitleLifecycleEnabled
        ProductOwnerRefreshEnabled = $qaG4ProductOwnerRefreshEnabled
        AdvancedProductOwnerDeactivationEnabled = $qaG4AdvancedProductOwnerDeactivationEnabled
        OfficialModProfile = $OfficialModProfile
        ExactProductProfileIds = if ($OfficialModProfile -eq 'Published11') { @(Get-SmokePublishedProductWorkshopIds | Sort-Object) } elseif ($OfficialModProfile -eq 'Local11') { @(Get-SmokePublishedProductLocalIds | Sort-Object) } else { @() }
        ExpectedQaEvidencePaths = @($qaG4ExpectedEvidencePaths)
        ProductionNativeUiRepairEnabled = $true
        SmokeRootIsolationProfile = $SmokeRootIsolationProfile
    } | ConvertTo-Json -Depth 4
    $SmokeRunnerExitCode = [int](0); return
}
if ($ValidateNoQaUiEvidenceGateOnly) {
    if (-not $AssertNoQaUiEvidence) {
        throw '-ValidateNoQaUiEvidenceGateOnly requires -AssertNoQaUiEvidence and its ordinary-player exact eleven-product flags.'
    }
    $comparisonSelfTest = Test-SmokeNoQaReceiptComparison
    if (-not [bool]$comparisonSelfTest.Passed) {
        throw 'The no-QA legacy-evidence receipt comparison self-test failed.'
    }
    $animalInputSequenceSelfTest = Test-SmokeNoQaAnimalInputSequences
    if (-not [bool]$animalInputSequenceSelfTest.Passed) {
        throw 'The no-QA animal input sequence comparison self-test failed.'
    }
    $deadlineSelfTest = Test-SmokeNoQaDeadlineContract
    if (-not [bool]$deadlineSelfTest.Passed) {
        throw 'The no-QA absolute-deadline self-test failed.'
    }
    $issue011ProjectionSelfTest = if ($Issue011Acceptance) {
        Test-SmokeIssue011AcceptanceProjection -RepoRoot $repo
    }
    else {
        [ordered]@{ PositivePassed = $true; RepeatedInputRejected = $true; StaleLastGiveRejected = $true; Passed = $true }
    }
    if (-not [bool]$issue011ProjectionSelfTest.Passed) {
        throw 'The ISSUE-011 acceptance projection self-test failed.'
    }
    [pscustomobject]@{
        SchemaVersion = 2
        AssertNoQaUiEvidence = $true
        Issue011Acceptance = [bool]$Issue011Acceptance
        AutoDriveNoQaAnimalViewer = [bool]$AutoDriveNoQaAnimalViewer
        StageQaHost = [bool]$StageQaHost
        IncludeHookProbe = [bool]$IncludeHookProbe
        UseSteam = [bool]$UseSteam
        DirectExe = [bool]$DirectExe
        SkipInstall = [bool]$SkipInstall
        SaveSlot = $SaveSlot
        OfficialModProfile = $OfficialModProfile
        IsolateAllOfficialMods = [bool]$IsolateAllOfficialMods
        RequiredProductCount = 11
        RequiredProductIds = if ($OfficialModProfile -eq 'Local11') { @(Get-SmokePublishedProductLocalIds | Sort-Object) } else { @(Get-SmokePublishedProductWorkshopIds | Sort-Object) }
        ProductSource = if ($OfficialModProfile -eq 'Local11') { 'Local' } else { 'Workshop' }
        Local11AuthorSourceStatePath = ''
        Local11AuthorSourceSelections = @()
        QaReceiptAndDllPolicy = 'activation-absent+qa-root-absent+qa-dll-absent+exact-five-runtime-dlls'
        LegacyEvidenceCases = @('DEBUG-CONSOLE-UI','ANIMAL-001','EQUIPMENT-SLOTS-UI')
        LegacyEvidenceComparison = 'existence+relative-directory-set+relative-file-path+length+sha256 exact before/after'
        YConsoleAction = 'runner-real-Y/Escape/short-tap/hold matrix; one foreground SendInput attempt per action; PostMessage fallback forbidden'
        Issue011Action = if ($Issue011Acceptance) { 'ordinary title-settings click/open plus manual non-empty Y search/hover/give, fresh last-give, collector dump-or-missing reason, and matching Steam process added/removed receipt' } else { 'not requested' }
        AnimalViewerAction = if ($AutoDriveNoQaAnimalViewer) { 'runner sends foreground SendInput A,E and waits briefly for the first neutral render; only if it did not open, one fallback A,E attempt is sent; then the second row is selected with one normalized foreground SendInput click before the second render and strict Escape cleanup' } else { 'player opens and switches a real configured animal viewer; two neutral render receipts then one strict runner Escape and a causal session-cleared receipt are required' }
        EquipmentSlotsAction = 'runner opens with strict B, performs a move-only hover sweep, and closes with strict Escape; neutral register/render/interaction/lifecycle receipts are required'
        ProductProfileTransaction = "$OfficialModProfile exact enablement plus post-run restoration"
        OrdinaryPlayerHandshake = 'runner prints title-screen -> tenth-save instructions before waiting for SaveLoaded; internal QA auto-load and ReturnHome navigation remain disabled'
        RunnerDeadlineBudgetSeconds = if ($AutoExitAfterSecondsOverride -gt 0) { $AutoExitAfterSecondsOverride } else { $TimeoutSeconds }
        RunnerDeadlineBudgetSource = if ($AutoExitAfterSecondsOverride -gt 0) { 'AutoExitAfterSecondsOverride' } else { 'TimeoutSeconds' }
        RunnerDeadlineScope = 'one launch-to-process-wait deadline; startup, GameLaunched, SaveLoaded, UI, and process waits consume only its remaining budget'
        InternalQaAutoLoad = $false
        InternalQaReturnHome = $false
        SelfTest = $comparisonSelfTest
        AnimalInputSequenceSelfTest = $animalInputSequenceSelfTest
        DeadlineSelfTest = $deadlineSelfTest
        Issue011ProjectionSelfTest = $issue011ProjectionSelfTest
        Passed = [bool]$comparisonSelfTest.Passed -and [bool]$animalInputSequenceSelfTest.Passed -and
            [bool]$deadlineSelfTest.Passed -and [bool]$issue011ProjectionSelfTest.Passed
    } | ConvertTo-Json -Depth 6
    $SmokeRunnerExitCode = [int](0); return
}
$qaG3OnlyRequested = [bool]$AutoExerciseFishRoeTooltip -or [bool]$AutoExerciseDiagnosticsSnapshot -or
    [bool]$QaObserveSaveLoaded -or [bool]$QaObserveSaveSaved -or [bool]$QaObserveWorkshopReloadCompleted
if ($qaG3OnlyRequested -and -not $StageQaHost) {
    throw 'FishRoe, diagnostics, and QA lifecycle observation switches require -StageQaHost.'
}
if ($QaObserveSaveLoaded -and $SaveSlot -le 0) {
    throw '-QaObserveSaveLoaded requires a positive -SaveSlot.'
}
if ($QaObserveSaveSaved -and -not $AutoSaveAfterLoad) {
    throw '-QaObserveSaveSaved requires -AutoSaveAfterLoad.'
}
if ($QaObserveSaveSaved -and $SaveSlot -le 0) {
    throw '-QaObserveSaveSaved requires a positive -SaveSlot.'
}
if ($QaObserveWorkshopReloadCompleted -and -not $AutoReloadMods) {
    throw '-QaObserveWorkshopReloadCompleted requires -AutoReloadMods.'
}
if ($AutoExerciseDiagnosticsSnapshot -and ($AutoExerciseActionSpeedInteraction -or $AutoExerciseAutoFishingPhase -or $AutoExerciseZoom)) {
    throw '-AutoExerciseDiagnosticsSnapshot is a standalone G3 case and cannot be combined with another action case that exports its own diagnostics snapshot.'
}
$qaCustomEntityContractEnabled = [bool]$StageQaHost -and [bool]$AutoExerciseCustomEntityApis
$qaFishRoeTooltipObservationEnabled = [bool]$StageQaHost -and ([bool]$AutoExerciseFishRoeTooltip -or [bool]$AutoExerciseExperimentalHooks)
$qaDiagnosticsSnapshotEnabled = [bool]$StageQaHost -and [bool]$AutoExerciseDiagnosticsSnapshot
$qaG3SaveScenarioRequested = $qaCustomEntityContractEnabled -or $qaFishRoeTooltipObservationEnabled -or $qaDiagnosticsSnapshotEnabled
if ($qaG3SaveScenarioRequested -and $SaveSlot -le 0) {
    throw 'QA CustomEntity, FishRoe, and diagnostics scenarios require a positive -SaveSlot.'
}
$qaG3SaveLoadedBranchRequested = $SaveSlot -gt 0 -and ($qaG3SaveScenarioRequested -or [bool]$QaObserveSaveLoaded -or [bool]$QaObserveSaveSaved)
$qaDiagnosticsExpectedFeatureIds = @('FishRoeTooltip')
if ($ValidateQaG3RoutingOnly) {
    if (-not $StageQaHost -or -not $qaG3SaveLoadedBranchRequested) {
        throw '-ValidateQaG3RoutingOnly expected an explicitly staged G3 SaveLoaded route.'
    }
    [pscustomobject]@{
        QaG3SaveLoadedBranchRequested = $qaG3SaveLoadedBranchRequested
        CustomEntityContractEnabled = $qaCustomEntityContractEnabled
        FishRoeTooltipObservationEnabled = $qaFishRoeTooltipObservationEnabled
        DiagnosticsSnapshotEnabled = $qaDiagnosticsSnapshotEnabled
        DiagnosticsExpectedFeatureIds = @($qaDiagnosticsExpectedFeatureIds)
        WaitForManualExit = [bool]$WaitForManualExit
    } | ConvertTo-Json -Depth 4
    $SmokeRunnerExitCode = [int](0); return
}
if ($AutoFishingMonoGate -and -not $AutoExerciseAutoFishingPhase) {
    throw '-AutoFishingMonoGate requires -AutoExerciseAutoFishingPhase.'
}
if ($AutoExerciseLegacyFishingCompatibility -and $AutoExerciseAutoFishingPhase) {
    throw '-AutoExerciseLegacyFishingCompatibility and -AutoExerciseAutoFishingPhase are separate smoke cases and cannot run together.'
}
$autoFishingPerformanceFishRun = [bool]$AutoFishingPerformance -and $AutoFishingPerformanceTargetFish -gt 0
if ($autoFishingPerformanceFishRun -and -not $batch5GcLadderAutoFishing) {
    $autoFishingScenarioEffective = 'CombinedInstantSkip'
    $AutoFishingCastChargeRatio = 0
}
$autoFishingPerformanceBaseline = [bool]$AutoFishingPerformance -and $AutoFishingPerformanceProfile -ne 'FishLoop'
$autoFishingNoDemandFrameProfile = $autoFishingPerformanceBaseline -and
    $AutoFishingPerformanceProfile -eq 'InactiveNoConsumer' -and
    $AutoFishingPerformanceTargetFrames -gt 0
$autoFishingMovementRequested = [bool]$AutoExerciseAutoFishingMovementCancel -or ($autoFishingPerformanceBaseline -and $AutoFishingPerformanceProfile -eq 'EnabledNoRod')
$autoFishingScenarioRequestsInstantBite = (-not $autoFishingPerformanceBaseline) -and (@('InstantBite','InstantSkip','CombinedInstantSkip','CombinedInstantComplete') -contains $autoFishingScenarioEffective)
$autoFishingScenarioRequestsSkip = (-not $autoFishingPerformanceBaseline) -and (@('SkipMiniGame','InstantSkip','CombinedInstantSkip') -contains $autoFishingScenarioEffective)
$autoFishingScenarioRequestsComplete = (-not $autoFishingPerformanceBaseline) -and (@('DefaultLoop','InstantBite','FastAnimations','CombinedInstantComplete') -contains $autoFishingScenarioEffective)
$autoFishingScenarioRequestsAnimationSpeed = (-not $autoFishingPerformanceBaseline) -and (@('FastAnimations','CombinedInstantSkip','CombinedInstantComplete') -contains $autoFishingScenarioEffective)
$autoFishingScenarioRequestsCastCharge = (-not $autoFishingPerformanceBaseline) -and $AutoFishingCastChargeRatio -gt 0
$autoFishingToggleKeyEffective = if ($Batch6AutoFishingManagerLifecycle -and $Batch6AutoFishingManagerMode -eq 'SameProcessDisable') {
    'F6'
}
elseif ([string]::IsNullOrWhiteSpace($AutoFishingToggleKey)) {
    'None'
}
else {
    $AutoFishingToggleKey.Trim()
}

$qaG6LifecycleCases = New-Object 'System.Collections.Generic.List[string]'
if ($StageQaHost) {
    foreach ($entry in @(
        [ordered]@{ Requested = [bool]$AutoExerciseModOwnerLifetime; Id = 'ModOwnerLifetime' },
        [ordered]@{ Requested = [bool]$AutoExerciseAutoFishingMovementCancel; Id = 'AutoFishingMovementCancel' },
        [ordered]@{ Requested = [bool]$AutoExerciseLegacyFishingCompatibility; Id = 'LegacyFishingCompatibility' },
        [ordered]@{ Requested = [bool]$AutoExerciseAutoFishingPhase; Id = 'AutoFishingPhase' },
        [ordered]@{ Requested = ([bool]$batch5GcLadderAutoFishing -and $Batch5GcLadderLevel -eq 'L0'); Id = 'AutoFishingNativeControl' },
        [ordered]@{ Requested = ([bool]$batch5GcLadderAutoFishing -and $Batch5GcLadderLevel -in @('L1','L2','L3') -and -not [bool]$AutoExerciseAutoFishingPhase); Id = 'AutoFishingPhase' },
        [ordered]@{ Requested = ([bool]$batch5GcLadderAutoFishing -and $Batch5GcLadderLevel -eq 'L4'); Id = 'AutoFishingDisableRecovery' },
        [ordered]@{ Requested = ([bool]$batch5GcLadderAutoFishing -and $Batch5GcLadderLevel -eq 'L5'); Id = 'AutoFishingTitleCycle' },
        [ordered]@{ Requested = [bool]$Batch6AutoFishingPilot; Id = 'Batch6AutoFishingPilot' },
        [ordered]@{ Requested = [bool]$Batch6AutoFishingManagerLifecycle; Id = 'Batch6AutoFishingManagerLifecycle' },
        [ordered]@{ Requested = [bool]$AutoExerciseSaveLoadCycle; Id = 'SaveLoadCycle' },
        [ordered]@{ Requested = [bool]$AutoExerciseSaveLoadCyclePendingPressure; Id = 'SaveLoadPendingPressure' },
        [ordered]@{ Requested = ($TitleIdleBeforeSaveSeconds -gt 0 -and -not [bool]$AutoExerciseSaveLoadCycle -and -not [bool]$AutoExerciseSaveLoadCyclePendingPressure); Id = 'LongTitleLoad' },
        [ordered]@{ Requested = ($MoreSavesFixed12AcceptancePhase -eq 'EnabledLifecycle'); Id = 'MoreSavesFixed12EnabledLifecycle' },
        [ordered]@{ Requested = ($MoreSavesFixed12AcceptancePhase -eq 'DisabledCold'); Id = 'MoreSavesFixed12DisabledCold' },
        [ordered]@{ Requested = ($MoreSavesFixed12AcceptancePhase -eq 'ReenabledCold'); Id = 'MoreSavesFixed12ReenabledCold' }
    )) {
        if ([bool]$entry.Requested) {
            $qaG6LifecycleCases.Add([string]$entry.Id) | Out-Null
        }
    }
}
$qaG6AnyRequested = $qaG6LifecycleCases.Count -gt 0 -or $SmokeRootIsolationProfile -ne 'None' -or
    $SmokeOwnerRootIsolationProfile -ne 'None' -or $SmokeNativeLoadContinuationProbe -ne 'None'
if ($Batch6AutoFishingPilot -and ($qaG6LifecycleCases.Count -ne 1 -or $qaG6LifecycleCases[0] -cne 'Batch6AutoFishingPilot' -or
    $SmokeRootIsolationProfile -ne 'None' -or $SmokeOwnerRootIsolationProfile -ne 'None' -or $SmokeNativeLoadContinuationProbe -ne 'None')) {
    throw 'Batch 6 AutoFishing pilot is an independent cold G6 route and cannot be combined with another G6 case or isolation probe.'
}
if ($Batch6AutoFishingManagerLifecycle -and ($qaG6LifecycleCases.Count -ne 1 -or $qaG6LifecycleCases[0] -cne 'Batch6AutoFishingManagerLifecycle' -or
    $SmokeRootIsolationProfile -ne 'None' -or $SmokeOwnerRootIsolationProfile -ne 'None' -or $SmokeNativeLoadContinuationProbe -ne 'None')) {
    throw 'Batch 6 AutoFishing Manager lifecycle is an independent cold G6 route and cannot be combined with another G6 case or isolation probe.'
}
if ($moreSavesFixed12AcceptanceRequested -and
    ($qaG6LifecycleCases.Count -ne 1 -or
     -not $qaG6LifecycleCases[0].StartsWith('MoreSavesFixed12', [System.StringComparison]::Ordinal) -or
     $SmokeRootIsolationProfile -ne 'None' -or
     $SmokeOwnerRootIsolationProfile -ne 'None' -or
     $SmokeNativeLoadContinuationProbe -ne 'None')) {
    throw 'A MoreSaves fixed-12 phase is an independent cold G6 route and cannot be combined with another G6 case or isolation probe.'
}
if ($qaG6LifecycleCases.Count -gt 0 -and ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -le 0)) {
        throw 'G6 fishing/lifecycle/save-load cases require -StageQaHost and an explicit positive -SaveSlot.'
}
if ($QaObserveEquipmentSlotsUi -and
    $MoreEquipmentSlotsTransitionPhase -ne 'U1') {
    $otherG4Requested = $qaG4AnyTitleUiEnabled -or $qaG4OfficialModUiEnabled -or $qaG4PauseMenuLayoutEnabled -or
        $qaG4DebugConsoleEnabled -or $qaG4SaveSlotsPagingEnabled -or $qaG4AnimalObservationEnabled -or
        $qaG4AudioObservationEnabled -or $qaG4HatchVoiceEnabled -or $qaG4CameraPlayableEnabled -or
        $qaG4ContentMetadataObservationEnabled -or $qaG4ExternalPlayerInputObservationEnabled -or
        $qaG4ProductOwnerRefreshEnabled -or $qaG4AdvancedProductOwnerDeactivationEnabled -or
        ((-not $moreEquipmentSlots100UiAcceptanceRequested) -and $qaG4TitleLifecycleEnabled)
    $otherG5Requested = if ($moreEquipmentSlots100UiAcceptanceRequested) {
        $qaG5WorldMutationCases.Count -ne 1 -or
            [string]$qaG5WorldMutationCases[0] -cne 'MoreEquipmentSlotsNoNativeSave'
    }
    else {
        $qaG5AnyRequested
    }
    $otherG3Requested = $qaCustomEntityContractEnabled -or $qaFishRoeTooltipObservationEnabled -or
        $qaDiagnosticsSnapshotEnabled -or [bool]$QaObserveSaveSaved -or [bool]$QaObserveWorkshopReloadCompleted
    if ($otherG4Requested -or $otherG5Requested -or $qaG6AnyRequested -or $otherG3Requested) {
        throw '-QaObserveEquipmentSlotsUi is an independent cold-run G4 route and cannot be combined with other G3/G4/G5/G6 cases.'
    }
    if ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -le 0) {
        throw '-QaObserveEquipmentSlotsUi requires an explicit positive -SaveSlot.'
    }
    if ($OfficialModProfile -notin @('Published11','Local11') -or -not $IsolateAllOfficialMods -or $IncludeHookProbe) {
        throw '-QaObserveEquipmentSlotsUi requires the exact Published11 or Local11 isolated profile and no HookProbe.'
    }
}
$g6SaveLoadCycleCountEffective = if ($AutoExerciseSaveLoadCycle -and $SaveLoadCycleCount -le 0) { 3 } else { $SaveLoadCycleCount }
$g6SaveLoadCycleInitialIdleEffective = if ($AutoExerciseSaveLoadCycle) { [Math]::Max(1, $SaveLoadCycleInitialTitleIdleSeconds) } else { 0 }
$g6PendingPressureSecondsEffective = if ($AutoExerciseSaveLoadCyclePendingPressure) { if ($SaveLoadCyclePendingPressureSeconds -le 0) { 1200 } else { $SaveLoadCyclePendingPressureSeconds } } else { 0 }
if ($ValidateQaG6RoutingOnly) {
    if (-not $StageQaHost -or -not $qaG6AnyRequested) {
        throw '-ValidateQaG6RoutingOnly expected an explicitly staged G6 route.'
    }
    [pscustomobject]@{
        ProtocolVersion = 7
        StageQaHost = [bool]$StageQaHost
        SaveSlot = $SaveSlot
        SaveSlotExplicit = [bool]$SmokeRunnerBoundParameters.ContainsKey('SaveSlot')
        Cases = @($qaG6LifecycleCases.ToArray())
        RootIsolationProfile = $SmokeRootIsolationProfile
        OwnerRootIsolationProfile = $SmokeOwnerRootIsolationProfile
        NativeLoadContinuationProbe = $SmokeNativeLoadContinuationProbe
        Batch6AutoFishingEnabled = [bool]$Batch6AutoFishingPilot
        Batch6AutoFishingLevel = $Batch6AutoFishingLevel
        Batch6AutoFishingScenario = $Batch6AutoFishingScenario
        Batch6AutoFishingFormal = [bool]$Batch6AutoFishingFormal
        Batch6AutoFishingFormalContract = $Batch6AutoFishingFormalContract
        Batch6AutoFishingCastChargeRatio = $Batch6AutoFishingCastChargeRatio
        Batch6AutoFishingManualMovementCancel = [bool]$Batch6AutoFishingManualMovementCancel
        Batch6AutoFishingToggleKey = $autoFishingToggleKeyEffective
        Batch6AutoFishingExpectedPackageSha256 = $Batch6AutoFishingExpectedPackageSha256
        Batch6AutoFishingExpectedEntrySha256 = $Batch6AutoFishingExpectedEntrySha256
        Batch6AutoFishingExpectedManifestSha256 = $Batch6AutoFishingExpectedManifestSha256
        Batch6AutoFishingExpectedPolicySha256 = $Batch6AutoFishingExpectedPolicySha256
        Batch6AutoFishingManagerEnabled = [bool]$Batch6AutoFishingManagerLifecycle
        Batch6AutoFishingManagerMode = $Batch6AutoFishingManagerMode
        Batch6AutoFishingManagerProductRoot = $Batch6AutoFishingManagerProductRoot
        Batch6AutoFishingManagerMarkerSha256 = $Batch6AutoFishingManagerMarkerSha256
        Batch6AutoFishingManagerExpectedPackageSha256 = $Batch6AutoFishingManagerExpectedPackageSha256
        Batch6AutoFishingManagerExpectedEntrySha256 = $Batch6AutoFishingManagerExpectedEntrySha256
        Batch6AutoFishingManagerExpectedManifestSha256 = $Batch6AutoFishingManagerExpectedManifestSha256
        Batch6AutoFishingManagerExpectedPolicySha256 = $Batch6AutoFishingManagerExpectedPolicySha256
        MoreSavesFixed12AcceptancePhase = $MoreSavesFixed12AcceptancePhase
        MoreSavesFixed12OfficialLocalRootMode = $moreSavesFixed12OfficialLocalRootMode
        MoreSavesFixed12OfficialLocalProductRoot = $moreSavesFixed12OfficialLocalProductRoot
        ExclusiveQaG6Owner = $true
        ProductionSaveLoadOrder = 'unchanged'
        CleanupReceipt = 'title+shutdown+runner-post-exit-transaction'
    } | ConvertTo-Json -Depth 4
    $SmokeRunnerExitCode = [int](0); return
}
