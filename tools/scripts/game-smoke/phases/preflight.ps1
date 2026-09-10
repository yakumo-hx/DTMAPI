if ($WaitForManualExit) {
    if ($SaveSlot -eq 0) {
        if ($StageQaHost -or $QaObserveSaveLoaded) {
            throw 'Title-only manual observation uses -SaveSlot 0 without -StageQaHost or -QaObserveSaveLoaded.'
        }
    }
    elseif ($SaveSlot -lt 0 -or -not $StageQaHost -or -not $QaObserveSaveLoaded) {
        throw 'Manual observation after loading requires -StageQaHost, -QaObserveSaveLoaded, and a positive UI -SaveSlot; use -SaveSlot 0 for title-only observation.'
    }
}

$debugConsoleSaveAcceptanceRequested =
    $DebugConsoleSaveAcceptancePhase -ne 'None'
$debugConsoleRequiredSaveSlot = 10
$debugConsoleTestRequested =
    [bool]$debugConsoleSaveAcceptanceRequested -or
    [bool]$AutoExerciseDebugConsole -or
    [bool]$AutoExerciseDebugConsoleMouseGive -or
    [bool]$AutoExerciseDebugInventory -or
    [bool]$AutoExerciseDebugWeather -or
    [bool]$AutoExerciseDebugTeleport -or
    [bool]$AutoExerciseDebugTime -or
    [bool]$AutoExerciseDebugMovement -or
    [bool]$AutoExerciseAdvancedDebug -or
    [bool]$RequireExternalPlayerInputGate -or
    [bool]$AssertNoQaUiEvidence -or
    $SmokeOwnerRootIsolationProfile -in @(
        'YConsoleZoomNoInput',
        'YConsoleZoomNoEvents',
        'YConsoleZoomNoInputEvents',
        'YConsoleZoomNoConfig',
        'YConsoleNoInput') -or
    @($AdvancedProductOwnerDeactivationOwnerIds | Where-Object {
        [string]$_ -ceq 'DTMAPI.DebugConsoleMod'
    }).Count -gt 0
$moreEquipmentSlotsTransitionRequested =
    $MoreEquipmentSlotsTransitionPhase -ne 'None'
$moreEquipmentSlots100UiAcceptanceRequested =
    [bool]$QaObserveEquipmentSlotsUi -and
    [bool]$AutoExerciseMoreEquipmentSlotsNoNativeSave -and
    -not [bool]$moreEquipmentSlotsTransitionRequested
$moreSavesFixed12AcceptanceRequested =
    $MoreSavesFixed12AcceptancePhase -ne 'None'
$moreSavesFixed12OfficialLocalRootMode =
    if ($moreSavesFixed12AcceptanceRequested) {
        'LiveOfficialUploadRoot'
    }
    else {
        'Default'
    }
$moreSavesFixed12OfficialLocalProductRoot =
    if ($moreSavesFixed12AcceptanceRequested) {
        [System.IO.Path]::GetFullPath(
            (Join-Path (
                [Environment]::GetFolderPath('UserProfile')) `
                'AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_MoreSaves'))
    }
    else {
        ''
    }
$moreEquipmentSlotsColdRecoveryTransitionRequested =
    $MoreEquipmentSlotsTransitionPhase -in @(
        'ColdPrepare',
        'ColdCommit',
        'ColdObserve')

if ($debugConsoleTestRequested -and
    ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or
     $SaveSlot -ne $debugConsoleRequiredSaveSlot)) {
    throw ('Y-console test routes require explicit -SaveSlot 10 ' +
        '(game UI slot 10 / native archive index 9).')
}

if ($debugConsoleSaveAcceptanceRequested) {
    if ($AutoExerciseInstantSave) {
        throw '-DebugConsoleSaveAcceptancePhase is an independent save-commit route and cannot combine with -AutoExerciseInstantSave.'
    }
    if (-not $StageQaHost -or
        (-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or
        $SaveSlot -ne $debugConsoleRequiredSaveSlot -or
        [string]::IsNullOrWhiteSpace($DisposableSaveFixtureRoot)) {
        throw '-DebugConsoleSaveAcceptancePhase requires StageQaHost, explicit SaveSlot 10, and an isolated disposable fixture.'
    }
    if ($DebugConsoleSaveAcceptancePhase -eq 'ColdObserve') {
        if ($SaveTestMode -ne 'NoNativeSave' -or
            (-not $SmokeRunnerBoundParameters.ContainsKey('ExpectedDebugConsoleMoney')) -or
            $ExpectedDebugConsoleMoney -lt 0) {
            throw 'DebugConsole ColdObserve requires SaveTestMode NoNativeSave and exact non-negative ExpectedDebugConsoleMoney from the preceding mutation log.'
        }
    }
    elseif ($SaveTestMode -ne 'NativeSaveExpected') {
        throw 'DebugConsole FailedMutation and SuccessfulMutation require SaveTestMode NativeSaveExpected.'
    }
    if ($DebugConsoleSaveAcceptancePhase -eq 'SuccessfulMutation' -and
        ((-not $SmokeRunnerBoundParameters.ContainsKey('ExpectedDebugConsoleMoney')) -or
         $ExpectedDebugConsoleMoney -lt 0)) {
        throw 'DebugConsole SuccessfulMutation requires exact ExpectedDebugConsoleMoney from the failed-mutation cold observer.'
    }
}

if ($AssertNoQaUiEvidence) {
    if ($AssertPublishedProductCombination -or $AssertPublishedProductsDisabled) {
        throw '-AssertNoQaUiEvidence is the ordinary-player lane; run the staged-QA enabled/disabled product gates as separate cold runs.'
    }
    $noQaUiAllowedParameters = @(
        'SaveSlot', 'TimeoutSeconds', 'UseSteam', 'SkipInstall', 'SkipBuild', 'SaveTestMode',
        'OfficialModProfile', 'OfficialModProfileExtraEnabledIds',
        'IsolateAllOfficialMods', 'AssertNoQaUiEvidence',
        'Issue011Acceptance', 'AutoDriveNoQaAnimalViewer',
        'ValidateNoQaUiEvidenceGateOnly', 'AutoExitAfterSecondsOverride',
        'ExternalPlayerInputHoldMilliseconds', 'FatalWindowCrashDumpGraceSeconds',
        'FatalWindowProcessDumpMode', 'FatalWindowPostCloseCrashDumpWaitSeconds'
    )
    $noQaUiUnexpectedParameters = @($SmokeRunnerBoundParameters.Keys | Where-Object {
        $noQaUiAllowedParameters -notcontains [string]$_
    })
    if ($noQaUiUnexpectedParameters.Count -gt 0) {
        throw ('-AssertNoQaUiEvidence is the exact ordinary-player no-QA UI gate and cannot be combined with: ' +
            [string]::Join(', ', @($noQaUiUnexpectedParameters | Sort-Object)) + '.')
    }
    if (@($OfficialModProfileExtraEnabledIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -ne 0) {
        throw '-AssertNoQaUiEvidence does not accept ExtraEnabledIds; the gate must select exactly one isolated eleven-product profile.'
    }
    if ($OfficialModProfile -notin @('Published11','Local11') -or -not $IsolateAllOfficialMods) {
        throw '-AssertNoQaUiEvidence requires -OfficialModProfile Published11 or Local11 and -IsolateAllOfficialMods so the three UI products run inside one exact eleven-product player profile.'
    }
    if (-not $UseSteam -or $DirectExe -or -not $SkipInstall -or $IncludeHookProbe -or $StageQaHost) {
        throw '-AssertNoQaUiEvidence requires explicit -UseSteam -SkipInstall, no -DirectExe, no -IncludeHookProbe, and no -StageQaHost.'
    }
    if ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne $debugConsoleRequiredSaveSlot) {
        throw '-AssertNoQaUiEvidence requires explicit -SaveSlot 10.'
    }
    if ($Issue011Acceptance -and $SaveTestMode -cne 'NoNativeSave') {
        throw '-Issue011Acceptance requires -SaveTestMode NoNativeSave.'
    }
}
elseif ($AutoDriveNoQaAnimalViewer) {
    throw '-AutoDriveNoQaAnimalViewer is an explicit ordinary-player input option and requires -AssertNoQaUiEvidence.'
}
elseif ($Issue011Acceptance) {
    throw '-Issue011Acceptance requires -AssertNoQaUiEvidence and its exact ordinary-player Local11 lane.'
}

$qaAutomationRequested = [bool]$AutoSaveAfterLoad -or [bool]$AutoReloadMods -or
    [bool]$AutoExerciseExperimentalHooks -or [bool]$AutoExerciseActionSpeedTool -or
    [bool]$AutoExerciseActionSpeedConfigApply -or [bool]$AutoExerciseActionSpeedInteraction -or
    [bool]$AutoExerciseOneActionResourceHit -or [bool]$AutoExerciseOneActionWrongTool -or
    [bool]$AutoExerciseOneActionFuelFeed -or [bool]$AutoExerciseOneActionVegetation -or
    [bool]$AssertAdvancedProductOwnerDeactivation -or
    [bool]$AutoPressOneActionMenuKey -or
    [bool]$AutoExerciseAutoFishingPhase -or [bool]$AutoExerciseLegacyFishingCompatibility -or
    [bool]$AutoExerciseAutoFishingMiniGameComplete -or [bool]$AutoFishingPerformance -or
    [bool]$AutoExercisePauseMenuLayout -or [bool]$AutoExerciseTitleButtonLifecycle -or
    [bool]$AutoExerciseModOwnerLifetime -or [bool]$AutoExerciseSaveLoadCycle -or
    [bool]$AutoExercisePreLoadGcProbe -or [bool]$AutoExerciseSaveLoadCyclePendingPressure -or
    [bool]$AutoExerciseInstantSave -or [bool]$debugConsoleSaveAcceptanceRequested -or
    [bool]$AutoPressAutoFishingHotkey -or
    [bool]$AutoExerciseAutoFishingMovementCancel -or [bool]$AutoOpenTitleSettingsMenu -or
    [bool]$AutoOpenTitleSettingsStatusPage -or [bool]$AutoOpenTitleSettingsManagerMvp -or
    [bool]$AutoOpenOfficialModUi -or [bool]$AutoOpenAnimalPanel -or
    [bool]$AutoExerciseDebugConsole -or [bool]$AutoExerciseDebugConsoleMouseGive -or
    [bool]$AutoExerciseDebugInventory -or [bool]$AutoExerciseDebugWeather -or
    [bool]$AutoExerciseDebugTeleport -or [bool]$AutoExerciseDebugTime -or
    [bool]$AutoExerciseDebugMovement -or [bool]$AutoExerciseAdvancedDebug -or
    [bool]$AutoExerciseNewContentApis -or [bool]$QaObserveContentMetadata -or [bool]$QaObserveEquipmentSlotsUi -or
    [bool]$AutoExerciseMineContentApis -or [bool]$AutoExerciseZoom -or
    [bool]$AutoExerciseZoomProductNative -or
    [bool]$AutoExerciseChestLocatorEnhancer -or
    [bool]$AutoExerciseMoreEquipmentSlots -or
    [bool]$SetupMoreEquipmentSlotsCommittedShield -or
    [bool]$DamageMoreEquipmentSlotsCommittedShieldNoNativeSave -or
    [bool]$BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave -or
    [bool]$ObserveMoreEquipmentSlotsInterruptedCandidateRecovery -or
    [bool]$AutoExerciseMoreEquipmentSlotsNoNativeSave -or
    [bool]$ObserveMoreEquipmentSlotsNoNativeSaveCold -or
    [bool]$AssertMoreEquipmentSlotsColdRecovery -or
    [bool]$moreEquipmentSlotsTransitionRequested -or
    [bool]$AutoExerciseMoreSavesOfficialSaveUi -or [bool]$moreSavesFixed12AcceptanceRequested -or [bool]$AutoExerciseStrongPlantingGun -or
    [bool]$AutoExerciseCropHarvestingApi -or [bool]$AutoExerciseCustomEntityApis -or
    [bool]$AutoExerciseFishRoeTooltip -or [bool]$AutoExerciseDiagnosticsSnapshot -or
    [bool]$QaObserveSaveLoaded -or [bool]$QaObserveSaveSaved -or
    [bool]$QaObserveWorkshopReloadCompleted -or [bool]$AutoExerciseAudioReplacement -or
    $Batch5GcLadderDomain -ne 'None' -or [bool]$Batch6AutoFishingPilot -or [bool]$Batch6AutoFishingManagerLifecycle -or
    [bool]$AutoExerciseHatchAnimalVoice -or $TitleIdleBeforeSaveSeconds -gt 0 -or
    $SmokeRootIsolationProfile -ne 'None' -or $SmokeOwnerRootIsolationProfile -ne 'None' -or
    $SmokeNativeLoadContinuationProbe -ne 'None'
if ($qaAutomationRequested -and -not $StageQaHost) {
    throw 'All automated fixture scenarios require -StageQaHost after the embedded harness removal.'
}

if ($Batch6AutoFishingPilot) {
    if ($Batch6AutoFishingSampleSeconds -gt $Batch6AutoFishingMeasureSeconds) {
        throw '-Batch6AutoFishingSampleSeconds cannot exceed -Batch6AutoFishingMeasureSeconds.'
    }
    foreach ($hash in @(
        $Batch6AutoFishingExpectedPackageSha256,
        $Batch6AutoFishingExpectedEntrySha256,
        $Batch6AutoFishingExpectedManifestSha256,
        $Batch6AutoFishingExpectedPolicySha256)) {
        if ([string]$hash -notmatch '^[0-9A-Fa-f]{64}$') {
            throw 'Batch 6 AutoFishing pilot requires all four exact expected SHA-256 values.'
        }
    }
    if ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 5) {
        throw 'Batch 6 AutoFishing pilot requires explicit -SaveSlot 5.'
    }
    if ($Batch6AutoFishingFormal -and $Batch6AutoFishingFormalContract -eq 'LongRun' -and
        ($Batch6AutoFishingMeasureSeconds -ne 600 -or $Batch6AutoFishingSampleSeconds -ne 30 -or
         $Batch6AutoFishingWarmupFish -ne 5 -or $Batch6AutoFishingTargetFish -ne 10)) {
        throw 'Formal Batch 6 AutoFishing LongRun requires exactly MeasureSeconds=600, SampleSeconds=30, WarmupFish=5, TargetFish=10.'
    }
    if ($Batch6AutoFishingFormal -and $Batch6AutoFishingFormalContract -eq 'Behavior' -and
        ($Batch6AutoFishingMeasureSeconds -ne 1 -or $Batch6AutoFishingSampleSeconds -ne 1 -or
         $Batch6AutoFishingWarmupFish -ne 0 -or $Batch6AutoFishingTargetFish -ne 1)) {
        throw 'Formal Batch 6 AutoFishing Behavior requires exactly MeasureSeconds=1, SampleSeconds=1, WarmupFish=0, TargetFish=1.'
    }
    if ($Batch6AutoFishingManualMovementCancel -and
        (-not $Batch6AutoFishingFormal -or $Batch6AutoFishingFormalContract -ne 'Behavior' -or $Batch6AutoFishingLevel -ne 'L1')) {
        throw 'Batch 6 AutoFishing manual movement cancel requires formal Behavior on the real-product L1 level.'
    }
    if ($Batch6AutoFishingManualMovementCancel -and
        -not [string]::Equals($AutoFishingToggleKey.Trim(), 'F7', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Batch 6 AutoFishing manual movement cancel requires -AutoFishingToggleKey F7 to prove the actual rebound path.'
    }
}

if ($Batch6AutoFishingManagerLifecycle) {
    throw '-Batch6AutoFishingManagerLifecycle is retired for DTMAPI 0.6.0: functional CodeMods use official Local/Workshop sources and every enablement, source, version, or order change takes effect after restart; the smoke runner must not mutate <game>/Mods/dtmapi.disabled.'
    if ($Batch6AutoFishingPilot) {
        throw 'Batch 6 AutoFishing Manager lifecycle and the formal product pilot must run in independent cold processes.'
    }
    if ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 5) {
        throw 'Batch 6 AutoFishing Manager lifecycle requires explicit -SaveSlot 5.'
    }
    foreach ($hash in @(
        $Batch6AutoFishingManagerMarkerSha256,
        $Batch6AutoFishingManagerExpectedPackageSha256,
        $Batch6AutoFishingManagerExpectedEntrySha256,
        $Batch6AutoFishingManagerExpectedManifestSha256,
        $Batch6AutoFishingManagerExpectedPolicySha256)) {
        if ([string]$hash -notmatch '^[0-9A-Fa-f]{64}$') {
            throw 'Batch 6 AutoFishing Manager lifecycle requires the exact marker and four SDK SHA-256 values.'
        }
    }
    if ([string]::IsNullOrWhiteSpace($Batch6AutoFishingManagerProductRoot) -or
        -not [System.IO.Path]::IsPathRooted($Batch6AutoFishingManagerProductRoot) -or
        [System.IO.Path]::GetFileName(([string]$Batch6AutoFishingManagerProductRoot).TrimEnd([char]92, [char]47)) -cne 'Yuuka.DTMAPI.AutoFishing') {
        throw 'Batch 6 AutoFishing Manager lifecycle requires the exact absolute managed AutoFishing product root.'
    }
    try { $batch6ManagerMarkerBytes = [Convert]::FromBase64String($Batch6AutoFishingManagerMarkerContentBase64) }
    catch { throw 'Batch 6 AutoFishing Manager lifecycle marker content must be valid Base64.' }
    $batch6ManagerMarkerSha = [System.Security.Cryptography.SHA256]::Create()
    try { $batch6ManagerMarkerActualSha256 = ([BitConverter]::ToString($batch6ManagerMarkerSha.ComputeHash($batch6ManagerMarkerBytes))).Replace('-', '') }
    finally { $batch6ManagerMarkerSha.Dispose() }
    if (-not [string]::Equals($batch6ManagerMarkerActualSha256, $Batch6AutoFishingManagerMarkerSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Batch 6 AutoFishing Manager lifecycle marker Base64 does not match its exact SHA-256.'
    }
}

if ($AutoExercisePreLoadGcProbe -and -not $AutoExerciseSaveLoadCycle) {
    throw '-AutoExercisePreLoadGcProbe requires -AutoExerciseSaveLoadCycle.'
}
if ($AutoExerciseAutoFishingMovementCancel) {
    throw '-AutoExerciseAutoFishingMovementCancel is retired because its historical single-session fixture cannot prove exact phases or rebound keybinding. Use run-batch6-autofishing-behavior-matrix.ps1 formal ManualMovementCancel instead.'
}
if ($ExpectOilAbsent -and -not $AutoExerciseNewContentApis -and -not $QaObserveContentMetadata) {
    throw '-ExpectOilAbsent requires -AutoExerciseNewContentApis or -QaObserveContentMetadata.'
}
if ($ExpectOilAbsent -and $AutoExerciseMineContentApis) {
    throw '-ExpectOilAbsent cannot be combined with -AutoExerciseMineContentApis.'
}
if ($OilOnly -and -not $AutoExerciseNewContentApis) {
    throw '-OilOnly requires -AutoExerciseNewContentApis.'
}
if ($OilOnly -and $ExpectOilAbsent) {
    throw '-OilOnly cannot be combined with -ExpectOilAbsent.'
}
if ($OilOnly -and $AutoExerciseMineContentApis) {
    throw '-OilOnly cannot be combined with -AutoExerciseMineContentApis.'
}
if ($AssertProductOwnerRefresh -and -not $AutoReloadMods) {
    throw '-AssertProductOwnerRefresh requires -AutoReloadMods.'
}
if ($AssertProductOwnerRefresh -and -not $StageQaHost) {
    throw '-AssertProductOwnerRefresh requires -StageQaHost after the embedded harness removal.'
}
if ($IsolateAllOfficialMods -and $OfficialModProfile -eq 'Current') {
    throw '-IsolateAllOfficialMods requires an explicit non-Current OfficialModProfile.'
}
if ($OfficialModProfile -eq 'Local11' -and -not $IsolateAllOfficialMods) {
    throw '-OfficialModProfile Local11 requires -IsolateAllOfficialMods so every other Local.* and Workshop.* entry is disabled for the current-worktree acceptance lane.'
}
if ($AssertProductOwnerRefresh -and $OfficialModProfile -eq 'CoreOnly') {
    throw '-AssertProductOwnerRefresh requires an ordinary non-CoreOnly official Mod profile.'
}
if ($AssertProductOwnerRefresh -and $TitleIdleBeforeSaveSeconds -lt 13) {
    throw '-AssertProductOwnerRefresh requires -TitleIdleBeforeSaveSeconds 13 or greater so the 12-second refresh completes before save load.'
}
if ($AssertProductOwnerRefresh -and -not $AutoExerciseActionSpeedConfigApply) {
    throw '-AssertProductOwnerRefresh requires -AutoExerciseActionSpeedConfigApply for the ActionSpeed restore boundary.'
}
if ($AssertProductOwnerRefresh) {
    foreach ($requiredId in @('Local.Yuuka_DTMAPI_ActionSpeed', 'Local.DTMAPI_CropHarvestingQA')) {
        if ($OfficialModProfileExtraEnabledIds -notcontains $requiredId) {
            throw "-AssertProductOwnerRefresh requires OfficialModProfileExtraEnabledIds to include $requiredId."
        }
    }
}
if ($AssertAdvancedProductOwnerDeactivation) {
    if (-not $StageQaHost -or -not $AutoExerciseTitleButtonLifecycle) {
        throw '-AssertAdvancedProductOwnerDeactivation requires -StageQaHost and -AutoExerciseTitleButtonLifecycle so Loader cleanup is the final post-title product action.'
    }
    if (@($AdvancedProductOwnerDeactivationOwnerIds).Count -eq 0) {
        throw '-AssertAdvancedProductOwnerDeactivation requires an explicit -AdvancedProductOwnerDeactivationOwnerIds set.'
    }
    $ownerEvidenceRequirements = @{
        'Yuuka.DTMAPI.ActionSpeed' = [bool]$AutoExerciseActionSpeedInteraction
        'Yuuka.DTMAPI.OneActionComplete' = [bool]$AutoExerciseOneActionResourceHit
        'Yuuka.DTMAPI.FishBreedingAssistant' = [bool]$AutoExerciseFishRoeTooltip
        'Yuuka.DTMAPI.AnimalHusbandryProgress' = [bool]$AutoOpenAnimalPanel
        'DTMAPI.MoreSavesMod' = [bool]$AutoExerciseMoreSavesOfficialSaveUi
        'DTMAPI.ChestLocatorEnhancerMod' = [bool]$AutoExerciseChestLocatorEnhancer
        'DTMAPI.MoreEquipmentSlotsMod' = [bool]$AutoExerciseMoreEquipmentSlots
        'DTMAPI.StrongPlantingGunMod' = [bool]$AutoExerciseStrongPlantingGun
        'DTMAPI.ZoomMod' = [bool]$AutoExerciseZoomProductNative
        'DTMAPI.MineMod' = [bool]$AutoExerciseMineContentApis
        'DTMAPI.DebugConsoleMod' = [bool]$AutoExerciseDebugConsole
    }
    foreach ($ownerId in @($AdvancedProductOwnerDeactivationOwnerIds)) {
        if (-not $ownerEvidenceRequirements[[string]$ownerId]) {
            throw "-AssertAdvancedProductOwnerDeactivation requires the matching in-save behavior request for owner $ownerId."
        }
    }
    $advancedOwnerDeactivationSaveSlot =
        if (@($AdvancedProductOwnerDeactivationOwnerIds | Where-Object {
                [string]$_ -ceq 'DTMAPI.DebugConsoleMod'
            }).Count -gt 0) {
            $debugConsoleRequiredSaveSlot
        }
        else {
            3
        }
    if ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or
        $SaveSlot -ne $advancedOwnerDeactivationSaveSlot) {
        throw "-AssertAdvancedProductOwnerDeactivation requires explicit -SaveSlot $advancedOwnerDeactivationSaveSlot."
    }
    if (@($AdvancedProductOwnerDeactivationOwnerIds | Where-Object { $_ -eq 'DTMAPI.StrongPlantingGunMod' }).Count -gt 0 -and
        (-not [bool]$AutoExerciseSaveLoadCycle -or $SaveLoadCycleCount -ne 1)) {
        throw 'StrongPlantingGun owner deactivation requires -AutoExerciseSaveLoadCycle -SaveLoadCycleCount 1 for its bounded native save/title re-entry proof.'
    }
}
elseif (@($AdvancedProductOwnerDeactivationOwnerIds).Count -gt 0) {
    throw '-AdvancedProductOwnerDeactivationOwnerIds requires -AssertAdvancedProductOwnerDeactivation.'
}
if ($AutoExerciseMoreEquipmentSlots -and
    ((-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 3)) {
    throw '-AutoExerciseMoreEquipmentSlots requires explicit -SaveSlot 3 so its protected transaction can bind the reviewed fixture.'
}
if ($moreEquipmentSlotsTransitionRequested) {
    $transitionExpectedSaveMode =
        if ($MoreEquipmentSlotsTransitionPhase -in @('U4','ColdObserve')) {
            'NoNativeSave'
        }
        else {
            'NativeSaveExpected'
        }
    if (-not $StageQaHost -or
        (-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or
        $SaveSlot -ne 3 -or
        -not $DirectExe -or
        $UseSteam -or
        -not $SkipInstall -or
        [string]::IsNullOrWhiteSpace(
            $DisposableSaveFixtureRoot) -or
        -not $AutoExerciseTitleButtonLifecycle -or
        $SaveTestMode -ne $transitionExpectedSaveMode -or
        $OfficialModProfile -ne 'CoreOnly' -or
        -not $IsolateAllOfficialMods) {
        throw '-MoreEquipmentSlotsTransitionPhase requires StageQaHost, explicit slot 3, DirectExe, SkipInstall, an isolated disposable fixture, title lifecycle, CoreOnly isolation, NativeSaveExpected for U1/Prepare/U3/MigratedSave/ColdPrepare/ColdCommit and NoNativeSave for U4/ColdObserve.'
    }
    $transitionExtraIds =
        @($OfficialModProfileExtraEnabledIds |
            Where-Object {
                -not [string]::IsNullOrWhiteSpace($_)
            })
    if ($MoreEquipmentSlotsTransitionPhase -eq 'U1') {
        if (-not $QaObserveEquipmentSlotsUi -or
            $transitionExtraIds.Count -ne 1 -or
            $transitionExtraIds[0] -ne 'Workshop.3744059735') {
            throw 'MoreEquipmentSlots U1 requires the QA equipment UI observer and exactly Workshop.3744059735 as the retained 0.3.1 source.'
        }
    }
    elseif ($MoreEquipmentSlotsTransitionPhase -eq 'MigratedSave') {
        if ($QaObserveEquipmentSlotsUi -or
            $transitionExtraIds.Count -ne 1 -or
            $transitionExtraIds[0] -ne 'Local.DTMAPI_MoreEquipmentSlots') {
            throw 'MoreEquipmentSlots MigratedSave requires exactly the local ProductNative source and forbids the legacy equipment UI observer.'
        }
    }
    elseif ($QaObserveEquipmentSlotsUi -or
        $transitionExtraIds.Count -ne 0) {
        throw 'MoreEquipmentSlots Prepare/U3/U4/cold-recovery phases forbid the old consumer, Product, extra profile IDs, and the equipment UI observer.'
    }
    if ($AutoExerciseMoreEquipmentSlots -or
        $SetupMoreEquipmentSlotsCommittedShield -or
        $DamageMoreEquipmentSlotsCommittedShieldNoNativeSave -or
        $BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave -or
        $ObserveMoreEquipmentSlotsInterruptedCandidateRecovery -or
        $AutoExerciseMoreEquipmentSlotsNoNativeSave -or
        $ObserveMoreEquipmentSlotsNoNativeSaveCold -or
        $AssertMoreEquipmentSlotsColdRecovery) {
        throw 'MoreEquipmentSlots transition acceptance is an independent route.'
    }
}
$committedShieldRouteCount =
    ([int][bool]$SetupMoreEquipmentSlotsCommittedShield) +
    ([int][bool]$DamageMoreEquipmentSlotsCommittedShieldNoNativeSave) +
    ([int][bool]$BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave)
if ($committedShieldRouteCount -gt 1) {
    throw 'Committed-shield setup, damage rollback, and break/replace rollback are independent processes.'
}
if ($committedShieldRouteCount -gt 0 -and
    ($AutoExerciseMoreEquipmentSlots -or
     $AutoExerciseMoreEquipmentSlotsNoNativeSave -or
     $ObserveMoreEquipmentSlotsNoNativeSaveCold -or
     $ObserveMoreEquipmentSlotsInterruptedCandidateRecovery -or
     $AssertMoreEquipmentSlotsColdRecovery)) {
    throw 'Each committed-shield mutation is an independent MoreEquipmentSlots acceptance process.'
}
if ($SetupMoreEquipmentSlotsCommittedShield) {
    if ($SaveTestMode -ne 'NativeSaveExpected' -or
        (-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or
        $SaveSlot -ne 3 -or
        -not $StageQaHost -or
        [string]::IsNullOrWhiteSpace($DisposableSaveFixtureRoot)) {
        throw '-SetupMoreEquipmentSlotsCommittedShield requires NativeSaveExpected, explicit slot 3, StageQaHost, and a disposable fixture.'
    }
}
if ($AutoExerciseMoreEquipmentSlotsNoNativeSave) {
    if ($SaveTestMode -ne 'NoNativeSave' -or
        (-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or
        $SaveSlot -ne 3 -or
        -not $StageQaHost -or
        -not $AutoExerciseTitleButtonLifecycle) {
        throw '-AutoExerciseMoreEquipmentSlotsNoNativeSave requires -SaveTestMode NoNativeSave, explicit -SaveSlot 3, -StageQaHost, and -AutoExerciseTitleButtonLifecycle.'
    }
    if ($AutoExerciseMoreEquipmentSlots -or
        $ObserveMoreEquipmentSlotsNoNativeSaveCold) {
        throw '-AutoExerciseMoreEquipmentSlotsNoNativeSave is an independent first process and cannot combine with another MoreEquipmentSlots acceptance route.'
    }
}
$expectedMoreEquipmentSlotsCommittedSlots = ''
$committedShieldRollbackRoute =
    [bool]$DamageMoreEquipmentSlotsCommittedShieldNoNativeSave -or
    [bool]$BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave
$requiresMoreEquipmentSlotsExpectedCommitted =
    $committedShieldRollbackRoute -or
    [bool]$ObserveMoreEquipmentSlotsNoNativeSaveCold -or
    ($MoreEquipmentSlotsTransitionPhase -eq 'MigratedSave')
if ($requiresMoreEquipmentSlotsExpectedCommitted) {
    $expectedKeys = @(
        'ExpectedMoreEquipmentSlotsBackpackBaseline',
        'ExpectedMoreEquipmentSlotsCommittedGeneration',
        'ExpectedMoreEquipmentSlotsCommittedOccupied',
        'ExpectedMoreEquipmentSlotsCommittedSlotsBase64'
    )
    $missingExpectedKeys = @($expectedKeys | Where-Object {
        -not $SmokeRunnerBoundParameters.ContainsKey($_)
    })
    $expectedCommittedSaveModeOk =
        if ($MoreEquipmentSlotsTransitionPhase -eq 'MigratedSave') {
            $SaveTestMode -eq 'NativeSaveExpected'
        }
        else {
            $SaveTestMode -eq 'NoNativeSave'
        }
    if (-not $expectedCommittedSaveModeOk -or
        (-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or
        $SaveSlot -ne 3 -or
        -not $StageQaHost -or
        ($committedShieldRollbackRoute -and
         [string]::IsNullOrWhiteSpace($DisposableSaveFixtureRoot)) -or
        ($committedShieldRollbackRoute -and
         -not $AutoExerciseTitleButtonLifecycle) -or
        $missingExpectedKeys.Count -gt 0 -or
        $ExpectedMoreEquipmentSlotsBackpackBaseline -lt 0 -or
        $ExpectedMoreEquipmentSlotsCommittedGeneration -lt 0 -or
        $ExpectedMoreEquipmentSlotsCommittedOccupied -lt 0 -or
        [string]::IsNullOrWhiteSpace(
            $ExpectedMoreEquipmentSlotsCommittedSlotsBase64)) {
        throw 'Committed-shield rollback, cold observation, and MigratedSave require explicit slot 3, StageQaHost, and all four expected values for the committed baseline; rollback/cold routes use NoNativeSave while MigratedSave uses NativeSaveExpected.'
    }
    if ($ObserveMoreEquipmentSlotsNoNativeSaveCold -and
        ($AutoExerciseMoreEquipmentSlots -or
        $AutoExerciseMoreEquipmentSlotsNoNativeSave -or
        $AssertMoreEquipmentSlotsColdRecovery)) {
        throw '-ObserveMoreEquipmentSlotsNoNativeSaveCold is an independent read-only second process.'
    }
    try {
        $expectedMoreEquipmentSlotsCommittedSlots =
            [System.Text.Encoding]::UTF8.GetString(
                [Convert]::FromBase64String(
                    $ExpectedMoreEquipmentSlotsCommittedSlotsBase64))
    }
    catch {
        throw '-ExpectedMoreEquipmentSlotsCommittedSlotsBase64 must be valid UTF-8 Base64 copied from the first-process log.'
    }
    if ([string]::IsNullOrWhiteSpace(
            $expectedMoreEquipmentSlotsCommittedSlots)) {
        throw '-ExpectedMoreEquipmentSlotsCommittedSlotsBase64 decoded to an empty slot description.'
    }
}
if ($ObserveMoreEquipmentSlotsInterruptedCandidateRecovery) {
    $candidateExpectedKeys = @(
        'ExpectedMoreEquipmentSlotsCandidatePreGeneration',
        'ExpectedMoreEquipmentSlotsCommittedOccupied',
        'ExpectedMoreEquipmentSlotsCommittedSlotsBase64'
    )
    $missingCandidateExpectedKeys =
        @($candidateExpectedKeys | Where-Object {
            -not $SmokeRunnerBoundParameters.ContainsKey($_)
        })
    if ($SaveTestMode -ne 'NativeSaveExpected' -or
        (-not $SmokeRunnerBoundParameters.ContainsKey('SaveSlot')) -or
        $SaveSlot -ne 3 -or
        -not $StageQaHost -or
        -not $AutoExerciseMoreEquipmentSlots -or
        [string]::IsNullOrWhiteSpace(
            $DisposableSaveFixtureRoot) -or
        $missingCandidateExpectedKeys.Count -gt 0 -or
        $ExpectedMoreEquipmentSlotsCandidatePreGeneration -lt 0 -or
        $ExpectedMoreEquipmentSlotsCandidatePreGeneration -eq
            [long]::MaxValue -or
        $ExpectedMoreEquipmentSlotsCommittedOccupied -lt 0 -or
        [string]::IsNullOrWhiteSpace(
            $ExpectedMoreEquipmentSlotsCommittedSlotsBase64)) {
        throw '-ObserveMoreEquipmentSlotsInterruptedCandidateRecovery requires NativeSaveExpected, a disposable fixture, StageQaHost, explicit slot3, the existing MoreEquipmentSlots route, and exact candidate pre-generation/Committed expectations.'
    }
    if ($AutoExerciseMoreEquipmentSlotsNoNativeSave -or
        $ObserveMoreEquipmentSlotsNoNativeSaveCold -or
        $AssertMoreEquipmentSlotsColdRecovery) {
        throw '-ObserveMoreEquipmentSlotsInterruptedCandidateRecovery cannot combine with a NoNativeSave or product-disabled recovery route.'
    }
    try {
        $expectedMoreEquipmentSlotsCommittedSlots =
            [System.Text.Encoding]::UTF8.GetString(
                [Convert]::FromBase64String(
                    $ExpectedMoreEquipmentSlotsCommittedSlotsBase64))
    }
    catch {
        throw '-ExpectedMoreEquipmentSlotsCommittedSlotsBase64 must be valid UTF-8 Base64 copied from the prestaged candidate fixture.'
    }
    if ([string]::IsNullOrWhiteSpace(
            $expectedMoreEquipmentSlotsCommittedSlots)) {
        throw '-ExpectedMoreEquipmentSlotsCommittedSlotsBase64 decoded to an empty slot description.'
    }
}
if ($AssertMoreEquipmentSlotsColdRecovery) {
    throw '-AssertMoreEquipmentSlotsColdRecovery is paused: the former one-process NoNativeSave route could pass without a native SaveGame/SaveSaved commit or a second cold-process replay check. Use the dedicated disposable three-process ColdPrepare/ColdCommit/ColdObserve route.'
}
if ($SkipInstall -and -not [string]::IsNullOrWhiteSpace($PackagePayloadRoot)) {
    throw '-SkipInstall cannot be combined with -PackagePayloadRoot.'
}
if (-not [string]::IsNullOrWhiteSpace($PackagePayloadRoot) -and -not (Test-Path -LiteralPath $PackagePayloadRoot -PathType Container)) {
    throw "-PackagePayloadRoot does not exist or is not a directory: $PackagePayloadRoot"
}
if ($AssertPublishedProductCombination -and $AssertPublishedProductsDisabled) {
    throw 'Use only one of -AssertPublishedProductCombination or -AssertPublishedProductsDisabled per cold game run.'
}
if ($AssertPublishedProductCombination) {
    $publishedCombinationAllowedParameters = @(
        'SaveSlot', 'TimeoutSeconds', 'UseSteam', 'SkipInstall', 'SkipBuild', 'SaveTestMode',
        'StageQaHost', 'OfficialModProfile', 'OfficialModProfileExtraEnabledIds',
        'IsolateAllOfficialMods', 'AssertPublishedProductCombination', 'AutoReloadMods',
        'AutoExerciseTitleButtonLifecycle', 'AutoExerciseModOwnerLifetime',
        'ValidateQaG4RoutingOnly', 'AutoExitAfterSecondsOverride',
        'ExternalPlayerInputHoldMilliseconds', 'FatalWindowCrashDumpGraceSeconds',
        'FatalWindowProcessDumpMode', 'FatalWindowPostCloseCrashDumpWaitSeconds'
    )
    $publishedCombinationUnexpectedParameters = @($SmokeRunnerBoundParameters.Keys | Where-Object {
        $publishedCombinationAllowedParameters -notcontains [string]$_
    })
    if ($publishedCombinationUnexpectedParameters.Count -gt 0) {
        throw ('-AssertPublishedProductCombination is the exact eleven-product cold-run gate and cannot be combined with: ' +
            [string]::Join(', ', @($publishedCombinationUnexpectedParameters | Sort-Object)) + '.')
    }
    if (@($OfficialModProfileExtraEnabledIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -ne 0) {
        throw '-AssertPublishedProductCombination does not accept ExtraEnabledIds; the gate must select exactly the eleven public Workshop products.'
    }
    if ($OfficialModProfile -ne 'Published11' -or -not $IsolateAllOfficialMods -or -not $AutoReloadMods -or -not $AutoExerciseTitleButtonLifecycle -or -not $AutoExerciseModOwnerLifetime -or -not $StageQaHost) {
        throw '-AssertPublishedProductCombination requires -OfficialModProfile Published11, -IsolateAllOfficialMods, -AutoReloadMods, -AutoExerciseTitleButtonLifecycle, -AutoExerciseModOwnerLifetime, and -StageQaHost.'
    }
    if (-not $UseSteam -or $DirectExe -or -not $SkipInstall -or $IncludeHookProbe) {
        throw '-AssertPublishedProductCombination requires explicit -UseSteam -SkipInstall, no -DirectExe, and no -IncludeHookProbe so the subscribed artifacts remain the tested payload.'
    }
    if ($SaveSlot -lt 1 -or $SaveSlot -gt 3) {
        throw '-AssertPublishedProductCombination requires a real player save slot in the range 1..3.'
    }
}
if ($AssertPublishedProductsDisabled) {
    $publishedDisabledAllowedParameters = @(
        'SaveSlot', 'TimeoutSeconds', 'UseSteam', 'SkipInstall', 'SkipBuild', 'SaveTestMode',
        'StageQaHost', 'OfficialModProfile', 'OfficialModProfileExtraEnabledIds',
        'IsolateAllOfficialMods', 'AssertPublishedProductsDisabled',
        'AutoExerciseTitleButtonLifecycle', 'ValidateQaG4RoutingOnly',
        'AutoExitAfterSecondsOverride', 'ExternalPlayerInputHoldMilliseconds',
        'FatalWindowCrashDumpGraceSeconds', 'FatalWindowProcessDumpMode',
        'FatalWindowPostCloseCrashDumpWaitSeconds'
    )
    $publishedDisabledUnexpectedParameters = @($SmokeRunnerBoundParameters.Keys | Where-Object {
        $publishedDisabledAllowedParameters -notcontains [string]$_
    })
    if ($publishedDisabledUnexpectedParameters.Count -gt 0) {
        throw ('-AssertPublishedProductsDisabled is the exact disabled-product cold-run gate and cannot be combined with: ' +
            [string]::Join(', ', @($publishedDisabledUnexpectedParameters | Sort-Object)) + '.')
    }
    if (@($OfficialModProfileExtraEnabledIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -ne 0) {
        throw '-AssertPublishedProductsDisabled does not accept ExtraEnabledIds.'
    }
    if ($OfficialModProfile -ne 'CoreOnly' -or -not $IsolateAllOfficialMods -or -not $AutoExerciseTitleButtonLifecycle -or -not $StageQaHost) {
        throw '-AssertPublishedProductsDisabled requires -OfficialModProfile CoreOnly, -IsolateAllOfficialMods, -AutoExerciseTitleButtonLifecycle, and -StageQaHost.'
    }
    if (-not $UseSteam -or $DirectExe -or -not $SkipInstall -or $IncludeHookProbe) {
        throw '-AssertPublishedProductsDisabled requires explicit -UseSteam -SkipInstall, no -DirectExe, and no -IncludeHookProbe.'
    }
    if ($SaveSlot -lt 1 -or $SaveSlot -gt 3) {
        throw '-AssertPublishedProductsDisabled requires a real player save slot in the range 1..3.'
    }
}
if ($RequireExternalPlayerInputGate) {
    $workshopYConsoleSelected = $OfficialModProfile -in @('CoreUi', 'Published11') -or $OfficialModProfileExtraEnabledIds -contains 'Workshop.3742714442'
    if (-not $AutoExerciseDebugConsole -or -not $workshopYConsoleSelected -or -not $IsolateAllOfficialMods) {
        throw '-RequireExternalPlayerInputGate requires -AutoExerciseDebugConsole, an explicit Workshop YConsole profile/extra ID, and -IsolateAllOfficialMods.'
    }
    if ($AutoExerciseTitleButtonLifecycle) {
        throw '-RequireExternalPlayerInputGate uses its own READY/terminal-marker ReturnHome handshake and cannot be combined with -AutoExerciseTitleButtonLifecycle.'
    }
    if (-not $UseSteam -or $DirectExe -or -not $SkipInstall -or $IncludeHookProbe) {
        throw '-RequireExternalPlayerInputGate requires explicit -UseSteam -SkipInstall, no -DirectExe, and no -IncludeHookProbe.'
    }
    if ($SaveSlot -ne $debugConsoleRequiredSaveSlot) {
        throw '-RequireExternalPlayerInputGate requires explicit -SaveSlot 10 (game UI slot 10 / native archive index 9).'
    }
    if (-not $StageQaHost) {
        throw '-RequireExternalPlayerInputGate requires -StageQaHost after the embedded harness removal.'
    }
}

$nativeSaveRouteRequested =
    [bool]$AutoSaveAfterLoad -or
    [bool]$AutoExerciseInstantSave -or
    ([bool]$debugConsoleSaveAcceptanceRequested -and
     $DebugConsoleSaveAcceptancePhase -ne 'ColdObserve') -or
    [bool]$AutoExerciseMoreEquipmentSlots -or
    [bool]$SetupMoreEquipmentSlotsCommittedShield -or
    [bool]$AutoExerciseStrongPlantingGun -or
    ([bool]$moreEquipmentSlotsTransitionRequested -and
     $MoreEquipmentSlotsTransitionPhase -notin @('U4','ColdObserve')) -or
    [bool]$QaObserveSaveSaved
$archiveMutationRouteRequested =
    [bool]$AutoExerciseMoreSavesOfficialSaveUi -or
    $MoreSavesFixed12AcceptancePhase -eq 'EnabledLifecycle'
$qaRoutingProjectionOnly =
    [bool]$ValidateQaG3RoutingOnly -or
    [bool]$ValidateQaG4RoutingOnly -or
    [bool]$ValidateQaG5RoutingOnly -or
    [bool]$ValidateQaG6RoutingOnly -or
    [bool]$ValidateQaG4EvidenceCleanupOnly -or
    [bool]$ValidateNoQaUiEvidenceGateOnly
$disposableSaveFixtureRootResolved = ''
$disposableSaveFixtureMarker = $null
$disposableSaveFixtureMarkerPath = ''
$disposableSaveFixtureMarkerSha256 = ''
$disposableSaveFixtureCleanupRequested = $false
$disposableSaveFixtureCleanupOk = $true
$disposableSaveFixtureRequested =
    -not [string]::IsNullOrWhiteSpace(
        $DisposableSaveFixtureRoot)

if ($RequirePlayerSaveRestore) {
    throw '-RequirePlayerSaveRestore is retired for new runs. Use -SaveTestMode NoNativeSave for pre-cleanup unchanged proof, or an isolated disposable fixture with NativeSaveExpected/ArchiveMutation. Historical PlayerSaveRestored receipt fields remain historical.'
}
if ($SaveTestMode -eq 'NoNativeSave') {
    if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_DOLOC_PERSISTENT_ROOT)) {
        throw 'NoNativeSave requires the runner to own its exact persistent root; clear DTMAPI_DOLOC_PERSISTENT_ROOT and use either the live default or -DisposableSaveFixtureRoot.'
    }
    if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_STATE_DIR)) {
        throw 'NoNativeSave requires the runner to own its exact DTMAPI state root; clear DTMAPI_STATE_DIR and use either the live default or -DisposableSaveFixtureRoot.'
    }
}
if ($RetainDisposableSaveFixtureOnSuccess -and
    -not $disposableSaveFixtureRequested) {
    throw '-RetainDisposableSaveFixtureOnSuccess requires an explicit disposable fixture.'
}
if (-not $qaRoutingProjectionOnly -and $SaveTestMode -eq 'NoNativeSave' -and ($nativeSaveRouteRequested -or $archiveMutationRouteRequested)) {
    throw 'NoNativeSave forbids every known native-save and archive-mutation route. Select NativeSaveExpected or ArchiveMutation with an isolated disposable fixture.'
}
if (-not $qaRoutingProjectionOnly -and $SaveTestMode -eq 'NativeSaveExpected' -and $archiveMutationRouteRequested) {
    throw 'The MoreSaves archive-management route requires SaveTestMode ArchiveMutation, not NativeSaveExpected.'
}
if (-not $qaRoutingProjectionOnly -and $SaveTestMode -eq 'ArchiveMutation' -and $nativeSaveRouteRequested) {
    throw 'ArchiveMutation cannot be combined with a native-save fixture route in one run.'
}
if ($moreSavesFixed12AcceptanceRequested) {
    $moreSavesPhaseContract = Assert-MoreSavesFixed12PhaseOptions -Options @{
        Phase = $MoreSavesFixed12AcceptancePhase; StageQaHost = [bool]$StageQaHost
        DirectExe = [bool]$DirectExe; SkipInstall = [bool]$SkipInstall
        SaveSlotExplicit = $SmokeRunnerBoundParameters.ContainsKey('SaveSlot'); SaveSlot = $SaveSlot
        FixtureRoot = $DisposableSaveFixtureRoot; SaveTestMode = $SaveTestMode
        OfficialModProfile = $OfficialModProfile; IsolateAllOfficialMods = [bool]$IsolateAllOfficialMods
        ExtraEnabledIds = @($OfficialModProfileExtraEnabledIds)
    }
}
if (-not $qaRoutingProjectionOnly -and
    ($SaveTestMode -ne 'NoNativeSave' -or
     $disposableSaveFixtureRequested)) {
    if (-not $StageQaHost) {
        throw "$SaveTestMode requires -StageQaHost so the test-only LocalSave cloudDirPath owner can prove exact disposable-fixture redirection before Runtime Mod loading."
    }
    if (-not $disposableSaveFixtureRequested) {
        throw "$SaveTestMode requires -DisposableSaveFixtureRoot."
    }
    if (-not $DirectExe -or $UseSteam) {
        throw "$SaveTestMode requires -DirectExe and forbids -UseSteam so the QA-only LocalSave.cloudDirPath redirect remains the sole save-storage owner."
    }

    $disposableSaveFixtureRootResolved = [System.IO.Path]::GetFullPath($DisposableSaveFixtureRoot)
    $livePersistentRoot = Get-DolocTownLivePersistentRootForSmoke
    $fixturePrefix = $disposableSaveFixtureRootResolved.TrimEnd('\','/') + [System.IO.Path]::DirectorySeparatorChar
    $livePrefix = $livePersistentRoot.TrimEnd('\','/') + [System.IO.Path]::DirectorySeparatorChar
    if ([string]::Equals($disposableSaveFixtureRootResolved, $livePersistentRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
        $fixturePrefix.StartsWith($livePrefix, [System.StringComparison]::OrdinalIgnoreCase) -or
        $livePrefix.StartsWith($fixturePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$SaveTestMode fixture must be outside the live Steam AutoCloud persistent-data tree."
    }
    if (-not (Test-Path -LiteralPath $disposableSaveFixtureRootResolved -PathType Container) -or
        -not (Test-Path -LiteralPath (Join-Path $disposableSaveFixtureRootResolved 'SAVE') -PathType Container) -or
        -not (Test-Path -LiteralPath (Join-Path $disposableSaveFixtureRootResolved 'DTMAPI') -PathType Container)) {
        throw "$SaveTestMode fixture must already contain isolated SAVE and DTMAPI directories."
    }
    Assert-SmokeOrdinaryFixtureTree -Root $disposableSaveFixtureRootResolved -Mode $SaveTestMode
    $fixtureMarkerPath = Join-Path $disposableSaveFixtureRootResolved '.dtmapi-disposable-save-fixture.json'
    if (-not (Test-Path -LiteralPath $fixtureMarkerPath -PathType Leaf)) {
        throw "$SaveTestMode fixture is missing .dtmapi-disposable-save-fixture.json."
    }
    try {
        $disposableSaveFixtureMarker = Get-Content -Raw -Encoding UTF8 -LiteralPath $fixtureMarkerPath | ConvertFrom-Json
    }
    catch {
        throw "$SaveTestMode fixture marker is invalid JSON: $($_.Exception.Message)"
    }
    if ([int]$disposableSaveFixtureMarker.schemaVersion -ne 1 -or
        -not [bool]$disposableSaveFixtureMarker.disposable -or
        -not [bool]$disposableSaveFixtureMarker.steamAutoCloudIsolated) {
        throw "$SaveTestMode fixture marker must assert schemaVersion=1, disposable=true, and steamAutoCloudIsolated=true."
    }
    $autoCloudMarkers = @(Get-ChildItem -LiteralPath $disposableSaveFixtureRootResolved -Recurse -Force -File -Filter 'steam_autocloud.vdf' -ErrorAction SilentlyContinue)
    if ($autoCloudMarkers.Count -gt 0) {
        throw "$SaveTestMode fixture contains steam_autocloud.vdf and is not isolated from Steam AutoCloud."
    }
    $disposableSaveFixtureMarkerPath = $fixtureMarkerPath
    $disposableSaveFixtureMarkerSha256 =
        (Get-FileHash -LiteralPath $fixtureMarkerPath -Algorithm SHA256).Hash
}

$previousDtmApiPersistentRoot =
    [Environment]::GetEnvironmentVariable(
        'DTMAPI_DOLOC_PERSISTENT_ROOT',
        [EnvironmentVariableTarget]::Process)
$previousDtmApiStateDir =
    [Environment]::GetEnvironmentVariable(
        'DTMAPI_STATE_DIR',
        [EnvironmentVariableTarget]::Process)
$smokeSaveEnvironmentScopeApplied = $false
