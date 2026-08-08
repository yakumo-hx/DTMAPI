param(
    [int] $SaveSlot = 3,
    [int] $TimeoutSeconds = 180,
    [switch] $IncludeHookProbe,
    [switch] $UseSteam,
    [switch] $DirectExe,
    [switch] $AutoSaveAfterLoad,
    [switch] $AutoReloadMods,
    [switch] $RequirePlayerSaveRestore,
    [ValidateSet('NoNativeSave','NativeSaveExpected','ArchiveMutation')]
    [string] $SaveTestMode = 'NoNativeSave',
    [string] $DisposableSaveFixtureRoot = '',
    [switch] $RetainDisposableSaveFixtureOnSuccess,
    [switch] $ValidateSaveTestModeOnly,
    [switch] $AssertProductOwnerRefresh,
    [switch] $AutoExerciseExperimentalHooks,
    [switch] $AutoExerciseActionSpeedTool,
    [switch] $AutoExerciseActionSpeedConfigApply,
    [switch] $AutoExerciseActionSpeedInteraction,
    [switch] $AutoExerciseOneActionResourceHit,
    [switch] $AutoExerciseOneActionWrongTool,
    [switch] $AutoExerciseOneActionFuelFeed,
    [switch] $AutoExerciseOneActionVegetation,
    [switch] $AssertAdvancedProductOwnerDeactivation,
    [ValidateSet('Yuuka.DTMAPI.ActionSpeed','Yuuka.DTMAPI.OneActionComplete','Yuuka.DTMAPI.FishBreedingAssistant','Yuuka.DTMAPI.AnimalHusbandryProgress','DTMAPI.MoreSavesMod','DTMAPI.ChestLocatorEnhancerMod','DTMAPI.MoreEquipmentSlotsMod','DTMAPI.StrongPlantingGunMod','DTMAPI.ZoomMod','DTMAPI.MineMod','DTMAPI.DebugConsoleMod')]
    [string[]] $AdvancedProductOwnerDeactivationOwnerIds = @(),
    [switch] $AutoPressOneActionMenuKey,
    [switch] $AutoExerciseAutoFishingPhase,
    [switch] $AutoExerciseLegacyFishingCompatibility,
    [switch] $AutoExerciseAutoFishingMiniGameComplete,
    [ValidateSet('DefaultLoop','InstantBite','SkipMiniGame','InstantSkip','FastAnimations','CombinedInstantSkip','CombinedInstantComplete')]
    [string] $AutoFishingScenario = 'DefaultLoop',
    [ValidateRange(0, 1)]
    [double] $AutoFishingCastChargeRatio = 0,
    [ValidateRange(0, 1000)]
    [int] $AutoFishingSoakLoops = 0,
    [string] $AutoFishingToggleKey = 'F6',
    [switch] $AutoFishingMonoGate,
    [switch] $AutoFishingPerformance,
    [ValidateSet('FishLoop','InactiveNoConsumer','EnabledNoRod')]
    [string] $AutoFishingPerformanceProfile = 'FishLoop',
    [ValidateRange(0, 500)]
    [int] $AutoFishingPerformanceTargetFish = 0,
    [ValidateRange(0, 100)]
    [int] $AutoFishingPerformanceWarmupFish = 5,
    [ValidateRange(0, 3600)]
    [int] $AutoFishingPerformanceZeroWarmupSeconds = 60,
    [ValidateRange(1, 7200)]
    [int] $AutoFishingPerformanceZeroMeasureSeconds = 600,
    [ValidateRange(0, 100000)]
    [int] $AutoFishingPerformanceWarmupFrames = 0,
    [ValidateRange(0, 1000000)]
    [int] $AutoFishingPerformanceTargetFrames = 0,
    [ValidateSet('None','ActionSpeed','AutoFishing')]
    [string] $Batch5GcLadderDomain = 'None',
    [ValidateSet('None','L0','L1','L2','L3','L4','L5')]
    [string] $Batch5GcLadderLevel = 'None',
    [ValidateSet('Tool','Interact','Eat','ContinuousUse','FishLoop')]
    [string] $Batch5GcLadderWorkload = 'Tool',
    [ValidateRange(1.0, 4.0)]
    [double] $Batch5GcLadderMultiplier = 1,
    [ValidateRange(1, 7200)]
    [int] $Batch5GcLadderMeasureSeconds = 600,
    [ValidateRange(1, 600)]
    [int] $Batch5GcLadderSampleSeconds = 30,
    [ValidateRange(1, 500)]
    [int] $Batch5GcLadderTargetUnits = 10,
    [switch] $Batch6AutoFishingPilot,
    [ValidateSet('L0','L1','L2','L3','L4','L5')]
    [string] $Batch6AutoFishingLevel = 'L0',
    [ValidateSet('DefaultLoop','InstantBite','SkipMiniGame','InstantSkip','FastAnimations','CombinedInstantSkip','CombinedInstantComplete')]
    [string] $Batch6AutoFishingScenario = 'CombinedInstantSkip',
    [ValidateRange(1, 7200)]
    [int] $Batch6AutoFishingMeasureSeconds = 600,
    [ValidateRange(1, 600)]
    [int] $Batch6AutoFishingSampleSeconds = 30,
    [ValidateRange(0, 100)]
    [int] $Batch6AutoFishingWarmupFish = 5,
    [ValidateRange(1, 500)]
    [int] $Batch6AutoFishingTargetFish = 10,
    [switch] $Batch6AutoFishingFormal,
    [ValidateSet('LongRun','Behavior')]
    [string] $Batch6AutoFishingFormalContract = 'LongRun',
    [ValidateRange(1.0, 4.0)]
    [double] $Batch6AutoFishingMultiplier = 1,
    [ValidateRange(0, 1)]
    [double] $Batch6AutoFishingCastChargeRatio = 0,
    [switch] $Batch6AutoFishingManualMovementCancel,
    [string] $Batch6AutoFishingExpectedPackageSha256 = '',
    [string] $Batch6AutoFishingExpectedEntrySha256 = '',
    [string] $Batch6AutoFishingExpectedManifestSha256 = '',
    [string] $Batch6AutoFishingExpectedPolicySha256 = '',
    [switch] $Batch6AutoFishingManagerLifecycle,
    [ValidateSet('SameProcessDisable','ColdDisabled')]
    [string] $Batch6AutoFishingManagerMode = 'SameProcessDisable',
    [string] $Batch6AutoFishingManagerProductRoot = '',
    [string] $Batch6AutoFishingManagerMarkerContentBase64 = '',
    [string] $Batch6AutoFishingManagerMarkerSha256 = '',
    [string] $Batch6AutoFishingManagerExpectedPackageSha256 = '',
    [string] $Batch6AutoFishingManagerExpectedEntrySha256 = '',
    [string] $Batch6AutoFishingManagerExpectedManifestSha256 = '',
    [string] $Batch6AutoFishingManagerExpectedPolicySha256 = '',
    [switch] $AutoExercisePauseMenuLayout,
    [int] $AutoExercisePauseMenuLayoutDelaySeconds = 12,
    [switch] $AutoExerciseTitleButtonLifecycle,
    [switch] $AutoExerciseModOwnerLifetime,
    [switch] $AutoExerciseSaveLoadCycle,
    [switch] $AutoExercisePreLoadGcProbe,
    [switch] $AutoExerciseSaveLoadCyclePendingPressure,
    [ValidateRange(0, 1000)]
    [int] $SaveLoadCycleCount = 0,
    [ValidateRange(0, 10800)]
    [int] $SaveLoadCycleInitialTitleIdleSeconds = 0,
    [ValidateRange(1, 3600)]
    [int] $SaveLoadCycleIntervalSeconds = 5,
    [ValidateRange(1, 600)]
    [int] $SaveLoadCycleInSaveSeconds = 5,
    [ValidateSet('Full','Lite','Off')]
    [string] $SaveLoadObjectSnapshotMode = 'Full',
    [ValidateSet('None','UiRuntime')]
    [string] $SmokeRootIsolationProfile = 'None',
    [ValidateSet('None','YConsoleZoomNoInput','YConsoleZoomNoEvents','YConsoleZoomNoInputEvents','YConsoleZoomNoConfig','ZoomNoInput','YConsoleNoInput')]
    [string] $SmokeOwnerRootIsolationProfile = 'None',
    [ValidateSet('None','VersionPatcher')]
    [string] $SmokeNativeLoadContinuationProbe = 'None',
    [ValidateRange(0, 10800)]
    [int] $SaveLoadCyclePendingPressureSeconds = 0,
    [ValidateRange(0.1, 60.0)]
    [double] $SaveLoadCyclePendingPressureIntervalSeconds = 2,
    [switch] $AutoExerciseInstantSave,
    [int] $AutoExerciseInstantSaveDelaySeconds = 3,
    [ValidateSet('None','FailedMutation','SuccessfulMutation','ColdObserve')]
    [string] $DebugConsoleSaveAcceptancePhase = 'None',
    [int] $ExpectedDebugConsoleMoney = -1,
    [switch] $AutoPressAutoFishingHotkey,
    [switch] $AutoExerciseAutoFishingMovementCancel,
    [switch] $AutoOpenTitleSettingsMenu,
    [switch] $AutoOpenTitleSettingsStatusPage,
    [switch] $AutoOpenTitleSettingsManagerMvp,
    [switch] $AutoOpenOfficialModUi,
    [switch] $AutoOpenAnimalPanel,
    [switch] $AutoExerciseDebugConsole,
    [switch] $AutoExerciseDebugConsoleMouseGive,
    [switch] $RequireExternalPlayerInputGate,
    [ValidateRange(40, 250)]
    [int] $ExternalPlayerInputHoldMilliseconds = 40,
    [switch] $AutoExerciseDebugInventory,
    [switch] $AutoExerciseDebugWeather,
    [switch] $AutoExerciseDebugTeleport,
    [switch] $AutoExerciseDebugTime,
    [switch] $AutoExerciseDebugMovement,
    [switch] $AutoExerciseAdvancedDebug,
    [switch] $AutoExerciseVehicle,
    [switch] $AutoExerciseNewContentApis,
    [switch] $ExpectOilAbsent,
    [switch] $QaObserveContentMetadata,
    [switch] $QaObserveEquipmentSlotsUi,
    [switch] $OilOnly,
    [switch] $AutoExerciseMineContentApis,
    [switch] $AutoExerciseZoom,
    [switch] $AutoExerciseZoomProductNative,
    [switch] $AutoExerciseChestLocatorEnhancer,
    [switch] $AutoExerciseMoreEquipmentSlots,
    [switch] $SetupMoreEquipmentSlotsCommittedShield,
    [switch] $DamageMoreEquipmentSlotsCommittedShieldNoNativeSave,
    [switch] $BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave,
    [switch] $ObserveMoreEquipmentSlotsInterruptedCandidateRecovery,
    [long] $ExpectedMoreEquipmentSlotsCandidatePreGeneration = -1,
    [ValidateSet('None','U1','Prepare','U3Backpack','U3Mail','U4','MigratedSave','ColdPrepare','ColdCommit','ColdObserve')]
    [string] $MoreEquipmentSlotsTransitionPhase = 'None',
    [switch] $AutoExerciseMoreEquipmentSlotsNoNativeSave,
    [switch] $ObserveMoreEquipmentSlotsNoNativeSaveCold,
    [int] $ExpectedMoreEquipmentSlotsBackpackBaseline = -1,
    [long] $ExpectedMoreEquipmentSlotsCommittedGeneration = -1,
    [int] $ExpectedMoreEquipmentSlotsCommittedOccupied = -1,
    [string] $ExpectedMoreEquipmentSlotsCommittedSlotsBase64 = '',
    [switch] $AssertMoreEquipmentSlotsColdRecovery,
    [switch] $AutoExerciseMoreSavesOfficialSaveUi,
    [ValidateSet('None','EnabledLifecycle','DisabledCold','ReenabledCold')]
    [string] $MoreSavesFixed12AcceptancePhase = 'None',
    [switch] $AutoExerciseStrongPlantingGun,
    [switch] $AutoExerciseCropHarvestingApi,
    [switch] $AutoExerciseCustomEntityApis,
    [switch] $AutoExerciseFishRoeTooltip,
    [switch] $AutoExerciseDiagnosticsSnapshot,
    [switch] $QaObserveSaveLoaded,
    [switch] $QaObserveSaveSaved,
    [switch] $QaObserveWorkshopReloadCompleted,
    [switch] $ValidateQaG3RoutingOnly,
    [switch] $ValidateQaG4RoutingOnly,
    [switch] $ValidateQaG5RoutingOnly,
    [switch] $ValidateQaG6RoutingOnly,
    [switch] $ValidateQaG4EvidenceCleanupOnly,
    [switch] $ValidateNoQaUiEvidenceGateOnly,
    [switch] $AutoExerciseAudioReplacement,
    [switch] $AutoExerciseHatchAnimalVoice,
    [switch] $DisableSecondMotorForSmoke,
    [int] $AutoExitAfterSecondsOverride = 0,
    [ValidateRange(0, 7200)]
    [int] $TitleIdleBeforeSaveSeconds = 0,
    [ValidateSet('Current','CoreOnly','CoreUi','CoreCustomAnimals','CoreAnimalVoice','CoreAutoFishing','Published11','Local11','FullKnown')]
    [string] $OfficialModProfile = 'Current',
    [string[]] $OfficialModProfileExtraEnabledIds = @(),
    [switch] $IsolateAllOfficialMods,
    [switch] $AssertPublishedProductCombination,
    [switch] $AssertPublishedProductsDisabled,
    [switch] $AssertNoQaUiEvidence,
    [switch] $Issue011Acceptance,
    [switch] $AutoDriveNoQaAnimalViewer,
    [ValidateRange(0, 600)]
    [int] $FatalWindowCrashDumpGraceSeconds = 0,
    [ValidateSet('None','ComSvcsFull','DbgHelpFull','Both')]
    [string] $FatalWindowProcessDumpMode = 'None',
    [ValidateRange(0, 600)]
    [int] $FatalWindowPostCloseCrashDumpWaitSeconds = 20,
    [switch] $StageQaHost,
    [switch] $SkipBuild,
    [switch] $SkipInstall,
    [string] $PackagePayloadRoot = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\dump-governance.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$debugConsoleSaveAcceptanceRequested =
    $DebugConsoleSaveAcceptancePhase -ne 'None'
$moreEquipmentSlotsTransitionRequested =
    $MoreEquipmentSlotsTransitionPhase -ne 'None'
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

if ($debugConsoleSaveAcceptanceRequested) {
    if ($AutoExerciseInstantSave) {
        throw '-DebugConsoleSaveAcceptancePhase is an independent save-commit route and cannot combine with -AutoExerciseInstantSave.'
    }
    if (-not $StageQaHost -or
        (-not $PSBoundParameters.ContainsKey('SaveSlot')) -or
        $SaveSlot -ne 3 -or
        [string]::IsNullOrWhiteSpace($DisposableSaveFixtureRoot)) {
        throw '-DebugConsoleSaveAcceptancePhase requires StageQaHost, explicit SaveSlot 3, and an isolated disposable fixture.'
    }
    if ($DebugConsoleSaveAcceptancePhase -eq 'ColdObserve') {
        if ($SaveTestMode -ne 'NoNativeSave' -or
            (-not $PSBoundParameters.ContainsKey('ExpectedDebugConsoleMoney')) -or
            $ExpectedDebugConsoleMoney -lt 0) {
            throw 'DebugConsole ColdObserve requires SaveTestMode NoNativeSave and exact non-negative ExpectedDebugConsoleMoney from the preceding mutation log.'
        }
    }
    elseif ($SaveTestMode -ne 'NativeSaveExpected') {
        throw 'DebugConsole FailedMutation and SuccessfulMutation require SaveTestMode NativeSaveExpected.'
    }
    if ($DebugConsoleSaveAcceptancePhase -eq 'SuccessfulMutation' -and
        ((-not $PSBoundParameters.ContainsKey('ExpectedDebugConsoleMoney')) -or
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
    $noQaUiUnexpectedParameters = @($PSBoundParameters.Keys | Where-Object {
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
    if ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 3) {
        throw '-AssertNoQaUiEvidence requires explicit -SaveSlot 3.'
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
    if ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 5) {
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
    if ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 5) {
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
    if ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 3) {
        throw '-AssertAdvancedProductOwnerDeactivation requires explicit -SaveSlot 3.'
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
    ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 3)) {
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
        (-not $PSBoundParameters.ContainsKey('SaveSlot')) -or
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
        (-not $PSBoundParameters.ContainsKey('SaveSlot')) -or
        $SaveSlot -ne 3 -or
        -not $StageQaHost -or
        [string]::IsNullOrWhiteSpace($DisposableSaveFixtureRoot)) {
        throw '-SetupMoreEquipmentSlotsCommittedShield requires NativeSaveExpected, explicit slot 3, StageQaHost, and a disposable fixture.'
    }
}
if ($AutoExerciseMoreEquipmentSlotsNoNativeSave) {
    if ($SaveTestMode -ne 'NoNativeSave' -or
        (-not $PSBoundParameters.ContainsKey('SaveSlot')) -or
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
        -not $PSBoundParameters.ContainsKey($_)
    })
    $expectedCommittedSaveModeOk =
        if ($MoreEquipmentSlotsTransitionPhase -eq 'MigratedSave') {
            $SaveTestMode -eq 'NativeSaveExpected'
        }
        else {
            $SaveTestMode -eq 'NoNativeSave'
        }
    if (-not $expectedCommittedSaveModeOk -or
        (-not $PSBoundParameters.ContainsKey('SaveSlot')) -or
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
            -not $PSBoundParameters.ContainsKey($_)
        })
    if ($SaveTestMode -ne 'NativeSaveExpected' -or
        (-not $PSBoundParameters.ContainsKey('SaveSlot')) -or
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
    $publishedCombinationUnexpectedParameters = @($PSBoundParameters.Keys | Where-Object {
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
    $publishedDisabledUnexpectedParameters = @($PSBoundParameters.Keys | Where-Object {
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
    if ($SaveSlot -lt 1 -or $SaveSlot -gt 3) {
        throw '-RequireExternalPlayerInputGate requires a real player save slot in the range 1..3.'
    }
    if (-not $StageQaHost) {
        throw '-RequireExternalPlayerInputGate requires -StageQaHost after the embedded harness removal.'
    }
}

function Get-DolocTownLivePersistentRootForSmoke {
    return [System.IO.Path]::GetFullPath(
        (Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\RedSawGames\DolocTown'))
}

function Get-DolocTownPersistentRootForSmoke {
    if (-not [string]::IsNullOrWhiteSpace($DisposableSaveFixtureRoot)) {
        return [System.IO.Path]::GetFullPath($DisposableSaveFixtureRoot)
    }
    if ($env:DTMAPI_DOLOC_PERSISTENT_ROOT) {
        return [System.IO.Path]::GetFullPath($env:DTMAPI_DOLOC_PERSISTENT_ROOT)
    }

    return Get-DolocTownLivePersistentRootForSmoke
}

function Assert-SmokeOrdinaryFixtureTree {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $Mode
    )

    $resolvedRoot = [System.IO.Path]::GetFullPath($Root)
    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $pending.Push($resolvedRoot)
    while ($pending.Count -gt 0) {
        $current = $pending.Pop()
        $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Mode fixture must not contain a junction, symbolic link, or other reparse point: $current"
        }
        if (-not $item.PSIsContainer) {
            continue
        }
        foreach ($child in @(Get-ChildItem -LiteralPath $current -Force -ErrorAction Stop)) {
            if (($child.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "$Mode fixture must not contain a junction, symbolic link, or other reparse point: $($child.FullName)"
            }
            if ($child.PSIsContainer) {
                $pending.Push($child.FullName)
            }
        }
    }
}

function Remove-SmokeDisposableSaveFixture {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $ExpectedMarkerSha256,
        [Parameter(Mandatory = $true)] [string] $Mode
    )

    $resolvedRoot = [System.IO.Path]::GetFullPath($Root)
    $livePersistentRoot = Get-DolocTownLivePersistentRootForSmoke
    $fixturePrefix = $resolvedRoot.TrimEnd('\','/') + [System.IO.Path]::DirectorySeparatorChar
    $livePrefix = $livePersistentRoot.TrimEnd('\','/') + [System.IO.Path]::DirectorySeparatorChar
    if ([string]::Equals($resolvedRoot, $livePersistentRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
        $fixturePrefix.StartsWith($livePrefix, [System.StringComparison]::OrdinalIgnoreCase) -or
        $livePrefix.StartsWith($fixturePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Mode fixture cleanup refused the live Steam AutoCloud persistent-data tree."
    }
    if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
        throw "$Mode fixture cleanup could not find the exact disposable root: $resolvedRoot"
    }

    Assert-SmokeOrdinaryFixtureTree -Root $resolvedRoot -Mode "$Mode cleanup"
    $markerPath = Join-Path $resolvedRoot '.dtmapi-disposable-save-fixture.json'
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "$Mode fixture cleanup refused a root without its ownership marker."
    }
    $actualMarkerSha256 = (Get-FileHash -LiteralPath $markerPath -Algorithm SHA256).Hash
    if (-not [string]::Equals(
        $actualMarkerSha256,
        $ExpectedMarkerSha256,
        [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Mode fixture cleanup refused a changed ownership marker."
    }
    $marker = Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath | ConvertFrom-Json
    if ([int]$marker.schemaVersion -ne 1 -or
        -not [bool]$marker.disposable -or
        -not [bool]$marker.steamAutoCloudIsolated) {
        throw "$Mode fixture cleanup refused a marker that no longer declares an isolated disposable fixture."
    }

    Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    if (Test-Path -LiteralPath $resolvedRoot) {
        throw "$Mode fixture cleanup did not remove the exact disposable root."
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
    $expectedMoreSavesFixed12SaveMode =
        if ($MoreSavesFixed12AcceptancePhase -eq 'EnabledLifecycle') {
            'ArchiveMutation'
        }
        else {
            'NoNativeSave'
        }
    if (-not $StageQaHost -or -not $DirectExe -or $UseSteam -or
        -not $SkipInstall -or
        -not $PSBoundParameters.ContainsKey('SaveSlot') -or
        $SaveSlot -ne 7 -or
        [string]::IsNullOrWhiteSpace($DisposableSaveFixtureRoot) -or
        $SaveTestMode -cne $expectedMoreSavesFixed12SaveMode) {
        throw "MoreSaves fixed-12 $MoreSavesFixed12AcceptancePhase requires -StageQaHost -DirectExe -SkipInstall, explicit -SaveSlot 7, an isolated -DisposableSaveFixtureRoot, and -SaveTestMode $expectedMoreSavesFixed12SaveMode."
    }
    if ($OfficialModProfile -cne 'CoreOnly' -or
        -not $IsolateAllOfficialMods) {
        throw 'MoreSaves fixed-12 phases require the exact isolated CoreOnly official profile.'
    }
    $requestedExtraIds = @(
        $OfficialModProfileExtraEnabledIds |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique)
    $expectMoreSavesEnabled =
        $MoreSavesFixed12AcceptancePhase -ne 'DisabledCold'
    if (($expectMoreSavesEnabled -and
            ($requestedExtraIds.Count -ne 1 -or
             $requestedExtraIds[0] -cne 'Local.DTMAPI_MoreSaves')) -or
        (-not $expectMoreSavesEnabled -and
            $requestedExtraIds.Count -ne 0)) {
        throw 'MoreSaves fixed-12 enabled phases require only Local.DTMAPI_MoreSaves as the extra enabled product; DisabledCold requires no extra enabled product.'
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
try {
    if ($disposableSaveFixtureRequested -and
        -not [string]::IsNullOrWhiteSpace(
            $disposableSaveFixtureRootResolved)) {
        $smokeSaveEnvironmentScopeApplied = $true
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT =
            if ($moreSavesFixed12AcceptanceRequested) {
                Get-DolocTownLivePersistentRootForSmoke
            }
            else {
                $disposableSaveFixtureRootResolved
            }
        $env:DTMAPI_STATE_DIR = [System.IO.Path]::GetFullPath(
            (Join-Path $disposableSaveFixtureRootResolved 'DTMAPI'))
        $disposableSaveFixtureCleanupRequested =
            -not [bool]$ValidateSaveTestModeOnly -and
            -not [bool]$RetainDisposableSaveFixtureOnSuccess
    }

    if ($ValidateSaveTestModeOnly) {
        [pscustomobject]@{
            SaveTestMode = $SaveTestMode
            NativeSaveRouteRequested = $nativeSaveRouteRequested
            ArchiveMutationRouteRequested = $archiveMutationRouteRequested
            MoreSavesFixed12AcceptancePhase =
                $MoreSavesFixed12AcceptancePhase
            MoreSavesFixed12OfficialLocalRootMode =
                $moreSavesFixed12OfficialLocalRootMode
            MoreSavesFixed12OfficialLocalProductRoot =
                $moreSavesFixed12OfficialLocalProductRoot
            DisposableSaveFixtureRoot = $disposableSaveFixtureRootResolved
            SteamAutoCloudIsolated = if ($null -ne $disposableSaveFixtureMarker) {
                [bool]$disposableSaveFixtureMarker.steamAutoCloudIsolated
            }
            else {
                $null
            }
            PlayerArchiveWritebackAllowed = $false
            DisposableSaveFixtureCleanupRequested = $false
            DisposableSaveFixtureRetentionRequested =
                [bool]$RetainDisposableSaveFixtureOnSuccess
            Passed = $true
        } | ConvertTo-Json -Depth 4
        exit 0
    }

function Write-SmokeJsonObject {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $json = $Value | ConvertTo-Json -Depth 10
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

function Get-SmokeQaHostVersionAuthority {
    param([Parameter(Mandatory = $true)] [string] $RepoRoot)

    $path = Join-Path $RepoRoot 'tools\release\dtmapi-runtime-version.props'
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "QA host staging requires the tracked Runtime version authority: $path"
    }
    [xml]$document = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $schema = [string]$document.Project.PropertyGroup.DtmApiVersionAuthoritySchema
    $release = [string]$document.Project.PropertyGroup.DtmApiReleaseVersion
    $binary = [string]$document.Project.PropertyGroup.DtmApiBinaryFileVersion
    $assembly = [string]$document.Project.PropertyGroup.DtmApiAssemblyCompatibilityVersion
    if ($schema -ne '1' -or [string]::IsNullOrWhiteSpace($release) -or
        $binary -notmatch '^\d+\.\d+\.\d+\.\d+$' -or
        $assembly -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        throw "QA host staging found an invalid Runtime version authority: $path"
    }
    return [pscustomobject]@{
        ReleaseVersion = $release
        BinaryVersion = $binary
        AssemblyVersion = $assembly
    }
}

function Get-SmokeFileSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $stream = $null
    $sha = $null
    try {
        $stream = [System.IO.File]::Open(
            [System.IO.Path]::GetFullPath($Path),
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            ([System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete))
        $sha = [System.Security.Cryptography.SHA256]::Create()
        return [System.BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '').ToUpperInvariant()
    }
    finally {
        if ($null -ne $sha) { $sha.Dispose() }
        if ($null -ne $stream) { $stream.Dispose() }
    }
}

function Get-SmokeFileMetadataSnapshot {
    param(
        [Parameter(Mandatory = $true)] [string] $Kind,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $exists = Test-Path -LiteralPath $resolved -PathType Leaf
    $item = if ($exists) { Get-Item -LiteralPath $resolved } else { $null }
    return [pscustomobject]@{
        Kind = $Kind
        Path = $resolved
        Existed = $exists
        Length = if ($exists) { [int64]$item.Length } else { [int64]0 }
        Sha256 = if ($exists) { Get-SmokeFileSha256 -Path $resolved } else { '' }
        LastWriteTimeUtc = if ($exists) { $item.LastWriteTimeUtc.ToString('o') } else { '' }
        LastWriteTimeUtcTicks = if ($exists) { [int64]$item.LastWriteTimeUtc.Ticks } else { [int64]0 }
    }
}

function Compare-SmokeFileMetadataSnapshots {
    param(
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [object[]] $Snapshots,
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $SaveTestMode
    )

    $checks = New-Object 'System.Collections.Generic.List[object]'
    $passed = $true
    foreach ($snapshot in @($Snapshots)) {
        $actual = Get-SmokeFileMetadataSnapshot -Kind ([string]$snapshot.Kind) -Path ([string]$snapshot.Path)
        $unchanged =
            [bool]$actual.Existed -eq [bool]$snapshot.Existed -and
            [int64]$actual.Length -eq [int64]$snapshot.Length -and
            [string]::Equals([string]$actual.Sha256, [string]$snapshot.Sha256, [System.StringComparison]::OrdinalIgnoreCase) -and
            [int64]$actual.LastWriteTimeUtcTicks -eq [int64]$snapshot.LastWriteTimeUtcTicks
        $passed = $passed -and $unchanged
        $checks.Add([ordered]@{
            Kind = [string]$snapshot.Kind
            Path = [string]$snapshot.Path
            ExistedBefore = [bool]$snapshot.Existed
            ExistsBeforeCleanup = [bool]$actual.Existed
            LengthBefore = [int64]$snapshot.Length
            LengthBeforeCleanup = [int64]$actual.Length
            Sha256Before = [string]$snapshot.Sha256
            Sha256BeforeCleanup = [string]$actual.Sha256
            LastWriteTimeUtcBefore = [string]$snapshot.LastWriteTimeUtc
            LastWriteTimeUtcBeforeCleanup = [string]$actual.LastWriteTimeUtc
            LastWriteTimeUtcTicksBefore = [int64]$snapshot.LastWriteTimeUtcTicks
            LastWriteTimeUtcTicksBeforeCleanup = [int64]$actual.LastWriteTimeUtcTicks
            UnchangedBeforeCleanup = $unchanged
        }) | Out-Null
    }
    Write-SmokeJsonObject -Path $EvidencePath -Value ([ordered]@{
        SaveTestMode = $SaveTestMode
        ComparedBeforeRunnerOrExternalRestore = $true
        RoutineByteBackupCreated = $false
        PlayerArchiveWritebackPerformed = $false
        Files = @($checks.ToArray())
        Passed = $passed
    })
    return $passed
}

function Get-SmokeCommittedSidecarPaths {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $DtmApiStateDir,
        [Parameter(Mandatory = $true)] [int] $ArchiveIndex
    )

    $catalogPath = Join-Path $RepoRoot 'tools\release\dtmapi-product-catalog.json'
    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
    $paths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($product in @($catalog.products)) {
        foreach ($templateValue in @($product.saveSidecars)) {
            $template = [string]$templateValue
            if ([string]::IsNullOrWhiteSpace($template)) {
                continue
            }
            $relative = $template.Replace('<archiveIndex>', $ArchiveIndex.ToString([System.Globalization.CultureInfo]::InvariantCulture)).Replace('/', '\')
            if ($relative -match '<[^>]+>') {
                throw "Committed sidecar template retained an unsupported placeholder: $template"
            }
            if (-not $relative.StartsWith('DTMAPI\', [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Committed sidecar template escaped the DTMAPI state root: $template"
            }
            $livePath =
                [System.IO.Path]::GetFullPath(
                    (Join-Path $DtmApiStateDir $relative.Substring('DTMAPI\'.Length)))
            $paths.Add($livePath) | Out-Null
            $paths.Add($livePath + '.previous') | Out-Null
        }
    }
    return @($paths.ToArray() | Sort-Object -Unique)
}

function Get-SmokeQaHostFileReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $Kind,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $item = Get-Item -LiteralPath $resolved -ErrorAction Stop
    if ($item.PSIsContainer -or $item.Length -le 0) {
        throw "QA host staged artifact is missing or empty: $resolved"
    }
    return [pscustomobject]@{
        Kind = $Kind
        Path = $resolved
        Length = [int64]$item.Length
        Sha256 = Get-SmokeFileSha256 -Path $resolved
    }
}

function Get-SmokeDirectoryReceipt {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $exists = Test-Path -LiteralPath $resolved -PathType Container
    $directories = @(if ($exists) {
        Get-ChildItem -LiteralPath $resolved -Recurse -Directory -Force | ForEach-Object {
            $_.FullName.Substring($resolved.Length).TrimStart('\','/').Replace('\','/')
        } | Sort-Object
    }
    else { @() })
    $files = @(if ($exists) {
        Get-ChildItem -LiteralPath $resolved -Recurse -File -Force | ForEach-Object {
            [ordered]@{
                RelativePath = $_.FullName.Substring($resolved.Length).TrimStart('\','/').Replace('\','/')
                Length = [int64]$_.Length
                Sha256 = Get-SmokeFileSha256 -Path $_.FullName
            }
        } | Sort-Object RelativePath
    }
    else { @() })
    return [pscustomobject]@{
        Path = $resolved
        Existed = $exists
        DirectoryCount = $directories.Count
        Directories = @($directories)
        FileCount = $files.Count
        Files = @($files)
    }
}

function Compare-SmokeDirectoryReceipt {
    param(
        [Parameter(Mandatory = $true)] $Before,
        [Parameter(Mandatory = $true)] $After
    )

    $beforeDirectoriesJson = @($Before.Directories) | ConvertTo-Json -Depth 5 -Compress
    $afterDirectoriesJson = @($After.Directories) | ConvertTo-Json -Depth 5 -Compress
    $beforeFilesJson = @($Before.Files) | ConvertTo-Json -Depth 5 -Compress
    $afterFilesJson = @($After.Files) | ConvertTo-Json -Depth 5 -Compress
    $passed = [bool]$Before.Existed -eq [bool]$After.Existed -and
        [int]$Before.DirectoryCount -eq [int]$After.DirectoryCount -and
        [string]::Equals($beforeDirectoriesJson, $afterDirectoriesJson, [System.StringComparison]::Ordinal) -and
        [int]$Before.FileCount -eq [int]$After.FileCount -and
        [string]::Equals($beforeFilesJson, $afterFilesJson, [System.StringComparison]::Ordinal)
    return [pscustomobject]@{
        Path = [string]$Before.Path
        ExistedBefore = [bool]$Before.Existed
        ExistsAfter = [bool]$After.Existed
        DirectoryCountBefore = [int]$Before.DirectoryCount
        DirectoryCountAfter = [int]$After.DirectoryCount
        DirectoriesBefore = @($Before.Directories)
        DirectoriesAfter = @($After.Directories)
        FileCountBefore = [int]$Before.FileCount
        FileCountAfter = [int]$After.FileCount
        FilesBefore = @($Before.Files)
        FilesAfter = @($After.Files)
        Passed = $passed
    }
}

function Get-SmokeNoQaRuntimeReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $StateDir
    )

    $activationPath = [System.IO.Path]::GetFullPath((Join-Path $StateDir 'qa-host-activation.json'))
    $qaRoot = [System.IO.Path]::GetFullPath((Join-Path $StateDir 'qa-host'))
    $runtimeRoot = [System.IO.Path]::GetFullPath((Join-Path $GameDir 'BepInEx\plugins\DTMAPI'))
    $expectedRuntimeDlls = @(
        'DTMAPI.Abstractions.dll',
        'DTMAPI.BepInExBootstrap.dll',
        'DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll',
        'DTMAPI.ModConfigMenu.dll'
    ) | Sort-Object
    $actualRuntimeDlls = if (Test-Path -LiteralPath $runtimeRoot -PathType Container) {
        @(Get-ChildItem -LiteralPath $runtimeRoot -Recurse -File -Filter '*.dll' | ForEach-Object { $_.Name } | Sort-Object)
    }
    else {
        @()
    }
    $qaDlls = New-Object 'System.Collections.Generic.List[string]'
    foreach ($searchRoot in @((Join-Path $GameDir 'BepInEx\plugins'), $StateDir)) {
        if (Test-Path -LiteralPath $searchRoot -PathType Container) {
            foreach ($file in @(Get-ChildItem -LiteralPath $searchRoot -Recurse -File -Filter 'DTMAPI.GameBridge.DolocTown.QA.dll' -ErrorAction SilentlyContinue)) {
                $qaDlls.Add([System.IO.Path]::GetFullPath($file.FullName)) | Out-Null
            }
        }
    }
    $qaDllPaths = @($qaDlls.ToArray() | Sort-Object -Unique)
    $runtimeShapePassed = [string]::Equals(
        (@($expectedRuntimeDlls) -join "`n"),
        (@($actualRuntimeDlls) -join "`n"),
        [System.StringComparison]::Ordinal)
    $activationAbsent = -not (Test-Path -LiteralPath $activationPath)
    $qaRootAbsent = -not (Test-Path -LiteralPath $qaRoot)
    return [pscustomobject]@{
        ActivationPath = $activationPath
        ActivationAbsent = $activationAbsent
        QaRootPath = $qaRoot
        QaRootAbsent = $qaRootAbsent
        RuntimeRoot = $runtimeRoot
        ExpectedRuntimeDlls = @($expectedRuntimeDlls)
        ActualRuntimeDlls = @($actualRuntimeDlls)
        ExactFiveRuntimeDlls = $runtimeShapePassed
        QaDllPaths = @($qaDllPaths)
        QaDllAbsent = $qaDllPaths.Count -eq 0
        Passed = $activationAbsent -and $qaRootAbsent -and $qaDllPaths.Count -eq 0 -and $runtimeShapePassed
    }
}

function Test-SmokeNoQaReceiptComparison {
    $absentPath = Join-Path $repo ('tmp\test-runs\noqa-directory-receipt-absent-' + [Guid]::NewGuid().ToString('N'))
    if (Test-Path -LiteralPath $absentPath) {
        throw "No-QA directory-receipt absent fixture unexpectedly exists: $absentPath"
    }
    $absentReceipt = Get-SmokeDirectoryReceipt -Path $absentPath
    $absentReceiptAccepted = -not [bool]$absentReceipt.Existed -and
        [int]$absentReceipt.DirectoryCount -eq 0 -and @($absentReceipt.Directories).Count -eq 0 -and
        [int]$absentReceipt.FileCount -eq 0 -and @($absentReceipt.Files).Count -eq 0
    $unchangedBefore = [pscustomobject]@{
        Path = 'X:\evidence\DEBUG-CONSOLE-UI'
        Existed = $true
        DirectoryCount = 1
        Directories = @('old')
        FileCount = 1
        Files = @([ordered]@{ RelativePath = 'old\summary.txt'; Length = 3; Sha256 = ('A' * 64) })
    }
    $unchangedAfter = [pscustomobject]@{
        Path = $unchangedBefore.Path
        Existed = $true
        DirectoryCount = 1
        Directories = @('old')
        FileCount = 1
        Files = @([ordered]@{ RelativePath = 'old\summary.txt'; Length = 3; Sha256 = ('A' * 64) })
    }
    $changedAfter = [pscustomobject]@{
        Path = $unchangedBefore.Path
        Existed = $true
        DirectoryCount = 2
        Directories = @('new', 'old')
        FileCount = 2
        Files = @(
            [ordered]@{ RelativePath = 'old\summary.txt'; Length = 3; Sha256 = ('A' * 64) },
            [ordered]@{ RelativePath = 'new\summary.txt'; Length = 3; Sha256 = ('B' * 64) }
        )
    }
    $emptyDirectoryChangedAfter = [pscustomobject]@{
        Path = $unchangedBefore.Path
        Existed = $true
        DirectoryCount = 2
        Directories = @('new-empty', 'old')
        FileCount = 1
        Files = @([ordered]@{ RelativePath = 'old\summary.txt'; Length = 3; Sha256 = ('A' * 64) })
    }
    $unchanged = Compare-SmokeDirectoryReceipt -Before $unchangedBefore -After $unchangedAfter
    $changed = Compare-SmokeDirectoryReceipt -Before $unchangedBefore -After $changedAfter
    $emptyDirectoryChanged = Compare-SmokeDirectoryReceipt -Before $unchangedBefore -After $emptyDirectoryChangedAfter
    return [pscustomobject]@{
        AbsentDirectoryReceiptAccepted = $absentReceiptAccepted
        AbsentDirectoryCount = [int]$absentReceipt.DirectoryCount
        AbsentFileCount = [int]$absentReceipt.FileCount
        UnchangedAccepted = [bool]$unchanged.Passed
        GrowthRejected = -not [bool]$changed.Passed
        EmptyDirectoryGrowthRejected = -not [bool]$emptyDirectoryChanged.Passed
        Passed = $absentReceiptAccepted -and [bool]$unchanged.Passed -and
            -not [bool]$changed.Passed -and -not [bool]$emptyDirectoryChanged.Passed
    }
}

function Test-SmokeDeadlineHasBudget {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [datetime] $ObservedAt = (Get-Date)
    )

    return $ObservedAt -lt $Deadline
}

function Get-SmokeRemainingBudgetMilliseconds {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [int] $MaximumMilliseconds = [int]::MaxValue,
        [datetime] $ObservedAt = (Get-Date)
    )

    if ($MaximumMilliseconds -le 0 -or -not (Test-SmokeDeadlineHasBudget -Deadline $Deadline -ObservedAt $ObservedAt)) {
        return 0
    }

    $remainingMilliseconds = [Math]::Floor(($Deadline - $ObservedAt).TotalMilliseconds)
    if ($remainingMilliseconds -le 0) {
        return 0
    }

    return [int][Math]::Min([double]$MaximumMilliseconds, $remainingMilliseconds)
}

function Get-SmokeRemainingBudgetSeconds {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [int] $MaximumSeconds = [int]::MaxValue,
        [datetime] $ObservedAt = (Get-Date)
    )

    $remainingSeconds = [int][Math]::Ceiling(($Deadline - $ObservedAt).TotalSeconds)
    if ($remainingSeconds -le 0 -or $MaximumSeconds -le 0) {
        return 0
    }

    return [Math]::Min($remainingSeconds, $MaximumSeconds)
}

function Get-SmokeCappedDeadline {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [Parameter(Mandatory = $true)] [int] $MaximumMilliseconds,
        [datetime] $ObservedAt = (Get-Date)
    )

    if ($MaximumMilliseconds -le 0 -or -not (Test-SmokeDeadlineHasBudget -Deadline $Deadline -ObservedAt $ObservedAt)) {
        return $ObservedAt
    }

    $candidate = $ObservedAt.AddMilliseconds($MaximumMilliseconds)
    if ($candidate -lt $Deadline) {
        return $candidate
    }

    return $Deadline
}

function Invoke-SmokeDeadlineGuardedAction {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [Parameter(Mandatory = $true)] [scriptblock] $Action,
        [ValidateRange(1, [int]::MaxValue)] [int] $MinimumExecutionBudgetMilliseconds = 1,
        [scriptblock] $Clock = { Get-Date }
    )

    $startedAt = [datetime](& $Clock)
    $remainingAtStart = Get-SmokeRemainingBudgetMilliseconds -Deadline $Deadline -ObservedAt $startedAt
    if ($remainingAtStart -lt $MinimumExecutionBudgetMilliseconds) {
        return [pscustomobject]@{
            Executed = $false
            Result = $false
            StartedAt = $startedAt
            CompletedAt = $startedAt
            RemainingAtStartMilliseconds = $remainingAtStart
            CompletedBeforeDeadline = $false
            Accepted = $false
            Reason = if ($remainingAtStart -le 0) { 'expired-at-entry' } else { 'insufficient-budget' }
        }
    }

    $result = & $Action
    $completedAt = [datetime](& $Clock)
    $completedBeforeDeadline = Test-SmokeDeadlineHasBudget -Deadline $Deadline -ObservedAt $completedAt
    return [pscustomobject]@{
        Executed = $true
        Result = $result
        StartedAt = $startedAt
        CompletedAt = $completedAt
        RemainingAtStartMilliseconds = $remainingAtStart
        CompletedBeforeDeadline = $completedBeforeDeadline
        Accepted = $completedBeforeDeadline -and [bool]$result
        Reason = if ($completedBeforeDeadline) { 'completed-before-deadline' } else { 'completed-after-deadline' }
    }
}

function Start-SmokeCappedSleep {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [Parameter(Mandatory = $true)] [ValidateRange(1, [int]::MaxValue)] [int] $Milliseconds
    )

    $sleepMilliseconds = Get-SmokeRemainingBudgetMilliseconds -Deadline $Deadline -MaximumMilliseconds $Milliseconds
    if ($sleepMilliseconds -le 0) {
        return $false
    }

    Start-Sleep -Milliseconds $sleepMilliseconds
    return Test-SmokeDeadlineHasBudget -Deadline $Deadline
}

function Test-SmokeNoQaDeadlineContract {
    $origin = [datetime]::SpecifyKind([datetime]'2026-07-19T00:00:00', [System.DateTimeKind]::Utc)

    $expiredCalls = New-Object 'System.Collections.Generic.List[int]'
    $expiredClock = { return $origin }.GetNewClosure()
    $expiredAction = { $expiredCalls.Add(1) | Out-Null; return $true }.GetNewClosure()
    $expired = Invoke-SmokeDeadlineGuardedAction -Deadline $origin -Action $expiredAction -Clock $expiredClock

    $shortCalls = New-Object 'System.Collections.Generic.List[int]'
    $shortClock = { return $origin }.GetNewClosure()
    $shortAction = { $shortCalls.Add(1) | Out-Null; return $true }.GetNewClosure()
    $short = Invoke-SmokeDeadlineGuardedAction `
        -Deadline $origin.AddMilliseconds(20) `
        -Action $shortAction `
        -MinimumExecutionBudgetMilliseconds 40 `
        -Clock $shortClock

    $lateClockValues = New-Object 'System.Collections.Generic.Queue[datetime]'
    $lateClockValues.Enqueue($origin)
    $lateClockValues.Enqueue($origin.AddMilliseconds(126))
    $lateClock = { return $lateClockValues.Dequeue() }.GetNewClosure()
    $lateCalls = New-Object 'System.Collections.Generic.List[int]'
    $lateAction = { $lateCalls.Add(1) | Out-Null; return $true }.GetNewClosure()
    $late = Invoke-SmokeDeadlineGuardedAction `
        -Deadline $origin.AddMilliseconds(125) `
        -Action $lateAction `
        -Clock $lateClock

    $onTimeClockValues = New-Object 'System.Collections.Generic.Queue[datetime]'
    $onTimeClockValues.Enqueue($origin)
    $onTimeClockValues.Enqueue($origin.AddMilliseconds(124))
    $onTimeClock = { return $onTimeClockValues.Dequeue() }.GetNewClosure()
    $onTime = Invoke-SmokeDeadlineGuardedAction `
        -Deadline $origin.AddMilliseconds(125) `
        -Action { return $true } `
        -Clock $onTimeClock

    $cappedMilliseconds = Get-SmokeRemainingBudgetMilliseconds `
        -Deadline $origin.AddMilliseconds(125) `
        -MaximumMilliseconds 5000 `
        -ObservedAt $origin
    $passed = -not [bool]$expired.Executed -and $expiredCalls.Count -eq 0 -and
        [string]$expired.Reason -eq 'expired-at-entry' -and
        -not [bool]$short.Executed -and $shortCalls.Count -eq 0 -and
        [string]$short.Reason -eq 'insufficient-budget' -and
        [bool]$late.Executed -and $lateCalls.Count -eq 1 -and
        -not [bool]$late.CompletedBeforeDeadline -and -not [bool]$late.Accepted -and
        [bool]$onTime.Executed -and [bool]$onTime.CompletedBeforeDeadline -and [bool]$onTime.Accepted -and
        $cappedMilliseconds -eq 125
    return [pscustomobject]@{
        ExpiredAtEntryDidNotExecute = -not [bool]$expired.Executed -and $expiredCalls.Count -eq 0
        ShortBudgetDidNotExecute = -not [bool]$short.Executed -and $shortCalls.Count -eq 0
        LateCompletionRejected = [bool]$late.Executed -and -not [bool]$late.Accepted
        OnTimeCompletionAccepted = [bool]$onTime.Accepted
        RequestedWaitMilliseconds = 5000
        CappedWaitMilliseconds = $cappedMilliseconds
        Passed = $passed
    }
}

function Get-SmokeNoQaAnimalInputSequences {
    param([Parameter(Mandatory = $true)] [bool] $AutoDrive)

    if (-not $AutoDrive) {
        return 'NoQaAnimalViewerEscape'
    }

    return @(
        [string]::Join("`n", [string[]]@(
            'NoQaAnimalApproachA1',
            'NoQaAnimalOpenE1',
            'NoQaAnimalSelectSecondRow',
            'NoQaAnimalViewerEscape'
        )),
        [string]::Join("`n", [string[]]@(
            'NoQaAnimalApproachA1',
            'NoQaAnimalOpenE1',
            'NoQaAnimalApproachA2',
            'NoQaAnimalOpenE2',
            'NoQaAnimalSelectSecondRow',
            'NoQaAnimalViewerEscape'
        ))
    )
}

function Test-SmokeNoQaAnimalInputSequences {
    $autoDrive = @(Get-SmokeNoQaAnimalInputSequences -AutoDrive $true)
    $manual = @(Get-SmokeNoQaAnimalInputSequences -AutoDrive $false)
    $oneAttempt = [string]::Join("`n", [string[]]@(
        'NoQaAnimalApproachA1',
        'NoQaAnimalOpenE1',
        'NoQaAnimalSelectSecondRow',
        'NoQaAnimalViewerEscape'
    ))
    $twoAttempts = [string]::Join("`n", [string[]]@(
        'NoQaAnimalApproachA1',
        'NoQaAnimalOpenE1',
        'NoQaAnimalApproachA2',
        'NoQaAnimalOpenE2',
        'NoQaAnimalSelectSecondRow',
        'NoQaAnimalViewerEscape'
    ))
    $passed = $autoDrive.Count -eq 2 -and
        [string]::Equals([string]$autoDrive[0], $oneAttempt, [System.StringComparison]::Ordinal) -and
        [string]::Equals([string]$autoDrive[1], $twoAttempts, [System.StringComparison]::Ordinal) -and
        $manual.Count -eq 1 -and
        [string]::Equals([string]$manual[0], 'NoQaAnimalViewerEscape', [System.StringComparison]::Ordinal) -and
        @($autoDrive | Where-Object { [string]$_ -match 'System\.Object\[\]' }).Count -eq 0
    return [pscustomobject]@{
        AutoDriveSequenceCount = $autoDrive.Count
        AutoDriveSequences = @($autoDrive)
        ManualSequences = @($manual)
        Passed = $passed
    }
}

function New-SmokeDirectoryBaseline {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $BackupPath
    )

    $receipt = Get-SmokeDirectoryReceipt -Path $Path
    $resolvedBackup = [System.IO.Path]::GetFullPath($BackupPath)
    if (Test-Path -LiteralPath $resolvedBackup) {
        throw "Directory transaction backup already exists: $resolvedBackup"
    }
    if ($receipt.Existed) {
        Copy-Item -LiteralPath $receipt.Path -Destination $resolvedBackup -Recurse
    }
    return [pscustomobject]@{
        Path = $receipt.Path
        BackupPath = $resolvedBackup
        Existed = $receipt.Existed
        DirectoryCount = $receipt.DirectoryCount
        Directories = @($receipt.Directories)
        FileCount = $receipt.FileCount
        Files = @($receipt.Files)
    }
}

function Restore-SmokeDirectoryBaseline {
    param(
        [Parameter(Mandatory = $true)] $Baseline,
        [Parameter(Mandatory = $true)] [string] $AllowedRoot,
        [Parameter(Mandatory = $true)] [string] $EvidencePath
    )

    $target = [System.IO.Path]::GetFullPath([string]$Baseline.Path)
    $allowed = [System.IO.Path]::GetFullPath($AllowedRoot).TrimEnd('\','/') + [System.IO.Path]::DirectorySeparatorChar
    if (-not $target.StartsWith($allowed, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Directory transaction target escaped its allowed root: target=$target allowed=$allowed"
    }
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force
    }
    if ([bool]$Baseline.Existed) {
        if (-not (Test-Path -LiteralPath ([string]$Baseline.BackupPath) -PathType Container)) {
            throw "Directory transaction backup is missing: $([string]$Baseline.BackupPath)"
        }
        Copy-Item -LiteralPath ([string]$Baseline.BackupPath) -Destination $target -Recurse
    }
    $after = Get-SmokeDirectoryReceipt -Path $target
    $expectedDirectoriesJson = @($Baseline.Directories) | ConvertTo-Json -Depth 5 -Compress
    $actualDirectoriesJson = @($after.Directories) | ConvertTo-Json -Depth 5 -Compress
    $expectedFilesJson = @($Baseline.Files) | ConvertTo-Json -Depth 5 -Compress
    $actualFilesJson = @($after.Files) | ConvertTo-Json -Depth 5 -Compress
    $passed = [bool]$Baseline.Existed -eq [bool]$after.Existed -and
        [int]$Baseline.DirectoryCount -eq [int]$after.DirectoryCount -and
        [string]::Equals($expectedDirectoriesJson, $actualDirectoriesJson, [System.StringComparison]::Ordinal) -and
        [int]$Baseline.FileCount -eq [int]$after.FileCount -and
        [string]::Equals($expectedFilesJson, $actualFilesJson, [System.StringComparison]::Ordinal)
    Write-SmokeJsonObject -Path $EvidencePath -Value ([ordered]@{
        TargetPath = $target
        BackupPath = [string]$Baseline.BackupPath
        ExistedBefore = [bool]$Baseline.Existed
        ExistsAfter = [bool]$after.Existed
        ExpectedDirectoryCount = [int]$Baseline.DirectoryCount
        ActualDirectoryCount = [int]$after.DirectoryCount
        ExpectedDirectories = @($Baseline.Directories)
        ActualDirectories = @($after.Directories)
        ExpectedFileCount = [int]$Baseline.FileCount
        ActualFileCount = [int]$after.FileCount
        ExpectedFiles = @($Baseline.Files)
        ActualFiles = @($after.Files)
        Passed = $passed
    })
    return $passed
}

function Get-SmokeQaG4ExpectedEvidencePaths {
    param(
        [Parameter(Mandatory = $true)] [bool] $TitleSettingsUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $ManagerStatusUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $ManagerMvpUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $OfficialModUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $PauseMenuLayoutEnabled,
        [Parameter(Mandatory = $true)] [bool] $DebugConsoleEnabled,
        [Parameter(Mandatory = $true)] [bool] $SaveSlotsPagingEnabled,
        [Parameter(Mandatory = $true)] [bool] $MoreSavesPostTitlePanelEnabled,
        [Parameter(Mandatory = $true)] [bool] $AnimalObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $EquipmentSlotsObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $CameraPlayableEnabled
    )

    $paths = New-Object 'System.Collections.Generic.List[string]'
    if ($TitleSettingsUiEnabled) { $paths.Add('ui\title-settings.png') }
    if ($ManagerStatusUiEnabled -or $ManagerMvpUiEnabled) { $paths.Add('ui\manager-status-page.png') }
    if ($ManagerMvpUiEnabled) {
        $paths.Add('ui\manager-mods-page.png')
        $paths.Add('ui\manager-advanced-page.png')
        $paths.Add('ui\manager-logs-page.png')
    }
    if ($OfficialModUiEnabled) { $paths.Add('ui\official-mod-ui.png') }
    if ($PauseMenuLayoutEnabled) { $paths.Add('ui\pause-menu-layout.png') }
    if ($DebugConsoleEnabled) { $paths.Add('ui\debug-console.png') }
    if ($SaveSlotsPagingEnabled) { $paths.Add('ui\official-save-ui.png') }
    if ($MoreSavesPostTitlePanelEnabled) {
        $paths.Add('ui\official-save-ui-post-title.png')
    }
    if ($AnimalObservationEnabled) { $paths.Add('ui\animal-panel.png') }
    if ($EquipmentSlotsObservationEnabled) {
        $paths.Add('ui\equipment-slots.png')
        $paths.Add('ui\equipment-slots-summary.txt')
    }
    if ($CameraPlayableEnabled) {
        foreach ($name in @('before.png','scale-4x.png','movement-start.png','movement-mid.png','movement-end.png','fallback-2x.png','reset.png','telemetry.csv')) {
            $paths.Add('camera-playable\' + $name)
        }
    }
    return @($paths.ToArray() | Sort-Object -Unique)
}

function New-SmokeQaHostStage {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $StateDir,
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [bool] $SkipQaBuild,
        [Parameter(Mandatory = $true)] [bool] $AutoFishingPerformanceEnabled,
        [Parameter(Mandatory = $true)] [string] $AutoFishingPerformanceProfile,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceTargetFish,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceWarmupFish,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceZeroWarmupSeconds,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceZeroMeasureSeconds,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceWarmupFrames,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceTargetFrames,
        [Parameter(Mandatory = $true)] [bool] $Batch5GcLadderEnabled,
        [Parameter(Mandatory = $true)] [string] $Batch5GcLadderDomain,
        [Parameter(Mandatory = $true)] [string] $Batch5GcLadderLevel,
        [Parameter(Mandatory = $true)] [string] $Batch5GcLadderWorkload,
        [Parameter(Mandatory = $true)] [double] $Batch5GcLadderMultiplier,
        [Parameter(Mandatory = $true)] [int] $Batch5GcLadderMeasureSeconds,
        [Parameter(Mandatory = $true)] [int] $Batch5GcLadderSampleSeconds,
        [Parameter(Mandatory = $true)] [int] $Batch5GcLadderTargetUnits,
        [Parameter(Mandatory = $true)] [bool] $Batch6AutoFishingEnabled,
        [Parameter(Mandatory = $true)] [string] $Batch6AutoFishingLevel,
        [Parameter(Mandatory = $true)] [string] $Batch6AutoFishingScenario,
        [Parameter(Mandatory = $true)] [int] $Batch6AutoFishingMeasureSeconds,
        [Parameter(Mandatory = $true)] [int] $Batch6AutoFishingSampleSeconds,
        [Parameter(Mandatory = $true)] [int] $Batch6AutoFishingWarmupFish,
        [Parameter(Mandatory = $true)] [int] $Batch6AutoFishingTargetFish,
        [Parameter(Mandatory = $true)] [bool] $Batch6AutoFishingFormal,
        [Parameter(Mandatory = $true)] [string] $Batch6AutoFishingFormalContract,
        [Parameter(Mandatory = $true)] [double] $Batch6AutoFishingMultiplier,
        [Parameter(Mandatory = $true)] [double] $Batch6AutoFishingCastChargeRatio,
        [Parameter(Mandatory = $true)] [bool] $Batch6AutoFishingManualMovementCancel,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingExpectedPackageSha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingExpectedEntrySha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingExpectedManifestSha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingExpectedPolicySha256,
        [Parameter(Mandatory = $true)] [bool] $Batch6AutoFishingManagerEnabled,
        [Parameter(Mandatory = $true)] [string] $Batch6AutoFishingManagerMode,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerProductRoot,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerMarkerSha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerExpectedPackageSha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerExpectedEntrySha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerExpectedManifestSha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerExpectedPolicySha256,
        [Parameter(Mandatory = $true)] [bool] $CustomEntityContractEnabled,
        [Parameter(Mandatory = $true)] [bool] $FishRoeTooltipObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $DiagnosticsSnapshotEnabled,
        [Parameter(Mandatory = $true)] [string] $DiagnosticsScenario,
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [string[]] $DiagnosticsExpectedFeatureIds,
        [Parameter(Mandatory = $true)] [bool] $ObserveSaveLoaded,
        [Parameter(Mandatory = $true)] [bool] $ObserveSaveSaved,
        [Parameter(Mandatory = $true)] [bool] $ObserveWorkshopReloadCompleted,
        [Parameter(Mandatory = $true)] [string] $SaveTestMode,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $DisposableSaveFixtureSaveRoot,
        [Parameter(Mandatory = $true)] [bool] $RequireDisposableSaveRedirect,
        [Parameter(Mandatory = $true)] [bool] $TitleSettingsUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $ManagerStatusUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $ManagerMvpUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $OfficialModUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $PauseMenuLayoutEnabled,
        [Parameter(Mandatory = $true)] [bool] $DebugConsoleEnabled,
        [Parameter(Mandatory = $true)] [bool] $SaveSlotsPagingEnabled,
        [Parameter(Mandatory = $true)] [int] $SaveSlot,
        [Parameter(Mandatory = $true)] [bool] $AnimalObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $EquipmentSlotsObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $AudioObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $HatchVoiceEnabled,
        [Parameter(Mandatory = $true)] [bool] $CameraPlayableEnabled,
        [Parameter(Mandatory = $true)] [bool] $ContentMetadataObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $ContentMetadataExpectOilAbsent,
        [Parameter(Mandatory = $true)] [bool] $ContinuousHomePageTerminalEnabled,
        [Parameter(Mandatory = $true)] [bool] $ContinuousHomePageRequiresSaveLoaded,
        [Parameter(Mandatory = $true)] [double] $ContinuousHomePageSeconds,
        [Parameter(Mandatory = $true)] [bool] $TitleLifecycleEnabled,
        [Parameter(Mandatory = $true)] [bool] $ProductOwnerRefreshEnabled,
        [Parameter(Mandatory = $true)] [bool] $AdvancedProductOwnerDeactivationEnabled,
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [string[]] $AdvancedProductOwnerDeactivationOwnerIds,
        [Parameter(Mandatory = $true)] [bool] $ExternalPlayerInputObservationEnabled,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $ExternalPlayerInputToken,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $ExternalPlayerInputMarkerPath,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $ExternalPlayerInputEvidenceRoot,
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [string[]] $G5WorldMutationCases,
        [Parameter(Mandatory = $true)] [string] $DebugConsoleSaveAcceptancePhase,
        [Parameter(Mandatory = $true)] [int] $ExpectedDebugConsoleMoney,
        [Parameter(Mandatory = $true)] [int] $ExpectedMoreEquipmentSlotsBackpackBaseline,
        [Parameter(Mandatory = $true)] [long] $ExpectedMoreEquipmentSlotsCommittedGeneration,
        [Parameter(Mandatory = $true)] [int] $ExpectedMoreEquipmentSlotsCommittedOccupied,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $ExpectedMoreEquipmentSlotsCommittedSlots,
        [Parameter(Mandatory = $true)] [bool] $MoreEquipmentSlotsInterruptedCandidateObservationEnabled,
        [Parameter(Mandatory = $true)] [long] $ExpectedMoreEquipmentSlotsCandidatePreGeneration,
        [Parameter(Mandatory = $true)] [string] $MoreEquipmentSlotsTransitionPhase,
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [string[]] $G6LifecycleCases,
        [Parameter(Mandatory = $true)] [int] $G6InitialDelaySeconds,
        [Parameter(Mandatory = $true)] [string] $G6AutoFishingScenario,
        [Parameter(Mandatory = $true)] [double] $G6AutoFishingCastChargeRatio,
        [Parameter(Mandatory = $true)] [int] $G6AutoFishingSoakLoops,
        [Parameter(Mandatory = $true)] [string] $G6AutoFishingToggleKey,
        [Parameter(Mandatory = $true)] [bool] $G6AutoFishingMiniGameComplete,
        [Parameter(Mandatory = $true)] [bool] $G6AutoFishingMonoGate,
        [Parameter(Mandatory = $true)] [int] $G6SaveLoadCycleCount,
        [Parameter(Mandatory = $true)] [int] $G6SaveLoadCycleInitialTitleIdleSeconds,
        [Parameter(Mandatory = $true)] [int] $G6SaveLoadCycleIntervalSeconds,
        [Parameter(Mandatory = $true)] [int] $G6SaveLoadCycleInSaveSeconds,
        [Parameter(Mandatory = $true)] [bool] $G6PreLoadGcProbe,
        [Parameter(Mandatory = $true)] [string] $G6SaveLoadObjectSnapshotMode,
        [Parameter(Mandatory = $true)] [string] $G6RootIsolationProfile,
        [Parameter(Mandatory = $true)] [string] $G6OwnerRootIsolationProfile,
        [Parameter(Mandatory = $true)] [string] $G6NativeLoadContinuationProbe,
        [Parameter(Mandatory = $true)] [int] $G6PendingPressureSeconds,
        [Parameter(Mandatory = $true)] [double] $G6PendingPressureIntervalSeconds,
        [Parameter(Mandatory = $true)] [int] $G6LongTitleIdleSeconds
    )

    $projectPath = Join-Path $RepoRoot 'src\DTMAPI.GameBridge.DolocTown.QA\DTMAPI.GameBridge.DolocTown.QA.csproj'
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        throw "QA host project is missing: $projectPath"
    }
    if (-not $SkipQaBuild) {
        $dotnet = Get-DotNetExe -RepoRoot $RepoRoot
        # A PowerShell function returns every uncaptured pipeline object. Capture
        # native build output so callers receive only the stage receipt object.
        $qaBuildOutput = @(& $dotnet build $projectPath -c Release --nologo 2>&1)
        $qaBuildExitCode = $LASTEXITCODE
        foreach ($line in $qaBuildOutput) {
            Write-Host $line
        }
        if ($qaBuildExitCode -ne 0) {
            throw "QA host Release build failed: $projectPath"
        }
    }

    $version = Get-SmokeQaHostVersionAuthority -RepoRoot $RepoRoot
    $sourceDll = Join-Path (Split-Path -Parent $projectPath) 'bin\Release\netstandard2.0\DTMAPI.GameBridge.DolocTown.QA.dll'
    if (-not (Test-Path -LiteralPath $sourceDll -PathType Leaf)) {
        throw "QA host Release DLL is missing. Run without -SkipBuild or build it first: $sourceDll"
    }
    $qaAssemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($sourceDll)
    $qaVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($sourceDll)
    if ([string]$qaAssemblyName.Name -ne 'DTMAPI.GameBridge.DolocTown.QA' -or
        [string]$qaAssemblyName.Version -ne [string]$version.AssemblyVersion -or
        [string]$qaVersion.FileVersion -ne [string]$version.BinaryVersion -or
        [string]$qaVersion.ProductVersion -ne [string]$version.ReleaseVersion) {
        throw "QA host metadata mismatch. Expected DTMAPI.GameBridge.DolocTown.QA/$($version.AssemblyVersion)/$($version.BinaryVersion)/$($version.ReleaseVersion); actual $($qaAssemblyName.Name)/$($qaAssemblyName.Version)/$($qaVersion.FileVersion)/$($qaVersion.ProductVersion)."
    }

    $runtimeRoot = Join-Path $GameDir 'BepInEx\plugins\DTMAPI'
    $runtimeFiles = @(
        'DTMAPI.BepInExBootstrap.dll',
        'DTMAPI.Abstractions.dll',
        'DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll',
        'DTMAPI.ModConfigMenu.dll'
    )
    $actualRuntimeDlls = if (Test-Path -LiteralPath $runtimeRoot -PathType Container) {
        @(Get-ChildItem -LiteralPath $runtimeRoot -Recurse -File -Filter '*.dll' | ForEach-Object { $_.Name } | Sort-Object)
    }
    else {
        @()
    }
    if (($actualRuntimeDlls -join '|') -ne (($runtimeFiles | Sort-Object) -join '|')) {
        throw "QA host staging requires the exact five installed production Runtime DLLs. Expected=$($runtimeFiles -join ',') Actual=$($actualRuntimeDlls -join ',')"
    }
    foreach ($runtimeFile in $runtimeFiles) {
        $runtimePath = Join-Path $runtimeRoot $runtimeFile
        $runtimeAssemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($runtimePath)
        $runtimeVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($runtimePath)
        if ([string]$runtimeAssemblyName.Name -ne [System.IO.Path]::GetFileNameWithoutExtension($runtimeFile) -or
            [string]$runtimeAssemblyName.Version -ne [string]$version.AssemblyVersion -or
            [string]$runtimeVersion.FileVersion -ne [string]$version.BinaryVersion -or
            [string]$runtimeVersion.ProductVersion -ne [string]$version.ReleaseVersion) {
            throw "QA host staging found incompatible installed Runtime metadata: $runtimePath"
        }
        $runtimeAssembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($runtimePath))
        if (@($runtimeAssembly.GetReferencedAssemblies() | Where-Object { $_.Name -eq 'DTMAPI.GameBridge.DolocTown.QA' }).Count -ne 0) {
            throw "Production Runtime assembly contains a forbidden QA AssemblyRef: $runtimePath"
        }
    }

    $runId = [Guid]::NewGuid().ToString('N')
    $qaRoot = Join-Path $StateDir 'qa-host'
    $runDir = Join-Path $qaRoot $runId
    $activationPath = Join-Path $StateDir 'qa-host-activation.json'
    $settingsPath = Join-Path $runDir 'qa-settings.json'
    $stagedDll = Join-Path $runDir 'DTMAPI.GameBridge.DolocTown.QA.dll'
    $expectedQaEvidencePaths = @(Get-SmokeQaG4ExpectedEvidencePaths `
        -TitleSettingsUiEnabled $TitleSettingsUiEnabled `
        -ManagerStatusUiEnabled $ManagerStatusUiEnabled `
        -ManagerMvpUiEnabled $ManagerMvpUiEnabled `
        -OfficialModUiEnabled $OfficialModUiEnabled `
        -PauseMenuLayoutEnabled $PauseMenuLayoutEnabled `
        -DebugConsoleEnabled $DebugConsoleEnabled `
        -SaveSlotsPagingEnabled $SaveSlotsPagingEnabled `
        -MoreSavesPostTitlePanelEnabled ([bool]$SaveSlotsPagingEnabled -and [bool]$AdvancedProductOwnerDeactivationEnabled -and @($AdvancedProductOwnerDeactivationOwnerIds | Where-Object { $_ -eq 'DTMAPI.MoreSavesMod' }).Count -gt 0) `
        -AnimalObservationEnabled $AnimalObservationEnabled `
        -EquipmentSlotsObservationEnabled $EquipmentSlotsObservationEnabled `
        -CameraPlayableEnabled $CameraPlayableEnabled)
    if (Test-Path -LiteralPath $activationPath) {
        throw "QA host activation already exists; refusing to overwrite unknown state: $activationPath"
    }
    if (Test-Path -LiteralPath $qaRoot) {
        throw "QA host staging root already exists; recover or classify it before another run: $qaRoot"
    }

    try {
        New-Item -ItemType Directory -Path $runDir -Force | Out-Null
        Copy-Item -LiteralPath $sourceDll -Destination $stagedDll
        Write-SmokeJsonObject -Path $settingsPath -Value ([ordered]@{
            schemaVersion = 1
            protocolVersion = 7
            runId = $runId
            mode = 'participant-only'
            SaveTestMode = $SaveTestMode
            DisposableSaveFixtureSaveRoot = $DisposableSaveFixtureSaveRoot
            RequireDisposableSaveRedirect = $RequireDisposableSaveRedirect
            AutoFishingPerformanceEnabled = $AutoFishingPerformanceEnabled
            AutoFishingPerformanceProfile = $AutoFishingPerformanceProfile
            AutoFishingPerformanceTargetFish = $AutoFishingPerformanceTargetFish
            AutoFishingPerformanceWarmupFish = $AutoFishingPerformanceWarmupFish
            AutoFishingPerformanceZeroWarmupSeconds = $AutoFishingPerformanceZeroWarmupSeconds
            AutoFishingPerformanceZeroMeasureSeconds = $AutoFishingPerformanceZeroMeasureSeconds
            AutoFishingPerformanceWarmupFrames = $AutoFishingPerformanceWarmupFrames
            AutoFishingPerformanceTargetFrames = $AutoFishingPerformanceTargetFrames
            Batch5NoDemandEnabled = $AutoFishingPerformanceEnabled -and
                $AutoFishingPerformanceProfile -ceq 'InactiveNoConsumer' -and
                $AutoFishingPerformanceTargetFrames -gt 0
            Batch5NoDemandWarmupFrames = $AutoFishingPerformanceWarmupFrames
            Batch5NoDemandTargetFrames = $AutoFishingPerformanceTargetFrames
            Batch5GcLadderEnabled = $Batch5GcLadderEnabled
            Batch5GcLadderDomain = $Batch5GcLadderDomain
            Batch5GcLadderLevel = $Batch5GcLadderLevel
            Batch5GcLadderWorkload = $Batch5GcLadderWorkload
            Batch5GcLadderMultiplier = $Batch5GcLadderMultiplier
            Batch5GcLadderMeasureSeconds = $Batch5GcLadderMeasureSeconds
            Batch5GcLadderSampleSeconds = $Batch5GcLadderSampleSeconds
            Batch5GcLadderTargetUnits = $Batch5GcLadderTargetUnits
            Batch6AutoFishingPilot = [ordered]@{
                Enabled = $Batch6AutoFishingEnabled
                Level = $Batch6AutoFishingLevel
                Scenario = $Batch6AutoFishingScenario
                MeasureSeconds = $Batch6AutoFishingMeasureSeconds
                SampleSeconds = $Batch6AutoFishingSampleSeconds
                WarmupFish = $Batch6AutoFishingWarmupFish
                TargetFish = $Batch6AutoFishingTargetFish
                Formal = [bool]$Batch6AutoFishingFormal
                FormalContract = $Batch6AutoFishingFormalContract
                Multiplier = $Batch6AutoFishingMultiplier
                CastChargeRatio = $Batch6AutoFishingCastChargeRatio
                ManualMovementCancel = $Batch6AutoFishingManualMovementCancel
                ToggleKey = $G6AutoFishingToggleKey
                ExpectedPackageSha256 = $Batch6AutoFishingExpectedPackageSha256
                ExpectedEntryDllSha256 = $Batch6AutoFishingExpectedEntrySha256
                ExpectedManifestSha256 = $Batch6AutoFishingExpectedManifestSha256
                ExpectedReferencePolicySha256 = $Batch6AutoFishingExpectedPolicySha256
            }
            Batch6AutoFishingManagerLifecycle = [ordered]@{
                Enabled = $Batch6AutoFishingManagerEnabled
                Mode = $Batch6AutoFishingManagerMode
                ExpectedProductRoot = $Batch6AutoFishingManagerProductRoot
                ExpectedDisabledMarkerSha256 = $Batch6AutoFishingManagerMarkerSha256
                ExpectedPackageSha256 = $Batch6AutoFishingManagerExpectedPackageSha256
                ExpectedEntryDllSha256 = $Batch6AutoFishingManagerExpectedEntrySha256
                ExpectedManifestSha256 = $Batch6AutoFishingManagerExpectedManifestSha256
                ExpectedReferencePolicySha256 = $Batch6AutoFishingManagerExpectedPolicySha256
            }
            CustomEntityContractEnabled = $CustomEntityContractEnabled
            FishRoeTooltipObservationEnabled = $FishRoeTooltipObservationEnabled
            DiagnosticsSnapshotEnabled = $DiagnosticsSnapshotEnabled
            DiagnosticsScenario = $DiagnosticsScenario
            DiagnosticsExpectedFeatureIds = @($DiagnosticsExpectedFeatureIds)
            ObserveSaveLoaded = $ObserveSaveLoaded
            ObserveSaveSaved = $ObserveSaveSaved
            ObserveWorkshopReloadCompleted = $ObserveWorkshopReloadCompleted
            TitleSettingsUiEnabled = $TitleSettingsUiEnabled
            ManagerStatusUiEnabled = $ManagerStatusUiEnabled
            ManagerMvpUiEnabled = $ManagerMvpUiEnabled
            OfficialModUiEnabled = $OfficialModUiEnabled
            PauseMenuLayoutEnabled = $PauseMenuLayoutEnabled
            DebugConsoleEnabled = $DebugConsoleEnabled
            SaveSlotsPagingEnabled = $SaveSlotsPagingEnabled
            SaveSlot = $SaveSlot
            AnimalObservationEnabled = $AnimalObservationEnabled
            EquipmentSlotsObservationEnabled = $EquipmentSlotsObservationEnabled
            AudioObservationEnabled = $AudioObservationEnabled
            HatchVoiceEnabled = $HatchVoiceEnabled
            CameraPlayableEnabled = $CameraPlayableEnabled
            ContentMetadataObservationEnabled = $ContentMetadataObservationEnabled
            ContentMetadataExpectOilAbsent = $ContentMetadataExpectOilAbsent
            ContinuousHomePageTerminalEnabled = $ContinuousHomePageTerminalEnabled
            ContinuousHomePageRequiresSaveLoaded = $ContinuousHomePageRequiresSaveLoaded
            ContinuousHomePageSeconds = $ContinuousHomePageSeconds
            TitleLifecycleEnabled = $TitleLifecycleEnabled
            ProductOwnerRefreshEnabled = $ProductOwnerRefreshEnabled
            AdvancedProductOwnerDeactivationEnabled = $AdvancedProductOwnerDeactivationEnabled
            AdvancedProductOwnerDeactivationOwnerIds = @($AdvancedProductOwnerDeactivationOwnerIds)
            ExternalPlayerInputObservationEnabled = $ExternalPlayerInputObservationEnabled
            ExternalPlayerInputToken = $ExternalPlayerInputToken
            ExternalPlayerInputMarkerPath = $ExternalPlayerInputMarkerPath
            ExternalPlayerInputEvidenceRoot = $ExternalPlayerInputEvidenceRoot
            G5WorldMutationCases = @($G5WorldMutationCases)
            DebugConsoleSaveAcceptancePhase =
                $DebugConsoleSaveAcceptancePhase
            ExpectedDebugConsoleMoney =
                $ExpectedDebugConsoleMoney
            ExpectedMoreEquipmentSlotsBackpackBaseline =
                $ExpectedMoreEquipmentSlotsBackpackBaseline
            ExpectedMoreEquipmentSlotsCommittedGeneration =
                $ExpectedMoreEquipmentSlotsCommittedGeneration
            ExpectedMoreEquipmentSlotsCommittedOccupied =
                $ExpectedMoreEquipmentSlotsCommittedOccupied
            ExpectedMoreEquipmentSlotsCommittedSlots =
                $ExpectedMoreEquipmentSlotsCommittedSlots
            ExpectedMoreEquipmentSlotsCandidatePreGeneration =
                $ExpectedMoreEquipmentSlotsCandidatePreGeneration
            MoreEquipmentSlotsInterruptedCandidateObservationEnabled =
                $MoreEquipmentSlotsInterruptedCandidateObservationEnabled
            MoreEquipmentSlotsTransitionPhase =
                $MoreEquipmentSlotsTransitionPhase
            G6LifecycleCases = @($G6LifecycleCases)
            G6InitialDelaySeconds = $G6InitialDelaySeconds
            G6AutoFishingScenario = $G6AutoFishingScenario
            G6AutoFishingCastChargeRatio = $G6AutoFishingCastChargeRatio
            G6AutoFishingSoakLoops = $G6AutoFishingSoakLoops
            G6AutoFishingToggleKey = $G6AutoFishingToggleKey
            G6AutoFishingMiniGameComplete = $G6AutoFishingMiniGameComplete
            G6AutoFishingMonoGate = $G6AutoFishingMonoGate
            G6SaveLoadCycleCount = $G6SaveLoadCycleCount
            G6SaveLoadCycleInitialTitleIdleSeconds = $G6SaveLoadCycleInitialTitleIdleSeconds
            G6SaveLoadCycleIntervalSeconds = $G6SaveLoadCycleIntervalSeconds
            G6SaveLoadCycleInSaveSeconds = $G6SaveLoadCycleInSaveSeconds
            G6PreLoadGcProbe = $G6PreLoadGcProbe
            G6SaveLoadObjectSnapshotMode = $G6SaveLoadObjectSnapshotMode
            G6RootIsolationProfile = $G6RootIsolationProfile
            G6OwnerRootIsolationProfile = $G6OwnerRootIsolationProfile
            G6NativeLoadContinuationProbe = $G6NativeLoadContinuationProbe
            G6PendingPressureSeconds = $G6PendingPressureSeconds
            G6PendingPressureIntervalSeconds = $G6PendingPressureIntervalSeconds
            G6LongTitleIdleSeconds = $G6LongTitleIdleSeconds
        })
        $dllReceipt = Get-SmokeQaHostFileReceipt -Kind 'QaAssembly' -Path $stagedDll
        $settingsReceipt = Get-SmokeQaHostFileReceipt -Kind 'QaSettings' -Path $settingsPath
        $activation = [ordered]@{
            schemaVersion = 1
            protocolVersion = 7
            runId = $runId
            runtimeReleaseVersion = [string]$version.ReleaseVersion
            runtimeBinaryVersion = [string]$version.BinaryVersion
            runtimeAssemblyVersion = [string]$version.AssemblyVersion
            assemblyName = [string]$qaAssemblyName.Name
            factoryTypeName = 'DTMAPI.GameBridge.DolocTown.QA.QaHostFactory'
            dllLength = [int64]$dllReceipt.Length
            dllSha256 = [string]$dllReceipt.Sha256
            dllAssemblyVersion = [string]$qaAssemblyName.Version
            dllFileVersion = [string]$qaVersion.FileVersion
            dllProductVersion = [string]$qaVersion.ProductVersion
            settingsLength = [int64]$settingsReceipt.Length
            settingsSha256 = [string]$settingsReceipt.Sha256
        }
        # The fixed activation receipt is the transaction commit point and must be written last.
        Write-SmokeJsonObject -Path $activationPath -Value $activation
        $activationReceipt = Get-SmokeQaHostFileReceipt -Kind 'Activation' -Path $activationPath
        $stage = [pscustomobject]@{
            RunId = $runId
            QaRoot = [System.IO.Path]::GetFullPath($qaRoot)
            RunDir = [System.IO.Path]::GetFullPath($runDir)
            ActivationPath = [System.IO.Path]::GetFullPath($activationPath)
            Artifacts = @($activationReceipt, $dllReceipt, $settingsReceipt)
            ExpectedQaEvidencePaths = @($expectedQaEvidencePaths)
            Activation = $activation
        }
        Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'qa-host-stage.json') -Value ([ordered]@{
            StagedAt = (Get-Date).ToUniversalTime().ToString('o')
            RunId = $stage.RunId
            QaRoot = $stage.QaRoot
            RunDir = $stage.RunDir
            ActivationPath = $stage.ActivationPath
            Artifacts = @($stage.Artifacts)
            ExpectedQaEvidencePaths = @($stage.ExpectedQaEvidencePaths)
            Activation = $stage.Activation
            ActivationWrittenLast = $true
        })
        return $stage
    }
    catch {
        foreach ($path in @($activationPath, $settingsPath, $stagedDll)) {
            if (Test-Path -LiteralPath $path -PathType Leaf) {
                Remove-Item -LiteralPath $path -Force
            }
        }
        if ((Test-Path -LiteralPath $runDir -PathType Container) -and @(Get-ChildItem -LiteralPath $runDir -Force).Count -eq 0) {
            Remove-Item -LiteralPath $runDir -Force
        }
        if ((Test-Path -LiteralPath $qaRoot -PathType Container) -and @(Get-ChildItem -LiteralPath $qaRoot -Force).Count -eq 0) {
            Remove-Item -LiteralPath $qaRoot -Force
        }
        throw
    }
}

function Get-SmokeQaUInt32BigEndian {
    param(
        [Parameter(Mandatory = $true)] [byte[]] $Bytes,
        [Parameter(Mandatory = $true)] [int] $Offset
    )

    if ($Offset -lt 0 -or $Offset + 4 -gt $Bytes.Length) {
        throw "Big-endian UInt32 read escaped the evidence buffer. offset=$Offset length=$($Bytes.Length)"
    }
    return [uint64]$Bytes[$Offset] * 16777216L +
        [uint64]$Bytes[$Offset + 1] * 65536L +
        [uint64]$Bytes[$Offset + 2] * 256L +
        [uint64]$Bytes[$Offset + 3]
}

function Get-SmokeQaPngCrc32 {
    param(
        [Parameter(Mandatory = $true)] [byte[]] $Bytes,
        [Parameter(Mandatory = $true)] [int] $Offset,
        [Parameter(Mandatory = $true)] [int] $Count
    )

    if ($Offset -lt 0 -or $Count -lt 0 -or $Offset + $Count -gt $Bytes.Length) {
        throw "CRC32 read escaped the evidence buffer. offset=$Offset count=$Count length=$($Bytes.Length)"
    }
    $crcTableVariable = Get-Variable -Scope Script -Name SmokeQaPngCrc32Table -ErrorAction SilentlyContinue
    if ($null -eq $crcTableVariable -or $null -eq $crcTableVariable.Value) {
        $polynomial = [Convert]::ToUInt32('EDB88320', 16)
        [uint32[]]$table = New-Object 'System.UInt32[]' 256
        for ($tableIndex = 0; $tableIndex -lt $table.Length; $tableIndex++) {
            [uint32]$value = [uint32]$tableIndex
            for ($bit = 0; $bit -lt 8; $bit++) {
                if (($value -band [uint32]1) -ne 0) {
                    $value = [uint32](($value -shr 1) -bxor $polynomial)
                }
                else {
                    $value = [uint32]($value -shr 1)
                }
            }
            $table[$tableIndex] = $value
        }
        $script:SmokeQaPngCrc32Table = $table
    }

    [uint32]$crc = [uint32]::MaxValue
    for ($index = 0; $index -lt $Count; $index++) {
        $tableIndex = [int](($crc -bxor [uint32]$Bytes[$Offset + $index]) -band [uint32]255)
        $crc = [uint32](($crc -shr 8) -bxor $script:SmokeQaPngCrc32Table[$tableIndex])
    }
    return [uint32]($crc -bxor [uint32]::MaxValue)
}

function Test-SmokeQaPngEvidence {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    try {
        $item = Get-Item -LiteralPath $Path -ErrorAction Stop
        if ($item.PSIsContainer -or [int64]$item.Length -gt [int]::MaxValue) {
            return [pscustomobject]@{ Valid = $false; Details = 'png-size-out-of-range' }
        }
        [byte[]]$bytes = [System.IO.File]::ReadAllBytes($item.FullName)
        [byte[]]$signature = @(137,80,78,71,13,10,26,10)
        if ($bytes.Length -lt 8) {
            return [pscustomobject]@{ Valid = $false; Details = 'png-signature-truncated' }
        }
        for ($signatureIndex = 0; $signatureIndex -lt $signature.Length; $signatureIndex++) {
            if ($bytes[$signatureIndex] -ne $signature[$signatureIndex]) {
                return [pscustomobject]@{ Valid = $false; Details = 'png-signature-invalid' }
            }
        }

        $offset = 8
        $chunkIndex = 0
        $width = 0L
        $height = 0L
        $idatBytes = 0L
        $seenIhdr = $false
        $seenIend = $false
        while ($offset -lt $bytes.Length) {
            if ($bytes.Length - $offset -lt 12) {
                return [pscustomobject]@{ Valid = $false; Details = "png-chunk-header-truncated;chunk=$chunkIndex" }
            }
            [uint64]$chunkLength = Get-SmokeQaUInt32BigEndian -Bytes $bytes -Offset $offset
            if ($chunkLength -gt [int]::MaxValue) {
                return [pscustomobject]@{ Valid = $false; Details = "png-chunk-length-out-of-range;chunk=$chunkIndex" }
            }
            [uint64]$chunkTotal = $chunkLength + 12
            if ([uint64]$offset + $chunkTotal -gt [uint64]$bytes.Length) {
                return [pscustomobject]@{ Valid = $false; Details = "png-chunk-out-of-bounds;chunk=$chunkIndex" }
            }

            $chunkType = [System.Text.Encoding]::ASCII.GetString($bytes, $offset + 4, 4)
            if ($chunkIndex -eq 0 -and -not $chunkType.Equals('IHDR', [System.StringComparison]::Ordinal)) {
                return [pscustomobject]@{ Valid = $false; Details = 'png-first-chunk-not-ihdr' }
            }
            if ($chunkType.Equals('IHDR', [System.StringComparison]::Ordinal)) {
                if ($seenIhdr -or $chunkIndex -ne 0 -or $chunkLength -ne 13) {
                    return [pscustomobject]@{ Valid = $false; Details = 'png-ihdr-invalid' }
                }
                $width = [int64](Get-SmokeQaUInt32BigEndian -Bytes $bytes -Offset ($offset + 8))
                $height = [int64](Get-SmokeQaUInt32BigEndian -Bytes $bytes -Offset ($offset + 12))
                if ($width -le 0 -or $height -le 0) {
                    return [pscustomobject]@{ Valid = $false; Details = 'png-dimensions-invalid' }
                }
                $seenIhdr = $true
            }
            elseif (-not $seenIhdr) {
                return [pscustomobject]@{ Valid = $false; Details = 'png-chunk-before-ihdr' }
            }

            $crcOffset = $offset + 8 + [int]$chunkLength
            [uint32]$expectedCrc = [uint32](Get-SmokeQaUInt32BigEndian -Bytes $bytes -Offset $crcOffset)
            [uint32]$actualCrc = Get-SmokeQaPngCrc32 -Bytes $bytes -Offset ($offset + 4) -Count ([int]$chunkLength + 4)
            if ($actualCrc -ne $expectedCrc) {
                return [pscustomobject]@{ Valid = $false; Details = "png-crc-invalid;chunk=$chunkIndex;type=$chunkType" }
            }

            if ($chunkType.Equals('IDAT', [System.StringComparison]::Ordinal) -and $chunkLength -gt 0) {
                $idatBytes += [int64]$chunkLength
            }
            if ($chunkType.Equals('IEND', [System.StringComparison]::Ordinal)) {
                if ($chunkLength -ne 0) {
                    return [pscustomobject]@{ Valid = $false; Details = 'png-iend-length-invalid' }
                }
                if ([uint64]$offset + $chunkTotal -ne [uint64]$bytes.Length) {
                    return [pscustomobject]@{ Valid = $false; Details = 'png-trailing-bytes-after-iend' }
                }
                $seenIend = $true
                $offset += [int]$chunkTotal
                $chunkIndex++
                break
            }

            $offset += [int]$chunkTotal
            $chunkIndex++
        }

        if (-not $seenIhdr -or $idatBytes -le 0 -or -not $seenIend -or $offset -ne $bytes.Length) {
            return [pscustomobject]@{ Valid = $false; Details = 'png-required-chunk-or-length-invalid' }
        }
        return [pscustomobject]@{
            Valid = $true
            Details = "png-structure-crc-ok;width=$width;height=$height;chunks=$chunkIndex;idatBytes=$idatBytes"
        }
    }
    catch {
        return [pscustomobject]@{ Valid = $false; Details = 'png-parse-failed:' + [string]$_.Exception.GetType().Name + ':' + [string]$_.Exception.Message }
    }
}

function Test-SmokeQaCameraTelemetryEvidence {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $reader = $null
    try {
        $reader = [System.IO.File]::OpenText($Path)
        $header = [string]$reader.ReadLine()
        $expectedHeader = 'timestamp,marker,agentX,agentY,agentZ,cameraX,cameraY,cameraZ,appliedScale,activeOwner'
        if (-not [string]::Equals($header, $expectedHeader, [System.StringComparison]::Ordinal)) {
            return [pscustomobject]@{ Valid = $false; Details = 'camera-telemetry-header-invalid' }
        }

        $rowCount = 0
        $lineNumber = 1
        while ($null -ne ($line = $reader.ReadLine())) {
            $lineNumber++
            [string[]]$fields = $line.Split([char]',')
            if ($fields.Length -ne 10) {
                return [pscustomobject]@{ Valid = $false; Details = "camera-telemetry-field-count-invalid;line=$lineNumber;actual=$($fields.Length)" }
            }
            [DateTimeOffset]$timestamp = [DateTimeOffset]::MinValue
            if (-not [DateTimeOffset]::TryParseExact(
                $fields[0],
                'O',
                [System.Globalization.CultureInfo]::InvariantCulture,
                [System.Globalization.DateTimeStyles]::RoundtripKind,
                [ref]$timestamp)) {
                return [pscustomobject]@{ Valid = $false; Details = "camera-telemetry-timestamp-invalid;line=$lineNumber" }
            }
            if ([string]::IsNullOrWhiteSpace($fields[1]) -or [string]::IsNullOrWhiteSpace($fields[9])) {
                return [pscustomobject]@{ Valid = $false; Details = "camera-telemetry-marker-or-owner-empty;line=$lineNumber" }
            }
            foreach ($fieldIndex in 2..8) {
                [double]$number = 0
                if (-not [double]::TryParse(
                    $fields[$fieldIndex],
                    [System.Globalization.NumberStyles]::Float,
                    [System.Globalization.CultureInfo]::InvariantCulture,
                    [ref]$number) -or [double]::IsNaN($number) -or [double]::IsInfinity($number)) {
                    return [pscustomobject]@{ Valid = $false; Details = "camera-telemetry-number-invalid;line=$lineNumber;field=$fieldIndex" }
                }
            }
            $rowCount++
        }
        if ($rowCount -lt 1) {
            return [pscustomobject]@{ Valid = $false; Details = 'camera-telemetry-no-data-rows' }
        }
        return [pscustomobject]@{ Valid = $true; Details = "camera-telemetry-rows-ok;rows=$rowCount" }
    }
    catch {
        return [pscustomobject]@{ Valid = $false; Details = 'camera-telemetry-parse-failed:' + [string]$_.Exception.GetType().Name + ':' + [string]$_.Exception.Message }
    }
    finally {
        if ($null -ne $reader) { $reader.Dispose() }
    }
}

function Remove-SmokeQaHostStage {
    param(
        [Parameter(Mandatory = $true)] $Stage,
        [Parameter(Mandatory = $true)] [string] $EvidencePath
    )

    $artifactStates = New-Object 'System.Collections.Generic.List[object]'
    $allArtifactsExact = $true
    foreach ($artifact in @($Stage.Artifacts)) {
        $exists = Test-Path -LiteralPath $artifact.Path -PathType Leaf
        $actualLength = if ($exists) { [int64](Get-Item -LiteralPath $artifact.Path).Length } else { [int64]0 }
        $actualHash = if ($exists) { Get-SmokeFileSha256 -Path $artifact.Path } else { '' }
        $exact = $exists -and $actualLength -eq [int64]$artifact.Length -and
            [string]::Equals($actualHash, [string]$artifact.Sha256, [System.StringComparison]::Ordinal)
        $allArtifactsExact = $allArtifactsExact -and $exact
        $artifactStates.Add([ordered]@{
            Kind = [string]$artifact.Kind
            Path = [string]$artifact.Path
            Exists = $exists
            ExpectedLength = [int64]$artifact.Length
            ActualLength = $actualLength
            ExpectedSha256 = [string]$artifact.Sha256
            ActualSha256 = $actualHash
            Exact = $exact
        }) | Out-Null
    }

    # QA evidence is an owned, bounded extension of the staged run tree. Copy
    # only the reviewed receipt names into the durable smoke evidence tree,
    # verify the copied bytes, then remove those exact source files so the
    # original activation/artifact transaction can use its strict cleanup.
    $qaEvidenceSourceRoot = Join-Path $Stage.RunDir 'qa-host\g4'
    $qaEvidenceArchiveRoot = Join-Path $EvidencePath 'qa-host\g4'
    $qaEvidenceStates = New-Object 'System.Collections.Generic.List[object]'
    $reviewedQaEvidenceFiles = @(
        'ui\title-settings.png',
        'ui\manager-status-page.png',
        'ui\manager-mods-page.png',
        'ui\manager-advanced-page.png',
        'ui\manager-logs-page.png',
        'ui\official-mod-ui.png',
        'ui\pause-menu-layout.png',
        'ui\official-save-ui.png',
        'ui\official-save-ui-post-title.png',
        'ui\animal-panel.png',
        'ui\equipment-slots.png',
        'ui\equipment-slots-summary.txt',
        'ui\debug-console.png',
        'camera-playable\before.png',
        'camera-playable\scale-4x.png',
        'camera-playable\movement-start.png',
        'camera-playable\movement-mid.png',
        'camera-playable\movement-end.png',
        'camera-playable\fallback-2x.png',
        'camera-playable\reset.png',
        'camera-playable\telemetry.csv'
    )
    $expectedQaEvidenceFiles = @($Stage.ExpectedQaEvidencePaths | ForEach-Object {
        ([string]$_).Replace('/', '\').TrimStart('\')
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    $expectedQaEvidenceDirectories = @($expectedQaEvidenceFiles | ForEach-Object {
        Split-Path -Parent $_
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    $unreviewedExpectedQaEvidence = @($expectedQaEvidenceFiles | Where-Object { $reviewedQaEvidenceFiles -notcontains $_ })
    $sourceFiles = @()
    $sourceDirectories = @()
    $actualQaEvidenceFiles = @()
    $actualQaEvidenceDirectories = @()
    if (Test-Path -LiteralPath $qaEvidenceSourceRoot -PathType Container) {
        $sourceRootWithSeparator = [System.IO.Path]::GetFullPath($qaEvidenceSourceRoot).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
        $sourceFiles = @(Get-ChildItem -LiteralPath $qaEvidenceSourceRoot -Recurse -Force -File)
        $sourceDirectories = @(Get-ChildItem -LiteralPath $qaEvidenceSourceRoot -Recurse -Force -Directory)
        $actualQaEvidenceFiles = @($sourceFiles | ForEach-Object {
            [System.IO.Path]::GetFullPath($_.FullName).Substring($sourceRootWithSeparator.Length).Replace('/', '\')
        } | Sort-Object -Unique)
        $actualQaEvidenceDirectories = @($sourceDirectories | ForEach-Object {
            [System.IO.Path]::GetFullPath($_.FullName).Substring($sourceRootWithSeparator.Length).Replace('/', '\')
        } | Sort-Object -Unique)
        foreach ($file in $sourceFiles) {
            $relativePath = [System.IO.Path]::GetFullPath($file.FullName).Substring($sourceRootWithSeparator.Length).Replace('/', '\')
            $sourceLength = [int64]$file.Length
            $sourceHash = if ($sourceLength -gt 0) { Get-SmokeFileSha256 -Path $file.FullName } else { '' }
            $contentValid = $sourceLength -gt 0
            $contentValidation = if ($contentValid) { 'non-empty' } else { 'empty' }
            if ($contentValid -and $relativePath.EndsWith('.png', [System.StringComparison]::OrdinalIgnoreCase)) {
                $pngValidation = Test-SmokeQaPngEvidence -Path $file.FullName
                $contentValid = [bool]$pngValidation.Valid
                $contentValidation = [string]$pngValidation.Details
            }
            elseif ($contentValid -and $relativePath.Equals('camera-playable\telemetry.csv', [System.StringComparison]::OrdinalIgnoreCase)) {
                $telemetryValidation = Test-SmokeQaCameraTelemetryEvidence -Path $file.FullName
                $contentValid = [bool]$telemetryValidation.Valid
                $contentValidation = [string]$telemetryValidation.Details
            }
            $qaEvidenceStates.Add([ordered]@{
                RelativePath = $relativePath
                SourcePath = [System.IO.Path]::GetFullPath($file.FullName)
                Length = $sourceLength
                Sha256 = $sourceHash
                Expected = $expectedQaEvidenceFiles -contains $relativePath
                Reviewed = $reviewedQaEvidenceFiles -contains $relativePath
                ContentValid = $contentValid
                ContentValidation = $contentValidation
            }) | Out-Null
        }
    }

    $missingQaEvidence = @($expectedQaEvidenceFiles | Where-Object { $actualQaEvidenceFiles -notcontains $_ })
    $unexpectedQaEvidence = @($actualQaEvidenceFiles | Where-Object { $expectedQaEvidenceFiles -notcontains $_ })
    $missingQaEvidenceDirectories = @($expectedQaEvidenceDirectories | Where-Object { $actualQaEvidenceDirectories -notcontains $_ })
    $unexpectedQaEvidenceDirectories = @($actualQaEvidenceDirectories | Where-Object { $expectedQaEvidenceDirectories -notcontains $_ })
    $damagedQaEvidence = @($qaEvidenceStates.ToArray() | Where-Object {
        [int64]$_.Length -le 0 -or [string]::IsNullOrWhiteSpace([string]$_.Sha256) -or -not [bool]$_.ContentValid
    } | ForEach-Object { [string]$_.RelativePath })
    $qaEvidenceExact = $unreviewedExpectedQaEvidence.Count -eq 0 -and
        $missingQaEvidence.Count -eq 0 -and $unexpectedQaEvidence.Count -eq 0 -and
        $missingQaEvidenceDirectories.Count -eq 0 -and $unexpectedQaEvidenceDirectories.Count -eq 0 -and
        $damagedQaEvidence.Count -eq 0

    # Prove the complete stage membership before archiving or removing any G4
    # evidence. Otherwise an artifact mismatch or an unknown sibling outside
    # qa-host\g4 could leave a retained stage whose evidence had already moved.
    $expectedRunFiles = @($Stage.Artifacts | Where-Object { [string]$_.Kind -ne 'Activation' } | ForEach-Object { [System.IO.Path]::GetFullPath([string]$_.Path) } | Sort-Object)
    $qaEvidenceParent = Split-Path -Parent $qaEvidenceSourceRoot
    $qaEvidenceSourceExists = Test-Path -LiteralPath $qaEvidenceSourceRoot -PathType Container
    $expectedRunEntriesBeforeArchive = @($expectedRunFiles)
    if ($qaEvidenceSourceExists) {
        $expectedRunEntriesBeforeArchive += [System.IO.Path]::GetFullPath($qaEvidenceParent)
    }
    $expectedRunEntriesBeforeArchive = @($expectedRunEntriesBeforeArchive | Sort-Object)
    $actualRunEntriesBeforeArchive = if (Test-Path -LiteralPath $Stage.RunDir -PathType Container) {
        @(Get-ChildItem -LiteralPath $Stage.RunDir -Force | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    }
    else {
        @()
    }
    $expectedQaEvidenceParentEntriesBeforeArchive = if ($qaEvidenceSourceExists) {
        @([System.IO.Path]::GetFullPath($qaEvidenceSourceRoot))
    }
    else {
        @()
    }
    $actualQaEvidenceParentEntriesBeforeArchive = if (Test-Path -LiteralPath $qaEvidenceParent -PathType Container) {
        @(Get-ChildItem -LiteralPath $qaEvidenceParent -Force | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    }
    else {
        @()
    }
    $expectedRootEntries = @([System.IO.Path]::GetFullPath([string]$Stage.RunDir))
    $actualRootEntriesBeforeArchive = if (Test-Path -LiteralPath $Stage.QaRoot -PathType Container) {
        @(Get-ChildItem -LiteralPath $Stage.QaRoot -Force | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    }
    else {
        @()
    }
    $treeExactBeforeArchive = ($actualRunEntriesBeforeArchive -join '|') -eq ($expectedRunEntriesBeforeArchive -join '|') -and
        ($actualQaEvidenceParentEntriesBeforeArchive -join '|') -eq ($expectedQaEvidenceParentEntriesBeforeArchive -join '|') -and
        ($actualRootEntriesBeforeArchive -join '|') -eq ($expectedRootEntries -join '|')

    if ($allArtifactsExact -and $qaEvidenceExact -and $treeExactBeforeArchive -and $qaEvidenceSourceExists) {
        try {
            foreach ($receipt in @($qaEvidenceStates.ToArray())) {
                $destination = Join-Path $qaEvidenceArchiveRoot ([string]$receipt.RelativePath)
                $destinationDirectory = Split-Path -Parent $destination
                New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
                if (Test-Path -LiteralPath $destination -PathType Leaf) {
                    $existingLength = [int64](Get-Item -LiteralPath $destination).Length
                    $existingHash = Get-SmokeFileSha256 -Path $destination
                    if ($existingLength -ne [int64]$receipt.Length -or
                        -not [string]::Equals($existingHash, [string]$receipt.Sha256, [System.StringComparison]::Ordinal)) {
                        throw "QA evidence archive already contains different bytes: $destination"
                    }
                }
                else {
                    Copy-Item -LiteralPath $receipt.SourcePath -Destination $destination
                }
                $copiedLength = [int64](Get-Item -LiteralPath $destination).Length
                $copiedHash = Get-SmokeFileSha256 -Path $destination
                if ($copiedLength -ne [int64]$receipt.Length -or
                    -not [string]::Equals($copiedHash, [string]$receipt.Sha256, [System.StringComparison]::Ordinal)) {
                    throw "QA evidence archive verification failed: $destination"
                }
            }
            foreach ($receipt in @($qaEvidenceStates.ToArray())) {
                Remove-Item -LiteralPath $receipt.SourcePath -Force
            }
            foreach ($directory in @($sourceDirectories | Sort-Object { $_.FullName.Length } -Descending)) {
                if ((Test-Path -LiteralPath $directory.FullName -PathType Container) -and @(Get-ChildItem -LiteralPath $directory.FullName -Force).Count -eq 0) {
                    Remove-Item -LiteralPath $directory.FullName -Force
                }
            }
            if (@(Get-ChildItem -LiteralPath $qaEvidenceSourceRoot -Force).Count -eq 0) {
                Remove-Item -LiteralPath $qaEvidenceSourceRoot -Force
            }
            if ((Test-Path -LiteralPath $qaEvidenceParent -PathType Container) -and @(Get-ChildItem -LiteralPath $qaEvidenceParent -Force).Count -eq 0) {
                Remove-Item -LiteralPath $qaEvidenceParent -Force
            }
        }
        catch {
            $qaEvidenceExact = $false
            $qaEvidenceStates.Add([ordered]@{ Error = [string]$_.Exception.Message }) | Out-Null
        }
    }

    $actualRunEntries = if (Test-Path -LiteralPath $Stage.RunDir -PathType Container) {
        @(Get-ChildItem -LiteralPath $Stage.RunDir -Force | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    }
    else {
        @()
    }
    $actualRootEntries = if (Test-Path -LiteralPath $Stage.QaRoot -PathType Container) {
        @(Get-ChildItem -LiteralPath $Stage.QaRoot -Force | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    }
    else {
        @()
    }
    $treeExactAfterArchive = ($actualRunEntries -join '|') -eq ($expectedRunFiles -join '|') -and
        ($actualRootEntries -join '|') -eq ($expectedRootEntries -join '|')
    $treeExact = $treeExactBeforeArchive -and $treeExactAfterArchive
    $cleaned = $false
    $reason = ''
    if ($allArtifactsExact -and $qaEvidenceExact -and $treeExact) {
        try {
            foreach ($artifact in @($Stage.Artifacts)) {
                Remove-Item -LiteralPath $artifact.Path -Force
            }
            if (@(Get-ChildItem -LiteralPath $Stage.RunDir -Force).Count -eq 0) {
                Remove-Item -LiteralPath $Stage.RunDir -Force
            }
            if (@(Get-ChildItem -LiteralPath $Stage.QaRoot -Force).Count -eq 0) {
                Remove-Item -LiteralPath $Stage.QaRoot -Force
            }
            $cleaned = -not (Test-Path -LiteralPath $Stage.ActivationPath) -and
                -not (Test-Path -LiteralPath $Stage.RunDir) -and
                -not (Test-Path -LiteralPath $Stage.QaRoot)
            if (-not $cleaned) {
                $reason = 'Known artifacts were removed but one or more owned paths remained.'
            }
        }
        catch {
            $reason = 'Exact-artifact cleanup failed: ' + [string]$_.Exception.Message
        }
    }
    else {
        $reason = 'Staged bytes, bounded QA evidence, or directory membership changed; the pre-archive gate retained the complete stage and G4 evidence.'
    }

    $result = [pscustomobject]@{
        RunId = [string]$Stage.RunId
        ExactArtifacts = $allArtifactsExact
        ExactQaEvidence = $qaEvidenceExact
        ExactTree = $treeExact
        Cleaned = $cleaned
        Retained = -not $cleaned
        Reason = $reason
        Artifacts = @($artifactStates.ToArray())
        QaEvidenceArchiveRoot = [System.IO.Path]::GetFullPath($qaEvidenceArchiveRoot)
        ExpectedQaEvidencePaths = @($expectedQaEvidenceFiles)
        ActualQaEvidencePaths = @($actualQaEvidenceFiles)
        MissingQaEvidencePaths = @($missingQaEvidence)
        UnexpectedQaEvidencePaths = @($unexpectedQaEvidence)
        DamagedQaEvidencePaths = @($damagedQaEvidence)
        QaEvidence = @($qaEvidenceStates.ToArray())
        TreeExactBeforeArchive = $treeExactBeforeArchive
        ActualRunEntriesBeforeArchive = @($actualRunEntriesBeforeArchive)
        ActualQaEvidenceParentEntriesBeforeArchive = @($actualQaEvidenceParentEntriesBeforeArchive)
        ActualRootEntriesBeforeArchive = @($actualRootEntriesBeforeArchive)
        ActualRunEntries = @($actualRunEntries)
        ActualRootEntries = @($actualRootEntries)
    }
    Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'qa-host-cleanup.json') -Value $result
    if (-not $cleaned) {
        $manualPath = Join-Path $EvidencePath 'qa-host-manual-recovery-required.json'
        Write-SmokeJsonObject -Path $manualPath -Value ([ordered]@{
            CreatedAt = (Get-Date).ToUniversalTime().ToString('o')
            Reason = $reason
            Rule = 'After proving DolocTown.exe is absent, remove only files whose length and SHA-256 still match the recorded stage receipt; remove directories only when empty.'
            Cleanup = $result
        })
        @(
            'DTMAPI QA host cleanup was intentionally incomplete.',
            'Do not load, move, delete, or overwrite changed or unknown files.',
            "Use the exact length/SHA-256 receipt in: $manualPath"
        ) | Set-Content -LiteralPath (Join-Path $EvidencePath 'QA-HOST-MANUAL-RECOVERY-REQUIRED.txt')
    }
    return $result
}

function Invoke-SmokeQaG4EvidenceCleanupSelfTest {
    $testRoot = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetTempPath()) ('dtmapi-qa-g4-cleanup-' + [Guid]::NewGuid().ToString('N'))))
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if (-not $testRoot.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "QA evidence cleanup self-test escaped the system temp root: $testRoot"
    }

    $caseResults = New-Object 'System.Collections.Generic.List[object]'
    try {
        [byte[]]$validPngBytes = [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=')
        $telemetryHeader = 'timestamp,marker,agentX,agentY,agentZ,cameraX,cameraY,cameraZ,appliedScale,activeOwner'
        $validTelemetryRow = '2026-07-16T00:00:00.0000000+00:00,movement,1,2,3,4,5,6,4,qa.camera.owner'
        foreach ($case in @(
            [pscustomobject]@{
                Name = 'exact'
                Expected = @('ui\title-settings.png','camera-playable\telemetry.csv')
                Files = @('ui\title-settings.png','camera-playable\telemetry.csv')
                Garbage = @()
                ExpectCleaned = $true
            },
            [pscustomobject]@{
                Name = 'g9-equipment-slots'
                Expected = @('ui\equipment-slots.png','ui\equipment-slots-summary.txt')
                Files = @('ui\equipment-slots.png','ui\equipment-slots-summary.txt')
                Garbage = @()
                ExpectCleaned = $true
            },
            [pscustomobject]@{ Name = 'missing'; Expected = @('ui\title-settings.png'); Files = @(); Garbage = @(); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'extra-allowlisted'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png','ui\official-mod-ui.png'); Garbage = @(); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'unknown'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png','ui\unknown.png'); Garbage = @(); ExpectCleaned = $false },
            [pscustomobject]@{
                Name = 'damaged'
                Expected = @('ui\title-settings.png','camera-playable\telemetry.csv')
                Files = @('ui\title-settings.png','camera-playable\telemetry.csv')
                Garbage = @('ui\title-settings.png','camera-playable\telemetry.csv')
                ExpectCleaned = $false
            },
            [pscustomobject]@{ Name = 'truncated-png'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png'); TruncatedPng = @('ui\title-settings.png'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'bad-crc-png'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png'); BadCrcPng = @('ui\title-settings.png'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'header-only-csv'; Expected = @('camera-playable\telemetry.csv'); Files = @('camera-playable\telemetry.csv'); HeaderOnlyCsv = @('camera-playable\telemetry.csv'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'bad-timestamp-csv'; Expected = @('camera-playable\telemetry.csv'); Files = @('camera-playable\telemetry.csv'); BadTimestampCsv = @('camera-playable\telemetry.csv'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'bad-number-csv'; Expected = @('camera-playable\telemetry.csv'); Files = @('camera-playable\telemetry.csv'); BadNumberCsv = @('camera-playable\telemetry.csv'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'empty-marker-owner-csv'; Expected = @('camera-playable\telemetry.csv'); Files = @('camera-playable\telemetry.csv'); EmptyMarkerOwnerCsv = @('camera-playable\telemetry.csv'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'artifact-mismatch-retains-evidence'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png'); TamperArtifact = $true; ExpectCleaned = $false; ExpectExactQaEvidence = $true },
            [pscustomobject]@{ Name = 'outside-g4-unknown-retains-evidence'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png'); AddOutsideG4Unknown = $true; ExpectCleaned = $false; ExpectExactQaEvidence = $true }
        )) {
            $caseRoot = Join-Path $testRoot ([string]$case.Name)
            $stateDir = Join-Path $caseRoot 'state'
            $qaRoot = Join-Path $stateDir 'qa-host'
            $runId = ([string]$case.Name).Replace('-', '') + '0123456789abcdef0123456789abcdef'
            $runId = $runId.Substring(0, 32)
            $runDir = Join-Path $qaRoot $runId
            $evidencePath = Join-Path $caseRoot 'evidence'
            $activationPath = Join-Path $stateDir 'qa-host-activation.json'
            $dllPath = Join-Path $runDir 'DTMAPI.GameBridge.DolocTown.QA.dll'
            $settingsPath = Join-Path $runDir 'qa-settings.json'
            New-Item -ItemType Directory -Force -Path $runDir, $evidencePath | Out-Null
            [System.IO.File]::WriteAllText($activationPath, 'activation-receipt')
            [System.IO.File]::WriteAllText($dllPath, 'qa-assembly')
            [System.IO.File]::WriteAllText($settingsPath, 'qa-settings')

            $garbagePaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'Garbage' } | ForEach-Object { @($_.Value) })
            $truncatedPngPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'TruncatedPng' } | ForEach-Object { @($_.Value) })
            $badCrcPngPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'BadCrcPng' } | ForEach-Object { @($_.Value) })
            $headerOnlyCsvPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'HeaderOnlyCsv' } | ForEach-Object { @($_.Value) })
            $badTimestampCsvPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'BadTimestampCsv' } | ForEach-Object { @($_.Value) })
            $badNumberCsvPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'BadNumberCsv' } | ForEach-Object { @($_.Value) })
            $emptyMarkerOwnerCsvPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'EmptyMarkerOwnerCsv' } | ForEach-Object { @($_.Value) })
            foreach ($relativePath in @($case.Files)) {
                $path = Join-Path (Join-Path $runDir 'qa-host\g4') ([string]$relativePath)
                New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
                if ($garbagePaths -contains [string]$relativePath) {
                    [System.IO.File]::WriteAllText($path, 'non-empty-damaged-evidence')
                }
                elseif ($truncatedPngPaths -contains [string]$relativePath) {
                    [byte[]]$truncated = New-Object byte[] ($validPngBytes.Length - 4)
                    [Array]::Copy($validPngBytes, $truncated, $truncated.Length)
                    [System.IO.File]::WriteAllBytes($path, $truncated)
                }
                elseif ($badCrcPngPaths -contains [string]$relativePath) {
                    [byte[]]$badCrc = New-Object byte[] $validPngBytes.Length
                    [Array]::Copy($validPngBytes, $badCrc, $badCrc.Length)
                    # Flip the first byte of the non-empty IDAT payload while
                    # retaining its original CRC, so the structural parser must
                    # reject a bounded-but-tampered PNG.
                    $badCrc[41] = [byte]($badCrc[41] -bxor 1)
                    [System.IO.File]::WriteAllBytes($path, $badCrc)
                }
                elseif ($headerOnlyCsvPaths -contains [string]$relativePath) {
                    [System.IO.File]::WriteAllText($path, $telemetryHeader + "`n")
                }
                elseif ($badTimestampCsvPaths -contains [string]$relativePath) {
                    [System.IO.File]::WriteAllText($path, $telemetryHeader + "`n" + 'not-a-timestamp,movement,1,2,3,4,5,6,4,qa.camera.owner' + "`n")
                }
                elseif ($badNumberCsvPaths -contains [string]$relativePath) {
                    [System.IO.File]::WriteAllText($path, $telemetryHeader + "`n" + '2026-07-16T00:00:00.0000000+00:00,movement,1,2,not-a-number,4,5,6,4,qa.camera.owner' + "`n")
                }
                elseif ($emptyMarkerOwnerCsvPaths -contains [string]$relativePath) {
                    [System.IO.File]::WriteAllText($path, $telemetryHeader + "`n" + '2026-07-16T00:00:00.0000000+00:00,,1,2,3,4,5,6,4,' + "`n")
                }
                elseif ([string]$relativePath -like '*.png') {
                    [System.IO.File]::WriteAllBytes($path, $validPngBytes)
                }
                elseif ([string]$relativePath -ieq 'camera-playable\telemetry.csv') {
                    [System.IO.File]::WriteAllText($path, $telemetryHeader + "`n" + $validTelemetryRow + "`n")
                }
                else {
                    [System.IO.File]::WriteAllText($path, 'qa-evidence-' + [string]$relativePath)
                }
            }

            $stage = [pscustomobject]@{
                RunId = $runId
                QaRoot = [System.IO.Path]::GetFullPath($qaRoot)
                RunDir = [System.IO.Path]::GetFullPath($runDir)
                ActivationPath = [System.IO.Path]::GetFullPath($activationPath)
                Artifacts = @(
                    (Get-SmokeQaHostFileReceipt -Kind 'Activation' -Path $activationPath),
                    (Get-SmokeQaHostFileReceipt -Kind 'QaAssembly' -Path $dllPath),
                    (Get-SmokeQaHostFileReceipt -Kind 'QaSettings' -Path $settingsPath)
                )
                ExpectedQaEvidencePaths = @($case.Expected)
            }
            if (@($case.PSObject.Properties | Where-Object { $_.Name -eq 'TamperArtifact' -and [bool]$_.Value }).Count -eq 1) {
                [System.IO.File]::AppendAllText($dllPath, '-changed-after-receipt')
            }
            if (@($case.PSObject.Properties | Where-Object { $_.Name -eq 'AddOutsideG4Unknown' -and [bool]$_.Value }).Count -eq 1) {
                $outsideG4Path = Join-Path $runDir 'qa-host\outside-g4-unknown.txt'
                New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outsideG4Path) | Out-Null
                [System.IO.File]::WriteAllText($outsideG4Path, 'unknown-outside-reviewed-g4-root')
            }
            $cleanup = Remove-SmokeQaHostStage -Stage $stage -EvidencePath $evidencePath
            $expectExactQaEvidenceProperty = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'ExpectExactQaEvidence' } | Select-Object -First 1)
            $expectExactQaEvidence = if ($expectExactQaEvidenceProperty.Count -eq 1) { [bool]$expectExactQaEvidenceProperty[0].Value } else { [bool]$case.ExpectCleaned }
            $sourceEvidenceRoot = Join-Path $runDir 'qa-host\g4'
            $sourceEvidenceRetained = @($case.Files | Where-Object {
                -not (Test-Path -LiteralPath (Join-Path $sourceEvidenceRoot ([string]$_)) -PathType Leaf)
            }).Count -eq 0
            $archiveEvidenceAbsent = @($case.Files | Where-Object {
                Test-Path -LiteralPath (Join-Path $cleanup.QaEvidenceArchiveRoot ([string]$_)) -PathType Leaf
            }).Count -eq 0
            $passed = [bool]$cleanup.Cleaned -eq [bool]$case.ExpectCleaned
            if ($case.ExpectCleaned) {
                $passed = $passed -and [bool]$cleanup.ExactQaEvidence -and
                    @($case.Expected | Where-Object {
                        $archive = Join-Path $cleanup.QaEvidenceArchiveRoot ([string]$_)
                        -not (Test-Path -LiteralPath $archive -PathType Leaf) -or (Get-Item -LiteralPath $archive).Length -le 0
                    }).Count -eq 0
            }
            else {
                $passed = $passed -and [bool]$cleanup.ExactQaEvidence -eq $expectExactQaEvidence -and
                    (Test-Path -LiteralPath $activationPath -PathType Leaf) -and
                    (Test-Path -LiteralPath $runDir -PathType Container) -and
                    $sourceEvidenceRetained -and $archiveEvidenceAbsent
            }
            $caseResults.Add([ordered]@{
                Name = [string]$case.Name
                Passed = $passed
                Cleaned = [bool]$cleanup.Cleaned
                ExactQaEvidence = [bool]$cleanup.ExactQaEvidence
                ExactArtifacts = [bool]$cleanup.ExactArtifacts
                ExactTree = [bool]$cleanup.ExactTree
                SourceEvidenceRetained = $sourceEvidenceRetained
                ArchiveEvidenceAbsent = $archiveEvidenceAbsent
                Missing = @($cleanup.MissingQaEvidencePaths)
                Unexpected = @($cleanup.UnexpectedQaEvidencePaths)
                Damaged = @($cleanup.DamagedQaEvidencePaths)
                ContentValidation = @($cleanup.QaEvidence | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.RelativePath) } | ForEach-Object {
                    [ordered]@{ RelativePath = [string]$_.RelativePath; Validation = [string]$_.ContentValidation }
                })
            }) | Out-Null
        }

        $allPassed = @($caseResults.ToArray() | Where-Object { -not [bool]$_.Passed }).Count -eq 0
        return [pscustomobject]@{
            Passed = $allPassed
            Cases = @($caseResults.ToArray())
        }
    }
    finally {
        if (Test-Path -LiteralPath $testRoot -PathType Container) {
            Remove-Item -LiteralPath $testRoot -Recurse -Force
        }
    }
}

if ($ValidateQaG4EvidenceCleanupOnly) {
    $selfTest = Invoke-SmokeQaG4EvidenceCleanupSelfTest
    $selfTest | ConvertTo-Json -Depth 6
    if (-not [bool]$selfTest.Passed) {
        exit 1
    }
    exit 0
}

function Test-SmokeQaHostLifecycleLog {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [string] $RunId
    )

    $states = @('validated', 'activated', 'attached', 'started', 'updated', 'closed')
    $counts = [ordered]@{
        Validated = 0
        Activated = 0
        Attached = 0
        Started = 0
        Updated = 0
        Closed = 0
        Failed = 0
    }
    $lineNumbers = [ordered]@{
        Validated = 0
        Activated = 0
        Attached = 0
        Started = 0
        Updated = 0
        Closed = 0
    }
    $allLifecycleMarkerCount = 0
    $currentRunLifecycleMarkerCount = 0
    $allMarkersCurrentRun = $false
    $ordered = $false
    $closedDetailsPassed = $false
    if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
        $escapedRunId = [regex]::Escape($RunId)
        $allLifecycleMarkers = @(Select-String -LiteralPath $LogPath -CaseSensitive -Pattern 'QA host lifecycle runId=[^;]+; state=[^;]+;')
        $currentRunLifecycleMarkers = @(Select-String -LiteralPath $LogPath -CaseSensitive -Pattern ("QA host lifecycle runId={0}; state=[^;]+;" -f $escapedRunId))
        $allLifecycleMarkerCount = $allLifecycleMarkers.Count
        $currentRunLifecycleMarkerCount = $currentRunLifecycleMarkers.Count
        $allMarkersCurrentRun = $allLifecycleMarkerCount -gt 0 -and
            $allLifecycleMarkerCount -eq $currentRunLifecycleMarkerCount

        $orderedLines = New-Object 'System.Collections.Generic.List[int]'
        foreach ($state in $states) {
            $matches = @(Select-String -LiteralPath $LogPath -CaseSensitive -Pattern ("QA host lifecycle runId={0}; state={1};" -f $escapedRunId, [regex]::Escape($state)))
            $key = $state.Substring(0, 1).ToUpperInvariant() + $state.Substring(1)
            $counts[$key] = $matches.Count
            if ($matches.Count -eq 1) {
                $lineNumbers[$key] = [int]$matches[0].LineNumber
                $orderedLines.Add([int]$matches[0].LineNumber) | Out-Null
            }
            if ($state -eq 'closed' -and $matches.Count -eq 1) {
                $closedLine = [string]$matches[0].Line
                $closedDetailsPassed = $closedLine -cmatch '(?:^|; )participantCloseSucceeded=true(?:;|$)' -and
                    $closedLine -cmatch '(?:^|; )listeners=0(?:;|$)' -and
                    $closedLine -cmatch '(?:^|; )participant=null(?:;|$)' -and
                    $closedLine -cmatch '(?:^|; )fallback=false(?:;|$)'
            }
        }
        $failedMatches = @(Select-String -LiteralPath $LogPath -CaseSensitive -Pattern ("QA host lifecycle runId={0}; state=failed;" -f $escapedRunId))
        $counts.Failed = $failedMatches.Count

        $ordered = $orderedLines.Count -eq $states.Count
        if ($ordered) {
            for ($index = 1; $index -lt $orderedLines.Count; $index++) {
                if ($orderedLines[$index] -le $orderedLines[$index - 1]) {
                    $ordered = $false
                    break
                }
            }
        }
    }

    $passed = $counts.Validated -eq 1 -and
        $counts.Activated -eq 1 -and
        $counts.Attached -eq 1 -and
        $counts.Started -eq 1 -and
        $counts.Updated -eq 1 -and
        $counts.Closed -eq 1 -and
        $counts.Failed -eq 0 -and
        $currentRunLifecycleMarkerCount -eq 6 -and
        $allMarkersCurrentRun -and
        $ordered -and
        $closedDetailsPassed
    return [pscustomobject]@{
        RunId = $RunId
        MarkerCounts = $counts
        MarkerLineNumbers = $lineNumbers
        AllLifecycleMarkerCount = $allLifecycleMarkerCount
        CurrentRunLifecycleMarkerCount = $currentRunLifecycleMarkerCount
        AllMarkersCurrentRun = $allMarkersCurrentRun
        Ordered = $ordered
        ClosedDetailsPassed = $closedDetailsPassed
        FailedStateAbsent = $counts.Failed -eq 0
        Passed = $passed
    }
}

function Wait-SmokeProcessExitBeforeRecovery {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [ValidateRange(1, 60)] [int] $TimeoutSeconds = 15
    )

    $initialProcesses = @(Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)
    $initialProcessIds = @($initialProcesses | ForEach-Object { [int]$_.Id })
    $gracefulCloseRequested = $false
    foreach ($process in $initialProcesses) {
        try {
            if ($process.CloseMainWindow()) {
                $gracefulCloseRequested = $true
            }
        }
        catch {
        }
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $remainingProcesses = @($initialProcesses)
    $stableAbsenceSeconds = 1.0
    $absenceObservedAt = $null
    while ((Get-Date) -lt $deadline) {
        $remainingProcesses = @(Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)
        if ($remainingProcesses.Count -eq 0) {
            if ($null -eq $absenceObservedAt) {
                $absenceObservedAt = Get-Date
            }
            elseif (((Get-Date) - $absenceObservedAt).TotalSeconds -ge $stableAbsenceSeconds) {
                break
            }
        }
        else {
            $absenceObservedAt = $null
        }
        Start-Sleep -Milliseconds 250
    }
    $remainingProcesses = @(Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)
    $stableAbsenceObserved = $remainingProcesses.Count -eq 0 -and
        $null -ne $absenceObservedAt -and
        ((Get-Date) - $absenceObservedAt).TotalSeconds -ge $stableAbsenceSeconds

    $result = [ordered]@{
        CheckedAt = (Get-Date).ToString('o')
        TimeoutSeconds = $TimeoutSeconds
        InitialProcessIds = @($initialProcessIds)
        GracefulCloseRequested = $gracefulCloseRequested
        RequiredStableAbsenceSeconds = $stableAbsenceSeconds
        StableAbsenceObserved = $stableAbsenceObserved
        RemainingProcessIds = @($remainingProcesses | ForEach-Object { [int]$_.Id })
        Exited = $stableAbsenceObserved
    }
    Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'process-exit-before-state-restore.json') -Value $result
    return [pscustomobject]$result
}

function Get-SmokePublishedProductDefinitions {
    $catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
    if (-not (Test-Path -LiteralPath $catalogPath -PathType Leaf)) {
        throw "Published-product smoke gate requires the tracked Product Catalog: $catalogPath"
    }

    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
    $products = @($catalog.products | Where-Object {
        [string]$_.role -eq 'PublishedProduct' -and [string]$_.distributionState -eq 'PublicWorkshop'
    } | Sort-Object { [string]$_.catalogId })
    if ($products.Count -ne 11) {
        throw "Published-product smoke gate requires exactly 11 public Product Catalog rows; found $($products.Count)."
    }
    foreach ($product in $products) {
        if ([string]::IsNullOrWhiteSpace([string]$product.workshopId) -or [string]::IsNullOrWhiteSpace([string]$product.uniqueId)) {
            throw "Published-product smoke gate found an incomplete Catalog row: $($product.catalogId)."
        }
    }

    return $products
}

function Get-SmokePublishedProductWorkshopIds {
    return @(Get-SmokePublishedProductDefinitions | ForEach-Object { 'Workshop.' + [string]$_.workshopId })
}

function Get-SmokePublishedProductLocalIds {
    $products = @(Get-SmokePublishedProductDefinitions)
    foreach ($product in $products) {
        if ([string]::IsNullOrWhiteSpace([string]$product.officialFolder)) {
            throw "Local11 smoke profile found a Catalog product without officialFolder: $($product.catalogId)."
        }
    }

    return @($products | ForEach-Object { 'Local.' + [string]$_.officialFolder })
}

function Get-SmokeAuthorSourceStatePath {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    $canonicalGameRoot = [System.IO.Path]::GetFullPath($GameDir).TrimEnd([char]92, [char]47)
    $stateBase = if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_AUTHOR_STATE_ROOT)) {
        [System.IO.Path]::GetFullPath($env:DTMAPI_AUTHOR_STATE_ROOT)
    }
    else {
        Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'DTMAPI\AuthorSdk\state'
    }
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        $gameRootHash = $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($canonicalGameRoot.ToUpperInvariant()))
    }
    finally {
        $hasher.Dispose()
    }
    $installationKey = -join @($gameRootHash[0..15] | ForEach-Object { $_.ToString('x2') })
    return Join-Path (Join-Path (Join-Path $stateBase 'installations') $installationKey) 'source-state.json'
}

function Get-SmokeFileSha256 {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::Open(
            [System.IO.Path]::GetFullPath($Path),
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete)
        try {
            return ([BitConverter]::ToString($sha256.ComputeHash($stream))).Replace('-', '')
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $sha256.Dispose()
    }
}

function Assert-SmokeExactJsonPropertySet {
    param(
        [Parameter(Mandatory = $true)] [object] $Value,
        [Parameter(Mandatory = $true)] [string[]] $Expected,
        [Parameter(Mandatory = $true)] [string] $Context
    )

    if ($null -eq $Value) {
        throw "$Context must contain one JSON object."
    }
    [string[]]$actualNames = @($Value.PSObject.Properties | ForEach-Object { [string]$_.Name })
    [string[]]$expectedNames = @($Expected | ForEach-Object { [string]$_ })
    [Array]::Sort($actualNames, [System.StringComparer]::Ordinal)
    [Array]::Sort($expectedNames, [System.StringComparer]::Ordinal)
    if ($actualNames.Count -ne $expectedNames.Count -or
        [string]::Join("`n", $actualNames) -cne [string]::Join("`n", $expectedNames)) {
        throw "$Context has an unknown, missing, duplicate, or wrong-version field."
    }
}

function Test-SmokeSha256Equal {
    param([string] $Actual, [string] $Expected)

    return $Actual -match '^[0-9a-fA-F]{64}$' -and
        $Expected -match '^[0-9a-fA-F]{64}$' -and
        [string]::Equals($Actual, $Expected, [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-SmokeMoreEquipmentSlotsOfficialPackage {
    param(
        [Parameter(Mandatory = $true)] [string] $ProductRoot,
        [Parameter(Mandatory = $true)] [string] $RepoRoot
    )

    $canonicalProductRoot = [System.IO.Path]::GetFullPath($ProductRoot).TrimEnd([char]92, [char]47)
    $contentRoot = Join-Path $canonicalProductRoot 'Content\DTMAPI'
    $infoPath = Join-Path $canonicalProductRoot 'info.json'
    $manifestPath = Join-Path $contentRoot 'manifest.json'
    $markerPath = Join-Path $contentRoot 'dtmapi-package.json'
    $receiptPath = Join-Path $contentRoot 'dtmapi-advanced-references.json'
    $entryDllPath = Join-Path $contentRoot 'DTMAPI.MoreEquipmentSlots.dll'
    $expectedFiles = @(
        'Content/DTMAPI/DTMAPI.MoreEquipmentSlots.dll',
        'Content/DTMAPI/dtmapi-advanced-references.json',
        'Content/DTMAPI/dtmapi-package.json',
        'Content/DTMAPI/manifest.json',
        'i18n/english.json',
        'i18n/schinese.json',
        'info.json'
    )
    if (-not (Test-Path -LiteralPath $canonicalProductRoot -PathType Container)) {
        throw 'MoreEquipmentSlots cold recovery requires its SDK-generated official Local package.'
    }
    $rootItem = Get-Item -LiteralPath $canonicalProductRoot -Force
    $entries = @(Get-ChildItem -LiteralPath $canonicalProductRoot -Recurse -Force -ErrorAction Stop)
    if (($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        @($entries | Where-Object { ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 }).Count -ne 0) {
        throw 'MoreEquipmentSlots official Local package may not contain reparse points.'
    }
    [string[]]$relativeFiles = @($entries | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
        $_.FullName.Substring($canonicalProductRoot.Length + 1).Replace([char]92, [char]47)
    })
    [string[]]$expectedFileSet = @($expectedFiles)
    [Array]::Sort($relativeFiles, [System.StringComparer]::Ordinal)
    [Array]::Sort($expectedFileSet, [System.StringComparer]::Ordinal)
    if (($relativeFiles -join '|') -cne ($expectedFileSet -join '|')) {
        throw 'MoreEquipmentSlots cold recovery refused a non-exact official Local package file set.'
    }
    foreach ($requiredPath in @($infoPath, $manifestPath, $markerPath, $receiptPath, $entryDllPath)) {
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "MoreEquipmentSlots official Local package is missing a required file: $requiredPath"
        }
    }

    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $RepoRoot 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json
    $catalogRows = @($catalog.products | Where-Object { [string]$_.catalogId -ceq 'more-equipment-slots' })
    if ($catalogRows.Count -ne 1) {
        throw 'MoreEquipmentSlots official package preflight requires one exact Catalog row.'
    }
    $product = $catalogRows[0]
    $policyId = [string]$product.referencePolicyId
    $registryPath = Join-Path $RepoRoot 'author-sdk\advanced-reference-policies\registry.json'
    $policyPath = Join-Path $RepoRoot ('author-sdk\advanced-reference-policies\' + $policyId + '.json')
    $registry = Get-Content -Raw -Encoding UTF8 -LiteralPath $registryPath | ConvertFrom-Json
    $registryRows = @($registry.policies | Where-Object { [string]$_.policyId -ceq $policyId })
    if ($registryRows.Count -ne 1) {
        throw "MoreEquipmentSlots official package preflight requires one exact policy row for $policyId."
    }
    $registryRow = $registryRows[0]
    $policy = Get-Content -Raw -Encoding UTF8 -LiteralPath $policyPath | ConvertFrom-Json
    $policySha256 = Get-SmokeFileSha256 -Path $policyPath
    if ([string]$product.uniqueId -cne 'DTMAPI.MoreEquipmentSlotsMod' -or
        [string]$product.sourceVersion -cne '1.0.0' -or
        [string]$product.sourceMinimumDtmApiVersion -cne '0.6.0' -or
        [string]$product.targetMinimumDtmApiVersion -cne '0.6.0' -or
        [string]$product.codeModKind -cne 'Advanced' -or
        [string]$product.canonicalHarmonyOwner -cne 'dtmapi.mod.dtmapi.moreequipmentslotsmod' -or
        [int]$registry.schemaVersion -ne 2 -or
        [string]$registryRow.requiredUniqueId -cne [string]$product.uniqueId -or
        [string]$registryRow.minimumDtmApiVersion -cne '0.6.0' -or
        -not (Test-SmokeSha256Equal -Actual ([string]$registryRow.policySha256) -Expected $policySha256) -or
        [int]$policy.schemaVersion -ne 1 -or
        [string]$policy.policyId -cne $policyId -or
        [int]$policy.policyVersion -ne 1) {
        throw 'MoreEquipmentSlots Catalog/registry/policy authority is inconsistent or understates the 0.6 Runtime floor.'
    }

    $info = Get-Content -Raw -Encoding UTF8 -LiteralPath $infoPath | ConvertFrom-Json
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    $marker = Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath | ConvertFrom-Json
    $receipt = Get-Content -Raw -Encoding UTF8 -LiteralPath $receiptPath | ConvertFrom-Json
    Assert-SmokeExactJsonPropertySet -Value $manifest -Expected @(
        'Name','Author','Version','Description','UniqueID','EntryDll','EntryType',
        'MinimumDTMApiVersion','Type','Dependencies','CodeModKind'
    ) -Context 'MoreEquipmentSlots manifest'
    Assert-SmokeExactJsonPropertySet -Value $marker -Expected @(
        'advancedReferenceReceiptPath','advancedReferenceReceiptSha256','authorSdkVersion','authority',
        'codeModKind','entryDllPath','entryDllSha256','manifestPath','manifestSha256','owner',
        'packageKind','schemaVersion','targetDtmApiVersion','uniqueId','version'
    ) -Context 'MoreEquipmentSlots SDK package marker'
    Assert-SmokeExactJsonPropertySet -Value $receipt -Expected @(
        'schemaVersion','receiptKind','referencePolicyId','referencePolicyVersion','referencePolicySha256',
        'uniqueId','codeModKind','targetFramework','gameBuildId','gameAssemblyRelativePath',
        'gameAssemblySha256','manifestPath','manifestSha256','entryDllPath','entryDllLength',
        'entryDllSha256','harmonyOwner','references'
    ) -Context 'MoreEquipmentSlots Advanced reference receipt'
    $dependencies = @($manifest.Dependencies)
    if ($dependencies.Count -ne 1) {
        throw 'MoreEquipmentSlots manifest must retain one exact ModConfigMenu dependency.'
    }
    Assert-SmokeExactJsonPropertySet -Value $dependencies[0] -Expected @('UniqueID','MinimumVersion','Required') -Context 'MoreEquipmentSlots manifest dependency'

    $manifestSha256 = Get-SmokeFileSha256 -Path $manifestPath
    $entryDllSha256 = Get-SmokeFileSha256 -Path $entryDllPath
    $receiptSha256 = Get-SmokeFileSha256 -Path $receiptPath
    $entryDllLength = [int64](Get-Item -LiteralPath $entryDllPath -Force).Length
    $bindingMatches = [string]$info.version -ceq '1.0.0' -and
        [string]$manifest.UniqueID -ceq [string]$product.uniqueId -and
        [string]$manifest.Version -ceq [string]$product.sourceVersion -and
        [string]$manifest.MinimumDTMApiVersion -ceq '0.6.0' -and
        [string]$manifest.Type -ceq 'CodeMod' -and
        [string]$manifest.CodeModKind -ceq 'Advanced' -and
        [string]$manifest.EntryDll -ceq 'Content/DTMAPI/DTMAPI.MoreEquipmentSlots.dll' -and
        [string]$manifest.EntryType -ceq 'DTMAPI.MoreEquipmentSlots.ModEntry' -and
        [string]$dependencies[0].UniqueID -ceq 'DTMAPI.ModConfigMenu' -and
        [string]$dependencies[0].MinimumVersion -ceq '0.5.5' -and
        ($dependencies[0].Required -is [bool]) -and [bool]$dependencies[0].Required -and
        [int]$marker.schemaVersion -eq 2 -and [string]$marker.owner -ceq 'DTMAPI' -and
        [string]$marker.uniqueId -ceq [string]$product.uniqueId -and
        [string]$marker.version -ceq [string]$product.sourceVersion -and
        [string]$marker.packageKind -ceq 'CodeMod' -and [string]$marker.codeModKind -ceq 'Advanced' -and
        [string]$marker.authorSdkVersion -ceq '0.1.0' -and [string]$marker.targetDtmApiVersion -ceq '0.5.5' -and
        [string]$marker.manifestPath -ceq 'Content/DTMAPI/manifest.json' -and
        (Test-SmokeSha256Equal -Actual ([string]$marker.manifestSha256) -Expected $manifestSha256) -and
        [string]$marker.entryDllPath -ceq 'Content/DTMAPI/DTMAPI.MoreEquipmentSlots.dll' -and
        (Test-SmokeSha256Equal -Actual ([string]$marker.entryDllSha256) -Expected $entryDllSha256) -and
        [string]$marker.advancedReferenceReceiptPath -ceq 'Content/DTMAPI/dtmapi-advanced-references.json' -and
        (Test-SmokeSha256Equal -Actual ([string]$marker.advancedReferenceReceiptSha256) -Expected $receiptSha256) -and
        [string]$marker.authority -ceq 'dtmapi-author-sdk-package-binding' -and
        [int]$receipt.schemaVersion -eq 1 -and [string]$receipt.receiptKind -ceq 'DTMAPI.AdvancedCodeMod.ReferenceReceipt' -and
        [string]$receipt.referencePolicyId -ceq $policyId -and
        [int]$receipt.referencePolicyVersion -eq [int]$policy.policyVersion -and
        (Test-SmokeSha256Equal -Actual ([string]$receipt.referencePolicySha256) -Expected $policySha256) -and
        [string]$receipt.uniqueId -ceq [string]$product.uniqueId -and [string]$receipt.codeModKind -ceq 'Advanced' -and
        [string]$receipt.targetFramework -ceq 'netstandard2.0' -and
        [string]$receipt.gameBuildId -ceq [string]$policy.gameBuildId -and
        [string]$receipt.gameAssemblyRelativePath -ceq [string]$policy.gameAssemblyRelativePath -and
        (Test-SmokeSha256Equal -Actual ([string]$receipt.gameAssemblySha256) -Expected ([string]$policy.gameAssemblySha256)) -and
        [string]$receipt.manifestPath -ceq 'Content/DTMAPI/manifest.json' -and
        (Test-SmokeSha256Equal -Actual ([string]$receipt.manifestSha256) -Expected $manifestSha256) -and
        [string]$receipt.entryDllPath -ceq 'Content/DTMAPI/DTMAPI.MoreEquipmentSlots.dll' -and
        [int64]$receipt.entryDllLength -eq $entryDllLength -and
        (Test-SmokeSha256Equal -Actual ([string]$receipt.entryDllSha256) -Expected $entryDllSha256) -and
        [string]$receipt.harmonyOwner -ceq [string]$product.canonicalHarmonyOwner
    if (-not $bindingMatches) {
        throw 'MoreEquipmentSlots cold recovery refused drifted official Local manifest/package/reference authority.'
    }

    $expectedReferences = @($policy.references)
    $actualReferences = @($receipt.references)
    if ($actualReferences.Count -ne $expectedReferences.Count) {
        throw 'MoreEquipmentSlots Advanced receipt reference count does not match its tracked policy.'
    }
    for ($referenceIndex = 0; $referenceIndex -lt $expectedReferences.Count; $referenceIndex++) {
        $expectedReference = $expectedReferences[$referenceIndex]
        $actualReference = $actualReferences[$referenceIndex]
        Assert-SmokeExactJsonPropertySet -Value $actualReference -Expected @(
            'gameRelativePath','assemblyName','length','sha256','copyLocal'
        ) -Context "MoreEquipmentSlots Advanced reference receipt row $referenceIndex"
        if ([string]$actualReference.gameRelativePath -cne [string]$expectedReference.gameRelativePath -or
            [string]$actualReference.assemblyName -cne [string]$expectedReference.assemblyName -or
            [int64]$actualReference.length -ne [int64]$expectedReference.length -or
            -not (Test-SmokeSha256Equal -Actual ([string]$actualReference.sha256) -Expected ([string]$expectedReference.sha256)) -or
            -not ($actualReference.copyLocal -is [bool]) -or [bool]$actualReference.copyLocal) {
            throw "MoreEquipmentSlots Advanced receipt reference row $referenceIndex does not match its tracked policy."
        }
    }

    return [ordered]@{
        ProductRoot = $canonicalProductRoot
        Files = @($relativeFiles)
        UniqueId = [string]$manifest.UniqueID
        Version = [string]$manifest.Version
        MinimumDtmApiVersion = [string]$manifest.MinimumDTMApiVersion
        CodeModKind = [string]$manifest.CodeModKind
        ReferencePolicyId = [string]$receipt.referencePolicyId
        ReferencePolicySha256 = $policySha256
        GameBuildId = [string]$receipt.gameBuildId
        HarmonyOwner = [string]$receipt.harmonyOwner
        ManifestSha256 = $manifestSha256
        EntryDllLength = $entryDllLength
        EntryDllSha256 = $entryDllSha256
        AdvancedReferenceReceiptSha256 = $receiptSha256
        ReferenceCount = $actualReferences.Count
        Passed = $true
    }
}

function Set-SmokeRecoveryOnlyAuthorSourceState {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $EvidencePath
    )

    $canonicalGameRoot = [System.IO.Path]::GetFullPath($GameDir).TrimEnd([char]92, [char]47)
    $statePath = Get-SmokeAuthorSourceStatePath -GameDir $canonicalGameRoot
    $backupPath = Join-Path $EvidencePath 'recovery-only-author-source-state.before.json'
    $originalExisted = Test-Path -LiteralPath $statePath -PathType Leaf
    $originalLength = if ($originalExisted) { [int64](Get-Item -LiteralPath $statePath).Length } else { [int64]0 }
    $originalSha256 = if ($originalExisted) { Get-SmokeFileSha256 -Path $statePath } else { '' }
    if ($originalExisted) {
        Copy-Item -LiteralPath $statePath -Destination $backupPath -Force
    }
    else {
        'No pre-existing author source state file.' | Set-Content -LiteralPath $backupPath -Encoding UTF8
    }

    $summary = [ordered]@{
        Applied = $false
        StatePath = [System.IO.Path]::GetFullPath($statePath)
        BackupPath = [System.IO.Path]::GetFullPath($backupPath)
        OriginalExisted = $originalExisted
        OriginalLength = $originalLength
        OriginalSha256 = $originalSha256
        GameRoot = $canonicalGameRoot
        Purpose = 'RecoveryOnlyClear'
        RequestedLocalIds = @()
        Selections = @()
    }
    try {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $statePath) | Out-Null
        Write-SmokeJsonObject -Path $statePath -Value ([ordered]@{
            schemaVersion = 1
            gameRoot = $canonicalGameRoot
            playerReproductionActive = $false
            reproductionSnapshotId = ''
            selections = @()
        })
        $summary.Applied = $true
        Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'recovery-only-author-source-state-summary.json') -Value $summary
        return $summary
    }
    catch {
        if ($originalExisted) {
            Copy-Item -LiteralPath $backupPath -Destination $statePath -Force
        }
        elseif (Test-Path -LiteralPath $statePath -PathType Leaf) {
            Remove-Item -LiteralPath $statePath -Force
        }
        throw
    }
}

function Restore-SmokeRecoveryOnlyAuthorSourceState {
    param(
        [Parameter(Mandatory = $true)] $Summary,
        [Parameter(Mandatory = $true)] [string] $EvidencePath
    )

    $statePath = [string]$Summary.StatePath
    if ([bool]$Summary.OriginalExisted) {
        Copy-Item -LiteralPath ([string]$Summary.BackupPath) -Destination $statePath -Force
    }
    elseif (Test-Path -LiteralPath $statePath -PathType Leaf) {
        Remove-Item -LiteralPath $statePath -Force
    }
    $existsAfter = Test-Path -LiteralPath $statePath -PathType Leaf
    $actualLength = if ($existsAfter) { [int64](Get-Item -LiteralPath $statePath).Length } else { [int64]0 }
    $actualSha256 = if ($existsAfter) { Get-SmokeFileSha256 -Path $statePath } else { '' }
    $passed = $existsAfter -eq [bool]$Summary.OriginalExisted -and
        $actualLength -eq [int64]$Summary.OriginalLength -and
        [string]::Equals($actualSha256, [string]$Summary.OriginalSha256, [System.StringComparison]::OrdinalIgnoreCase)
    Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'recovery-only-author-source-state-restore-verification.json') -Value ([ordered]@{
        StatePath = $statePath
        OriginalExisted = [bool]$Summary.OriginalExisted
        ExistsAfter = $existsAfter
        ExpectedLength = [int64]$Summary.OriginalLength
        ActualLength = $actualLength
        ExpectedSha256 = [string]$Summary.OriginalSha256
        ActualSha256 = $actualSha256
        Passed = $passed
    })
    return $passed
}

function Get-SmokeWorkshopArtifactSnapshot {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Product,
        [ValidateSet('Auto', 'Retained', 'CurrentPublished')]
        [string] $ArtifactBoundary = 'Auto'
    )

    $currentPublishedArtifactProperty = $Product.PSObject.Properties['currentPublishedArtifact']
    $currentPublishedArtifact = if ($null -ne $currentPublishedArtifactProperty) {
        $currentPublishedArtifactProperty.Value
    }
    else {
        $null
    }
    $effectiveArtifactBoundary = if ($ArtifactBoundary -eq 'Auto') {
        if ($null -ne $currentPublishedArtifact) { 'CurrentPublished' } else { 'Retained' }
    }
    else {
        $ArtifactBoundary
    }
    if ($effectiveArtifactBoundary -eq 'CurrentPublished' -and $null -eq $currentPublishedArtifact) {
        throw "Published-product artifact snapshot requires currentPublishedArtifact for $($Product.catalogId)."
    }

    $expectedArtifact = if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        $currentPublishedArtifact
    }
    else {
        $Product.retainedArtifact
    }
    if ($null -eq $expectedArtifact) {
        throw "Published-product artifact snapshot requires $effectiveArtifactBoundary metadata for $($Product.catalogId)."
    }
    $treeDigestAlgorithm = if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        [string]$expectedArtifact.treeDigestAlgorithm
    }
    else {
        'DTMAPI-Retained-SHA256SUMS-v1'
    }
    $expectedFileCount = if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        [int]$expectedArtifact.steamDeliveredFileCount
    }
    else {
        [int]$expectedArtifact.fileCount
    }
    $expectedBytes = if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        [int64]$expectedArtifact.steamDeliveredBytes
    }
    else {
        [int64]$expectedArtifact.bytes
    }
    $expectedTreeSha256 = if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        [string]$expectedArtifact.steamDeliveredTreeSha256
    }
    else {
        [string]$expectedArtifact.treeSha256
    }
    $snapshot = [ordered]@{
        CatalogId = [string]$Product.catalogId
        UniqueId = [string]$Product.uniqueId
        WorkshopId = [string]$Product.workshopId
        ArtifactBoundary = $effectiveArtifactBoundary
        TreeDigestAlgorithm = $treeDigestAlgorithm
        Path = [System.IO.Path]::GetFullPath($Path)
        Exists = Test-Path -LiteralPath $Path -PathType Container
        ExpectedFileCount = $expectedFileCount
        ExpectedBytes = $expectedBytes
        ExpectedTreeSha256 = $expectedTreeSha256
        ActualFileCount = 0
        ActualBytes = [int64]0
        ActualTreeSha256 = ''
        ExcludedRelativePaths = @()
        ReparsePointPaths = @()
        ReparsePointFree = $false
        Passed = $false
    }
    if (-not $snapshot.Exists) {
        return $snapshot
    }

    $rootItem = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    $entries = @(Get-ChildItem -LiteralPath $Path -Recurse -Force -ErrorAction Stop)
    $reparsePointPaths = @(
        @($rootItem) + $entries |
            Where-Object {
                ($_.Attributes -band
                    [System.IO.FileAttributes]::ReparsePoint) -ne 0
            } |
            ForEach-Object {
                $_.FullName
            } |
            Sort-Object -Unique)
    $snapshot.ReparsePointPaths = @($reparsePointPaths)
    $snapshot.ReparsePointFree = $reparsePointPaths.Count -eq 0

    $fileRows = @($entries | Where-Object {
        -not $_.PSIsContainer
    } | ForEach-Object {
        [pscustomobject]@{
            File = $_
            RelativePath = $_.FullName.Substring($Path.Length).TrimStart([char]92, [char]47).Replace([char]92, [char]47)
        }
    })
    if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        $excludedRows = @($fileRows | Where-Object {
            $_.RelativePath.StartsWith(
                'Content/.tools/bepinex/extract/',
                [System.StringComparison]::OrdinalIgnoreCase)
        })
        $snapshot.ExcludedRelativePaths = @($excludedRows | ForEach-Object { [string]$_.RelativePath })
        $includedRowsByPath = New-Object 'System.Collections.Generic.SortedDictionary[string,object]' ([System.StringComparer]::Ordinal)
        foreach ($row in @($fileRows | Where-Object {
            -not $_.RelativePath.StartsWith(
                'Content/.tools/bepinex/extract/',
                [System.StringComparison]::OrdinalIgnoreCase)
        })) {
            $includedRowsByPath.Add([string]$row.RelativePath, $row)
        }
        $files = @($includedRowsByPath.Values)
    }
    else {
        $retainedRowsByPath = New-Object 'System.Collections.Generic.SortedDictionary[string,object]' ([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($row in $fileRows) {
            $retainedRowsByPath.Add([string]$row.RelativePath, $row)
        }
        $files = @($retainedRowsByPath.Values)
    }
    $lines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($row in $files) {
        $fileHash = (Get-SmokeFileSha256 -Path $row.File.FullName).ToLowerInvariant()
        $lines.Add($fileHash + '  ' + [string]$row.RelativePath) | Out-Null
        $snapshot.ActualBytes += [int64]$row.File.Length
    }
    $snapshot.ActualFileCount = $files.Count
    $normalizedTree = $lines.ToArray() -join "`n"
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        $snapshot.ActualTreeSha256 = [System.BitConverter]::ToString($hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($normalizedTree))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }
    $snapshot.Passed = $snapshot.ActualFileCount -eq $snapshot.ExpectedFileCount -and
        $snapshot.ActualBytes -eq $snapshot.ExpectedBytes -and
        $snapshot.ReparsePointFree -and
        [string]::Equals($snapshot.ActualTreeSha256, $snapshot.ExpectedTreeSha256, [System.StringComparison]::OrdinalIgnoreCase)
    return $snapshot
}

function Get-SmokeUnityCrashRootCandidates {
    $roots = New-Object 'System.Collections.Generic.List[string]'
    $tempRoot = [System.IO.Path]::GetTempPath()
    foreach ($relative in @(
            'RedSawGames\DolocTown\Crashes',
            'RedSawGames\Doloc Town\Crashes'
        )) {
        $path = [System.IO.Path]::GetFullPath((Join-Path $tempRoot $relative))
        if (-not $roots.Contains($path)) {
            $roots.Add($path) | Out-Null
        }
    }

    return @($roots.ToArray())
}

function Get-SmokeCrashDirectorySortTimeUtc {
    param([Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Directory)

    $latest = $Directory.LastWriteTimeUtc
    foreach ($file in @(Get-ChildItem -LiteralPath $Directory.FullName -File -ErrorAction SilentlyContinue)) {
        if ($file.LastWriteTimeUtc -gt $latest) {
            $latest = $file.LastWriteTimeUtc
        }
    }

    return $latest
}

function Write-SmokeCrashBaseline {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [datetime] $RunStartedAt
    )

    $lines = New-Object 'System.Collections.Generic.List[string]'
    $lines.Add("runStartedAt=$($RunStartedAt.ToString('o'))") | Out-Null
    $latestCrashId = ''
    $latestCrashTime = [datetime]::MinValue
    foreach ($root in @(Get-SmokeUnityCrashRootCandidates)) {
        $exists = Test-Path -LiteralPath $root -PathType Container
        $lines.Add("CrashRoot=$root") | Out-Null
        $lines.Add("Exists=$exists") | Out-Null
        if (-not $exists) {
            continue
        }

        $dirs = @(Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue |
            Sort-Object @{ Expression = { Get-SmokeCrashDirectorySortTimeUtc -Directory $_ }; Descending = $true })
        $lines.Add("DirectoryCount=$($dirs.Count)") | Out-Null
        foreach ($dir in $dirs) {
            $sortTime = Get-SmokeCrashDirectorySortTimeUtc -Directory $dir
            if ($sortTime -gt $latestCrashTime) {
                $latestCrashTime = $sortTime
                $latestCrashId = $dir.Name
            }

            $files = @(Get-ChildItem -LiteralPath $dir.FullName -File -ErrorAction SilentlyContinue |
                ForEach-Object { "$($_.Name):$($_.Length):$($_.LastWriteTime.ToString('o'))" })
            $lines.Add("Directory=$($dir.FullName)") | Out-Null
            $lines.Add("DirectoryLastWrite=$($dir.LastWriteTime.ToString('o'))") | Out-Null
            $lines.Add("DirectorySortTime=$($sortTime.ToString('o'))") | Out-Null
            $lines.Add("Files=$([string]::Join('|', $files))") | Out-Null
        }
    }

    if ($latestCrashTime -ne [datetime]::MinValue) {
        $lines.Add("latestPreexistingCrashId=$latestCrashId") | Out-Null
        $lines.Add("latestPreexistingCrashSortTime=$($latestCrashTime.ToString('o'))") | Out-Null
    }
    else {
        $lines.Add('latestPreexistingCrashId=') | Out-Null
        $lines.Add('latestPreexistingCrashSortTime=') | Out-Null
    }

    $lines | Set-Content -LiteralPath $Path
}

function Get-SmokeUnityCrashFreshness {
    param(
        [Parameter(Mandatory = $true)] [string] $SummaryPath,
        [Parameter(Mandatory = $true)] [datetime] $RunStartedAt,
        [object] $FatalDetectedAt = $null,
        [string] $Phase = 'unknown'
    )

    $result = [ordered]@{
        Phase = $Phase
        Status = 'missing'
        FreshDirectories = @()
        StaleDirectories = @()
        CopiedFiles = 0
        RunStartedAt = $RunStartedAt.ToString('o')
        FatalDetectedAt = if ($null -ne $FatalDetectedAt) { ([datetime]$FatalDetectedAt).ToString('o') } else { '' }
        Reason = ''
    }

    if (-not (Test-Path -LiteralPath $SummaryPath -PathType Leaf)) {
        $result.Reason = 'Unity-Crashes/summary.txt missing.'
        return $result
    }

    $lines = @(Get-Content -LiteralPath $SummaryPath -ErrorAction SilentlyContinue)
    $currentDirectory = ''
    $copiedFiles = @()
    foreach ($line in $lines) {
        if ($line -like 'Directory=*') {
            $currentDirectory = $line.Substring('Directory='.Length)
            continue
        }

        if ($line -like 'DirectoryLastWrite=*') {
            if ([string]::IsNullOrWhiteSpace($currentDirectory)) {
                continue
            }

            $rawTime = $line.Substring('DirectoryLastWrite='.Length)
            $parsedTime = [datetime]::MinValue
            if ([datetime]::TryParse($rawTime, [ref]$parsedTime)) {
                if ($parsedTime.ToUniversalTime() -ge $RunStartedAt.ToUniversalTime().AddSeconds(-5)) {
                    $result.FreshDirectories += @($currentDirectory)
                }
                else {
                    $result.StaleDirectories += @($currentDirectory)
                }
            }
            else {
                $result.StaleDirectories += @("$currentDirectory (unparsed-time=$rawTime)")
            }
            continue
        }

        if ($line -like 'CopiedFile=*') {
            $copiedFiles += @($line.Substring('CopiedFile='.Length))
        }
    }

    $result.CopiedFiles = $copiedFiles.Count
    if (@($result.FreshDirectories).Count -gt 0) {
        $result.Status = 'fresh'
        $result.Reason = 'At least one copied crash directory was created or modified after this smoke run started.'
    }
    elseif ($copiedFiles.Count -gt 0 -or @($result.StaleDirectories).Count -gt 0) {
        $result.Status = 'stale-only'
        $result.Reason = 'Copied crash evidence only came from directories older than this smoke run.'
    }
    else {
        $result.Status = 'missing'
        $result.Reason = 'No copied crash files or directories could be attributed to this run.'
    }

    return $result
}

function Get-SmokeIssue011FileReceipt {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "ISSUE-011 required evidence file is missing: $fullPath"
    }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if ([int64]$item.Length -le 0) {
        throw "ISSUE-011 required evidence file is empty: $fullPath"
    }
    return [ordered]@{
        Path = $fullPath
        Length = [int64]$item.Length
        Sha256 = (Get-SmokeFileSha256 -Path $fullPath).ToLowerInvariant()
    }
}

function Get-SmokeIssue011LastGiveBaseline {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $exists = Test-Path -LiteralPath $fullPath -PathType Leaf
    $item = if ($exists) { Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop } else { $null }
    return [ordered]@{
        Path = $fullPath
        Existed = $exists
        Length = if ($exists) { [int64]$item.Length } else { [int64]0 }
        Sha256 = if ($exists) { (Get-SmokeFileSha256 -Path $fullPath).ToLowerInvariant() } else { '' }
        LastWriteTimeUtc = if ($exists) { $item.LastWriteTimeUtc.ToString('o') } else { '' }
    }
}

function Get-SmokeIssue011RuntimeAssemblies {
    param([Parameter(Mandatory = $true)] [string] $GameDirectory)

    $runtimeRoot = [System.IO.Path]::GetFullPath((Join-Path $GameDirectory 'BepInEx\plugins\DTMAPI'))
    $expected = @(
        'DTMAPI.Abstractions.dll',
        'DTMAPI.BepInExBootstrap.dll',
        'DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll',
        'DTMAPI.ModConfigMenu.dll'
    ) | Sort-Object
    $actual = if (Test-Path -LiteralPath $runtimeRoot -PathType Container) {
        @(Get-ChildItem -LiteralPath $runtimeRoot -Recurse -Force -File -Filter '*.dll' -ErrorAction Stop |
            ForEach-Object { $_.FullName.Substring($runtimeRoot.Length + 1).Replace([char]92, [char]47) } |
            Sort-Object)
    }
    else {
        @()
    }
    if (-not [string]::Equals((@($expected) -join "`n"), (@($actual) -join "`n"), [System.StringComparison]::Ordinal)) {
        throw "ISSUE-011 requires the exact five-DLL installed Runtime. expected=$($expected -join '|') actual=$($actual -join '|')"
    }
    return @($expected | ForEach-Object {
        $receipt = Get-SmokeIssue011FileReceipt -Path (Join-Path $runtimeRoot $_)
        [ordered]@{
            FileName = [string]$_
            Path = [string]$receipt.Path
            Length = [int64]$receipt.Length
            Sha256 = [string]$receipt.Sha256
        }
    })
}

function Get-SmokeIssue011SteamLifecycleEvidence {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [datetime] $RunStartedAt,
        [ValidateRange(0, 30)] [int] $WaitSeconds = 15
    )

    $steamInfoPath = Join-Path $EvidencePath 'steam-info.txt'
    $steamRoot = ''
    if (Test-Path -LiteralPath $steamInfoPath -PathType Leaf) {
        $steamRootLine = @(Get-Content -LiteralPath $steamInfoPath -ErrorAction SilentlyContinue |
            Where-Object { [string]$_ -like 'SteamRoot=*' } | Select-Object -First 1)
        if ($steamRootLine.Count -eq 1) {
            $steamRoot = [string]$steamRootLine[0].Substring('SteamRoot='.Length)
        }
    }
    $sourcePath = if ([string]::IsNullOrWhiteSpace($steamRoot)) { '' } else {
        [System.IO.Path]::GetFullPath((Join-Path $steamRoot 'logs\console_log.txt'))
    }
    $snapshotPath = [System.IO.Path]::GetFullPath((Join-Path $EvidencePath 'issue-011-steam-lifecycle.txt'))
    $deadline = (Get-Date).AddSeconds($WaitSeconds)
    $lines = @()
    $addedIndex = -1
    $removedIndex = -1
    $addedLine = ''
    $removedLine = ''
    $processId = 0
    $addedAt = [datetime]::MinValue
    $removedAt = [datetime]::MinValue
    do {
        if (-not [string]::IsNullOrWhiteSpace($sourcePath) -and (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            $lines = @(Get-Content -LiteralPath $sourcePath -Tail 2000 -ErrorAction SilentlyContinue)
            $addedIndex = -1
            for ($index = 0; $index -lt $lines.Count; $index++) {
                $match = [regex]::Match(
                    [string]$lines[$index],
                    '^\[(?<Time>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] Game process added : AppID 2285550 .*ProcID (?<Pid>\d+)')
                if (-not $match.Success) { continue }
                $parsed = [datetime]::MinValue
                if (-not [datetime]::TryParseExact(
                        $match.Groups['Time'].Value,
                        'yyyy-MM-dd HH:mm:ss',
                        [Globalization.CultureInfo]::InvariantCulture,
                        [Globalization.DateTimeStyles]::AssumeLocal,
                        [ref]$parsed)) { continue }
                if ($parsed.ToUniversalTime() -ge $RunStartedAt.ToUniversalTime().AddSeconds(-10)) {
                    $addedIndex = $index
                    $addedLine = [string]$lines[$index]
                    $processId = [int]$match.Groups['Pid'].Value
                    $addedAt = $parsed
                }
            }
            if ($addedIndex -ge 0) {
                for ($index = $addedIndex + 1; $index -lt $lines.Count; $index++) {
                    $match = [regex]::Match(
                        [string]$lines[$index],
                        '^\[(?<Time>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] Game process removed: AppID 2285550 .*ProcID ' + [regex]::Escape([string]$processId) + '\s*$')
                    if (-not $match.Success) { continue }
                    $parsed = [datetime]::MinValue
                    if ([datetime]::TryParseExact(
                            $match.Groups['Time'].Value,
                            'yyyy-MM-dd HH:mm:ss',
                            [Globalization.CultureInfo]::InvariantCulture,
                            [Globalization.DateTimeStyles]::AssumeLocal,
                            [ref]$parsed)) {
                        $removedIndex = $index
                        $removedLine = [string]$lines[$index]
                        $removedAt = $parsed
                        break
                    }
                }
            }
        }
        if ($removedIndex -ge 0 -or (Get-Date) -ge $deadline) { break }
        Start-Sleep -Milliseconds 500
    } while ($true)

    $waitingAfterRemoval = 0
    if ($removedIndex -ge 0 -and ($removedIndex + 1) -lt $lines.Count) {
        $waitingAfterRemoval = @($lines[($removedIndex + 1)..($lines.Count - 1)] | Where-Object {
            [string]$_ -match 'AppID 2285550.*WaitingForExit|WaitingForExit.*AppID 2285550'
        }).Count
    }
    $snapshotLines = New-Object 'System.Collections.Generic.List[string]'
    $snapshotLines.Add("SourceConsoleLog=$sourcePath") | Out-Null
    $snapshotLines.Add("ObservedAt=$((Get-Date).ToString('o'))") | Out-Null
    $snapshotLines.Add("RunStartedAt=$($RunStartedAt.ToString('o'))") | Out-Null
    if ($addedIndex -ge 0) { $snapshotLines.Add("Added=$addedLine") | Out-Null }
    if ($removedIndex -ge 0) { $snapshotLines.Add("Removed=$removedLine") | Out-Null }
    $snapshotLines.Add("WaitingForExitAfterRemovalCount=$waitingAfterRemoval") | Out-Null
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($snapshotPath, ([string]::Join([Environment]::NewLine, $snapshotLines.ToArray()) + [Environment]::NewLine), $utf8NoBom)
    $passed = $addedIndex -ge 0 -and $removedIndex -gt $addedIndex -and
        $removedAt -ge $addedAt -and $waitingAfterRemoval -eq 0
    return [ordered]@{
        SourceConsoleLogPath = $sourcePath
        LifecycleSnapshot = Get-SmokeIssue011FileReceipt -Path $snapshotPath
        ProcessId = $processId
        AddedAt = if ($addedAt -ne [datetime]::MinValue) { $addedAt.ToString('o') } else { '' }
        RemovedAt = if ($removedAt -ne [datetime]::MinValue) { $removedAt.ToString('o') } else { '' }
        AddedLine = $addedLine
        RemovedLine = $removedLine
        WaitingForExitAfterRemovalCount = $waitingAfterRemoval
        Passed = $passed
    }
}

function New-SmokeIssue011AcceptanceProjection {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $GameDirectory,
        [Parameter(Mandatory = $true)] [datetime] $RunStartedAt,
        [Parameter(Mandatory = $true)] $LastGiveBaseline,
        [Parameter(Mandatory = $true)] $CrashFreshness,
        [Parameter(Mandatory = $true)] [bool] $NoQaGatePassed,
        [Parameter(Mandatory = $true)] [bool] $NoQaPublishedOwnerSetPassed,
        [Parameter(Mandatory = $true)] [bool] $SaveLoadedPassed,
        [Parameter(Mandatory = $true)] [bool] $InteractiveEvidenceObserved,
        [Parameter(Mandatory = $true)] [int] $FatalWindowCount,
        [Parameter(Mandatory = $true)] [bool] $ProcessExited,
        [Parameter(Mandatory = $true)] [bool] $ForcedClose
    )

    $evidenceRoot = [System.IO.Path]::GetFullPath($EvidencePath)
    $receiptPath = [System.IO.Path]::GetFullPath((Join-Path $evidenceRoot 'issue-011-acceptance.json'))
    $noQaPath = Join-Path $evidenceRoot 'no-qa-ui-evidence-gate.json'
    $noQaReceipt = Get-SmokeIssue011FileReceipt -Path $noQaPath
    $bepInExReceipt = Get-SmokeIssue011FileReceipt -Path (Join-Path $evidenceRoot 'BepInEx-LogOutput.log')
    $logLines = @(Get-Content -LiteralPath $bepInExReceipt.Path -ErrorAction Stop)

    $titleClicks = @($logLines | Where-Object { [string]$_ -like '*DTMAPI title settings button clicked.*' })
    $titleOpened = @($logLines | Where-Object { [string]$_ -like '*DTMAPI title settings menu opened.*' })
    $titlePassed = $titleClicks.Count -gt 0 -and $titleOpened.Count -gt 0

    $tooltipRows = New-Object 'System.Collections.Generic.List[string]'
    $searchRows = New-Object 'System.Collections.Generic.List[object]'
    foreach ($line in $logLines) {
        $match = [regex]::Match([string]$line, 'DebugConsole status UI\.DebugConsoleItemTooltip=visible .*searchText=(?<Search>[^\.\r\n]+)\.')
        $kind = 'tooltip'
        if ($match.Success) {
            $tooltipRows.Add([string]$line) | Out-Null
        }
        else {
            $match = [regex]::Match([string]$line, 'Debug console open lifecycle state searchText=(?<Search>.*?) category=')
            $kind = 'open-lifecycle'
        }
        if ($match.Success -and -not [string]::Equals($match.Groups['Search'].Value.Trim(), '<empty>', [System.StringComparison]::OrdinalIgnoreCase) -and
            -not [string]::IsNullOrWhiteSpace($match.Groups['Search'].Value)) {
            $searchRows.Add([ordered]@{ Text = $match.Groups['Search'].Value.Trim(); Kind = $kind; Line = [string]$line }) | Out-Null
        }
    }
    $searchEvidence = if ($searchRows.Count -gt 0) { $searchRows[$searchRows.Count - 1] } else { $null }

    $lastGivePath = Join-Path $evidenceRoot 'debug-console-last-give.txt'
    $lastGiveReceipt = if (Test-Path -LiteralPath $lastGivePath -PathType Leaf) {
        Get-SmokeIssue011FileReceipt -Path $lastGivePath
    }
    else { $null }
    $lastGiveTimestamp = [DateTimeOffset]::MinValue
    $lastGiveStatus = ''
    $lastGiveItem = ''
    $lastGiveRequested = 0
    $lastGiveGiven = 0
    $lastGiveRightClick = $false
    $lastGiveParsed = $false
    if ($null -ne $lastGiveReceipt) {
        $lastGiveText = (Get-Content -Raw -LiteralPath $lastGiveReceipt.Path -ErrorAction Stop).Trim()
        $lastGiveMatch = [regex]::Match(
            $lastGiveText,
            '^(?<Time>\S+)\s+status=(?<Status>\S+)\s+source=(?<Source>\S+)\s+item=(?<Item>\S+)\s+requested=(?<Requested>\d+)\s+given=(?<Given>\d+)\s+rightClick=(?<RightClick>True|False)\s+failure=(?<Failure>.*)$')
        if ($lastGiveMatch.Success) {
            $parsedOffset = [DateTimeOffset]::MinValue
            if ([DateTimeOffset]::TryParse($lastGiveMatch.Groups['Time'].Value, [ref]$parsedOffset)) {
                $lastGiveTimestamp = $parsedOffset
                $lastGiveStatus = $lastGiveMatch.Groups['Status'].Value
                $lastGiveItem = $lastGiveMatch.Groups['Item'].Value
                $lastGiveRequested = [int]$lastGiveMatch.Groups['Requested'].Value
                $lastGiveGiven = [int]$lastGiveMatch.Groups['Given'].Value
                $lastGiveRightClick = [string]::Equals($lastGiveMatch.Groups['RightClick'].Value, 'True', [System.StringComparison]::OrdinalIgnoreCase)
                $lastGiveParsed = $true
            }
        }
    }
    $lastGiveChanged = $null -ne $lastGiveReceipt -and (
        -not [bool]$LastGiveBaseline.Existed -or
        -not [string]::Equals([string]$LastGiveBaseline.Sha256, [string]$lastGiveReceipt.Sha256, [System.StringComparison]::OrdinalIgnoreCase))
    $lastGiveFresh = $lastGiveParsed -and $lastGiveTimestamp.UtcDateTime -ge $RunStartedAt.ToUniversalTime().AddSeconds(-5)
    $lastGivePassed = $lastGiveChanged -and $lastGiveFresh -and $lastGiveStatus -ceq 'verified' -and
        -not [string]::IsNullOrWhiteSpace($lastGiveItem) -and $lastGiveRequested -gt 0 -and $lastGiveGiven -gt 0
    $debugPassed = $NoQaGatePassed -and $InteractiveEvidenceObserved -and
        $tooltipRows.Count -gt 0 -and $searchRows.Count -gt 0 -and $lastGivePassed

    $uninitializedLines = @($logLines | Where-Object { [string]$_ -match 'Input System not(?: yet)? initialized' })
    $fallbackFailureLines = @($logLines | Where-Object {
        [string]$_ -match 'Title settings EventSystem fallback creation failed; retrying\.|Debug console EventSystem fallback creation failed; retrying\.'
    })
    $inputSystemPassed = $uninitializedLines.Count -le 1 -and $fallbackFailureLines.Count -eq 0

    $summaryPath = Join-Path $evidenceRoot 'Unity-Crashes\summary.txt'
    $summaryReceipt = Get-SmokeIssue011FileReceipt -Path $summaryPath
    $summaryLines = @(Get-Content -LiteralPath $summaryReceipt.Path -ErrorAction Stop)
    $collectedAt = [DateTimeOffset]::MinValue
    $collectedLine = @($summaryLines | Where-Object { [string]$_ -like 'Collected=*' } | Select-Object -First 1)
    if ($collectedLine.Count -eq 1) {
        $parsedCollected = [DateTimeOffset]::MinValue
        if ([DateTimeOffset]::TryParse($collectedLine[0].Substring('Collected='.Length), [ref]$parsedCollected)) {
            $collectedAt = $parsedCollected
        }
    }
    $crashDumpPaths = @(Get-ChildItem -LiteralPath (Join-Path $evidenceRoot 'Unity-Crashes') -Recurse -Force -File -Filter 'crash.dmp' -ErrorAction SilentlyContinue |
        ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    $missingReasonPath = Join-Path $evidenceRoot 'Unity-Crashes\MISSING-CRASH-DUMP-README.txt'
    $missingReason = if (Test-Path -LiteralPath $missingReasonPath -PathType Leaf) {
        (Get-Content -Raw -LiteralPath $missingReasonPath -ErrorAction SilentlyContinue).Trim()
    }
    elseif (@($summaryLines | Where-Object { [string]$_ -like '*No Unity crash report directories were found*' }).Count -gt 0) {
        'No Unity crash report directories were found for the current Windows user.'
    }
    else { '' }
    $collectorFresh = $collectedAt -ne [DateTimeOffset]::MinValue -and
        $collectedAt.UtcDateTime -ge $RunStartedAt.ToUniversalTime().AddSeconds(-5)
    $packageExplainsDumpState = $crashDumpPaths.Count -gt 0 -or -not [string]::IsNullOrWhiteSpace($missingReason)
    $noFreshNativeCrash = [string]$CrashFreshness.Status -cne 'fresh' -and $FatalWindowCount -eq 0
    $crashPackagePassed = $collectorFresh -and $packageExplainsDumpState -and $noFreshNativeCrash

    $steamLifecycle = Get-SmokeIssue011SteamLifecycleEvidence -EvidencePath $evidenceRoot -RunStartedAt $RunStartedAt
    $runtimeAssemblies = @(Get-SmokeIssue011RuntimeAssemblies -GameDirectory $GameDirectory)
    $processPassed = $FatalWindowCount -eq 0 -and $ProcessExited -and -not $ForcedClose -and [bool]$steamLifecycle.Passed
    $passed = $SaveLoadedPassed -and $NoQaGatePassed -and $NoQaPublishedOwnerSetPassed -and
        $titlePassed -and $debugPassed -and $inputSystemPassed -and $crashPackagePassed -and $processPassed

    return [ordered]@{
        SchemaVersion = 1
        ReceiptKind = 'DTMAPI.ISSUE011.CurrentCandidateAcceptance'
        EvidenceRoot = $evidenceRoot
        ReceiptPath = $receiptPath
        GameDir = [System.IO.Path]::GetFullPath($GameDirectory)
        RunStartedAt = $RunStartedAt.ToString('o')
        CompletedAt = (Get-Date).ToString('o')
        SaveSlot = 3
        SaveTestMode = 'NoNativeSave'
        LaunchMode = 'Steam'
        OfficialModProfile = 'Local11'
        AssertNoQaUiEvidence = $true
        RuntimeAssemblies = $runtimeAssemblies
        NoQaGate = [ordered]@{
            Path = [string]$noQaReceipt.Path
            Length = [int64]$noQaReceipt.Length
            Sha256 = [string]$noQaReceipt.Sha256
            Passed = $NoQaGatePassed
        }
        BepInExLog = $bepInExReceipt
        TitleSettings = [ordered]@{
            ButtonClickCount = $titleClicks.Count
            MenuOpenedCount = $titleOpened.Count
            ButtonClickLine = if ($titleClicks.Count -gt 0) { [string]$titleClicks[0] } else { '' }
            MenuOpenedLine = if ($titleOpened.Count -gt 0) { [string]$titleOpened[0] } else { '' }
            Passed = $titlePassed
        }
        DebugConsole = [ordered]@{
            OpenUseClosePassed = $NoQaGatePassed
            InteractiveObserved = $InteractiveEvidenceObserved
            TooltipEvidenceCount = $tooltipRows.Count
            TooltipLine = if ($tooltipRows.Count -gt 0) { [string]$tooltipRows[$tooltipRows.Count - 1] } else { '' }
            SearchEvidenceCount = $searchRows.Count
            SearchText = if ($null -ne $searchEvidence) { [string]$searchEvidence.Text } else { '' }
            SearchEvidenceKind = if ($null -ne $searchEvidence) { [string]$searchEvidence.Kind } else { '' }
            SearchLine = if ($null -ne $searchEvidence) { [string]$searchEvidence.Line } else { '' }
            LastGiveBaseline = $LastGiveBaseline
            LastGiveFile = $lastGiveReceipt
            LastGiveTimestamp = if ($lastGiveTimestamp -ne [DateTimeOffset]::MinValue) { $lastGiveTimestamp.ToString('o') } else { '' }
            LastGiveStatus = $lastGiveStatus
            LastGiveItem = $lastGiveItem
            LastGiveRequested = $lastGiveRequested
            LastGiveGiven = $lastGiveGiven
            LastGiveRightClick = $lastGiveRightClick
            Passed = $debugPassed
        }
        InputSystem = [ordered]@{
            UninitializedCount = $uninitializedLines.Count
            FallbackFailureCount = $fallbackFailureLines.Count
            UninitializedLines = @($uninitializedLines | Select-Object -First 5)
            FallbackFailureLines = @($fallbackFailureLines | Select-Object -First 5)
            Passed = $inputSystemPassed
        }
        CrashPackage = [ordered]@{
            Summary = $summaryReceipt
            CollectedAt = if ($collectedAt -ne [DateTimeOffset]::MinValue) { $collectedAt.ToString('o') } else { '' }
            FreshnessStatus = [string]$CrashFreshness.Status
            FreshnessReason = [string]$CrashFreshness.Reason
            FreshDirectories = @($CrashFreshness.FreshDirectories)
            CrashDumpPaths = $crashDumpPaths
            MissingReasonPath = if (Test-Path -LiteralPath $missingReasonPath -PathType Leaf) { [System.IO.Path]::GetFullPath($missingReasonPath) } else { '' }
            MissingReason = $missingReason
            PackageExplainsDumpState = $packageExplainsDumpState
            NoFreshNativeCrash = $noFreshNativeCrash
            Passed = $crashPackagePassed
        }
        SteamLifecycle = $steamLifecycle
        ProcessExit = [ordered]@{
            NoFatalInstanceWindow = $FatalWindowCount -eq 0
            ProcessExited = $ProcessExited
            ForcedClose = $ForcedClose
            Passed = $processPassed
        }
        SmokeResult = $null
        Passed = $passed
    }
}

function Test-SmokeIssue011AcceptanceProjection {
    param([Parameter(Mandatory = $true)] [string] $RepoRoot)

    $managedParent = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot 'tmp\test-runs\issue011-acceptance'))
    $root = [System.IO.Path]::GetFullPath((Join-Path $managedParent ([Guid]::NewGuid().ToString('N'))))
    if (-not $root.StartsWith($managedParent + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'ISSUE-011 projection self-test root escaped its managed parent.'
    }
    [System.IO.Directory]::CreateDirectory($root) | Out-Null
    try {
        $gameDir = Join-Path $root 'game'
        $runtimeRoot = Join-Path $gameDir 'BepInEx\plugins\DTMAPI'
        foreach ($name in @(
                'DTMAPI.Abstractions.dll',
                'DTMAPI.BepInExBootstrap.dll',
                'DTMAPI.Core.dll',
                'DTMAPI.GameBridge.DolocTown.dll',
                'DTMAPI.ModConfigMenu.dll'
            )) {
            [System.IO.Directory]::CreateDirectory($runtimeRoot) | Out-Null
            [System.IO.File]::WriteAllText((Join-Path $runtimeRoot $name), "fixture-$name", (New-Object System.Text.UTF8Encoding($false)))
        }
        $evidence = Join-Path $root 'evidence'
        [System.IO.Directory]::CreateDirectory((Join-Path $evidence 'Unity-Crashes')) | Out-Null
        $runStarted = (Get-Date).AddSeconds(-2)
        $now = [DateTimeOffset]::Now
        Write-SmokeJsonObject -Path (Join-Path $evidence 'no-qa-ui-evidence-gate.json') -Value ([ordered]@{ Passed = $true })
        $cleanLog = @(
            "$($now.ToString('o')) [Info] DTMAPI title settings button clicked.",
            "$($now.ToString('o')) [Info] DTMAPI title settings menu opened.",
            "$($now.ToString('o')) [Info] DebugConsole status UI.DebugConsoleItemTooltip=visible source=Unity UI pointer hover details=item=fixture_item, source=Vanilla, searchText=<empty>..",
            "$($now.ToString('o')) [Info] Debug console open lifecycle state searchText=fixture category=<empty> sourceFilter=__base modItemsOnly=False itemPage=0."
        ) -join [Environment]::NewLine
        [System.IO.File]::WriteAllText((Join-Path $evidence 'BepInEx-LogOutput.log'), $cleanLog, (New-Object System.Text.UTF8Encoding($false)))
        $lastGivePath = Join-Path $evidence 'debug-console-last-give.txt'
        [System.IO.File]::WriteAllText($lastGivePath, ($now.ToString('o') + ' status=verified source=YConsole.ButtonLeftClick item=fixture_item requested=1 given=1 rightClick=False failure=reason=none.'), (New-Object System.Text.UTF8Encoding($false)))
        [System.IO.File]::WriteAllText((Join-Path $evidence 'Unity-Crashes\summary.txt'), "Collected=$($now.ToString('o'))`nNo Unity crash report directories were found for the current Windows user.`nFilesCopiedTotal=0`n", (New-Object System.Text.UTF8Encoding($false)))
        $steamRoot = Join-Path $root 'steam'
        [System.IO.Directory]::CreateDirectory((Join-Path $steamRoot 'logs')) | Out-Null
        [System.IO.File]::WriteAllText((Join-Path $evidence 'steam-info.txt'), "SteamRoot=$steamRoot`n", (New-Object System.Text.UTF8Encoding($false)))
        $steamAdded = [DateTime]::Now.AddSeconds(-1)
        $steamRemoved = [DateTime]::Now
        $steamLines = @(
            ('[' + $steamAdded.ToString('yyyy-MM-dd HH:mm:ss') + '] Game process added : AppID 2285550 "fixture", ProcID 43210, IP 0.0.0.0:0'),
            ('[' + $steamRemoved.ToString('yyyy-MM-dd HH:mm:ss') + '] Game process removed: AppID 2285550 "fixture", ProcID 43210 ')
        ) -join [Environment]::NewLine
        [System.IO.File]::WriteAllText((Join-Path $steamRoot 'logs\console_log.txt'), $steamLines, (New-Object System.Text.UTF8Encoding($false)))
        $baseline = [ordered]@{
            Path = Join-Path $root 'state\debug-console-last-give.txt'
            Existed = $false
            Length = [int64]0
            Sha256 = ''
            LastWriteTimeUtc = ''
        }
        $crash = [pscustomobject]@{
            Status = 'missing'
            Reason = 'No copied crash files or directories could be attributed to this run.'
            FreshDirectories = @()
        }
        $positive = New-SmokeIssue011AcceptanceProjection `
            -EvidencePath $evidence -GameDirectory $gameDir -RunStartedAt $runStarted `
            -LastGiveBaseline $baseline -CrashFreshness $crash -NoQaGatePassed $true `
            -NoQaPublishedOwnerSetPassed $true -SaveLoadedPassed $true -InteractiveEvidenceObserved $true `
            -FatalWindowCount 0 -ProcessExited $true -ForcedClose $false

        [System.IO.File]::AppendAllText(
            (Join-Path $evidence 'BepInEx-LogOutput.log'),
            "`nInput System not yet initialized`nInput System not initialized`nTitle settings EventSystem fallback creation failed; retrying.",
            (New-Object System.Text.UTF8Encoding($false)))
        $repeatedInput = New-SmokeIssue011AcceptanceProjection `
            -EvidencePath $evidence -GameDirectory $gameDir -RunStartedAt $runStarted `
            -LastGiveBaseline $baseline -CrashFreshness $crash -NoQaGatePassed $true `
            -NoQaPublishedOwnerSetPassed $true -SaveLoadedPassed $true -InteractiveEvidenceObserved $true `
            -FatalWindowCount 0 -ProcessExited $true -ForcedClose $false

        [System.IO.File]::WriteAllText((Join-Path $evidence 'BepInEx-LogOutput.log'), $cleanLog, (New-Object System.Text.UTF8Encoding($false)))
        $staleGiveAt = $now.AddHours(-1)
        [System.IO.File]::WriteAllText($lastGivePath, ($staleGiveAt.ToString('o') + ' status=verified source=YConsole.ButtonLeftClick item=fixture_item requested=1 given=1 rightClick=False failure=reason=none.'), (New-Object System.Text.UTF8Encoding($false)))
        $staleLastGive = New-SmokeIssue011AcceptanceProjection `
            -EvidencePath $evidence -GameDirectory $gameDir -RunStartedAt $runStarted `
            -LastGiveBaseline $baseline -CrashFreshness $crash -NoQaGatePassed $true `
            -NoQaPublishedOwnerSetPassed $true -SaveLoadedPassed $true -InteractiveEvidenceObserved $true `
            -FatalWindowCount 0 -ProcessExited $true -ForcedClose $false

        return [ordered]@{
            PositivePassed = [bool]$positive.Passed
            RepeatedInputRejected = -not [bool]$repeatedInput.Passed -and -not [bool]$repeatedInput.InputSystem.Passed
            StaleLastGiveRejected = -not [bool]$staleLastGive.Passed -and -not [bool]$staleLastGive.DebugConsole.Passed
            Passed = [bool]$positive.Passed -and
                (-not [bool]$repeatedInput.Passed) -and (-not [bool]$repeatedInput.InputSystem.Passed) -and
                (-not [bool]$staleLastGive.Passed) -and (-not [bool]$staleLastGive.DebugConsole.Passed)
        }
    }
    finally {
        if (Test-Path -LiteralPath $root -PathType Container) {
            [System.IO.Directory]::Delete($root, $true)
        }
    }
}

function Write-SmokeCrashFreshness {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Freshness
    )

    $lines = New-Object 'System.Collections.Generic.List[string]'
    $lines.Add("Phase=$($Freshness.Phase)") | Out-Null
    $lines.Add("Status=$($Freshness.Status)") | Out-Null
    $lines.Add("RunStartedAt=$($Freshness.RunStartedAt)") | Out-Null
    $lines.Add("FatalDetectedAt=$($Freshness.FatalDetectedAt)") | Out-Null
    $lines.Add("CopiedFiles=$($Freshness.CopiedFiles)") | Out-Null
    $lines.Add("FreshDirectories=$([string]::Join('|', @($Freshness.FreshDirectories)))") | Out-Null
    $lines.Add("StaleDirectories=$([string]::Join('|', @($Freshness.StaleDirectories)))") | Out-Null
    $lines.Add("Reason=$($Freshness.Reason)") | Out-Null
    $lines | Set-Content -LiteralPath $Path
}

function Invoke-DtmApiDumpTempScavenge {
    param([Parameter(Mandatory = $true)] [string] $Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return
    }

    $now = [DateTime]::UtcNow
    $totalBytes = 0L
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -File -Recurse -ErrorAction SilentlyContinue)) {
        $totalBytes += [long]$file.Length
    }
    if ($totalBytes -ge 6GB) {
        Write-Warning "Managed DTMAPI dump temp is at or above 6 GiB: $Root ($totalBytes bytes)."
    }

    $receiptArchive = Join-Path $Root '_receipts'
    foreach ($directory in @(Get-ChildItem -LiteralPath $Root -Directory -ErrorAction SilentlyContinue)) {
        if ($directory.Name -eq '_receipts' -or ($directory.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            continue
        }

        $receiptPath = Join-Path $directory.FullName 'dump-session.json'
        if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
            Write-Warning "Unmanaged entry exists under DTMAPI dump temp and was not removed: $($directory.FullName)"
            continue
        }

        try {
            $receipt = Get-Content -Raw -LiteralPath $receiptPath | ConvertFrom-Json
            if ($receipt.owner -ne 'DTMAPI.DumpCapture' -or [int]$receipt.schemaVersion -ne 1) {
                Write-Warning "Unrecognized dump receipt; directory was not removed: $($directory.FullName)"
                continue
            }
        }
        catch {
            Write-Warning "Unreadable dump receipt; directory was not removed: $($directory.FullName)"
            continue
        }

        $leasePath = Join-Path $directory.FullName 'active.lock'
        $leaseProbe = $null
        try {
            if (Test-Path -LiteralPath $leasePath -PathType Leaf) {
                $leaseProbe = [System.IO.File]::Open($leasePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            }
        }
        catch {
            continue
        }
        finally {
            if ($leaseProbe) {
                $leaseProbe.Dispose()
            }
        }

        $age = $now - $directory.LastWriteTimeUtc
        $completed = [string]$receipt.status -like 'completed-*' -or [string]$receipt.status -eq 'cleanup-pending'
        if ($completed -or $age.TotalHours -ge 24) {
            New-Item -ItemType Directory -Force -Path $receiptArchive | Out-Null
            Copy-Item -Force -LiteralPath $receiptPath -Destination (Join-Path $receiptArchive ($directory.Name + '.json'))
            Remove-Item -LiteralPath $directory.FullName -Recurse -Force
            Write-Host "Recovered expired DTMAPI dump temp session: $($directory.FullName)"
        }
        elseif ($age.TotalHours -ge 2) {
            Write-Warning "DTMAPI dump temp session is older than two hours: $($directory.FullName)"
        }
    }

    if (Test-Path -LiteralPath $receiptArchive -PathType Container) {
        foreach ($receiptFile in @(Get-ChildItem -LiteralPath $receiptArchive -File -Filter '*.json' -ErrorAction SilentlyContinue)) {
            if (($now - $receiptFile.LastWriteTimeUtc).TotalDays -ge 7) {
                Remove-Item -LiteralPath $receiptFile.FullName -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

function Invoke-SmokeFatalProcessDump {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $Mode,
        [System.Diagnostics.Process] $Process
    )

    $dumpDir = Join-Path $EvidencePath 'Process-Dumps'
    New-Item -ItemType Directory -Force -Path $dumpDir | Out-Null
    $summaryPath = Join-Path $dumpDir 'process-dump-summary.txt'
    $errorPath = Join-Path $dumpDir 'process-dump-error.txt'
    $commandPath = Join-Path $dumpDir 'process-dump-command.txt'
    $stdoutPath = Join-Path $dumpDir 'process-dump-stdout.txt'
    $stderrPath = Join-Path $dumpDir 'process-dump-stderr.txt'
    $missingPath = Join-Path $dumpDir 'MISSING-PROCESS-DUMP-README.txt'
    $summary = New-Object 'System.Collections.Generic.List[string]'
    $summary.Add("Mode=$Mode") | Out-Null
    $summary.Add("StartedAt=$(Get-Date -Format o)") | Out-Null
    $tempDumpRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'DTMAPI-Dumps'
    Invoke-DtmApiDumpTempScavenge -Root $tempDumpRoot
    '' | Set-Content -LiteralPath $commandPath
    '' | Set-Content -LiteralPath $stdoutPath
    '' | Set-Content -LiteralPath $stderrPath
    if (Test-Path -LiteralPath $errorPath) {
        Remove-Item -LiteralPath $errorPath -Force
    }
    if (Test-Path -LiteralPath $missingPath) {
        Remove-Item -LiteralPath $missingPath -Force
    }

    if ($Mode -eq 'None') {
        $summary.Add('Status=Skipped') | Out-Null
        $summary.Add('Reason=FatalWindowProcessDumpMode=None') | Out-Null
        $summary | Set-Content -LiteralPath $summaryPath
        return 'Skipped'
    }

    if ($null -eq $Process) {
        $summary.Add('Status=MissingProcess') | Out-Null
        $summary.Add('Reason=No DolocTown process was available for live dump capture.') | Out-Null
        $summary | Set-Content -LiteralPath $summaryPath
        'No DolocTown process was available for live dump capture.' | Set-Content -LiteralPath $missingPath
        return 'MissingProcess'
    }

    $targetPid = $Process.Id
    $summary.Add("ProcessId=$targetPid") | Out-Null
    $summary.Add("MainWindowTitle=$($Process.MainWindowTitle)") | Out-Null
    $tempDumpRunId = "$(Get-Date -Format 'yyyyMMdd-HHmmss')-$targetPid-$([Guid]::NewGuid().ToString('N').Substring(0, 12))"
    $tempDumpDir = Join-Path $tempDumpRoot $tempDumpRunId
    New-Item -ItemType Directory -Force -Path $tempDumpDir | Out-Null
    $tempDumpLeasePath = Join-Path $tempDumpDir 'active.lock'
    $tempDumpLease = [System.IO.File]::Open($tempDumpLeasePath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    $tempDumpReceiptPath = Join-Path $tempDumpDir 'dump-session.json'
    [ordered]@{
        owner = 'DTMAPI.DumpCapture'
        schemaVersion = 1
        runId = $tempDumpRunId
        processId = $targetPid
        startedAtUtc = [DateTime]::UtcNow.ToString('o')
        status = 'active'
        sourceRoot = $tempDumpDir
    } | ConvertTo-Json | Set-Content -LiteralPath $tempDumpReceiptPath -Encoding UTF8
    $summary.Add("TempDumpDir=$tempDumpDir") | Out-Null

    function Complete-DumpTempSession {
        param(
            [Parameter(Mandatory = $true)] [string] $Status,
            [Parameter(Mandatory = $true)] [bool] $RetainWhenFilesRemain
        )

        $remainingDumps = @(Get-ChildItem -LiteralPath $tempDumpDir -File -Filter '*.dmp' -ErrorAction SilentlyContinue)
        $retain = $RetainWhenFilesRemain -and $remainingDumps.Count -gt 0
        $remainingDumpBytes = if ($remainingDumps.Count -gt 0) { [long](($remainingDumps | Measure-Object -Property Length -Sum).Sum) } else { 0L }
        [ordered]@{
            owner = 'DTMAPI.DumpCapture'
            schemaVersion = 1
            runId = $tempDumpRunId
            processId = $targetPid
            startedAtUtc = $summary[1].Substring('StartedAt='.Length)
            completedAtUtc = [DateTime]::UtcNow.ToString('o')
            status = $(if ($retain) { 'handoff-failed-retained' } else { $Status })
            retainedUntilUtc = $(if ($retain) { [DateTime]::UtcNow.AddHours(24).ToString('o') } else { $null })
            sourceRoot = $tempDumpDir
            remainingDumpCount = $remainingDumps.Count
            remainingDumpBytes = $remainingDumpBytes
        } | ConvertTo-Json | Set-Content -LiteralPath $tempDumpReceiptPath -Encoding UTF8

        if ($tempDumpLease) {
            $tempDumpLease.Dispose()
            $tempDumpLease = $null
        }
        if (-not $retain) {
            Remove-Item -LiteralPath $tempDumpDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path -LiteralPath $tempDumpDir -PathType Container) {
            Write-Warning "DTMAPI dump temp cleanup remains pending: $tempDumpDir"
        }
    }

    function Add-DumpText {
        param(
            [Parameter(Mandatory = $true)] [string] $Path,
            [AllowNull()] [string] $Text
        )

        if ($null -eq $Text) {
            '' | Add-Content -LiteralPath $Path
            return
        }

        $Text | Add-Content -LiteralPath $Path
    }

    function Copy-DumpToEvidence {
        param(
            [Parameter(Mandatory = $true)] [string] $SourcePath,
            [Parameter(Mandatory = $true)] [string] $DestinationPath
        )

        return Copy-DtmApiVerifiedFile -SourcePath $SourcePath -DestinationPath $DestinationPath
    }

    function Write-TooLargeDumpReadme {
        param(
            [Parameter(Mandatory = $true)] $DumpItem,
            [Parameter(Mandatory = $true)] [string] $Mode,
            [Parameter(Mandatory = $true)] [string] $DumpSha256
        )

        $readmePath = Join-Path $dumpDir 'TOO-LARGE-DUMP-README.txt'
        $hash = $DumpSha256
        @(
            'A full live process dump was captured but intentionally omitted from routine evidence packages.',
            "CapturedMode=$Mode",
            "DumpPath=$($DumpItem.FullName)",
            "DumpSize=$($DumpItem.Length)",
            "DumpSha256=$hash",
            "GeneratedAt=$(Get-Date -Format o)"
        ) | Set-Content -LiteralPath $readmePath
        $summary.Add("DumpSha256=$hash") | Out-Null
        $summary.Add("TooLargeDumpReadme=$readmePath") | Out-Null
    }

    function Invoke-ComSvcsDumpAttempt {
        $attempt = [ordered]@{
            Mode = 'ComSvcsFull'
            Status = 'MissingDump'
            DumpPath = $null
            TempDumpPath = $null
            DumpSize = 0
            DumpSha256 = $null
            HandoffVerified = $false
            ExitCode = $null
            Error = $null
        }

        $rundll32Path = Join-Path $env:WINDIR 'System32\rundll32.exe'
        $comsvcsPath = Join-Path $env:WINDIR 'System32\comsvcs.dll'
        $tempDumpPath = Join-Path $tempDumpDir ("DolocTown-$targetPid-fatal-live-comsvcs.dmp")
        $finalDumpPath = Join-Path $dumpDir ("DolocTown-$targetPid-fatal-live-comsvcs.dmp")
        $modeStdoutPath = Join-Path $dumpDir 'process-dump-comsvcs-stdout.txt'
        $modeStderrPath = Join-Path $dumpDir 'process-dump-comsvcs-stderr.txt'
        $attempt.TempDumpPath = $tempDumpPath
        $attempt.DumpPath = $finalDumpPath

        try {
            if (-not (Test-Path -LiteralPath $rundll32Path -PathType Leaf)) {
                throw "rundll32.exe was not found at '$rundll32Path'."
            }

            if (-not (Test-Path -LiteralPath $comsvcsPath -PathType Leaf)) {
                throw "comsvcs.dll was not found at '$comsvcsPath'."
            }

            Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $finalDumpPath -Force -ErrorAction SilentlyContinue
            $argumentParts = @("$comsvcsPath,", 'MiniDump', [string]$targetPid, $tempDumpPath, 'full')
            $commandLine = "`"$rundll32Path`" $([string]::Join(' ', $argumentParts))"
            Add-DumpText -Path $commandPath -Text "[ComSvcsFull] $commandLine"
            $startedAt = Get-Date
            $dumpProcess = Start-Process -FilePath $rundll32Path -ArgumentList $argumentParts -Wait -PassThru -NoNewWindow -RedirectStandardOutput $modeStdoutPath -RedirectStandardError $modeStderrPath
            $finishedAt = Get-Date
            $attempt.ExitCode = $dumpProcess.ExitCode
            Add-DumpText -Path $stdoutPath -Text "[ComSvcsFull stdout]"
            if (Test-Path -LiteralPath $modeStdoutPath) {
                Add-DumpText -Path $stdoutPath -Text (Get-Content -Raw -LiteralPath $modeStdoutPath -ErrorAction SilentlyContinue)
            }
            Add-DumpText -Path $stderrPath -Text "[ComSvcsFull stderr]"
            if (Test-Path -LiteralPath $modeStderrPath) {
                Add-DumpText -Path $stderrPath -Text (Get-Content -Raw -LiteralPath $modeStderrPath -ErrorAction SilentlyContinue)
            }

            $copiedDump = Copy-DumpToEvidence -SourcePath $tempDumpPath -DestinationPath $finalDumpPath
            if ($null -ne $copiedDump) {
                $attempt.Status = 'Captured'
                $attempt.DumpSize = $copiedDump.Length
                $attempt.DumpSha256 = $copiedDump.Sha256
                $attempt.HandoffVerified = [bool]$copiedDump.Verified
            }
            else {
                $attempt.Status = 'MissingDump'
            }

            $summary.Add("ComSvcsFull.StartedAt=$($startedAt.ToString('o'))") | Out-Null
            $summary.Add("ComSvcsFull.FinishedAt=$($finishedAt.ToString('o'))") | Out-Null
            $summary.Add("ComSvcsFull.ExitCode=$($attempt.ExitCode)") | Out-Null
            $summary.Add("ComSvcsFull.TempDumpPath=$tempDumpPath") | Out-Null
            $summary.Add("ComSvcsFull.DumpPath=$finalDumpPath") | Out-Null
            $summary.Add("ComSvcsFull.Status=$($attempt.Status)") | Out-Null
            $summary.Add("ComSvcsFull.DumpSize=$($attempt.DumpSize)") | Out-Null
            $summary.Add("ComSvcsFull.DumpSha256=$($attempt.DumpSha256)") | Out-Null
            $summary.Add("ComSvcsFull.HandoffVerified=$($attempt.HandoffVerified)") | Out-Null
            return [pscustomobject]$attempt
        }
        catch {
            $attempt.Status = 'Error'
            $attempt.Error = "$($_.Exception.GetType().Name): $($_.Exception.Message)"
            $summary.Add("ComSvcsFull.Status=Error") | Out-Null
            $summary.Add("ComSvcsFull.Error=$($attempt.Error)") | Out-Null
            Add-DumpText -Path $errorPath -Text "[ComSvcsFull] $($attempt.Error)"
            return [pscustomobject]$attempt
        }
        finally {
            if ($attempt.HandoffVerified -and (Test-Path -LiteralPath $tempDumpPath -PathType Leaf)) {
                try {
                    Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction Stop
                    $summary.Add("ComSvcsFull.TempCleanup=DeletedAfterVerifiedHandoff") | Out-Null
                }
                catch {
                    $summary.Add("ComSvcsFull.TempCleanup=CleanupPending") | Out-Null
                    Add-DumpText -Path $errorPath -Text "[ComSvcsFull cleanup] $($_.Exception.GetType().Name): $($_.Exception.Message)"
                }
            }
        }
    }

    function Invoke-DbgHelpDumpAttempt {
        $attempt = [ordered]@{
            Mode = 'DbgHelpFull'
            Status = 'MissingDump'
            DumpPath = $null
            TempDumpPath = $null
            DumpSize = 0
            DumpSha256 = $null
            HandoffVerified = $false
            ExitCode = $null
            Error = $null
        }

        $tempDumpPath = Join-Path $tempDumpDir ("DolocTown-$targetPid-fatal-live-dbghelp.dmp")
        $finalDumpPath = Join-Path $dumpDir ("DolocTown-$targetPid-fatal-live-dbghelp.dmp")
        $attempt.TempDumpPath = $tempDumpPath
        $attempt.DumpPath = $finalDumpPath
        Add-DumpText -Path $commandPath -Text "[DbgHelpFull] MiniDumpWriteDump(pid=$targetPid, dumpType=MiniDumpWithFullMemory, tempPath=$tempDumpPath)"

        try {
            if (-not ([System.Management.Automation.PSTypeName]'SmokeMiniDumpWriter').Type) {
                Add-Type @'
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class SmokeMiniDumpWriter
{
    [DllImport("Dbghelp.dll", SetLastError = true)]
    public static extern bool MiniDumpWriteDump(
        IntPtr hProcess,
        int processId,
        SafeFileHandle hFile,
        int dumpType,
        IntPtr exceptionParam,
        IntPtr userStreamParam,
        IntPtr callbackParam);

    public static int GetLastError()
    {
        return Marshal.GetLastWin32Error();
    }
}
'@
            }

            Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $finalDumpPath -Force -ErrorAction SilentlyContinue
            $startedAt = Get-Date
            $fileStream = [System.IO.File]::Open($tempDumpPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            try {
                $miniDumpWithFullMemory = 2
                $ok = [SmokeMiniDumpWriter]::MiniDumpWriteDump(
                    $Process.Handle,
                    $targetPid,
                    $fileStream.SafeFileHandle,
                    $miniDumpWithFullMemory,
                    [IntPtr]::Zero,
                    [IntPtr]::Zero,
                    [IntPtr]::Zero)
                $lastError = [SmokeMiniDumpWriter]::GetLastError()
            }
            finally {
                $fileStream.Dispose()
            }
            $finishedAt = Get-Date

            if (-not $ok) {
                $attempt.Status = 'Error'
                $attempt.Error = "MiniDumpWriteDump returned false. LastWin32Error=$lastError"
                Add-DumpText -Path $errorPath -Text "[DbgHelpFull] $($attempt.Error)"
            }

            $copiedDump = Copy-DumpToEvidence -SourcePath $tempDumpPath -DestinationPath $finalDumpPath
            if ($null -ne $copiedDump) {
                $attempt.Status = 'Captured'
                $attempt.DumpSize = $copiedDump.Length
                $attempt.DumpSha256 = $copiedDump.Sha256
                $attempt.HandoffVerified = [bool]$copiedDump.Verified
            }
            elseif ($attempt.Status -ne 'Error') {
                $attempt.Status = 'MissingDump'
            }

            $summary.Add("DbgHelpFull.StartedAt=$($startedAt.ToString('o'))") | Out-Null
            $summary.Add("DbgHelpFull.FinishedAt=$($finishedAt.ToString('o'))") | Out-Null
            $summary.Add("DbgHelpFull.LastWin32Error=$lastError") | Out-Null
            $summary.Add("DbgHelpFull.TempDumpPath=$tempDumpPath") | Out-Null
            $summary.Add("DbgHelpFull.DumpPath=$finalDumpPath") | Out-Null
            $summary.Add("DbgHelpFull.Status=$($attempt.Status)") | Out-Null
            $summary.Add("DbgHelpFull.DumpSize=$($attempt.DumpSize)") | Out-Null
            $summary.Add("DbgHelpFull.DumpSha256=$($attempt.DumpSha256)") | Out-Null
            $summary.Add("DbgHelpFull.HandoffVerified=$($attempt.HandoffVerified)") | Out-Null
            return [pscustomobject]$attempt
        }
        catch {
            $attempt.Status = 'Error'
            $attempt.Error = "$($_.Exception.GetType().Name): $($_.Exception.Message)"
            $summary.Add("DbgHelpFull.Status=Error") | Out-Null
            $summary.Add("DbgHelpFull.Error=$($attempt.Error)") | Out-Null
            Add-DumpText -Path $errorPath -Text "[DbgHelpFull] $($attempt.Error)"
            return [pscustomobject]$attempt
        }
        finally {
            if ($attempt.HandoffVerified -and (Test-Path -LiteralPath $tempDumpPath -PathType Leaf)) {
                try {
                    Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction Stop
                    $summary.Add("DbgHelpFull.TempCleanup=DeletedAfterVerifiedHandoff") | Out-Null
                }
                catch {
                    $summary.Add("DbgHelpFull.TempCleanup=CleanupPending") | Out-Null
                    Add-DumpText -Path $errorPath -Text "[DbgHelpFull cleanup] $($_.Exception.GetType().Name): $($_.Exception.Message)"
                }
            }
        }
    }

    $attempts = New-Object 'System.Collections.Generic.List[object]'
    if ($Mode -eq 'ComSvcsFull' -or $Mode -eq 'Both') {
        $attempts.Add((Invoke-ComSvcsDumpAttempt)) | Out-Null
    }
    if ($Mode -eq 'DbgHelpFull' -or ($Mode -eq 'Both' -and -not ($attempts | Where-Object { $_.Status -eq 'Captured' }))) {
        $attempts.Add((Invoke-DbgHelpDumpAttempt)) | Out-Null
    }
    elseif ($Mode -eq 'Both') {
        $summary.Add('DbgHelpFull.Status=Skipped') | Out-Null
        $summary.Add('DbgHelpFull.Reason=ComSvcsFull captured a dump; fallback was not needed.') | Out-Null
    }

    $captured = @($attempts | Where-Object { $_.Status -eq 'Captured' })
    if ($captured.Count -gt 0) {
        $firstCapture = $captured | Select-Object -First 1
        $summary.Add("Status=Captured") | Out-Null
        $summary.Add("CapturedMode=$($firstCapture.Mode)") | Out-Null
        $summary.Add("DumpPath=$($firstCapture.DumpPath)") | Out-Null
        $summary.Add("DumpSize=$($firstCapture.DumpSize)") | Out-Null
        $summary.Add("DumpSha256=$($firstCapture.DumpSha256)") | Out-Null
        $summary.Add("HandoffVerified=$($firstCapture.HandoffVerified)") | Out-Null
        $capturedDumpItem = Get-Item -LiteralPath $firstCapture.DumpPath -ErrorAction SilentlyContinue
        if ($capturedDumpItem) {
            Write-TooLargeDumpReadme -DumpItem $capturedDumpItem -Mode $firstCapture.Mode -DumpSha256 $firstCapture.DumpSha256
        }
        [ordered]@{
            owner = 'DTMAPI.ProcessDumpEvidence'
            schemaVersion = 1
            runId = $tempDumpRunId
            status = 'captured-verified'
            captureMode = $firstCapture.Mode
            dumpPath = $firstCapture.DumpPath
            dumpSize = [long]$firstCapture.DumpSize
            dumpSha256 = $firstCapture.DumpSha256
            handoffVerified = [bool]$firstCapture.HandoffVerified
            analysisStatus = 'not-analyzed'
            duplicatePolicy = 'retain-one-canonical-per-reviewed-signature-or-hash'
            createdAtUtc = [DateTime]::UtcNow.ToString('o')
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dumpDir 'process-dump-receipt.json') -Encoding UTF8
        $summary | Set-Content -LiteralPath $summaryPath
        Complete-DumpTempSession -Status 'completed-verified' -RetainWhenFilesRemain $false
        return "Captured:$($firstCapture.Mode)"
    }

    $errors = @($attempts | Where-Object { $_.Status -eq 'Error' })
    if ($errors.Count -gt 0) {
        $summary.Add('Status=Error') | Out-Null
        $summary.Add("Reason=No dump was captured and at least one dump attempt failed. See process-dump-error.txt and process-dump-command.txt.") | Out-Null
        $summary | Set-Content -LiteralPath $summaryPath
        "No dump was captured. See process-dump-summary.txt, process-dump-command.txt, and process-dump-error.txt." | Set-Content -LiteralPath $missingPath
        [ordered]@{
            owner = 'DTMAPI.ProcessDumpEvidence'
            schemaVersion = 1
            runId = $tempDumpRunId
            status = 'capture-error'
            captureMode = $Mode
            handoffVerified = $false
            analysisStatus = 'not-applicable'
            createdAtUtc = [DateTime]::UtcNow.ToString('o')
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dumpDir 'process-dump-receipt.json') -Encoding UTF8
        Complete-DumpTempSession -Status 'capture-error' -RetainWhenFilesRemain $true
        return 'Error'
    }

    $summary.Add('Status=MissingDump') | Out-Null
    $summary.Add('DumpSize=0') | Out-Null
    $summary.Add("Reason=No configured dump attempt produced a non-empty dump file.") | Out-Null
    $summary | Set-Content -LiteralPath $summaryPath
    "No configured process dump attempt produced a non-empty dump file. See process-dump-summary.txt and process-dump-command.txt." | Set-Content -LiteralPath $missingPath
    [ordered]@{
        owner = 'DTMAPI.ProcessDumpEvidence'
        schemaVersion = 1
        runId = $tempDumpRunId
        status = 'missing-dump'
        captureMode = $Mode
        handoffVerified = $false
        analysisStatus = 'not-applicable'
        createdAtUtc = [DateTime]::UtcNow.ToString('o')
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dumpDir 'process-dump-receipt.json') -Encoding UTF8
    Complete-DumpTempSession -Status 'missing-dump' -RetainWhenFilesRemain $true
    return 'MissingDump'
}

function Get-SmokeOfficialModProfileKnownIds {
    return @(
        'Local.DTMAPI',
        'Local.DTMAPI_ChestLocatorEnhancer',
        'Local.DTMAPI_DreckoAssets',
        'Local.DTMAPI_ExtraVehicle',
        'Local.DTMAPI_HatchAssets',
        'Local.DTMAPI_LightningChicken',
        'Local.DTMAPI_Mine',
        'Local.DTMAPI_MoleAssets',
        'Local.DTMAPI_MoreEquipmentSlots',
        'Local.DTMAPI_MoreSaves',
        'Local.DTMAPI_Oil',
        'Local.DTMAPI_OilfloaterAssets',
        'Local.DTMAPI_ShellCrab',
        'Local.DTMAPI_StrongPlantingGun',
        'Local.DTMAPI_YKeyConsole',
        'Local.DTMAPI_Zoom',
        'Local.Yuuka_DTMAPI_ActionSpeed',
        'Local.Yuuka_DTMAPI_AnimalHusbandryProgress',
        'Local.Yuuka_DTMAPI_AutoFishing',
        'Local.Yuuka_DTMAPI_ChickenPetBag',
        'Local.Yuuka_DTMAPI_FishBreedingAssistant',
        'Local.Yuuka_DTMAPI_ManboCardboardAudio',
        'Local.Yuuka_DTMAPI_OneActionComplete',
        'Workshop.3742714442',
        'Workshop.3742717440',
        'Workshop.3742763050',
        'Workshop.3742763309',
        'Workshop.3742763540',
        'Workshop.3742763706',
        'Workshop.3742763843',
        'Workshop.3742765514',
        'Workshop.3743016467',
        'Workshop.3743799721',
        'Workshop.3744059735',
        'Workshop.3746319981'
    )
}

function Set-SmokeModInfoEnabled {
    param(
        [Parameter(Mandatory = $true)] $Entry,
        [Parameter(Mandatory = $true)] [bool] $Enabled,
        [Parameter(Mandatory = $true)] [int] $Priority
    )

    if ($Entry.PSObject.Properties['enabled']) {
        $Entry.enabled = $Enabled
    }
    else {
        $Entry | Add-Member -MemberType NoteProperty -Name 'enabled' -Value $Enabled
    }

    if ($Entry.PSObject.Properties['priority']) {
        $Entry.priority = $Priority
    }
    else {
        $Entry | Add-Member -MemberType NoteProperty -Name 'priority' -Value $Priority
    }
}

function Set-SmokeOfficialModProfile {
    param(
        [Parameter(Mandatory = $true)] [string] $Profile,
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [string[]] $ExtraEnabledIds = @(),
        [bool] $IsolateAll = $false
    )

    # Native Workshop source capture reads the official ModManager before the
    # QA host redirects LocalSave.cloudDirPath. The temporary profile therefore
    # still targets the live pre-launch enablement file; Runtime Mod discovery
    # occurs only after the pre-Runtime save guard is installed.
    $persistentRoot = Get-DolocTownLivePersistentRootForSmoke
    $enablementPath = Join-Path $persistentRoot 'SAVE\mod_infos.json'
    $summaryPath = Join-Path $EvidencePath 'official-mod-profile-summary.json'
    $normalizedExtraEnabledIds = @($ExtraEnabledIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() } | Sort-Object -Unique)
    $profileRequiredEnabledIds = if ($Profile -eq 'Published11') {
        @(Get-SmokePublishedProductWorkshopIds)
    }
    elseif ($Profile -eq 'Local11') {
        @(Get-SmokePublishedProductLocalIds)
    }
    else {
        @()
    }
    $requiredEnabledIds = @($normalizedExtraEnabledIds + $profileRequiredEnabledIds | Sort-Object -Unique)
    if ($Profile -eq 'Current') {
        $summary = [ordered]@{
            Profile = $Profile
            Applied = $false
            Reason = if ($normalizedExtraEnabledIds.Count -gt 0) { 'Current profile cannot apply ExtraEnabledIds; mod_infos.json was left unchanged.' } else { 'Current profile requested; mod_infos.json was left unchanged.' }
            EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
            BackupPath = ''
            EnabledIds = @()
            DisabledIds = @()
            ExtraEnabledIds = @($normalizedExtraEnabledIds)
            MissingExtraEnabledIds = @($normalizedExtraEnabledIds)
            IsolateAllOfficialMods = $IsolateAll
        }
        Write-SmokeJsonObject -Path $summaryPath -Value $summary
        return $summary
    }

    if (-not (Test-Path -LiteralPath $enablementPath -PathType Leaf)) {
        $summary = [ordered]@{
            Profile = $Profile
            Applied = $false
            Reason = 'mod_infos.json was not found; profile could not be applied.'
            EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
            BackupPath = ''
            EnabledIds = @()
            DisabledIds = @()
            ExtraEnabledIds = @($normalizedExtraEnabledIds)
            MissingExtraEnabledIds = @($normalizedExtraEnabledIds)
            IsolateAllOfficialMods = $IsolateAll
        }
        Write-SmokeJsonObject -Path $summaryPath -Value $summary
        return $summary
    }

    $data = Get-Content -Raw -Encoding UTF8 -LiteralPath $enablementPath | ConvertFrom-Json
    if ($null -eq $data.modInfos) {
        $data | Add-Member -MemberType NoteProperty -Name 'modInfos' -Value ([pscustomobject]@{}) -Force
    }

    $missingExtraEnabledIds = @($requiredEnabledIds | Where-Object { -not $data.modInfos.PSObject.Properties[$_] })
    if ($missingExtraEnabledIds.Count -gt 0) {
        $summary = [ordered]@{
            Profile = $Profile
            Applied = $false
            Reason = 'One or more required profile/extra enabled IDs are missing from mod_infos.json; profile was not applied.'
            EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
            BackupPath = ''
            EnabledIds = @()
            DisabledIds = @()
            ExtraEnabledIds = @($normalizedExtraEnabledIds)
            RequiredEnabledIds = @($requiredEnabledIds)
            MissingExtraEnabledIds = @($missingExtraEnabledIds)
            IsolateAllOfficialMods = $IsolateAll
        }
        Write-SmokeJsonObject -Path $summaryPath -Value $summary
        return $summary
    }

    $targetIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($id in @(Get-SmokeOfficialModProfileKnownIds)) {
        [void]$targetIds.Add($id)
    }

    if ($IsolateAll) {
        foreach ($property in @($data.modInfos.PSObject.Properties)) {
            if ($property.Name -like 'Local.*' -or $property.Name -like 'Workshop.*') {
                [void]$targetIds.Add($property.Name)
            }
        }
    }

    foreach ($property in @($data.modInfos.PSObject.Properties)) {
        $title = if ($property.Value -and $property.Value.PSObject.Properties['title']) { [string]$property.Value.title } else { '' }
        if (($property.Name -like 'Local.*' -or $property.Name -like 'Workshop.*') -and
            ($property.Name -match 'DTMAPI' -or $title -match 'DTMAPI|Y键控制台|哈奇|抛壳蟹|浮游生物|田鼠|壁虎|曼波')) {
            [void]$targetIds.Add($property.Name)
        }
    }

    $enabledIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    function Add-PreferredSmokeModId {
        param([string[]] $Candidates)

        foreach ($candidate in $Candidates) {
            if ($data.modInfos.PSObject.Properties[$candidate]) {
                [void]$enabledIds.Add($candidate)
                return
            }
        }

        if ($Candidates.Count -gt 0) {
            [void]$enabledIds.Add($Candidates[0])
        }
    }

    switch ($Profile) {
        'CoreOnly' {
        }
        'CoreUi' {
            Add-PreferredSmokeModId @('Workshop.3742717440','Local.DTMAPI_Zoom')
            Add-PreferredSmokeModId @('Workshop.3742763050','Local.DTMAPI_MoreSaves')
            Add-PreferredSmokeModId @('Workshop.3744059735','Local.DTMAPI_MoreEquipmentSlots')
            Add-PreferredSmokeModId @('Workshop.3742714442','Local.DTMAPI_YKeyConsole')
        }
        'CoreCustomAnimals' {
            Add-PreferredSmokeModId @('Local.DTMAPI_ShellCrab')
            Add-PreferredSmokeModId @('Local.DTMAPI_HatchAssets')
            Add-PreferredSmokeModId @('Local.DTMAPI_OilfloaterAssets')
            Add-PreferredSmokeModId @('Local.DTMAPI_MoleAssets')
            Add-PreferredSmokeModId @('Local.DTMAPI_DreckoAssets')
        }
        'CoreAnimalVoice' {
            Add-PreferredSmokeModId @('Local.DTMAPI_HatchAssets')
        }
        'CoreAutoFishing' {
            Add-PreferredSmokeModId @('Local.Yuuka_DTMAPI_AutoFishing','Workshop.3743799721')
        }
        'Published11' {
            foreach ($id in @(Get-SmokePublishedProductWorkshopIds)) {
                [void]$enabledIds.Add($id)
                [void]$targetIds.Add($id)
            }
        }
        'Local11' {
            foreach ($id in @(Get-SmokePublishedProductLocalIds)) {
                [void]$enabledIds.Add($id)
                [void]$targetIds.Add($id)
            }
        }
        'FullKnown' {
            foreach ($id in @($targetIds)) {
                [void]$enabledIds.Add($id)
            }
        }
    }
    foreach ($id in $normalizedExtraEnabledIds) {
        [void]$enabledIds.Add($id)
        [void]$targetIds.Add($id)
    }

    $backupPath = Join-Path $EvidencePath 'official-mod-profile.mod_infos.before.json'
    Copy-Item -Force -LiteralPath $enablementPath -Destination $backupPath
    $maxPriority = -1
    foreach ($property in @($data.modInfos.PSObject.Properties)) {
        $entry = $property.Value
        if ($entry -and $entry.PSObject.Properties['enabled'] -and [bool]$entry.enabled -and $entry.PSObject.Properties['priority']) {
            try {
                $maxPriority = [Math]::Max($maxPriority, [int]$entry.priority)
            }
            catch {
            }
        }
    }

    $enabledApplied = New-Object 'System.Collections.Generic.List[string]'
    $disabledApplied = New-Object 'System.Collections.Generic.List[string]'
    foreach ($property in @($data.modInfos.PSObject.Properties)) {
        if (-not $targetIds.Contains($property.Name)) {
            continue
        }

        if ($enabledIds.Contains($property.Name)) {
            $currentPriority = -1
            if ($property.Value -and $property.Value.PSObject.Properties['priority']) {
                try {
                    $currentPriority = [int]$property.Value.priority
                }
                catch {
                    $currentPriority = -1
                }
            }
            if ($currentPriority -lt 0) {
                $maxPriority++
                $currentPriority = $maxPriority
            }
            Set-SmokeModInfoEnabled -Entry $property.Value -Enabled $true -Priority $currentPriority
            $enabledApplied.Add($property.Name) | Out-Null
        }
        else {
            Set-SmokeModInfoEnabled -Entry $property.Value -Enabled $false -Priority -1
            $disabledApplied.Add($property.Name) | Out-Null
        }
    }

    try {
        Write-SmokeJsonObject -Path $enablementPath -Value $data
        Copy-Item -Force -LiteralPath $enablementPath -Destination (Join-Path $EvidencePath 'official-mod-profile.mod_infos.applied.json')
        $summary = [ordered]@{
            Profile = $Profile
            Applied = $true
            Reason = 'Temporary smoke feature profile applied; run-game-smoke restores the original mod_infos.json during cleanup.'
            EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
            BackupPath = [System.IO.Path]::GetFullPath($backupPath)
            EnabledIds = @($enabledApplied.ToArray())
            DisabledIds = @($disabledApplied.ToArray())
            ExtraEnabledIds = @($normalizedExtraEnabledIds)
            RequiredEnabledIds = @($requiredEnabledIds)
            MissingExtraEnabledIds = @()
            IsolateAllOfficialMods = $IsolateAll
        }
        Write-SmokeJsonObject -Path $summaryPath -Value $summary
        return $summary
    }
    catch {
        $applyError = $_
        try {
            [void](Restore-SmokeOfficialModProfile -Summary ([ordered]@{
                Profile = $Profile
                Applied = $true
                EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
                BackupPath = [System.IO.Path]::GetFullPath($backupPath)
            }) -EvidencePath $EvidencePath -Phase 'apply')
        }
        catch {
            $restoreError = $_
            try {
                Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'official-mod-profile-restore-failure.json') -Value ([ordered]@{
                    Phase = 'apply'
                    Profile = $Profile
                    EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
                    BackupPath = [System.IO.Path]::GetFullPath($backupPath)
                    ApplyError = [string]$applyError.Exception.Message
                    RestoreError = [string]$restoreError.Exception.Message
                })
            }
            catch {
            }
            throw "Official Mod smoke profile apply failed and its backup could not be restored. applyError=$($applyError.Exception.Message); restoreError=$($restoreError.Exception.Message)"
        }
        throw $applyError
    }
}

function Restore-SmokeOfficialModProfile {
    param(
        [Parameter(Mandatory = $true)] $Summary,
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $Phase
    )

    if (-not [bool]$Summary.Applied) {
        return $true
    }

    $backupPath = [string]$Summary.BackupPath
    $enablementPath = [string]$Summary.EnablementPath
    if ([string]::IsNullOrWhiteSpace($backupPath) -or -not (Test-Path -LiteralPath $backupPath -PathType Leaf)) {
        try {
            Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'official-mod-profile-restore-failure.json') -Value ([ordered]@{
                Phase = $Phase
                Profile = [string]$Summary.Profile
                EnablementPath = $enablementPath
                BackupPath = $backupPath
                RestoreError = 'The required mod_infos.json backup is missing.'
            })
        }
        catch {
        }
        throw "Official Mod smoke profile backup is missing during $Phase restoration: $backupPath"
    }

    try {
        $expectedLength = [int64](Get-Item -LiteralPath $backupPath).Length
        $expectedHash = Get-SmokeFileSha256 -Path $backupPath
        Copy-Item -Force -LiteralPath $backupPath -Destination $enablementPath
        $actualLength = [int64](Get-Item -LiteralPath $enablementPath).Length
        $actualHash = Get-SmokeFileSha256 -Path $enablementPath
        $passed = $actualLength -eq $expectedLength -and
            [string]::Equals($actualHash, $expectedHash, [System.StringComparison]::OrdinalIgnoreCase)
        Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'official-mod-profile-restore-verification.json') -Value ([ordered]@{
            Phase = $Phase
            Profile = [string]$Summary.Profile
            EnablementPath = $enablementPath
            BackupPath = $backupPath
            ExpectedLength = $expectedLength
            ActualLength = $actualLength
            ExpectedSha256 = $expectedHash
            ActualSha256 = $actualHash
            Passed = $passed
        })
        if (-not $passed) {
            throw 'The restored mod_infos.json does not match its byte-for-byte backup.'
        }
        return $true
    }
    catch {
        $restoreError = $_
        try {
            Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'official-mod-profile-restore-failure.json') -Value ([ordered]@{
                Phase = $Phase
                Profile = [string]$Summary.Profile
                EnablementPath = $enablementPath
                BackupPath = $backupPath
                RestoreError = [string]$restoreError.Exception.Message
            })
        }
        catch {
        }
        throw "Official Mod smoke profile restoration failed during $Phase. restoreError=$($restoreError.Exception.Message)"
    }
}

function Get-SmokeStatus {
    param(
        [bool] $Requested,
        [bool] $Passed,
        [bool] $Blocked = $false
    )

    if ($Blocked) {
        return 'Blocked'
    }
    if (-not $Requested) {
        return 'Skipped'
    }
    if ($Passed) {
        return 'Passed'
    }
    return 'Failed'
}

function Get-SmokeAlwaysStatus {
    param(
        [bool] $Passed,
        [bool] $Blocked = $false
    )

    return Get-SmokeStatus -Requested $true -Passed $Passed -Blocked $Blocked
}

function Write-SmokeBlockedResult {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $Reason,
        [string] $RunStatus = 'Blocked'
    )

    Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'result.json') -Value @{
        SchemaVersion = 2
        RunStatus = $RunStatus
        RunStatusReason = $Reason
        StartupLog = 'Blocked'
        GameLaunched = 'Blocked'
        HookProbe = 'Skipped'
        SaveLoaded = 'Skipped'
        NoFatalInstanceWindow = if ($Reason -match 'Fatal') { 'Failed' } else { 'Passed' }
        ProcessExited = if ($Reason -match 'DolocTown\.exe') { 'Failed' } else { 'Skipped' }
        ForcedClose = 'Skipped'
        LifecycleObservation = 'Skipped'
        LifecycleBoundaryContract = 'Skipped'
        LifecycleBoundaryContractSummary = ''
        ShadowContentRegistry = 'Skipped'
        ContentRegistry = 'Skipped'
        ManifestRegistry = 'Skipped'
        DependencyCompatibility = 'Skipped'
        ContentPackOwnership = 'Skipped'
        RegistryDiffs = 'Skipped'
        RefactorScaffoldFlags = 'Skipped'
        RefactorScaffoldFlagsSummary = ''
        ResourceLifecycleLedger = 'Skipped'
        ResourceLifecycleCleanup = 'Skipped'
        ResourceLifecycleSummary = ''
        TitleIdleResourceGrowth = 'Skipped'
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
        LongTitleIdleBeforeSave = 'Skipped'
        TitleIdleBeforeSaveSeconds = $TitleIdleBeforeSaveSeconds
        SaveLoadCycle = 'Skipped'
        SaveLoadCycleSummary = ''
        SaveLoadObjectSnapshotMode = $SaveLoadObjectSnapshotMode
        SmokeOwnerRootIsolationProfile = $SmokeOwnerRootIsolationProfile
        SmokeOwnerRootIsolationSummary = ''
        SmokeNativeLoadContinuationProbe = $SmokeNativeLoadContinuationProbe
        SmokeNativeLoadContinuationSummary = ''
        LastNativeContinuationLine = ''
        SaveLoadCyclePendingPressure = 'Skipped'
        SaveLoadCyclePendingPressureSummary = ''
        AutoFishingLifecycle = 'Skipped'
        AutoFishingLifecycleSummary = ''
        AutoFishingSoak = 'Skipped'
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
        Completed = Get-Date -Format o
    }
}

$autoFishingScenarioEffective = if (-not $PSBoundParameters.ContainsKey('AutoFishingScenario') -and $AutoExerciseAutoFishingMiniGameComplete) {
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
    if ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne $batch5RequiredSaveSlot) {
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
# MouseGive is a G5 mutation case. When it is requested, the complete
# debug-console case stays in that participant route instead of splitting one
# run between two owners.
$qaG4DebugConsoleEnabled = [bool]$StageQaHost -and [bool]$AutoExerciseDebugConsole -and -not [bool]$AutoExerciseDebugConsoleMouseGive
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
$qaExternalStateProtectionRequested = [bool]$qaG5AnyRequested -or [bool]$AssertMoreEquipmentSlotsColdRecovery
if ($qaG5AnyRequested -and ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -le 0)) {
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
        SaveSlotExplicit = [bool]$PSBoundParameters.ContainsKey('SaveSlot')
        Cases = @($qaG5WorldMutationCases.ToArray())
        ExclusiveQaG5Owner = $true
        SetupReceipt = 'explicit-g5-whitelist'
        CommitReceipt = 'per-case-existing-Smoke-status'
        CleanupReceipt = 'case-local-finally+runner-post-exit-save-config-profile-transaction'
        RealWorldFallbackAllowed = $false
    } | ConvertTo-Json -Depth 4
    exit 0
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
if ($qaG4RequiresSaveLoaded -and ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -le 0)) {
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
    $equipmentObservationUnexpectedParameters = @($PSBoundParameters.Keys | Where-Object {
        $equipmentObservationAllowedParameters -notcontains [string]$_
    })
    if ($equipmentObservationUnexpectedParameters.Count -gt 0) {
        throw ('-QaObserveEquipmentSlotsUi is an independent cold-run G4 route and cannot be combined with: ' +
            [string]::Join(', ', @($equipmentObservationUnexpectedParameters | Sort-Object)) + '.')
    }
    if (-not $UseSteam -or $DirectExe -or -not $SkipInstall) {
        throw '-QaObserveEquipmentSlotsUi requires explicit -UseSteam -SkipInstall and no -DirectExe.'
    }
    if ($OfficialModProfile -notin @('Published11','Local11') -or -not $IsolateAllOfficialMods -or
        @($OfficialModProfileExtraEnabledIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -ne 0 -or
        -not $PSBoundParameters.ContainsKey('SaveSlot') -or $SaveSlot -ne 3) {
        throw '-QaObserveEquipmentSlotsUi requires the exact isolated Published11 or Local11 profile, no extra enabled products, and explicit third save slot 3.'
    }
}
if ($ValidateQaG4RoutingOnly) {
    [pscustomobject]@{
        ProtocolVersion = 7
        StageQaHost = [bool]$StageQaHost
        SaveSlotExplicit = [bool]$PSBoundParameters.ContainsKey('SaveSlot')
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
        DebugConsoleMouseGiveOwner = 'qa-G5'
        SaveSlotsPagingEnabled = $qaG4SaveSlotsPagingEnabled
        SaveSlot = $SaveSlot
        AnimalObservationEnabled = $qaG4AnimalObservationEnabled
        EquipmentSlotsObservationEnabled = $qaG4EquipmentSlotsObservationEnabled
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
    exit 0
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
        OrdinaryPlayerHandshake = 'runner prints title-screen -> third-save instructions before waiting for SaveLoaded; internal QA auto-load and ReturnHome navigation remain disabled'
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
    exit 0
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
    } | ConvertTo-Json -Depth 4
    exit 0
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
if ($qaG6LifecycleCases.Count -gt 0 -and ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -le 0)) {
        throw 'G6 fishing/lifecycle/save-load cases require -StageQaHost and an explicit positive -SaveSlot.'
}
if ($QaObserveEquipmentSlotsUi -and
    $MoreEquipmentSlotsTransitionPhase -ne 'U1') {
    $otherG4Requested = $qaG4AnyTitleUiEnabled -or $qaG4OfficialModUiEnabled -or $qaG4PauseMenuLayoutEnabled -or
        $qaG4DebugConsoleEnabled -or $qaG4SaveSlotsPagingEnabled -or $qaG4AnimalObservationEnabled -or
        $qaG4AudioObservationEnabled -or $qaG4HatchVoiceEnabled -or $qaG4CameraPlayableEnabled -or
        $qaG4ContentMetadataObservationEnabled -or $qaG4ExternalPlayerInputObservationEnabled -or
        $qaG4TitleLifecycleEnabled -or $qaG4ProductOwnerRefreshEnabled -or $qaG4AdvancedProductOwnerDeactivationEnabled
    $otherG3Requested = $qaCustomEntityContractEnabled -or $qaFishRoeTooltipObservationEnabled -or
        $qaDiagnosticsSnapshotEnabled -or [bool]$QaObserveSaveSaved -or [bool]$QaObserveWorkshopReloadCompleted
    if ($otherG4Requested -or $qaG5AnyRequested -or $qaG6AnyRequested -or $otherG3Requested) {
        throw '-QaObserveEquipmentSlotsUi is an independent cold-run G4 route and cannot be combined with other G3/G4/G5/G6 cases.'
    }
    if ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -le 0) {
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
        SaveSlotExplicit = [bool]$PSBoundParameters.ContainsKey('SaveSlot')
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
    exit 0
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
    exit 1
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
    exit 1
}
if (($batch5GcLadderAutoFishing -or ($AutoExerciseAutoFishingPhase -and -not $autoFishingNoDemandFrameProfile)) -and
    ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 5)) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'AutoFishing phase and Batch 5 AutoFishing smoke require the real fifth save fixture. Re-run with explicit -SaveSlot 5.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason`nSaveSlot=$SaveSlot`nSaveSlotExplicit=$($PSBoundParameters.ContainsKey('SaveSlot'))`nAutoExerciseAutoFishingPhase=$AutoExerciseAutoFishingPhase`nAutoFishingScenario=$autoFishingScenarioEffective`nAutoFishingCastChargeRatio=$AutoFishingCastChargeRatio`nAutoFishingSoakLoops=$AutoFishingSoakLoops`nAutoFishingToggleKey=$autoFishingToggleKeyEffective" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
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
    exit 1
}
if ($AutoExerciseVehicle) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'Vehicle custom motor smoke is archived after 2026-06-15 manual QA found severe texture pollution in the SecondMotor sample. IMotorVehicleApi and MotorVehicle GameBridge hooks are retired/removed from active runtime code; no active SecondMotor package is installed or validated.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason`nSaveSlot=$SaveSlot`nSaveSlotExplicit=$($PSBoundParameters.ContainsKey('SaveSlot'))`nAutoExerciseVehicle=$AutoExerciseVehicle" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
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
    exit 1
}
if ($AutoExerciseAudioReplacement -and ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or (($SaveSlot -ne 8) -and ($SaveSlot -ne 10)))) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'Audio replacement cardboard smoke requires a real paper-box fixture save. Re-run with explicit -SaveSlot 8 or -SaveSlot 10.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason`nSaveSlot=$SaveSlot`nSaveSlotExplicit=$($PSBoundParameters.ContainsKey('SaveSlot'))`nAutoExerciseAudioReplacement=$AutoExerciseAudioReplacement" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
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
    exit 1
}
if ($AutoExerciseHatchAnimalVoice -and ((-not $PSBoundParameters.ContainsKey('SaveSlot')) -or $SaveSlot -ne 7)) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'Hatch AnimalVoice smoke requires the real seventh save fixture. Re-run with explicit -SaveSlot 7.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason`nSaveSlot=$SaveSlot`nSaveSlotExplicit=$($PSBoundParameters.ContainsKey('SaveSlot'))`nAutoExerciseHatchAnimalVoice=$AutoExerciseHatchAnimalVoice" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
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
    exit 1
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
    & "$PSScriptRoot\install-to-game.ps1" @installArguments
    $installExit = if (Get-Variable LASTEXITCODE -ErrorAction SilentlyContinue) { $LASTEXITCODE } else { 0 }
    if ($installExit -ne 0) {
        exit $installExit
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
    $archiveIndex = $SaveSlot - 1
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
try {
if ($moreEquipmentSlotsColdRecoveryTransitionRequested) {
    $coldOfficialModsRoot = [System.IO.Path]::GetFullPath(
        (Join-Path (Get-DolocTownLivePersistentRootForSmoke) 'MODS'))
    $coldProductRoot = [System.IO.Path]::GetFullPath(
        (Join-Path $coldOfficialModsRoot 'DTMAPI_MoreEquipmentSlots'))
    $coldPackageBinding = Test-SmokeMoreEquipmentSlotsOfficialPackage `
        -ProductRoot $coldProductRoot -RepoRoot $repo
    $moreEquipmentSlotsColdOfficialPackagePreflightOk =
        [bool]$coldPackageBinding.Passed
    Write-SmokeJsonObject -Path (
        Join-Path $evidence `
            'more-equipment-slots-cold-official-package.json') `
        -Value ([ordered]@{
            ProductRoot = $coldProductRoot
            SourceId = 'Local.DTMAPI_MoreEquipmentSlots'
            OfficialRelativePath = 'MODS/DTMAPI_MoreEquipmentSlots'
            FileCount = @($coldPackageBinding.Files).Count
            Files = @($coldPackageBinding.Files)
            UniqueId = [string]$coldPackageBinding.UniqueId
            Version = [string]$coldPackageBinding.Version
            MinimumDtmApiVersion =
                [string]$coldPackageBinding.MinimumDtmApiVersion
            CodeModKind = [string]$coldPackageBinding.CodeModKind
            ReferencePolicyId = [string]$coldPackageBinding.ReferencePolicyId
            ReferencePolicySha256 = [string]$coldPackageBinding.ReferencePolicySha256
            GameBuildId = [string]$coldPackageBinding.GameBuildId
            HarmonyOwner = [string]$coldPackageBinding.HarmonyOwner
            ManifestSha256 = [string]$coldPackageBinding.ManifestSha256
            EntryDllLength = [int64]$coldPackageBinding.EntryDllLength
            EntryDllSha256 = [string]$coldPackageBinding.EntryDllSha256
            AdvancedReferenceReceiptSha256 = [string]$coldPackageBinding.AdvancedReferenceReceiptSha256
            ReferenceCount = [int]$coldPackageBinding.ReferenceCount
            Passed = $moreEquipmentSlotsColdOfficialPackagePreflightOk
        })
    if (-not $moreEquipmentSlotsColdOfficialPackagePreflightOk) {
        throw 'MoreEquipmentSlots cold recovery requires an exact current-worktree official Local package before any game launch.'
    }
}
if ($AutoExerciseStrongPlantingGun) {
    $strongPlantingGunConfigPath = Join-Path $dtmapiDir 'config\DTMAPI.StrongPlantingGunMod.json'
    $strongPlantingGunConfigBackup = Join-Path $evidence 'DTMAPI.StrongPlantingGunMod.before.json'
    $strongPlantingGunConfigHadOriginal = Test-Path -LiteralPath $strongPlantingGunConfigPath -PathType Leaf
    if ($strongPlantingGunConfigHadOriginal) {
        Copy-Item -LiteralPath $strongPlantingGunConfigPath -Destination $strongPlantingGunConfigBackup
    }
    $strongPlantingGunConfig = if (Test-Path -LiteralPath $strongPlantingGunConfigPath -PathType Leaf) {
        Get-Content -Raw -Encoding UTF8 -LiteralPath $strongPlantingGunConfigPath | ConvertFrom-Json
    }
    else {
        [pscustomobject]@{}
    }
    foreach ($setting in @(
        [ordered]@{ Name = 'Enabled'; Value = $true },
        [ordered]@{ Name = 'IncludeSeeds'; Value = $true },
        [ordered]@{ Name = 'IncludeFilms'; Value = $true },
        [ordered]@{ Name = 'IncludeFertilizers'; Value = $true },
        [ordered]@{ Name = 'SlotCount'; Value = 3 }
    )) {
        if ($strongPlantingGunConfig.PSObject.Properties.Name -contains [string]$setting.Name) {
            $strongPlantingGunConfig.PSObject.Properties[[string]$setting.Name].Value = $setting.Value
        }
        else {
            $strongPlantingGunConfig | Add-Member -NotePropertyName ([string]$setting.Name) -NotePropertyValue $setting.Value
        }
    }
    Write-SmokeJsonObject -Path $strongPlantingGunConfigPath -Value $strongPlantingGunConfig
    Write-SmokeJsonObject -Path (Join-Path $evidence 'strong-planting-gun-config-stage.json') -Value ([ordered]@{
        ConfigPath = [System.IO.Path]::GetFullPath($strongPlantingGunConfigPath)
        Enabled = $true
        IncludeSeeds = $true
        IncludeFilms = $true
        IncludeFertilizers = $true
        SlotCount = 3
        ExactFileBaselineCapturedBeforeStage = $true
        RestoreOwner = 'strong-planting-gun-exact-config-file'
    })
}
$qaHostStage = if ($StageQaHost) {
    New-SmokeQaHostStage -RepoRoot $repo -GameDir $gameDir -StateDir $dtmapiDir -EvidencePath $evidence -SkipQaBuild ([bool]$SkipBuild) `
        -AutoFishingPerformanceEnabled ([bool]$AutoFishingPerformance) `
        -AutoFishingPerformanceProfile $AutoFishingPerformanceProfile `
        -AutoFishingPerformanceTargetFish $AutoFishingPerformanceTargetFish `
        -AutoFishingPerformanceWarmupFish $AutoFishingPerformanceWarmupFish `
        -AutoFishingPerformanceZeroWarmupSeconds $AutoFishingPerformanceZeroWarmupSeconds `
        -AutoFishingPerformanceZeroMeasureSeconds $AutoFishingPerformanceZeroMeasureSeconds `
        -AutoFishingPerformanceWarmupFrames $AutoFishingPerformanceWarmupFrames `
        -AutoFishingPerformanceTargetFrames $AutoFishingPerformanceTargetFrames `
        -Batch5GcLadderEnabled ([bool]$batch5GcLadderEnabled) `
        -Batch5GcLadderDomain $Batch5GcLadderDomain `
        -Batch5GcLadderLevel $Batch5GcLadderLevel `
        -Batch5GcLadderWorkload $Batch5GcLadderWorkload `
        -Batch5GcLadderMultiplier $Batch5GcLadderMultiplier `
        -Batch5GcLadderMeasureSeconds $Batch5GcLadderMeasureSeconds `
        -Batch5GcLadderSampleSeconds $Batch5GcLadderSampleSeconds `
        -Batch5GcLadderTargetUnits $Batch5GcLadderTargetUnits `
        -Batch6AutoFishingEnabled ([bool]$Batch6AutoFishingPilot) `
        -Batch6AutoFishingLevel $Batch6AutoFishingLevel `
        -Batch6AutoFishingScenario $Batch6AutoFishingScenario `
        -Batch6AutoFishingMeasureSeconds $Batch6AutoFishingMeasureSeconds `
        -Batch6AutoFishingSampleSeconds $Batch6AutoFishingSampleSeconds `
        -Batch6AutoFishingWarmupFish $Batch6AutoFishingWarmupFish `
        -Batch6AutoFishingTargetFish $Batch6AutoFishingTargetFish `
        -Batch6AutoFishingFormal ([bool]$Batch6AutoFishingFormal) `
        -Batch6AutoFishingFormalContract $Batch6AutoFishingFormalContract `
        -Batch6AutoFishingMultiplier $Batch6AutoFishingMultiplier `
        -Batch6AutoFishingCastChargeRatio $Batch6AutoFishingCastChargeRatio `
        -Batch6AutoFishingManualMovementCancel ([bool]$Batch6AutoFishingManualMovementCancel) `
        -Batch6AutoFishingExpectedPackageSha256 $Batch6AutoFishingExpectedPackageSha256 `
        -Batch6AutoFishingExpectedEntrySha256 $Batch6AutoFishingExpectedEntrySha256 `
        -Batch6AutoFishingExpectedManifestSha256 $Batch6AutoFishingExpectedManifestSha256 `
        -Batch6AutoFishingExpectedPolicySha256 $Batch6AutoFishingExpectedPolicySha256 `
        -Batch6AutoFishingManagerEnabled ([bool]$Batch6AutoFishingManagerLifecycle) `
        -Batch6AutoFishingManagerMode $Batch6AutoFishingManagerMode `
        -Batch6AutoFishingManagerProductRoot $Batch6AutoFishingManagerProductRoot `
        -Batch6AutoFishingManagerMarkerSha256 $Batch6AutoFishingManagerMarkerSha256 `
        -Batch6AutoFishingManagerExpectedPackageSha256 $Batch6AutoFishingManagerExpectedPackageSha256 `
        -Batch6AutoFishingManagerExpectedEntrySha256 $Batch6AutoFishingManagerExpectedEntrySha256 `
        -Batch6AutoFishingManagerExpectedManifestSha256 $Batch6AutoFishingManagerExpectedManifestSha256 `
        -Batch6AutoFishingManagerExpectedPolicySha256 $Batch6AutoFishingManagerExpectedPolicySha256 `
        -CustomEntityContractEnabled $qaCustomEntityContractEnabled `
        -FishRoeTooltipObservationEnabled $qaFishRoeTooltipObservationEnabled `
        -DiagnosticsSnapshotEnabled $qaDiagnosticsSnapshotEnabled `
        -DiagnosticsScenario 'G3' `
        -DiagnosticsExpectedFeatureIds $qaDiagnosticsExpectedFeatureIds `
        -ObserveSaveLoaded ([bool]$QaObserveSaveLoaded) `
        -ObserveSaveSaved ([bool]$QaObserveSaveSaved) `
        -ObserveWorkshopReloadCompleted ([bool]$QaObserveWorkshopReloadCompleted) `
        -SaveTestMode $SaveTestMode `
        -DisposableSaveFixtureSaveRoot $(if ($disposableSaveFixtureRequested) { [System.IO.Path]::GetFullPath((Join-Path $disposableSaveFixtureRootResolved 'SAVE')) } else { '' }) `
        -RequireDisposableSaveRedirect $disposableSaveFixtureRequested `
        -TitleSettingsUiEnabled $qaG4TitleSettingsUiEnabled `
        -ManagerStatusUiEnabled $qaG4ManagerStatusUiEnabled `
        -ManagerMvpUiEnabled $qaG4ManagerMvpUiEnabled `
        -OfficialModUiEnabled $qaG4OfficialModUiEnabled `
        -PauseMenuLayoutEnabled $qaG4PauseMenuLayoutEnabled `
        -DebugConsoleEnabled $qaG4DebugConsoleEnabled `
        -SaveSlotsPagingEnabled $qaG4SaveSlotsPagingEnabled `
        -SaveSlot $SaveSlot `
        -AnimalObservationEnabled $qaG4AnimalObservationEnabled `
        -EquipmentSlotsObservationEnabled $qaG4EquipmentSlotsObservationEnabled `
        -AudioObservationEnabled $qaG4AudioObservationEnabled `
        -HatchVoiceEnabled $qaG4HatchVoiceEnabled `
        -CameraPlayableEnabled $qaG4CameraPlayableEnabled `
        -ContentMetadataObservationEnabled $qaG4ContentMetadataObservationEnabled `
        -ContentMetadataExpectOilAbsent ([bool]$ExpectOilAbsent) `
        -ContinuousHomePageTerminalEnabled $qaG4ContinuousHomePageTerminalEnabled `
        -ContinuousHomePageRequiresSaveLoaded $qaG4ContinuousHomePageRequiresSaveLoaded `
        -ContinuousHomePageSeconds $qaG4ContinuousHomePageSeconds `
        -TitleLifecycleEnabled $qaG4TitleLifecycleEnabled `
        -ProductOwnerRefreshEnabled $qaG4ProductOwnerRefreshEnabled `
        -AdvancedProductOwnerDeactivationEnabled $qaG4AdvancedProductOwnerDeactivationEnabled `
        -AdvancedProductOwnerDeactivationOwnerIds @($AdvancedProductOwnerDeactivationOwnerIds) `
        -ExternalPlayerInputObservationEnabled $qaG4ExternalPlayerInputObservationEnabled `
        -ExternalPlayerInputToken $externalPlayerInputHandshakeToken `
        -ExternalPlayerInputMarkerPath $externalPlayerInputCompletionMarkerPath `
        -ExternalPlayerInputEvidenceRoot $(if ($RequireExternalPlayerInputGate) { [System.IO.Path]::GetFullPath($evidence) } else { '' }) `
        -G5WorldMutationCases @($qaG5WorldMutationCases.ToArray()) `
        -DebugConsoleSaveAcceptancePhase $DebugConsoleSaveAcceptancePhase `
        -ExpectedDebugConsoleMoney $ExpectedDebugConsoleMoney `
        -ExpectedMoreEquipmentSlotsBackpackBaseline $ExpectedMoreEquipmentSlotsBackpackBaseline `
        -ExpectedMoreEquipmentSlotsCommittedGeneration $ExpectedMoreEquipmentSlotsCommittedGeneration `
        -ExpectedMoreEquipmentSlotsCommittedOccupied $ExpectedMoreEquipmentSlotsCommittedOccupied `
        -ExpectedMoreEquipmentSlotsCommittedSlots $expectedMoreEquipmentSlotsCommittedSlots `
        -MoreEquipmentSlotsInterruptedCandidateObservationEnabled ([bool]$ObserveMoreEquipmentSlotsInterruptedCandidateRecovery) `
        -ExpectedMoreEquipmentSlotsCandidatePreGeneration $ExpectedMoreEquipmentSlotsCandidatePreGeneration `
        -MoreEquipmentSlotsTransitionPhase $MoreEquipmentSlotsTransitionPhase `
        -G6LifecycleCases @($qaG6LifecycleCases.ToArray()) `
        -G6InitialDelaySeconds 3 `
        -G6AutoFishingScenario $autoFishingScenarioEffective `
        -G6AutoFishingCastChargeRatio $AutoFishingCastChargeRatio `
        -G6AutoFishingSoakLoops $AutoFishingSoakLoops `
        -G6AutoFishingToggleKey $autoFishingToggleKeyEffective `
        -G6AutoFishingMiniGameComplete ([bool]$AutoExerciseAutoFishingMiniGameComplete) `
        -G6AutoFishingMonoGate ([bool]$AutoFishingMonoGate) `
        -G6SaveLoadCycleCount $g6SaveLoadCycleCountEffective `
        -G6SaveLoadCycleInitialTitleIdleSeconds $g6SaveLoadCycleInitialIdleEffective `
        -G6SaveLoadCycleIntervalSeconds $SaveLoadCycleIntervalSeconds `
        -G6SaveLoadCycleInSaveSeconds $SaveLoadCycleInSaveSeconds `
        -G6PreLoadGcProbe ([bool]$AutoExercisePreLoadGcProbe) `
        -G6SaveLoadObjectSnapshotMode $SaveLoadObjectSnapshotMode `
        -G6RootIsolationProfile $SmokeRootIsolationProfile `
        -G6OwnerRootIsolationProfile $SmokeOwnerRootIsolationProfile `
        -G6NativeLoadContinuationProbe $SmokeNativeLoadContinuationProbe `
        -G6PendingPressureSeconds $g6PendingPressureSecondsEffective `
        -G6PendingPressureIntervalSeconds $SaveLoadCyclePendingPressureIntervalSeconds `
        -G6LongTitleIdleSeconds $TitleIdleBeforeSaveSeconds
}
else {
    $null
}
$officialModProfileSummary = Set-SmokeOfficialModProfile -Profile $OfficialModProfile -EvidencePath $evidence -ExtraEnabledIds $OfficialModProfileExtraEnabledIds -IsolateAll ([bool]$IsolateAllOfficialMods)
$officialModProfileRestored = -not [bool]$officialModProfileSummary.Applied
$requestedOfficialModProfileExtraIds = @($OfficialModProfileExtraEnabledIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() } | Sort-Object -Unique)
$officialModProfileAppliedOk = $OfficialModProfile -eq 'Current' -or [bool]$officialModProfileSummary.Applied
$officialModProfileExtraEnabledOk = @($requestedOfficialModProfileExtraIds | Where-Object { @($officialModProfileSummary.EnabledIds) -notcontains $_ }).Count -eq 0
$officialModProfileOilDisabledOk = -not [bool]$ExpectOilAbsent -or @($officialModProfileSummary.DisabledIds) -contains 'Local.DTMAPI_Oil'
$exactElevenProductProfileGateRequested = [bool]$AssertPublishedProductCombination -or [bool]$AssertPublishedProductsDisabled -or
    [bool]$AssertNoQaUiEvidence -or
    ([bool]$QaObserveEquipmentSlotsUi -and
     $MoreEquipmentSlotsTransitionPhase -ne 'U1') -or
    $OfficialModProfile -eq 'Local11'
$publishedProductExpectedEnabledIds = if ($OfficialModProfile -eq 'Published11' -and -not $AssertPublishedProductsDisabled) {
    @(Get-SmokePublishedProductWorkshopIds | Sort-Object)
}
elseif ($OfficialModProfile -eq 'Local11') {
    @(Get-SmokePublishedProductLocalIds | Sort-Object)
}
else {
    @()
}
$publishedProductActualEnabledIds = @($officialModProfileSummary.EnabledIds | Sort-Object)
$publishedProductSelectionOk = if ($exactElevenProductProfileGateRequested) {
    [string]::Equals(
        (@($publishedProductExpectedEnabledIds) -join "`n"),
        (@($publishedProductActualEnabledIds) -join "`n"),
        [System.StringComparison]::Ordinal)
}
else {
    $true
}
$externalPlayerInputWorkshopSelectionOk = -not [bool]$RequireExternalPlayerInputGate -or
    (@($officialModProfileSummary.EnabledIds) -contains 'Workshop.3742714442' -and
     @($officialModProfileSummary.EnabledIds) -notcontains 'Local.DTMAPI_YKeyConsole')
$officialModProfileSelectionOk = $officialModProfileAppliedOk -and $officialModProfileExtraEnabledOk -and $officialModProfileOilDisabledOk -and $publishedProductSelectionOk -and $externalPlayerInputWorkshopSelectionOk
if (-not $officialModProfileSelectionOk) {
    if ([bool]$officialModProfileSummary.Applied) {
        $officialModProfileRestored = Restore-SmokeOfficialModProfile -Summary $officialModProfileSummary -EvidencePath $evidence -Phase 'selection-gate'
    }
    Write-SmokeJsonObject -Path (Join-Path $evidence 'official-mod-profile-gate.json') -Value ([ordered]@{
        Profile = $OfficialModProfile
        IsolateAllOfficialMods = [bool]$IsolateAllOfficialMods
        AppliedOk = $officialModProfileAppliedOk
        ExtraEnabledOk = $officialModProfileExtraEnabledOk
        OilDisabledOk = $officialModProfileOilDisabledOk
        PublishedProductSelectionOk = $publishedProductSelectionOk
        ExternalPlayerInputWorkshopSelectionOk = $externalPlayerInputWorkshopSelectionOk
        RestoredAfterFailure = $officialModProfileRestored
        ExpectedEnabledIds = @($publishedProductExpectedEnabledIds)
        RequestedExtraEnabledIds = @($requestedOfficialModProfileExtraIds)
        ActualEnabledIds = @($officialModProfileSummary.EnabledIds)
        ActualDisabledIds = @($officialModProfileSummary.DisabledIds)
        Reason = [string]$officialModProfileSummary.Reason
    })
    throw "Official Mod smoke profile gate failed before launch. profile=$OfficialModProfile; appliedOk=$officialModProfileAppliedOk; extraEnabledOk=$officialModProfileExtraEnabledOk; oilDisabledOk=$officialModProfileOilDisabledOk; externalWorkshopYConsoleOk=$externalPlayerInputWorkshopSelectionOk; restored=$officialModProfileRestored."
}
$actualEnabledModInfoIds = @(Get-DtmApiEnabledModInfoIds -ModInfoPath ([string]$officialModProfileSummary.EnablementPath))
if ($moreEquipmentSlotsColdRecoveryTransitionRequested) {
    $moreEquipmentSlotsColdOfficialDisabledOk =
        $actualEnabledModInfoIds -notcontains
            'Local.DTMAPI_MoreEquipmentSlots' -and
        @($officialModProfileSummary.DisabledIds) -contains
            'Local.DTMAPI_MoreEquipmentSlots'
    Write-SmokeJsonObject -Path (
        Join-Path $evidence `
            'more-equipment-slots-cold-official-disable.json') `
        -Value ([ordered]@{
            SourceId = 'Local.DTMAPI_MoreEquipmentSlots'
            Profile = $OfficialModProfile
            IsolateAllOfficialMods = [bool]$IsolateAllOfficialMods
            EnabledIds = @($actualEnabledModInfoIds)
            DisabledIds = @($officialModProfileSummary.DisabledIds)
            Passed = $moreEquipmentSlotsColdOfficialDisabledOk
        })
    if (-not $moreEquipmentSlotsColdOfficialDisabledOk) {
        throw 'MoreEquipmentSlots cold recovery requires the official Local source to be disabled by the reversible native profile.'
    }
}
$enabledMoreSavesIds = @($actualEnabledModInfoIds | Where-Object {
    $_ -in @('Workshop.3742763050', 'Local.DTMAPI_MoreSaves')
} | Sort-Object -Unique)
$moreSavesStartupMigrationRoot = if ($disposableSaveFixtureRequested) {
    [System.IO.Path]::GetFullPath((Join-Path $disposableSaveFixtureRootResolved 'SAVE'))
}
else {
    [System.IO.Path]::GetFullPath((Join-Path (Get-DolocTownLivePersistentRootForSmoke) 'SAVE'))
}
$moreSavesLegacyCandidates = @(Get-DtmApiMoreSavesLegacyArchiveCandidates -SaveRoot $moreSavesStartupMigrationRoot)
$moreSavesStartupMigrationDetected = $enabledMoreSavesIds.Count -gt 0 -and $moreSavesLegacyCandidates.Count -gt 0
$moreSavesStartupMigrationClassified = -not $moreSavesStartupMigrationDetected -or
    ($SaveTestMode -eq 'ArchiveMutation' -and
     $disposableSaveFixtureRequested -and
     [bool]$StageQaHost -and
     [bool]$DirectExe -and
     -not [bool]$UseSteam)
Write-SmokeJsonObject -Path (Join-Path $evidence 'moresaves-startup-migration-preflight.json') -Value ([ordered]@{
    Profile = $OfficialModProfile
    EnablementPath = [System.IO.Path]::GetFullPath([string]$officialModProfileSummary.EnablementPath)
    EnabledMoreSavesIds = @($enabledMoreSavesIds)
    EffectiveSaveRoot = $moreSavesStartupMigrationRoot
    ExactLegacyCandidateDomain = 'indices 6..11 x current/prev/bak legacy 1.00 names'
    LegacyCandidateCount = $moreSavesLegacyCandidates.Count
    LegacyCandidates = @($moreSavesLegacyCandidates)
    StartupMigrationDetected = $moreSavesStartupMigrationDetected
    RequiredSaveTestModeWhenDetected = 'ArchiveMutation'
    SaveTestMode = $SaveTestMode
    DisposableFixtureRequested = $disposableSaveFixtureRequested
    StageQaHost = [bool]$StageQaHost
    DirectExe = [bool]$DirectExe
    UseSteam = [bool]$UseSteam
    PreRuntimeSaveGuardRequired = $moreSavesStartupMigrationDetected
    Passed = $moreSavesStartupMigrationClassified
})
if (-not $moreSavesStartupMigrationClassified) {
    throw "Enabled MoreSaves has $($moreSavesLegacyCandidates.Count) exact legacy archive candidate(s) under its effective SAVE root. Startup can rename those files, so this run must use SaveTestMode ArchiveMutation with a disposable fixture, StageQaHost, DirectExe, and the pre-Runtime LocalSave.cloudDirPath guard."
}
# Official Local packages are selected by Local.* enabled state and native
# priority. Functional smoke profiles never publish LocalDevelopment source
# overrides. The recovery-only state clear below is retained solely for the
# bounded Core-only legacy isolation fixture.
$localAuthorSourceRequestedIds = @()
$coreOnlyNoDemandSourceIsolation = $autoFishingNoDemandFrameProfile -and
    $OfficialModProfile -eq 'CoreOnly' -and [bool]$IsolateAllOfficialMods
$localAuthorSourceRequested = $localAuthorSourceRequestedIds.Count -gt 0 -or $coreOnlyNoDemandSourceIsolation
if ($localAuthorSourceRequested) {
    $local11AuthorSourceSummary = Set-SmokeRecoveryOnlyAuthorSourceState -GameDir $gameDir -EvidencePath $evidence
    $local11AuthorSourceAppliedOk = [bool]$local11AuthorSourceSummary.Applied -and @($local11AuthorSourceSummary.Selections).Count -eq $localAuthorSourceRequestedIds.Count
    $local11AuthorSourceRestored = -not [bool]$local11AuthorSourceSummary.Applied
    if (-not $local11AuthorSourceAppliedOk) {
        throw 'Recovery-only Author source isolation did not publish an empty selection set.'
    }
}
if ($DisableSecondMotorForSmoke) {
    Write-Host "SecondMotor smoke package is archived; -DisableSecondMotorForSmoke is now a no-op compatibility flag."
}
$usesOneActionConfigSmoke = [bool]$AutoExerciseOneActionResourceHit -or [bool]$AutoExerciseOneActionWrongTool -or [bool]$AutoExerciseOneActionFuelFeed -or [bool]$AutoExerciseOneActionVegetation -or [bool]$AutoPressOneActionMenuKey
$usesActionSpeedConfigSmoke = [bool]$AutoExerciseActionSpeedTool -or [bool]$AutoExerciseActionSpeedConfigApply -or [bool]$AutoExerciseActionSpeedInteraction -or [bool]$AutoOpenTitleSettingsMenu -or [bool]$batch5GcLadderActionSpeed
$usesAutoFishingConfigSmoke = [bool]$AutoExerciseAutoFishingPhase -or [bool]$AutoPressAutoFishingHotkey -or [bool]$AutoExerciseAutoFishingMovementCancel -or [bool]$AutoOpenTitleSettingsMenu -or [bool]$batch5GcLadderAutoFishing -or [bool]$Batch6AutoFishingPilot -or ([bool]$Batch6AutoFishingManagerLifecycle -and $Batch6AutoFishingManagerMode -eq 'SameProcessDisable')
if ($usesActionSpeedConfigSmoke) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $actionSpeedConfigPath) | Out-Null
    if (Test-Path $actionSpeedConfigPath) {
        Copy-Item -Force -LiteralPath $actionSpeedConfigPath -Destination $actionSpeedConfigBackup
        $actionSpeedConfigHadOriginal = $true
    }
    else {
        'No pre-existing ActionSpeed config file.' | Set-Content -LiteralPath $actionSpeedConfigBackup
    }
    $actionSpeedMultiplier = if ($batch5GcLadderActionSpeed) { $Batch5GcLadderMultiplier } elseif ($AutoExerciseActionSpeedConfigApply) { 2 } else { 3 }
    $actionSpeedProductEnabled = (-not $batch5GcLadderActionSpeed) -or $Batch5GcLadderLevel -ne 'L0'
    $actionSpeedLadderFeatureEnabled = [bool]$batch5GcLadderActionSpeed -and $Batch5GcLadderLevel -ne 'L0'
    $actionSpeedToolEnabled = if ($batch5GcLadderActionSpeed) { $actionSpeedLadderFeatureEnabled -and $Batch5GcLadderWorkload -eq 'Tool' } else { [bool]$AutoExerciseActionSpeedTool -or [bool]$AutoExerciseActionSpeedConfigApply }
    $actionSpeedInteractionEnabled = [bool]$AutoExerciseActionSpeedInteraction -or [bool]$AutoOpenTitleSettingsMenu
    $actionSpeedMachineEnabled = $actionSpeedInteractionEnabled -or ($actionSpeedLadderFeatureEnabled -and $Batch5GcLadderWorkload -eq 'Interact')
    $actionSpeedEatEnabled = $actionSpeedInteractionEnabled -or ($actionSpeedLadderFeatureEnabled -and $Batch5GcLadderWorkload -in @('Eat','ContinuousUse'))
    $actionSpeedContinuousEnabled = $actionSpeedInteractionEnabled -or ($actionSpeedLadderFeatureEnabled -and $Batch5GcLadderWorkload -eq 'ContinuousUse')
    @{
        Enabled = $actionSpeedProductEnabled
        Profile = 'Debug'
        ToolSpeedEnabled = $actionSpeedToolEnabled
        ToolMultiplier = $actionSpeedMultiplier
        BottleFillSpeedEnabled = $actionSpeedContinuousEnabled
        BottleFillMultiplier = $actionSpeedMultiplier
        EatDrinkSpeedEnabled = $actionSpeedEatEnabled
        EatDrinkMultiplier = $actionSpeedMultiplier
        MachineAddSpeedEnabled = $actionSpeedMachineEnabled
        MachineAddMultiplier = $actionSpeedMultiplier
        HarvestSpeedEnabled = $actionSpeedInteractionEnabled
        HarvestMultiplier = 3
        PlantSpeedEnabled = $actionSpeedInteractionEnabled
        PlantMultiplier = 3
        AutoFillBottle = $actionSpeedInteractionEnabled
        AutoFillStrong = $actionSpeedInteractionEnabled
        ContinuousDrinkWithRightClick = $actionSpeedContinuousEnabled
        ContinuousDrinkHoldKey = 'None'
        MenuKey = 'F10'
        AutoActionCooldownSeconds = 0.25
        DebugLabel = 'DTMAPI smoke'
    } | ConvertTo-Json | Set-Content -LiteralPath $actionSpeedConfigPath
}
if ($usesOneActionConfigSmoke) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $oneActionConfigPath) | Out-Null
    if (Test-Path $oneActionConfigPath) {
        Copy-Item -Force -LiteralPath $oneActionConfigPath -Destination $oneActionConfigBackup
        $oneActionConfigHadOriginal = $true
    }
    else {
        'No pre-existing OneAction config file.' | Set-Content -LiteralPath $oneActionConfigBackup
    }
    @{
        Enabled = $true
        CompleteTrees = $true
        CompleteOres = $true
        CompleteGarbage = $true
        CompleteWeeds = $true
        CompleteMachineFuel = [bool]$AutoExerciseOneActionFuelFeed
        CompleteFeeder = [bool]$AutoExerciseOneActionFuelFeed
        VerboseLogging = $true
        MenuKey = 'F11'
    } | ConvertTo-Json | Set-Content -LiteralPath $oneActionConfigPath
}
if ($usesAutoFishingConfigSmoke) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $autoFishingConfigPath) | Out-Null
    if (Test-Path $autoFishingConfigPath) {
        Copy-Item -Force -LiteralPath $autoFishingConfigPath -Destination $autoFishingConfigBackup
        $autoFishingConfigHadOriginal = $true
    }
    else {
        'No pre-existing AutoFishing config file.' | Set-Content -LiteralPath $autoFishingConfigBackup
    }
    $autoFishingSkip = $false
    $autoFishingInstantBite = $false
    $autoFishingFastAnimations = $false
    switch ($autoFishingScenarioEffective) {
        'DefaultLoop' { }
        'InstantBite' { $autoFishingInstantBite = $true }
        'SkipMiniGame' { $autoFishingSkip = $true }
        'InstantSkip' {
            $autoFishingSkip = $true
            $autoFishingInstantBite = $true
        }
        'FastAnimations' {
            $autoFishingFastAnimations = $true
        }
        'CombinedInstantComplete' {
            $autoFishingInstantBite = $true
            $autoFishingFastAnimations = $true
        }
        'CombinedInstantSkip' {
            $autoFishingSkip = $true
            $autoFishingInstantBite = $true
            $autoFishingFastAnimations = $true
        }
        default { }
    }
    [ordered]@{
        AnimationMultiplier = $(if ($Batch6AutoFishingPilot) { $Batch6AutoFishingMultiplier } elseif ($Batch6AutoFishingManagerLifecycle) { 1 } elseif ($batch5GcLadderAutoFishing) { $Batch5GcLadderMultiplier } elseif ($autoFishingPerformanceFishRun -or $AutoFishingMonoGate) { 4 } else { 3 })
        CastChargeRatio = $(if ($Batch6AutoFishingPilot) { $Batch6AutoFishingCastChargeRatio } elseif ($Batch6AutoFishingManagerLifecycle) { 0 } else { $AutoFishingCastChargeRatio })
        FastAnimations = $autoFishingFastAnimations
        InstantBite = $autoFishingInstantBite
        SkipMiniGame = $autoFishingSkip
        ToggleKey = $autoFishingToggleKeyEffective
    } | ConvertTo-Json | Set-Content -LiteralPath $autoFishingConfigPath
}
function Initialize-DtmApiSmokeInput {
    if (-not ('DtmApiSmokeInput' -as [type])) {
        Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Threading;

public struct DtmApiSmokeRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

public struct DtmApiSmokePoint
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct DtmApiSmokeMouseInput
{
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct DtmApiSmokeInputEvent
{
    public uint type;
    public DtmApiSmokeInputUnion data;
}

[StructLayout(LayoutKind.Sequential)]
public struct DtmApiSmokeKeyboardInput
{
    public ushort wVk;
    public ushort wScan;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Explicit)]
public struct DtmApiSmokeInputUnion
{
    [FieldOffset(0)]
    public DtmApiSmokeMouseInput mi;

    [FieldOffset(0)]
    public DtmApiSmokeKeyboardInput ki;
}

public static class DtmApiSmokeInput
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    public static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, UIntPtr lParam);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr hWnd, ref DtmApiSmokeRect lpRect);

    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(IntPtr hWnd, ref DtmApiSmokePoint lpPoint);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, DtmApiSmokeInputEvent[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    public static bool SendMouseButton(uint downFlag, uint upFlag, int holdMilliseconds)
    {
        var down = new DtmApiSmokeInputEvent
        {
            type = 0,
            data = new DtmApiSmokeInputUnion { mi = new DtmApiSmokeMouseInput { dwFlags = downFlag } }
        };
        var up = new DtmApiSmokeInputEvent
        {
            type = 0,
            data = new DtmApiSmokeInputUnion { mi = new DtmApiSmokeMouseInput { dwFlags = upFlag } }
        };
        int size = Marshal.SizeOf(typeof(DtmApiSmokeInputEvent));
        uint downCount = SendInput(1, new[] { down }, size);
        Thread.Sleep(Math.Max(40, holdMilliseconds));
        uint upCount = SendInput(1, new[] { up }, size);
        return downCount == 1 && upCount == 1;
    }

    public static bool SendKeyboardKey(ushort virtualKey, int holdMilliseconds)
    {
        ushort scanCode = (ushort)MapVirtualKey(virtualKey, 0);
        var down = new DtmApiSmokeInputEvent
        {
            type = 1,
            data = new DtmApiSmokeInputUnion { ki = new DtmApiSmokeKeyboardInput { wVk = 0, wScan = scanCode, dwFlags = 0x0008 } }
        };
        var up = new DtmApiSmokeInputEvent
        {
            type = 1,
            data = new DtmApiSmokeInputUnion { ki = new DtmApiSmokeKeyboardInput { wVk = 0, wScan = scanCode, dwFlags = 0x0008 | 0x0002 } }
        };
        int size = Marshal.SizeOf(typeof(DtmApiSmokeInputEvent));
        uint downCount = SendInput(1, new[] { down }, size);
        Thread.Sleep(Math.Max(40, holdMilliseconds));
        uint upCount = SendInput(1, new[] { up }, size);
        return downCount == 1 && upCount == 1;
    }
}
"@
    }
}

function Get-DolocTownInputProcess {
    param(
        [int] $TimeoutSeconds = 20,
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $deadline = Get-SmokeCappedDeadline -Deadline $Deadline -MaximumMilliseconds ([Math]::Max(0, $TimeoutSeconds) * 1000)
    if (-not (Test-SmokeDeadlineHasBudget -Deadline $deadline)) {
        return $null
    }
    Initialize-DtmApiSmokeInput
    do {
        if (-not (Test-SmokeDeadlineHasBudget -Deadline $deadline)) {
            break
        }
        $proc = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
        if ($proc) {
            $targetPid = 0
            $targetThread = [DtmApiSmokeInput]::GetWindowThreadProcessId($proc.MainWindowHandle, [ref]$targetPid)
            $foreground = [DtmApiSmokeInput]::GetForegroundWindow()
            $foregroundPid = 0
            $foregroundThread = if ($foreground -ne [IntPtr]::Zero) { [DtmApiSmokeInput]::GetWindowThreadProcessId($foreground, [ref]$foregroundPid) } else { 0 }
            $currentThread = [DtmApiSmokeInput]::GetCurrentThreadId()

            if ($targetThread -ne 0) {
                [void][DtmApiSmokeInput]::AttachThreadInput($currentThread, $targetThread, $true)
            }
            if ($foregroundThread -ne 0) {
                [void][DtmApiSmokeInput]::AttachThreadInput($currentThread, $foregroundThread, $true)
            }
            try {
                [void][DtmApiSmokeInput]::ShowWindow($proc.MainWindowHandle, 9)
                [void][DtmApiSmokeInput]::BringWindowToTop($proc.MainWindowHandle)
                [DtmApiSmokeInput]::SwitchToThisWindow($proc.MainWindowHandle, $true)
                [void][DtmApiSmokeInput]::SetForegroundWindow($proc.MainWindowHandle)
                [void][DtmApiSmokeInput]::SetActiveWindow($proc.MainWindowHandle)
                [void][DtmApiSmokeInput]::SetFocus($proc.MainWindowHandle)
            }
            finally {
                if ($foregroundThread -ne 0) {
                    [void][DtmApiSmokeInput]::AttachThreadInput($currentThread, $foregroundThread, $false)
                }
                if ($targetThread -ne 0) {
                    [void][DtmApiSmokeInput]::AttachThreadInput($currentThread, $targetThread, $false)
                }
            }

            if (-not (Start-SmokeCappedSleep -Deadline $deadline -Milliseconds 800)) {
                return $null
            }
            $foreground = [DtmApiSmokeInput]::GetForegroundWindow()
            $foregroundPid = 0
            if ($foreground -ne [IntPtr]::Zero) {
                [void][DtmApiSmokeInput]::GetWindowThreadProcessId($foreground, [ref]$foregroundPid)
            }
            if ($foregroundPid -ne $proc.Id) {
                if (-not (Start-SmokeCappedSleep -Deadline $deadline -Milliseconds 250)) {
                    return $null
                }
                continue
            }

            return $proc
        }
        if (-not (Start-SmokeCappedSleep -Deadline $deadline -Milliseconds 250)) {
            break
        }
    } while (Test-SmokeDeadlineHasBudget -Deadline $deadline)

    return $null
}

function Send-DolocTownKey {
    param(
        [Parameter(Mandatory = $true)] [int] $VirtualKey,
        [string] $Name = 'key',
        [int] $TimeoutSeconds = 20,
        [int] $HoldMilliseconds = 260,
        [string] $InputLogPath = '',
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $script:lastDolocTownInputEvidence = [ordered]@{
        DolocTownPid = 0
        ForegroundPidAtSend = 0
        ForegroundMatchedAtSend = $false
        SendInputSucceeded = $false
        PostMessageFallbackUsed = $false
        KeyDownAt = $null
        KeyUpAt = $null
        LogOffsetAtSend = [int64]0
        CompletedBeforeDeadline = $false
    }
    $effectiveDeadline = Get-SmokeCappedDeadline -Deadline $Deadline -MaximumMilliseconds ([Math]::Max(0, $TimeoutSeconds) * 1000)
    if (-not (Test-SmokeDeadlineHasBudget -Deadline $effectiveDeadline)) {
        return $false
    }
    $proc = Get-DolocTownInputProcess -TimeoutSeconds $TimeoutSeconds -Deadline $effectiveDeadline
    if (-not $proc) {
        return $false
    }

    $foregroundWindow = [DtmApiSmokeInput]::GetForegroundWindow()
    $foregroundPid = 0
    if ($foregroundWindow -ne [IntPtr]::Zero) {
        [void][DtmApiSmokeInput]::GetWindowThreadProcessId($foregroundWindow, [ref]$foregroundPid)
    }
    $keyParam = [UIntPtr]::new([uint64]$VirtualKey)
    $logOffsetAtSend = if ($InputLogPath -and (Test-Path -LiteralPath $InputLogPath -PathType Leaf)) { [int64](Get-Item -LiteralPath $InputLogPath).Length } else { [int64]0 }
    $effectiveHold = [Math]::Max(40, $HoldMilliseconds)
    $sendReceipt = Invoke-SmokeDeadlineGuardedAction `
        -Deadline $effectiveDeadline `
        -MinimumExecutionBudgetMilliseconds ($effectiveHold + 50) `
        -Action { return [DtmApiSmokeInput]::SendKeyboardKey([uint16]$VirtualKey, $effectiveHold) }
    $keyDownAt = $sendReceipt.StartedAt.ToUniversalTime().ToString('o')
    $keyUpAt = $sendReceipt.CompletedAt.ToUniversalTime().ToString('o')
    $sentInput = [bool]$sendReceipt.Accepted
    $postMessageFallbackRequested = -not ([bool]$RequireExternalPlayerInputGate -or [bool]$AssertNoQaUiEvidence -or [bool]$qaG4DebugConsoleEnabled -or [bool]$qaG4EquipmentSlotsObservationEnabled -or [bool]$moreEquipmentSlotsTransitionRequested -or [bool]$Batch6AutoFishingPilot -or [bool]$Batch6AutoFishingManagerLifecycle -or [bool]$AutoPressOneActionMenuKey)
    $postMessageFallbackUsed = $false
    if ($sentInput -and $postMessageFallbackRequested) {
        $postReceipt = Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -Action {
            $downPosted = [DtmApiSmokeInput]::PostMessage($proc.MainWindowHandle, 0x0100, $keyParam, [UIntPtr]::Zero)
            $upPosted = [DtmApiSmokeInput]::PostMessage($proc.MainWindowHandle, 0x0101, $keyParam, [UIntPtr]::Zero)
            return $downPosted -and $upPosted
        }
        $postMessageFallbackUsed = [bool]$postReceipt.Accepted
    }
    $script:lastDolocTownInputEvidence = [ordered]@{
        DolocTownPid = [int]$proc.Id
        ForegroundPidAtSend = [int]$foregroundPid
        ForegroundMatchedAtSend = [bool]($foregroundPid -eq $proc.Id)
        SendInputSucceeded = [bool]$sentInput
        PostMessageFallbackUsed = [bool]$postMessageFallbackUsed
        KeyDownAt = $keyDownAt
        KeyUpAt = $keyUpAt
        LogOffsetAtSend = [int64]$logOffsetAtSend
        CompletedBeforeDeadline = [bool]$sendReceipt.CompletedBeforeDeadline
    }
    return $sentInput
}

function Resolve-DolocTownVirtualKey {
    param(
        [string] $Key
    )

    if ([string]::IsNullOrWhiteSpace($Key) -or $Key.Equals('None', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $null
    }

    $normalized = $Key.Trim()
    if ($normalized -match '^F([1-9]|1[0-9]|2[0-4])$') {
        return 0x70 + [int]$Matches[1] - 1
    }
    if ($normalized -match '^[A-Z]$') {
        return [int][char]$normalized
    }
    if ($normalized -match '^Alpha([0-9])$') {
        return [int][char]$Matches[1]
    }

    switch ($normalized) {
        'Escape' { return 0x1B }
        'Backspace' { return 0x08 }
        'Delete' { return 0x2E }
        'Space' { return 0x20 }
        'Tab' { return 0x09 }
        'Return' { return 0x0D }
        'KeypadEnter' { return 0x0D }
        'LeftShift' { return 0xA0 }
        'RightShift' { return 0xA1 }
        'LeftControl' { return 0xA2 }
        'RightControl' { return 0xA3 }
        'LeftAlt' { return 0xA4 }
        'RightAlt' { return 0xA5 }
        'Insert' { return 0x2D }
        'Home' { return 0x24 }
        'End' { return 0x23 }
        'PageUp' { return 0x21 }
        'PageDown' { return 0x22 }
        'Plus' { return 0xBB }
        'Equals' { return 0xBB }
        'Minus' { return 0xBD }
        'KeypadPlus' { return 0x6B }
        'KeypadMinus' { return 0x6D }
        'UpArrow' { return 0x26 }
        'DownArrow' { return 0x28 }
        'LeftArrow' { return 0x25 }
        'RightArrow' { return 0x27 }
        default { return $null }
    }
}

function Send-DolocTownNamedKey {
    param(
        [string] $Key,
        [int] $TimeoutSeconds = 20,
        [int] $HoldMilliseconds = 260,
        [string] $InputLogPath = '',
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $virtualKey = Resolve-DolocTownVirtualKey -Key $Key
    if ($null -eq $virtualKey) {
        return $false
    }

    return Send-DolocTownKey -VirtualKey $virtualKey -Name $Key -TimeoutSeconds $TimeoutSeconds -HoldMilliseconds $HoldMilliseconds -InputLogPath $InputLogPath -Deadline $Deadline
}

function Wait-Batch6AutoFishingHandshakeState {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [int64] $Offset,
        [Parameter(Mandatory = $true)] [string] $State,
        [Parameter(Mandatory = $true)] [int] $TimeoutSeconds,
        [Parameter(Mandatory = $true)] [System.Collections.IList] $Receipts,
        [string] $HookId = 'Smoke.Batch6.AutoFishingPilot.Handshake'
    )

    # Wait-ForLogLineAfterOffset owns literal escaping. Keep this value literal
    # and include the separator after the exact state to prevent prefix matches.
    $pattern = $HookId + ' state=' + $State + ' '
    $startedAt = (Get-Date).ToUniversalTime()
    $passed = Wait-ForLogLineAfterOffset -LogPath $LogPath -Offset $Offset -Pattern $pattern -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    $receipt = [ordered]@{
        Sequence = $Receipts.Count + 1
        State = $State
        LogOffset = [int64]$Offset
        Pattern = $pattern
        StartedAtUtc = $startedAt.ToString('o')
        CompletedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        Observed = [bool]$passed
    }
    [void]$Receipts.Add($receipt)
    return [pscustomobject]$receipt
}

function Send-Batch6AutoFishingRecordedKey {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [int64] $Offset,
        [Parameter(Mandatory = $true)] [string] $State,
        [Parameter(Mandatory = $true)] [int] $TimeoutSeconds,
        [Parameter(Mandatory = $true)] [System.Collections.IList] $InputReceipts,
        [Parameter(Mandatory = $true)] [bool] $HandshakeObserved,
        [ValidateSet('F6','F7','A')] [string] $Key = 'F6',
        [ValidateSet('ConfiguredToggle','ManualMovement')] [string] $Purpose = 'ConfiguredToggle',
        [ValidateRange(1, 1000)] [int] $HoldMilliseconds = 260
    )

    $sent = $false
    $inputEvidence = [ordered]@{
        DolocTownPid = 0
        ForegroundPidAtSend = 0
        ForegroundMatchedAtSend = $false
        SendInputSucceeded = $false
        PostMessageFallbackUsed = $false
        KeyDownAt = $null
        KeyUpAt = $null
        LogOffsetAtSend = [int64]0
        CompletedBeforeDeadline = $false
    }
    if ($HandshakeObserved) {
        $sent = Send-DolocTownNamedKey -Key $Key -TimeoutSeconds $TimeoutSeconds -HoldMilliseconds $HoldMilliseconds -InputLogPath $LogPath
        $inputEvidence = $script:lastDolocTownInputEvidence
    }
    $provenancePassed = $HandshakeObserved -and [bool]$sent -and
        [bool]$inputEvidence.ForegroundMatchedAtSend -and [bool]$inputEvidence.SendInputSucceeded -and
        -not [bool]$inputEvidence.PostMessageFallbackUsed -and [bool]$inputEvidence.CompletedBeforeDeadline
    $receipt = [ordered]@{
        Sequence = $InputReceipts.Count + 1
        Purpose = $Purpose
        HandshakeState = $State
        HandshakeLogOffset = [int64]$Offset
        HandshakeObserved = $HandshakeObserved
        Key = $Key
        HoldMilliseconds = $HoldMilliseconds
        Sent = [bool]$sent
        DolocTownPid = [int]$inputEvidence.DolocTownPid
        ForegroundPidAtSend = [int]$inputEvidence.ForegroundPidAtSend
        ForegroundMatchedAtSend = [bool]$inputEvidence.ForegroundMatchedAtSend
        SendInputSucceeded = [bool]$inputEvidence.SendInputSucceeded
        PostMessageFallbackUsed = [bool]$inputEvidence.PostMessageFallbackUsed
        KeyDownAt = $inputEvidence.KeyDownAt
        KeyUpAt = $inputEvidence.KeyUpAt
        InputLogOffset = [int64]$inputEvidence.LogOffsetAtSend
        CompletedBeforeDeadline = [bool]$inputEvidence.CompletedBeforeDeadline
        ProvenancePassed = [bool]$provenancePassed
    }
    [void]$InputReceipts.Add($receipt)
    return [pscustomobject]$receipt
}

function Send-Batch6AutoFishingF6AfterHandshake {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [int64] $Offset,
        [Parameter(Mandatory = $true)] [string] $State,
        [Parameter(Mandatory = $true)] [int] $TimeoutSeconds,
        [Parameter(Mandatory = $true)] [System.Collections.IList] $HandshakeReceipts,
        [Parameter(Mandatory = $true)] [System.Collections.IList] $InputReceipts,
        [ValidateSet('F6','F7','A')] [string] $Key = 'F6',
        [string] $HookId = 'Smoke.Batch6.AutoFishingPilot.Handshake'
    )

    $handshake = Wait-Batch6AutoFishingHandshakeState -LogPath $LogPath -Offset $Offset -State $State -TimeoutSeconds $TimeoutSeconds -Receipts $HandshakeReceipts -HookId $HookId
    $purpose = if ([string]::Equals($Key, 'A', [System.StringComparison]::Ordinal)) { 'ManualMovement' } else { 'ConfiguredToggle' }
    return Send-Batch6AutoFishingRecordedKey -LogPath $LogPath -Offset $Offset -State $State -TimeoutSeconds $TimeoutSeconds `
        -InputReceipts $InputReceipts -HandshakeObserved ([bool]$handshake.Observed) -Key $Key -Purpose $purpose
}

function Send-DolocTownMouseClick {
    param(
        [ValidateSet('Left', 'Right')] [string] $Button = 'Left',
        [Parameter(Mandatory = $true)] [int] $ClientX,
        [Parameter(Mandatory = $true)] [int] $ClientY,
        [int] $TimeoutSeconds = 20,
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $effectiveDeadline = Get-SmokeCappedDeadline -Deadline $Deadline -MaximumMilliseconds ([Math]::Max(0, $TimeoutSeconds) * 1000)
    if (-not (Test-SmokeDeadlineHasBudget -Deadline $effectiveDeadline)) {
        return $false
    }
    $proc = Get-DolocTownInputProcess -TimeoutSeconds $TimeoutSeconds -Deadline $effectiveDeadline
    if (-not $proc) {
        return $false
    }

    $rect = New-Object DtmApiSmokeRect
    if (-not [DtmApiSmokeInput]::GetClientRect($proc.MainWindowHandle, [ref]$rect)) {
        return $false
    }

    $clientWidth = $rect.Right - $rect.Left
    $clientHeight = $rect.Bottom - $rect.Top
    if ($ClientX -lt 0 -or $ClientY -lt 0 -or $ClientX -ge $clientWidth -or $ClientY -ge $clientHeight) {
        return $false
    }

    $point = New-Object DtmApiSmokePoint
    $point.X = $ClientX
    $point.Y = $ClientY
    if (-not [DtmApiSmokeInput]::ClientToScreen($proc.MainWindowHandle, [ref]$point)) {
        return $false
    }

    $cursorReceipt = Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -Action {
        return [DtmApiSmokeInput]::SetCursorPos($point.X, $point.Y)
    }
    if (-not [bool]$cursorReceipt.Accepted -or -not (Start-SmokeCappedSleep -Deadline $effectiveDeadline -Milliseconds 160)) {
        return $false
    }
    $mouseHoldMilliseconds = if ($Button -eq 'Right') { 320 } else { 180 }
    $mouseReceipt = if ($Button -eq 'Right') {
        Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -MinimumExecutionBudgetMilliseconds ($mouseHoldMilliseconds + 50) -Action {
            return [DtmApiSmokeInput]::SendMouseButton([uint32]0x0008, [uint32]0x0010, $mouseHoldMilliseconds)
        }
    }
    else {
        Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -MinimumExecutionBudgetMilliseconds ($mouseHoldMilliseconds + 50) -Action {
            return [DtmApiSmokeInput]::SendMouseButton([uint32]0x0002, [uint32]0x0004, $mouseHoldMilliseconds)
        }
    }
    return [bool]$mouseReceipt.Accepted
}

function Send-DolocTownNormalizedMouseClick {
    param(
        [ValidateSet('Left', 'Right')] [string] $Button = 'Left',
        [Parameter(Mandatory = $true)] [ValidateRange(0.0, 1.0)] [double] $NormalizedX,
        [Parameter(Mandatory = $true)] [ValidateRange(0.0, 1.0)] [double] $NormalizedY,
        [int] $TimeoutSeconds = 20,
        [string] $InputLogPath = '',
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $script:lastDolocTownMouseInputEvidence = [ordered]@{
        DolocTownPid = 0
        ForegroundPidAtSend = 0
        ForegroundMatchedAtSend = $false
        SendInputSucceeded = $false
        PostMessageFallbackUsed = $false
        SetCursorPosSucceeded = $false
        NormalizedX = $NormalizedX
        NormalizedY = $NormalizedY
        ClientX = -1
        ClientY = -1
        ClientWidth = 0
        ClientHeight = 0
        SentAtUtc = $null
        LogOffsetAtSend = [int64]0
        CompletedBeforeDeadline = $false
    }
    $effectiveDeadline = Get-SmokeCappedDeadline -Deadline $Deadline -MaximumMilliseconds ([Math]::Max(0, $TimeoutSeconds) * 1000)
    if (-not (Test-SmokeDeadlineHasBudget -Deadline $effectiveDeadline)) {
        return $false
    }
    $proc = Get-DolocTownInputProcess -TimeoutSeconds $TimeoutSeconds -Deadline $effectiveDeadline
    if (-not $proc) {
        return $false
    }

    $foregroundWindow = [DtmApiSmokeInput]::GetForegroundWindow()
    $foregroundPid = 0
    if ($foregroundWindow -ne [IntPtr]::Zero) {
        [void][DtmApiSmokeInput]::GetWindowThreadProcessId($foregroundWindow, [ref]$foregroundPid)
    }
    $rect = New-Object DtmApiSmokeRect
    if (-not [DtmApiSmokeInput]::GetClientRect($proc.MainWindowHandle, [ref]$rect)) {
        return $false
    }
    $clientWidth = $rect.Right - $rect.Left
    $clientHeight = $rect.Bottom - $rect.Top
    if ($clientWidth -le 0 -or $clientHeight -le 0) {
        return $false
    }
    $clientX = [Math]::Max(0, [Math]::Min($clientWidth - 1, [int][Math]::Round(($clientWidth - 1) * $NormalizedX)))
    $clientY = [Math]::Max(0, [Math]::Min($clientHeight - 1, [int][Math]::Round(($clientHeight - 1) * $NormalizedY)))
    $point = New-Object DtmApiSmokePoint
    $point.X = $clientX
    $point.Y = $clientY
    if (-not [DtmApiSmokeInput]::ClientToScreen($proc.MainWindowHandle, [ref]$point)) {
        return $false
    }

    $logOffsetAtSend = if ($InputLogPath -and (Test-Path -LiteralPath $InputLogPath -PathType Leaf)) { [int64](Get-Item -LiteralPath $InputLogPath).Length } else { [int64]0 }
    $cursorReceipt = Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -Action {
        return [DtmApiSmokeInput]::SetCursorPos($point.X, $point.Y)
    }
    $cursorSet = [bool]$cursorReceipt.Accepted
    $mouseHoldMilliseconds = if ($Button -eq 'Right') { 320 } else { 180 }
    $mouseReceipt = $null
    if ($cursorSet -and (Start-SmokeCappedSleep -Deadline $effectiveDeadline -Milliseconds 160)) {
        $mouseReceipt = if ($Button -eq 'Right') {
            Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -MinimumExecutionBudgetMilliseconds ($mouseHoldMilliseconds + 50) -Action {
                return [DtmApiSmokeInput]::SendMouseButton([uint32]0x0008, [uint32]0x0010, $mouseHoldMilliseconds)
            }
        }
        else {
            Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -MinimumExecutionBudgetMilliseconds ($mouseHoldMilliseconds + 50) -Action {
                return [DtmApiSmokeInput]::SendMouseButton([uint32]0x0002, [uint32]0x0004, $mouseHoldMilliseconds)
            }
        }
    }
    $sentAtUtc = if ($null -ne $mouseReceipt) { $mouseReceipt.StartedAt.ToUniversalTime().ToString('o') } else { $null }
    $sentInput = $null -ne $mouseReceipt -and [bool]$mouseReceipt.Accepted
    $script:lastDolocTownMouseInputEvidence = [ordered]@{
        DolocTownPid = [int]$proc.Id
        ForegroundPidAtSend = [int]$foregroundPid
        ForegroundMatchedAtSend = [bool]($foregroundPid -eq $proc.Id)
        SendInputSucceeded = [bool]$sentInput
        PostMessageFallbackUsed = $false
        SetCursorPosSucceeded = [bool]$cursorSet
        NormalizedX = $NormalizedX
        NormalizedY = $NormalizedY
        ClientX = $clientX
        ClientY = $clientY
        ClientWidth = $clientWidth
        ClientHeight = $clientHeight
        SentAtUtc = $sentAtUtc
        LogOffsetAtSend = [int64]$logOffsetAtSend
        CompletedBeforeDeadline = $null -ne $mouseReceipt -and [bool]$mouseReceipt.CompletedBeforeDeadline
    }
    return [bool]$sentInput
}

function Send-DolocTownF6 {
    param(
        [int] $TimeoutSeconds = 20,
        [datetime] $Deadline = [datetime]::MaxValue
    )

    return Send-DolocTownNamedKey -Key 'F6' -TimeoutSeconds $TimeoutSeconds -Deadline $Deadline
}

function Wait-ForStartupLogWithTimeline {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $TimeoutSeconds,
        [string] $EvidenceDir,
        [datetime] $LaunchCommandStartedAt,
        [datetime] $LaunchCommandFinishedAt,
        [string] $LaunchMode,
        [datetime] $AbsoluteDeadline = [datetime]::MaxValue
    )

    function Format-DateOrNull {
        param($Value)
        if ($null -eq $Value) {
            return $null
        }
        return ([datetime]$Value).ToString('o')
    }

    function Get-ElapsedMsOrNull {
        param($From, $To)
        if ($null -eq $From -or $null -eq $To) {
            return $null
        }
        return [int64]([datetime]$To - [datetime]$From).TotalMilliseconds
    }

    $waitStartedAt = Get-Date
    $deadline = $waitStartedAt.AddSeconds($TimeoutSeconds)
    if ($AbsoluteDeadline -lt $deadline) {
        $deadline = $AbsoluteDeadline
    }
    $firstProcessAt = $null
    $firstProcessId = $null
    $firstLogFileAt = $null
    $firstPatternAt = $null
    $fatalWindowAt = $null

    while ((Get-Date) -lt $deadline) {
        if ($null -eq $firstProcessAt) {
            $proc = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($proc) {
                $firstProcessAt = Get-Date
                $firstProcessId = $proc.Id
            }
        }

        if (Test-Path $LogPath) {
            if ($null -eq $firstLogFileAt) {
                $firstLogFileAt = Get-Date
            }

            $text = Get-Content -Raw -LiteralPath $LogPath -ErrorAction SilentlyContinue
            if ((Test-SmokeDeadlineHasBudget -Deadline $deadline) -and $text -match [regex]::Escape($Pattern)) {
                $firstPatternAt = Get-Date
                break
            }
        }

        if (Test-FatalInstanceWindow) {
            $fatalWindowAt = Get-Date
            break
        }

        Start-Sleep -Seconds 1
    }

    $finishedAt = Get-Date
    $found = $null -ne $firstPatternAt
    $timeline = [ordered]@{
        LaunchMode = $LaunchMode
        LaunchCommandStartedAt = Format-DateOrNull $LaunchCommandStartedAt
        LaunchCommandFinishedAt = Format-DateOrNull $LaunchCommandFinishedAt
        WaitStartedAt = Format-DateOrNull $waitStartedAt
        WaitFinishedAt = Format-DateOrNull $finishedAt
        WaitTimeoutSeconds = $TimeoutSeconds
        StartupPattern = $Pattern
        StartupLogFound = $found
        TimedOut = (-not $found) -and ($null -eq $fatalWindowAt)
        FatalInstanceWindowAt = Format-DateOrNull $fatalWindowAt
        FirstDolocTownProcessAt = Format-DateOrNull $firstProcessAt
        FirstDolocTownProcessId = $firstProcessId
        LaunchToProcessMs = Get-ElapsedMsOrNull $LaunchCommandStartedAt $firstProcessAt
        FirstDtmapiLogFileAt = Format-DateOrNull $firstLogFileAt
        LaunchToDtmapiLogFileMs = Get-ElapsedMsOrNull $LaunchCommandStartedAt $firstLogFileAt
        FirstStartupPatternAt = Format-DateOrNull $firstPatternAt
        LaunchToStartupPatternMs = Get-ElapsedMsOrNull $LaunchCommandStartedAt $firstPatternAt
        WaitDurationMs = Get-ElapsedMsOrNull $waitStartedAt $finishedAt
    }
    $timeline | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $EvidenceDir 'startup-timeline.json')

    return $found
}

function Wait-ForLogLineCount {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $MinimumCount,
        [int] $TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $LogPath) {
            $count = @(Select-String -LiteralPath $LogPath -SimpleMatch -Pattern $Pattern -ErrorAction SilentlyContinue).Count
            if ($count -ge $MinimumCount) {
                return $true
            }
        }

        if (Test-FatalInstanceWindow) {
            return $false
        }

        Start-Sleep -Milliseconds 500
    }

    return $false
}

function Invoke-DolocTownHoverSweep {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [int64] $LogOffset,
        [Parameter(Mandatory = $true)] [string] $Pattern,
        [Parameter(Mandatory = $true)] [datetime] $Deadline
    )

    $receipt = [ordered]@{
        DolocTownPid = 0
        ForegroundMatched = $false
        MoveOnly = $true
        MouseClickSent = $false
        PointsVisited = 0
        MatchedExpectedLog = $false
    }
    $remaining = Get-SmokeRemainingBudgetSeconds -Deadline $Deadline -MaximumSeconds 20
    if ($remaining -le 0) {
        return [PSCustomObject]$receipt
    }
    $proc = Get-DolocTownInputProcess -TimeoutSeconds $remaining -Deadline $Deadline
    if (-not $proc) {
        return [PSCustomObject]$receipt
    }

    $receipt.DolocTownPid = [int]$proc.Id
    $foregroundWindow = [DtmApiSmokeInput]::GetForegroundWindow()
    $foregroundPid = 0
    if ($foregroundWindow -ne [IntPtr]::Zero) {
        [void][DtmApiSmokeInput]::GetWindowThreadProcessId($foregroundWindow, [ref]$foregroundPid)
    }
    $receipt.ForegroundMatched = [bool]($foregroundPid -eq $proc.Id)
    if (-not $receipt.ForegroundMatched) {
        return [PSCustomObject]$receipt
    }

    $rect = New-Object DtmApiSmokeRect
    if (-not [DtmApiSmokeInput]::GetClientRect($proc.MainWindowHandle, [ref]$rect)) {
        return [PSCustomObject]$receipt
    }
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $minX = [int][Math]::Max(12, [Math]::Floor($width * 0.18))
    $maxX = [int][Math]::Min($width - 12, [Math]::Ceiling($width * 0.82))
    $minY = [int][Math]::Max(12, [Math]::Floor($height * 0.18))
    $maxY = [int][Math]::Min($height - 12, [Math]::Ceiling($height * 0.88))

    for ($y = $minY; $y -le $maxY -and (Test-SmokeDeadlineHasBudget -Deadline $Deadline); $y += 44) {
        for ($x = $minX; $x -le $maxX -and (Test-SmokeDeadlineHasBudget -Deadline $Deadline); $x += 44) {
            $point = New-Object DtmApiSmokePoint
            $point.X = $x
            $point.Y = $y
            if ([DtmApiSmokeInput]::ClientToScreen($proc.MainWindowHandle, [ref]$point)) {
                $cursorReceipt = Invoke-SmokeDeadlineGuardedAction -Deadline $Deadline -Action {
                    return [DtmApiSmokeInput]::SetCursorPos($point.X, $point.Y)
                }
                if (-not [bool]$cursorReceipt.Accepted) {
                    return [PSCustomObject]$receipt
                }
                $receipt.PointsVisited++
                if (-not (Start-SmokeCappedSleep -Deadline $Deadline -Milliseconds 55)) {
                    return [PSCustomObject]$receipt
                }
                $tail = Get-LogTextAfterOffset -LogPath $LogPath -Offset $LogOffset
                if ((Test-SmokeDeadlineHasBudget -Deadline $Deadline) -and $tail.IndexOf($Pattern, [System.StringComparison]::Ordinal) -ge 0) {
                    $receipt.MatchedExpectedLog = $true
                    return [PSCustomObject]$receipt
                }
            }
        }
    }
    return [PSCustomObject]$receipt
}

function Get-LogTextAfterOffset {
    param(
        [string] $LogPath,
        [int64] $Offset
    )

    if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf)) {
        return ''
    }

    $stream = $null
    $reader = $null
    try {
        $share = [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete
        $stream = [System.IO.File]::Open($LogPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, $share)
        $safeOffset = [Math]::Min([Math]::Max([int64]0, $Offset), $stream.Length)
        [void]$stream.Seek($safeOffset, [System.IO.SeekOrigin]::Begin)
        $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::UTF8, $true, 4096, $true)
        return $reader.ReadToEnd()
    }
    finally {
        if ($reader) { $reader.Dispose() }
        if ($stream) { $stream.Dispose() }
    }
}

function Wait-ForLogLineAfterOffsetUntilDeadline {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [string] $Pattern,
        [Parameter(Mandatory = $true)] [int64] $Offset,
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [switch] $AbortOnFatalInstanceWindow
    )

    while (Test-SmokeDeadlineHasBudget -Deadline $Deadline) {
        $text = Get-LogTextAfterOffset -LogPath $LogPath -Offset $Offset
        $observedAt = Get-Date
        if (-not (Test-SmokeDeadlineHasBudget -Deadline $Deadline -ObservedAt $observedAt)) {
            return $false
        }
        if ($text.IndexOf($Pattern, [System.StringComparison]::Ordinal) -ge 0) {
            return $true
        }
        if ($text -match 'QA host lifecycle .*state=failed' -or
            $text -match 'QA scenario .* FAILED' -or
            $text -match 'InternalFixture\.QaHost = failed-closed') {
            return $false
        }
        if ($AbortOnFatalInstanceWindow -and (Test-FatalInstanceWindow)) {
            return $false
        }
        if (-not (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Select-Object -First 1)) {
            return $false
        }
        if (-not (Start-SmokeCappedSleep -Deadline $Deadline -Milliseconds 250)) {
            return $false
        }
    }

    return $false
}

function Wait-ForAutoFishingLogLine {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $LogPath) {
            $text = Get-Content -Raw -LiteralPath $LogPath -ErrorAction SilentlyContinue
            if ($text -match [regex]::Escape($Pattern)) {
                return $true
            }
            if ($text -match 'Hook status: Smoke\.AutoFishing[^=]* = failed\.') {
                return $false
            }
        }

        if (Test-FatalInstanceWindow) {
            return $false
        }

        if (-not (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)) {
            return $false
        }

        Start-Sleep -Seconds 1
    }

    return $false
}

function Wait-ForAutoFishingPerformanceTerminalStatus {
    param(
        [string] $LogPath,
        [int] $TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while (Test-SmokeDeadlineHasBudget -Deadline $deadline) {
        if (Test-Path $LogPath) {
            $text = Get-Content -Raw -LiteralPath $LogPath -ErrorAction SilentlyContinue
            $match = [regex]::Match($text, 'Hook status: Smoke\.AutoFishingPerformance = (verified|blocked)\.')
            if ($match.Success) {
                return $match.Groups[1].Value
            }
            if ($text -match 'Hook status: Smoke\.AutoFishingPerformance = failed\.') {
                return 'failed'
            }
        }

        if (Test-FatalInstanceWindow) {
            return 'failed'
        }
        if (-not (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)) {
            return 'failed'
        }
        if (-not (Start-SmokeCappedSleep -Deadline $deadline -Milliseconds 1000)) {
            break
        }
    }

    return 'failed'
}

function Get-LogRegexCount {
    param(
        [string] $LogPath,
        [string] $Pattern
    )

    if (-not (Test-Path $LogPath)) {
        return 0
    }

    return @(Select-String -LiteralPath $LogPath -Pattern $Pattern -ErrorAction SilentlyContinue).Count
}

function Wait-ForLogRegexCount {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $MinimumCount,
        [int] $TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ((Get-LogRegexCount -LogPath $LogPath -Pattern $Pattern) -ge $MinimumCount) {
            return $true
        }

        if (Test-FatalInstanceWindow) {
            return $false
        }

        Start-Sleep -Milliseconds 500
    }

    return $false
}

function Get-DebugConsoleFirstItemCellPoint {
    $proc = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if (-not $proc) {
        return $null
    }

    Initialize-DtmApiSmokeInput
    $rect = New-Object DtmApiSmokeRect
    if (-not [DtmApiSmokeInput]::GetClientRect($proc.MainWindowHandle, [ref]$rect)) {
        return $null
    }

    $clientWidth = $rect.Right - $rect.Left
    $clientHeight = $rect.Bottom - $rect.Top
    $panelWidth = 1500
    $panelHeight = 900
    $itemCenterX = 388 + 50
    $itemCenterY = 150 + 48
    $clientX = [int][Math]::Round((($clientWidth - $panelWidth) / 2.0) + $itemCenterX)
    $clientY = [int][Math]::Round((($clientHeight - $panelHeight) / 2.0) + $itemCenterY)

    return [PSCustomObject]@{
        ClientX = $clientX
        ClientY = $clientY
        ClientWidth = $clientWidth
        ClientHeight = $clientHeight
        PanelWidth = $panelWidth
        PanelHeight = $panelHeight
    }
}

$externalPlayerInputAttempts = New-Object 'System.Collections.Generic.List[object]'
$equipmentSlotsPlayerInputAttempts = New-Object 'System.Collections.Generic.List[object]'
$animalViewerPlayerInputAttempts = New-Object 'System.Collections.Generic.List[object]'

function Invoke-DebugConsoleSmokeKey {
    param(
        [string] $Label,
        [int] $VirtualKey,
        [string] $Pattern,
        [int] $MinimumCount,
        [int] $Attempts = 1,
        [int] $WaitSeconds = 5,
        [int] $HoldMilliseconds = 0,
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $strictPlayerInputGate = [bool]$RequireExternalPlayerInputGate -or [bool]$AssertNoQaUiEvidence -or [bool]$qaG4DebugConsoleEnabled
    $effectiveHoldMilliseconds = if ($HoldMilliseconds -gt 0) { $HoldMilliseconds } elseif ($strictPlayerInputGate) { $ExternalPlayerInputHoldMilliseconds } else { 260 }
    if ($strictPlayerInputGate -and $Attempts -ne 1) {
        throw "The strict external player-input gate permits exactly one send attempt per label; requested label=$Label attempts=$Attempts."
    }

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        if (-not (Test-SmokeDeadlineHasBudget -Deadline $Deadline)) {
            break
        }
        $attemptStartedAt = (Get-Date).ToUniversalTime().ToString('o')
        $sent = Send-DolocTownKey -VirtualKey $VirtualKey -Name $Label -HoldMilliseconds $effectiveHoldMilliseconds -InputLogPath $logPath -Deadline $Deadline
        $sendEvidence = $script:lastDolocTownInputEvidence
        "SentExternalDebugConsole${Label}Attempt$attempt=$(Get-Date -Format o);ok=$sent;holdMilliseconds=$effectiveHoldMilliseconds" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        $matchedExpectedLog = if (-not $sent) {
            $false
        }
        elseif ($strictPlayerInputGate) {
            $waitDeadline = Get-SmokeCappedDeadline -Deadline $Deadline -MaximumMilliseconds ([Math]::Max(0, $WaitSeconds) * 1000)
            Wait-ForLogLineAfterOffsetUntilDeadline -LogPath $logPath -Offset ([int64]$sendEvidence.LogOffsetAtSend) -Pattern $Pattern -Deadline $waitDeadline
        }
        else {
            Wait-ForLogLineCount -LogPath $logPath -Pattern $Pattern -MinimumCount $MinimumCount -TimeoutSeconds $WaitSeconds
        }
        if ($strictPlayerInputGate) {
            $externalPlayerInputAttempts.Add([ordered]@{
                Label = $Label
                Attempt = $attempt
                Timestamp = [string]$sendEvidence.KeyDownAt
                AttemptStartedAt = $attemptStartedAt
                KeyDownAt = [string]$sendEvidence.KeyDownAt
                KeyUpAt = [string]$sendEvidence.KeyUpAt
                VirtualKey = $VirtualKey
                HoldMilliseconds = $effectiveHoldMilliseconds
                LogOffset = [int64]$sendEvidence.LogOffsetAtSend
                Sent = [bool]$sent
                MatchedExpectedLog = [bool]$matchedExpectedLog
                DolocTownPid = [int]$sendEvidence.DolocTownPid
                ForegroundPid = [int]$sendEvidence.ForegroundPidAtSend
                ForegroundMatched = [bool]$sendEvidence.ForegroundMatchedAtSend
                SendInputSucceeded = [bool]$sendEvidence.SendInputSucceeded
                CompletedBeforeDeadline = [bool]$sendEvidence.CompletedBeforeDeadline
                PostMessageFallbackUsed = [bool]$sendEvidence.PostMessageFallbackUsed
            }) | Out-Null
        }
        if ($matchedExpectedLog) {
            return $true
        }
        if ($strictPlayerInputGate -and $sent) {
            break
        }
    }

    return $false
}

function Invoke-DebugConsoleMouseGiveSmoke {
    $point = Get-DebugConsoleFirstItemCellPoint
    if (-not $point) {
        "DebugConsoleMouseGivePoint=$(Get-Date -Format o);ok=False;reason=no-window-or-client" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        return $false
    }

    $leftPattern = 'Inventory debug give owner=DTMAPI\.DebugConsoleMod .* requested=1 .* success=True'
    $rightPattern = 'Inventory debug give owner=DTMAPI\.DebugConsoleMod .* requested=10 .* success=True'
    $leftBefore = Get-LogRegexCount -LogPath $logPath -Pattern $leftPattern
    $rightBefore = Get-LogRegexCount -LogPath $logPath -Pattern $rightPattern

    "DebugConsoleMouseGivePoint=$(Get-Date -Format o);clientX=$($point.ClientX);clientY=$($point.ClientY);clientWidth=$($point.ClientWidth);clientHeight=$($point.ClientHeight);panelWidth=$($point.PanelWidth);panelHeight=$($point.PanelHeight)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Start-Sleep -Milliseconds 1200

    $leftSent = Send-DolocTownMouseClick -Button Left -ClientX $point.ClientX -ClientY $point.ClientY
    $leftOk = $leftSent -and (Wait-ForLogRegexCount -LogPath $logPath -Pattern $leftPattern -MinimumCount ($leftBefore + 1) -TimeoutSeconds 8)
    "DebugConsoleMouseGiveLeft=$(Get-Date -Format o);sent=$leftSent;before=$leftBefore;ok=$leftOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')

    Start-Sleep -Milliseconds 900
    $rightSent = Send-DolocTownMouseClick -Button Right -ClientX $point.ClientX -ClientY $point.ClientY
    $rightOk = $rightSent -and (Wait-ForLogRegexCount -LogPath $logPath -Pattern $rightPattern -MinimumCount ($rightBefore + 1) -TimeoutSeconds 8)
    "DebugConsoleMouseGiveRight=$(Get-Date -Format o);sent=$rightSent;before=$rightBefore;ok=$rightOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')

    return ($leftOk -and $rightOk)
}

$saveLoadCycleCountEffective = if ($AutoExerciseSaveLoadCycle -and $SaveLoadCycleCount -le 0) { 3 } else { $SaveLoadCycleCount }
$saveLoadCycleInitialIdleEffective = if ($AutoExerciseSaveLoadCycle) { [Math]::Max(1, $SaveLoadCycleInitialTitleIdleSeconds) } else { 0 }
$saveLoadCyclePendingPressureSecondsEffective = if ($AutoExerciseSaveLoadCyclePendingPressure) { if ($SaveLoadCyclePendingPressureSeconds -le 0) { 1200 } else { $SaveLoadCyclePendingPressureSeconds } } else { 0 }
$minimumLongIdleExitSeconds = if ($TitleIdleBeforeSaveSeconds -gt 0) { $TitleIdleBeforeSaveSeconds + 90 } elseif ($AutoExerciseSaveLoadCycle) { $saveLoadCycleInitialIdleEffective + 90 } elseif ($AutoExerciseSaveLoadCyclePendingPressure) { $saveLoadCyclePendingPressureSecondsEffective + 180 } else { 0 }
$minimumAutoFishingPerformanceExitSeconds = if (-not $AutoFishingPerformance) { 0 } elseif ($AutoFishingPerformanceTargetFrames -gt 0) { [int][Math]::Ceiling(($AutoFishingPerformanceWarmupFrames + $AutoFishingPerformanceTargetFrames) / 20.0) + 180 } elseif ($AutoFishingPerformanceTargetFish -eq 0) { $AutoFishingPerformanceZeroWarmupSeconds + $AutoFishingPerformanceZeroMeasureSeconds + 120 } else { ($AutoFishingPerformanceTargetFish * 3) + 180 }
$minimumLongIdleExitSeconds = [Math]::Max($minimumLongIdleExitSeconds, $minimumAutoFishingPerformanceExitSeconds)
if ($batch5GcLadderEnabled) {
    $minimumLongIdleExitSeconds = [Math]::Max($minimumLongIdleExitSeconds, $Batch5GcLadderMeasureSeconds + 180)
}
$noQaRunnerBudgetSeconds = if ($AssertNoQaUiEvidence -and $AutoExitAfterSecondsOverride -gt 0) {
    $AutoExitAfterSecondsOverride
}
elseif ($AssertNoQaUiEvidence) {
    $TimeoutSeconds
}
else {
    0
}
$noQaRunnerBudgetSource = if (-not $AssertNoQaUiEvidence) {
    'NotRequested'
}
elseif ($AutoExitAfterSecondsOverride -gt 0) {
    'AutoExitAfterSecondsOverride'
}
else {
    'TimeoutSeconds'
}
$noQaRunnerDeadline = $null
$noQaUiDeadline = $null
$noQaOrdinaryPlayerHandshakeShown = $false
$noQaSaveLoadedWaitSeconds = 0
$noQaSaveLoadedDeadlineExpired = $false
$noQaProcessWaitSeconds = 0
$noQaBehaviorCompletedAt = $null
$noQaBehaviorCompletedBeforeDeadlineOk = -not [bool]$AssertNoQaUiEvidence
$noQaBehaviorRemainingMilliseconds = 0
$autoExitAfterSeconds = if ($AssertNoQaUiEvidence) {
    $noQaRunnerBudgetSeconds
}
elseif ($AutoExitAfterSecondsOverride -gt 0) {
    $AutoExitAfterSecondsOverride
}
elseif (($titleSettingsRequested -or $AutoOpenOfficialModUi) -and $SaveSlot -le 0) {
    [Math]::Max([Math]::Max(25, $TimeoutSeconds - 20), $minimumLongIdleExitSeconds)
}
else {
    [Math]::Max([Math]::Max(90, $TimeoutSeconds - 30), $minimumLongIdleExitSeconds)
}
$smokeRootIsolationSummaryPath = Join-Path $evidence 'smoke-root-isolation-summary.txt'
$smokeRootIsolationDisabled = if ($SmokeRootIsolationProfile -eq 'UiRuntime') { 'SaveSlots|EquipmentSlots|Camera' } else { '' }
$smokeRootIsolationUnsupported = if ($SmokeRootIsolationProfile -eq 'UiRuntime') { 'DebugConsoleHost' } else { '' }
$smokeRootIsolationActive = if ($SmokeRootIsolationProfile -eq 'UiRuntime') { 'AllExceptUiRuntimeDisabledAndUnsupported' } else { 'AllDefault' }
@(
    "SmokeRootIsolationProfile=$SmokeRootIsolationProfile",
    "SmokeRootIsolation.Disabled=$smokeRootIsolationDisabled",
    "SmokeRootIsolation.Unsupported=$smokeRootIsolationUnsupported",
    "SmokeRootIsolation.Active=$smokeRootIsolationActive",
    "GeneratedAt=$(Get-Date -Format o)"
) | Set-Content -LiteralPath $smokeRootIsolationSummaryPath
$ownerRootTypeIsolationSummaryPath = Join-Path $evidence 'owner-root-type-isolation-summary.txt'
$ownerRootTargetOwners = switch ($SmokeOwnerRootIsolationProfile) {
    'YConsoleZoomNoInput' { 'DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod' }
    'YConsoleZoomNoEvents' { 'DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod' }
    'YConsoleZoomNoInputEvents' { 'DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod' }
    'YConsoleZoomNoConfig' { 'DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod' }
    'ZoomNoInput' { 'DTMAPI.ZoomMod' }
    'YConsoleNoInput' { 'DTMAPI.DebugConsoleMod' }
    default { '' }
}
$ownerRootTargetTypes = switch ($SmokeOwnerRootIsolationProfile) {
    'YConsoleZoomNoInput' { 'InputButton' }
    'YConsoleZoomNoEvents' { 'EventHandler' }
    'YConsoleZoomNoInputEvents' { 'InputButton|EventHandler' }
    'YConsoleZoomNoConfig' { 'ConfigPage' }
    'ZoomNoInput' { 'InputButton' }
    'YConsoleNoInput' { 'InputButton' }
    default { '' }
}
@(
    "SmokeOwnerRootIsolationProfile=$SmokeOwnerRootIsolationProfile",
    "TargetOwners=$ownerRootTargetOwners",
    "TargetRootTypes=$ownerRootTargetTypes",
    "SuppressedRootsExpected=$(if ($SmokeOwnerRootIsolationProfile -eq 'None') { 'False' } else { 'True' })",
    "SuppressedRootsObserved=",
    "SuppressionSucceeded=",
    "UnexpectedRemainingSuppressedRoots=",
    "UnexpectedDisabledCodeOwners=",
    "GeneratedAt=$(Get-Date -Format o)"
) | Set-Content -LiteralPath $ownerRootTypeIsolationSummaryPath
$nativeContinuationProbeSummaryPath = Join-Path $evidence 'native-continuation-probe-summary.txt'
$nativeContinuationSupported = if ($SmokeNativeLoadContinuationProbe -eq 'VersionPatcher') { 'DolocAPI.AfterLoadArchiveData|DolocTown.VersionPatcher.LoadAllVersionPatches|DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond|DolocTown.MapManager.Init' } else { '' }
$nativeContinuationUnsupported = if ($SmokeNativeLoadContinuationProbe -eq 'VersionPatcher') { 'DolocTown.TextureUtils.DrawArea(skipped-high-frequency)' } else { '' }
@(
    "SmokeNativeLoadContinuationProbe=$SmokeNativeLoadContinuationProbe",
    "ExpectedSupported=$nativeContinuationSupported",
    "ExpectedUnsupported=$nativeContinuationUnsupported",
    "GeneratedAt=$(Get-Date -Format o)"
) | Set-Content -LiteralPath $nativeContinuationProbeSummaryPath
$freshLogPath = Join-Path $dtmapiDir 'logs\latest.log'
$freshBepLogPath = Join-Path $gameDir 'BepInEx\LogOutput.log'
if (Test-Path $freshLogPath) {
    Remove-Item -Force -LiteralPath $freshLogPath
}
if (Test-Path $freshBepLogPath) {
    Remove-Item -Force -LiteralPath $freshBepLogPath
}

"Started=$($smokeRunStartedAt.ToString('o'))`nGameDir=$gameDir`nDtmApiStateDir=$dtmapiDir`nSaveTestMode=$SaveTestMode`nDisposableSaveFixtureRoot=$disposableSaveFixtureRootResolved`nSaveSlot=$SaveSlot`nIncludeHookProbe=$IncludeHookProbe`nLaunchMode=$(if ($launchViaSteam) { 'Steam' } else { 'DirectExe' })`nDisableSecondMotorForSmoke=$DisableSecondMotorForSmoke`nAutoSaveAfterLoad=$AutoSaveAfterLoad`nAutoReloadMods=$AutoReloadMods`nAutoExerciseExperimentalHooks=$AutoExerciseExperimentalHooks`nAutoExerciseActionSpeedTool=$AutoExerciseActionSpeedTool`nAutoExerciseActionSpeedConfigApply=$AutoExerciseActionSpeedConfigApply`nAutoExerciseActionSpeedInteraction=$AutoExerciseActionSpeedInteraction`nAutoExerciseOneActionResourceHit=$AutoExerciseOneActionResourceHit`nAutoExerciseOneActionWrongTool=$AutoExerciseOneActionWrongTool`nAutoExerciseOneActionFuelFeed=$AutoExerciseOneActionFuelFeed`nAutoExerciseOneActionVegetation=$AutoExerciseOneActionVegetation`nAutoExerciseAutoFishingPhase=$AutoExerciseAutoFishingPhase`nAutoExerciseLegacyFishingCompatibility=$AutoExerciseLegacyFishingCompatibility`nAutoExerciseAutoFishingMiniGameComplete=$AutoExerciseAutoFishingMiniGameComplete`nAutoFishingScenario=$autoFishingScenarioEffective`nAutoFishingCastChargeRatio=$AutoFishingCastChargeRatio`nAutoFishingSoakLoops=$AutoFishingSoakLoops`nAutoFishingToggleKey=$autoFishingToggleKeyEffective`nAutoFishingPerformance=$AutoFishingPerformance`nAutoFishingPerformanceProfile=$AutoFishingPerformanceProfile`nAutoExercisePauseMenuLayout=$AutoExercisePauseMenuLayout`nAutoExercisePauseMenuLayoutDelaySeconds=$AutoExercisePauseMenuLayoutDelaySeconds`nAutoExerciseTitleButtonLifecycle=$AutoExerciseTitleButtonLifecycle`nAutoExerciseModOwnerLifetime=$AutoExerciseModOwnerLifetime`nAutoExerciseSaveLoadCycle=$AutoExerciseSaveLoadCycle`nSaveLoadCycleCount=$saveLoadCycleCountEffective`nSaveLoadCycleInitialTitleIdleSeconds=$saveLoadCycleInitialIdleEffective`nSaveLoadCycleIntervalSeconds=$SaveLoadCycleIntervalSeconds`nSaveLoadCycleInSaveSeconds=$SaveLoadCycleInSaveSeconds`nAutoExercisePreLoadGcProbe=$AutoExercisePreLoadGcProbe`nSaveLoadObjectSnapshotMode=$SaveLoadObjectSnapshotMode`nSmokeRootIsolationProfile=$SmokeRootIsolationProfile`nSmokeNativeLoadContinuationProbe=$SmokeNativeLoadContinuationProbe`nAutoExerciseSaveLoadCyclePendingPressure=$AutoExerciseSaveLoadCyclePendingPressure`nSaveLoadCyclePendingPressureSeconds=$saveLoadCyclePendingPressureSecondsEffective`nAutoExerciseInstantSave=$AutoExerciseInstantSave`nAutoExerciseInstantSaveDelaySeconds=$AutoExerciseInstantSaveDelaySeconds`nDebugConsoleSaveAcceptancePhase=$DebugConsoleSaveAcceptancePhase`nExpectedDebugConsoleMoney=$ExpectedDebugConsoleMoney`nAutoExerciseDebugConsole=$AutoExerciseDebugConsole`nAutoExerciseDebugConsoleMouseGive=$AutoExerciseDebugConsoleMouseGive`nAutoExerciseDebugInventory=$AutoExerciseDebugInventory`nAutoExerciseDebugWeather=$AutoExerciseDebugWeather`nAutoExerciseDebugTeleport=$AutoExerciseDebugTeleport`nAutoExerciseDebugTime=$AutoExerciseDebugTime`nAutoExerciseDebugMovement=$AutoExerciseDebugMovement`nAutoExerciseAdvancedDebug=$AutoExerciseAdvancedDebug`nAutoExerciseNewContentApis=$AutoExerciseNewContentApis`nExpectOilAbsent=$ExpectOilAbsent`nOilOnly=$OilOnly`nAutoExerciseMineContentApis=$AutoExerciseMineContentApis`nAutoExerciseZoom=$AutoExerciseZoom`nAutoExerciseZoomProductNative=$AutoExerciseZoomProductNative`nAutoExerciseChestLocatorEnhancer=$AutoExerciseChestLocatorEnhancer`nAutoExerciseMoreSavesOfficialSaveUi=$AutoExerciseMoreSavesOfficialSaveUi`nAutoExerciseStrongPlantingGun=$AutoExerciseStrongPlantingGun`nAutoExerciseCropHarvestingApi=$AutoExerciseCropHarvestingApi`nAutoExerciseCustomEntityApis=$AutoExerciseCustomEntityApis`nAutoExerciseFishRoeTooltip=$AutoExerciseFishRoeTooltip`nAutoExerciseDiagnosticsSnapshot=$AutoExerciseDiagnosticsSnapshot`nQaObserveSaveLoaded=$QaObserveSaveLoaded`nQaObserveSaveSaved=$QaObserveSaveSaved`nQaObserveWorkshopReloadCompleted=$QaObserveWorkshopReloadCompleted`nAutoExerciseAudioReplacement=$AutoExerciseAudioReplacement`nAutoExerciseHatchAnimalVoice=$AutoExerciseHatchAnimalVoice`nAutoPressAutoFishingHotkey=$AutoPressAutoFishingHotkey`nAutoOpenTitleSettingsMenu=$AutoOpenTitleSettingsMenu`nAutoOpenTitleSettingsStatusPage=$AutoOpenTitleSettingsStatusPage`nAutoOpenTitleSettingsManagerMvp=$AutoOpenTitleSettingsManagerMvp`nAutoOpenOfficialModUi=$AutoOpenOfficialModUi`nAutoOpenAnimalPanel=$AutoOpenAnimalPanel`nTitleIdleBeforeSaveSeconds=$TitleIdleBeforeSaveSeconds`nFatalWindowCrashDumpGraceSeconds=$FatalWindowCrashDumpGraceSeconds`nFatalWindowProcessDumpMode=$FatalWindowProcessDumpMode`nFatalWindowPostCloseCrashDumpWaitSeconds=$FatalWindowPostCloseCrashDumpWaitSeconds`nAutoExitAfterSeconds=$autoExitAfterSeconds" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoExerciseMoreEquipmentSlots=$([bool]$AutoExerciseMoreEquipmentSlots)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"SetupMoreEquipmentSlotsCommittedShield=$([bool]$SetupMoreEquipmentSlotsCommittedShield)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"MoreEquipmentSlotsTransitionPhase=$MoreEquipmentSlotsTransitionPhase" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"DamageMoreEquipmentSlotsCommittedShieldNoNativeSave=$([bool]$DamageMoreEquipmentSlotsCommittedShieldNoNativeSave)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave=$([bool]$BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"ObserveMoreEquipmentSlotsInterruptedCandidateRecovery=$([bool]$ObserveMoreEquipmentSlotsInterruptedCandidateRecovery)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoExerciseMoreEquipmentSlotsNoNativeSave=$([bool]$AutoExerciseMoreEquipmentSlotsNoNativeSave)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"ObserveMoreEquipmentSlotsNoNativeSaveCold=$([bool]$ObserveMoreEquipmentSlotsNoNativeSaveCold)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertMoreEquipmentSlotsColdRecovery=$([bool]$AssertMoreEquipmentSlotsColdRecovery)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"SmokeOwnerRootIsolationProfile=$SmokeOwnerRootIsolationProfile" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoPressOneActionMenuKey=$([bool]$AutoPressOneActionMenuKey)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertProductOwnerRefresh=$([bool]$AssertProductOwnerRefresh)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertAdvancedProductOwnerDeactivation=$([bool]$AssertAdvancedProductOwnerDeactivation)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AdvancedProductOwnerDeactivationOwnerIds=$([string]::Join('|', @($AdvancedProductOwnerDeactivationOwnerIds)))" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoReturnHomeAfterProductOwnerRefresh=$([bool]$AssertProductOwnerRefresh)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoExerciseAutoFishingMovementCancel=$([bool]$AutoExerciseAutoFishingMovementCancel)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"SkipInstall=$([bool]$SkipInstall)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"PackagePayloadRoot=$PackagePayloadRoot" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"RequireExternalPlayerInputGate=$([bool]$RequireExternalPlayerInputGate)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"ExternalPlayerInputHoldMilliseconds=$ExternalPlayerInputHoldMilliseconds" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"ExternalPlayerInputHandshakeToken=$externalPlayerInputHandshakeToken" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"ExternalPlayerInputCompletionMarkerPath=$externalPlayerInputCompletionMarkerPath" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertPublishedProductCombination=$([bool]$AssertPublishedProductCombination)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertPublishedProductsDisabled=$([bool]$AssertPublishedProductsDisabled)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertNoQaUiEvidence=$([bool]$AssertNoQaUiEvidence)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"Issue011Acceptance=$([bool]$Issue011Acceptance)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoDriveNoQaAnimalViewer=$([bool]$AutoDriveNoQaAnimalViewer)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"NoQaRunnerBudgetSeconds=$noQaRunnerBudgetSeconds" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"NoQaRunnerBudgetSource=$noQaRunnerBudgetSource" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"NoQaAutoExitSemantics=$(if ($AssertNoQaUiEvidence) { 'RunnerDeadlineOnly;InternalQaAutoLoad=False;InternalQaReturnHome=False' } else { 'NotRequested' })" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"StageQaHost=$([bool]$StageQaHost)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaObserveContentMetadata=$([bool]$QaObserveContentMetadata)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4TitleSettingsUiEnabled=$qaG4TitleSettingsUiEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4OfficialModUiEnabled=$qaG4OfficialModUiEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4PauseMenuLayoutEnabled=$qaG4PauseMenuLayoutEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4DebugConsoleEnabled=$qaG4DebugConsoleEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4SaveSlotsPagingEnabled=$qaG4SaveSlotsPagingEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"MoreSavesFixed12AcceptancePhase=$MoreSavesFixed12AcceptancePhase" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"MoreSavesFixed12OfficialLocalRootMode=$moreSavesFixed12OfficialLocalRootMode" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"MoreSavesFixed12OfficialLocalProductRoot=$moreSavesFixed12OfficialLocalProductRoot" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4AnimalObservationEnabled=$qaG4AnimalObservationEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4EquipmentSlotsObservationEnabled=$qaG4EquipmentSlotsObservationEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4AudioObservationEnabled=$qaG4AudioObservationEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4HatchVoiceEnabled=$qaG4HatchVoiceEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4CameraPlayableEnabled=$qaG4CameraPlayableEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4ContentMetadataObservationEnabled=$qaG4ContentMetadataObservationEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4ExternalPlayerInputObservationEnabled=$qaG4ExternalPlayerInputObservationEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4ContinuousHomePageTerminalEnabled=$qaG4ContinuousHomePageTerminalEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4ContinuousHomePageRequiresSaveLoaded=$qaG4ContinuousHomePageRequiresSaveLoaded" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaHostRunId=$(if ($null -ne $qaHostStage) { [string]$qaHostStage.RunId } else { '' })" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')

$officialModProfileSummaryLines = @(
    "OfficialModProfile=$OfficialModProfile",
    "IsolateAllOfficialMods=$([bool]$IsolateAllOfficialMods)",
    "OfficialModProfileApplied=$($officialModProfileSummary.Applied)",
    "OfficialModProfileEnabledIds=$([string]::Join('|', @($officialModProfileSummary.EnabledIds)))",
    "OfficialModProfileDisabledIds=$([string]::Join('|', @($officialModProfileSummary.DisabledIds)))",
    "OfficialModProfileBackupPath=$($officialModProfileSummary.BackupPath)"
)
$officialModProfileSummaryLines | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')

$launchCommandStartedAt = Get-Date
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
$startupAbsoluteDeadline = if ($AssertNoQaUiEvidence) { $noQaRunnerDeadline } else { [datetime]::MaxValue }
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
$debugTeleportCsvOk = -not [bool]$AutoExerciseDebugTeleport
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
        $saveFixtureIsolationOk = Wait-ForLogLine `
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
        $gameLaunchedWaitSeconds -gt 0 -and (Wait-ForLogLine -LogPath $logPath -Pattern 'GameLaunched dispatched.' -TimeoutSeconds $gameLaunchedWaitSeconds -AbortOnFatalInstanceWindow)
    }
    if ($gameLaunchedOk -and $AssertNoQaUiEvidence) {
        $noQaSaveLoadedWaitSeconds = Get-SmokeRemainingBudgetSeconds -Deadline $noQaRunnerDeadline
        $noQaOrdinaryPlayerHandshakeShown = $true
        $noQaTitleInstruction = if ($Issue011Acceptance) {
            'on the title screen open the DTMAPI settings button once, close that menu, then select save slot 3'
        }
        else {
            'from the title screen select save slot 3'
        }
        Write-Host ("NO-QA ORDINARY-PLAYER HANDSHAKE: no internal QA auto-load or ReturnHome navigation is active. Use normal player controls now: {0}. Waiting for SaveLoaded with {1}s remaining; shared runner deadline={2}." -f $noQaTitleInstruction, $noQaSaveLoadedWaitSeconds, $noQaRunnerDeadline.ToString('o')) -ForegroundColor Yellow
        "NoQaOrdinaryPlayerHandshake=$(Get-Date -Format o);shown=True;instruction=$noQaTitleInstruction;internalQaAutoLoad=False;internalQaReturnHome=False;remainingSeconds=$noQaSaveLoadedWaitSeconds;deadline=$($noQaRunnerDeadline.ToString('o'))" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    }
    if ($gameLaunchedOk -and $qaG4AnyTitleUiEnabled) {
        # The optional QA case owns the complete selected title-UI observation.
        # Its verified terminal is emitted only after the selected QA screenshots exist, so
        # do not wait on unrelated button/menu screenshot breadcrumbs that this
        # ownership route deliberately disables.
        $titleMenuOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.TitleSettingsMenu = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
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
        $titleButtonOk = Wait-ForLogLine -LogPath $logPath -Pattern 'DTMAPI title settings button visible on HomePageUiState.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        if ($titleButtonOk) {
            $titleButtonScreenshotOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Title settings button screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        if ($titleButtonScreenshotOk) {
            $titleMenuOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI title settings menu.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        if ($titleMenuOk) {
            $titleMenuScreenshotOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Title settings menu screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
    }
    if ($titleMenuOk -and $managerStatusRequested -and -not $qaG4AnyTitleUiEnabled) {
        $managerStatusPageOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Status page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($managerStatusPageOk -and $managerStatusRequested -and -not $qaG4AnyTitleUiEnabled) {
        $managerStatusSummaryTextOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Status summary text OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($managerStatusSummaryTextOk -and $managerStatusRequested -and -not $qaG4AnyTitleUiEnabled) {
        if ($managerMvpRequested) {
            $managerStatusSummaryCopyOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Status summary copy OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        $managerStatusPageScreenshotOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Status page screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($managerStatusPageScreenshotOk -and $managerMvpRequested -and -not $qaG4AnyTitleUiEnabled) {
        $managerModsPageOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Mods page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $managerErrorsPageOk = $managerModsPageOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Errors page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerHooksPageOk = $managerErrorsPageOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Hooks page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerFeaturesPageOk = $managerHooksPageOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Features page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsPageOk = $managerFeaturesPageOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Logs page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsExportButtonOk = $managerLogsPageOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Logs export button OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsExportStateTextOk = $managerLogsExportButtonOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Logs export state OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsPageScreenshotOk = $managerLogsExportStateTextOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Logs page screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
    }
    if ($gameLaunchedOk -and $AutoOpenOfficialModUi) {
        $officialModUiOk = if ($qaG4OfficialModUiEnabled) {
            Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.OfficialModUi = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        else {
            Wait-ForLogLine -LogPath $logPath -Pattern 'Official Mod UI evidence OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
    }
    if ($gameLaunchedOk -and $IncludeHookProbe) {
        $probeOk = Wait-ForLogLine -LogPath $logPath -Pattern 'HookProbe GameLaunched OK' -TimeoutSeconds 60 -AbortOnFatalInstanceWindow
    }
    if ($probeOk -and $IncludeHookProbe -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'HookProbe SaveLoaded OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $qaG3SaveLoadedBranchRequested) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseTitleButtonLifecycle -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseSaveLoadCycle -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseSaveLoadCyclePendingPressure -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and
        $MoreSavesFixed12AcceptancePhase -in @('EnabledLifecycle', 'ReenabledCold') -and
        $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoOpenAnimalPanel -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseExperimentalHooks -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseActionSpeedTool -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseActionSpeedConfigApply -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseActionSpeedInteraction -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionResourceHit -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionWrongTool -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionFuelFeed -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionVegetation -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoPressOneActionMenuKey -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and ($AutoPressAutoFishingHotkey -or $AutoExerciseAutoFishingMovementCancel) -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and ($AutoExerciseAutoFishingPhase -or $AutoExerciseLegacyFishingCompatibility) -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $autoFishingNoDemandFrameProfile -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExercisePauseMenuLayout -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and
        ($AutoExerciseInstantSave -or
         $debugConsoleSaveAcceptanceRequested) -and
        $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and ($Batch6AutoFishingPilot -or $Batch6AutoFishingManagerLifecycle) -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $batch5GcLadderEnabled -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
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
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and ($AutoExerciseModOwnerLifetime -or $TitleIdleBeforeSaveSeconds -gt 0) -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
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
            (Wait-ForLogLine -LogPath $logPath -Pattern $batch6AutoFishingTerminalPattern -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $batch6AutoFishingCleanupOk = $batch6AutoFishingPilotOk -and
            (Wait-ForLogLine -LogPath $logPath -Pattern 'Hook status: Smoke.Batch6.AutoFishingPilot.Cleanup = verified.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
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
            (Wait-ForLogLine -LogPath $logPath -Pattern $managerTerminalPattern -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $batch6AutoFishingManagerCleanupOk = $batch6AutoFishingManagerLifecycleOk -and
            (Wait-ForLogLine -LogPath $logPath -Pattern 'Hook status: Smoke.Batch6.AutoFishingManagerLifecycle.Cleanup = verified.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
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
        if ($equipmentOpenProvenanceOk) {
            $equipmentEvidenceCaptured = Wait-ForLogLineAfterOffset -LogPath $logPath -Offset $equipmentLogOffset -Pattern 'EVIDENCE_CAPTURED_WAITING_ESCAPE' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        $equipmentSlotsUiEvidenceCapturedOk = [bool]$equipmentEvidenceCaptured
        $equipmentHoverReceipt = if ($equipmentEvidenceCaptured -and (Get-Date) -lt $equipmentObservationDeadline) {
            Invoke-DolocTownHoverSweep -LogPath $logPath -LogOffset $equipmentLogOffset -Pattern 'EquipmentSlots UI hover owner=' -Deadline $equipmentObservationDeadline
        }
        else {
            [PSCustomObject]@{ DolocTownPid = 0; ForegroundMatched = $false; MoveOnly = $true; MouseClickSent = $false; PointsVisited = 0; MatchedExpectedLog = $false }
        }
        $equipmentHoverOk = [bool]$equipmentHoverReceipt.ForegroundMatched -and [bool]$equipmentHoverReceipt.MoveOnly -and
            -not [bool]$equipmentHoverReceipt.MouseClickSent -and [bool]$equipmentHoverReceipt.MatchedExpectedLog
        $equipmentSlotsUiHoverOk = $equipmentHoverOk
        $equipmentSlotsPlayerInputAttempts.Add([ordered]@{
            Label = 'EquipmentSlotsMoveOnlyHoverSweep'
            ForegroundMatched = [bool]$equipmentHoverReceipt.ForegroundMatched
            MoveOnly = [bool]$equipmentHoverReceipt.MoveOnly
            MouseClickSent = [bool]$equipmentHoverReceipt.MouseClickSent
            PointsVisited = [int]$equipmentHoverReceipt.PointsVisited
            MatchedExpectedLog = [bool]$equipmentHoverReceipt.MatchedExpectedLog
            LogOffset = $equipmentLogOffset
        }) | Out-Null
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
            if ($equipmentCloseProvenanceOk) {
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
                (Wait-ForLogLine `
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
            (Wait-ForLogLine `
                -LogPath $logPath `
                -Pattern ("Smoke exercise MoreEquipmentSlotsTransition OK phase={0};" -f $MoreEquipmentSlotsTransitionPhase) `
                -TimeoutSeconds $transitionTerminalWaitSeconds `
                -AbortOnFatalInstanceWindow)
    }
    if ($moreSavesFixed12AcceptanceRequested) {
        $moreSavesFixed12CaseId =
            'MoreSavesFixed12' + $MoreSavesFixed12AcceptancePhase
        $moreSavesFixed12LifecycleOk =
            Wait-ForLogLine `
                -LogPath $logPath `
                -Pattern (
                    'Smoke exercise MoreSavesFixed12Lifecycle OK case=' +
                    $moreSavesFixed12CaseId + ';') `
                -TimeoutSeconds $TimeoutSeconds `
                -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseNewContentApis) {
        if ($ExpectOilAbsent) {
            $newContentOilAbsentOk = if ($qaG4ContentMetadataObservationEnabled) {
                Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.ContentMetadataObservation = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            }
            else {
                Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentOilAbsent OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            }
            $contentMetadataObservationOk = $newContentOilAbsentOk
            $newContentApisOk = $newContentOilAbsentOk
        }
        elseif ($OilOnly) {
            $newContentOilOnlyOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentOilOnly OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            if ($newContentOilOnlyOk) {
                $newContentOilItemMetadataOk = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Smoke exercise NewContentOilItemMetadata OK' -ErrorAction SilentlyContinue).Count -gt 0
                $newContentOilCoalDropOk = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Smoke exercise NewContentOilCoalDrop OK' -ErrorAction SilentlyContinue).Count -gt 0
            }
            $newContentApisOk = $newContentOilItemMetadataOk -and $newContentOilCoalDropOk -and $newContentOilOnlyOk
        }
        else {
            $mineOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Mine ProductNative activation complete owner=dtmapi.mod.dtmapi.minemod hooks=3 schedulerMode=session-derived' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $equipmentSlotsOk = Wait-ForLogLine -LogPath $logPath -Pattern 'MoreEquipmentSlots API register success=True reason=SaveLoaded' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentOilItemMetadataOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentOilItemMetadata OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentOilCoalDropOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentOilCoalDrop OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentMineOfficialJsonOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentMineOfficialJson OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentEquipmentSlotsOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentEquipmentSlots OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentMineProductionOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentMineProduction OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $newContentApisOk = $mineOk -and $equipmentSlotsOk -and $newContentOilItemMetadataOk -and $newContentOilCoalDropOk -and $newContentMineOfficialJsonOk -and $newContentEquipmentSlotsOk -and $newContentMineProductionOk
        }
    }
    if ($saveLoadedOk -and $qaG4ContentMetadataObservationEnabled -and -not $AutoExerciseNewContentApis) {
        $contentMetadataObservationOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.ContentMetadataObservation = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        if ($ExpectOilAbsent) {
            $newContentOilAbsentOk = $contentMetadataObservationOk
        }
    }
    if ($saveLoadedOk -and $AutoExerciseMineContentApis) {
        $mineOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Mine ProductNative activation complete owner=dtmapi.mod.dtmapi.minemod hooks=3 schedulerMode=session-derived' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineOfficialJsonOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentMineOfficialJson OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineOfficialTechTreeUiOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Mine official tech tree UI evidence OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineProductionOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentMineProduction OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineApisOk = $mineOk -and $newContentMineOfficialJsonOk -and $newContentMineOfficialTechTreeUiOk -and $newContentMineProductionOk
    }
    if ($saveLoadedOk -and $AutoOpenAnimalPanel) {
        $animalViewerUiOk = if ($qaG4AnimalObservationEnabled) {
            Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.AnimalViewerRendering = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        else {
            Wait-ForLogLine -LogPath $logPath -Pattern 'Animal viewer UI evidence OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
    }
    if ($saveLoadedOk -and $AutoPressOneActionMenuKey) {
        $oneActionMenuVirtualKey = Resolve-DolocTownVirtualKey -Key 'F11'
        $oneActionMenuOpenSent = $null -ne $oneActionMenuVirtualKey -and (Send-DolocTownKey -VirtualKey $oneActionMenuVirtualKey -Name 'OneActionCompleteF11' -HoldMilliseconds 80 -InputLogPath $logPath)
        $oneActionMenuOpenEvidence = $script:lastDolocTownInputEvidence
        $oneActionMenuOpened = $oneActionMenuOpenSent -and (Wait-ForLogLine -LogPath $logPath -Pattern 'OneActionComplete hotkey OpenConfig OK key=F11' -TimeoutSeconds 20 -AbortOnFatalInstanceWindow)
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
        $experimentalHooksOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.ExperimentalHookExercise = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedTool) {
        $actionSpeedToolOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedTool OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedConfigApply) {
        $actionSpeedConfigApplyOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedConfigApply OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedInteraction) {
        $actionSpeedInteractionOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedInteraction OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        if ($actionSpeedInteractionOk) {
            $actionSpeedDiagnosticsOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.DiagnosticsSnapshot = verified. scenario=ActionSpeed' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            if ($actionSpeedDiagnosticsOk) {
                $diagnosticsReportExportPassedScenarios += 'ActionSpeed'
            }
        }
        else {
            $diagnosticsReportExportOk = $false
        }
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionResourceHit) {
        $oneActionResourceHitOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise OneActionResourceHit OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $oneActionPartialEnergyOk = [bool](Select-String -Path $logPath -Pattern 'Smoke exercise OneActionPartialEnergy OK' | Select-Object -Last 1)
        $oneActionConfigReloadOk = [bool](Select-String -Path $logPath -Pattern 'Smoke exercise OneActionConfigReload OK' | Select-Object -Last 1)
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionWrongTool) {
        $oneActionWrongToolOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise OneActionWrongTool OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionFuelFeed) {
        $oneActionFuelFeedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise OneActionFuelFeed OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionVegetation) {
        $oneActionVegetationOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise OneActionVegetation OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseSaveLoadCycle) {
        $saveLoadCycleOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise SaveLoadCycle OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $saveLoadCycleSummaryMatch = Select-String -Path $logPath -Pattern 'Smoke exercise SaveLoadCycle OK' -ErrorAction SilentlyContinue | Select-Object -Last 1
        if ($saveLoadCycleSummaryMatch) {
            $saveLoadCycleSummary = $saveLoadCycleSummaryMatch.Line
        }
    }
    if ($saveLoadedOk -and $AutoExerciseSaveLoadCyclePendingPressure) {
        $saveLoadCyclePendingPressureOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise SaveLoadCyclePendingPressure OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $saveLoadCyclePendingPressureSummaryMatch = Select-String -Path $logPath -Pattern 'Smoke exercise SaveLoadCyclePendingPressure OK' -ErrorAction SilentlyContinue | Select-Object -Last 1
        if ($saveLoadCyclePendingPressureSummaryMatch) {
            $saveLoadCyclePendingPressureSummary = $saveLoadCyclePendingPressureSummaryMatch.Line
        }
    }
    if ($saveLoadedOk -and $AutoExerciseAudioReplacement) {
        $audioReadyOk = Wait-ForLogLine -LogPath $logPath -Pattern 'AudioReplacement local WAV ready owner=Yuuka.DTMAPI.ManboCardboardAudio' -TimeoutSeconds 20 -AbortOnFatalInstanceWindow
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
                $audioPaperBoxInteractOk = Wait-ForLogLine -LogPath $logPath -Pattern $audioPaperBoxPattern -TimeoutSeconds 2 -AbortOnFatalInstanceWindow
                $audioReplacementEventOk = Wait-ForLogLine -LogPath $logPath -Pattern $audioReplacementPattern -TimeoutSeconds 2 -AbortOnFatalInstanceWindow
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
                (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.AudioReplacement = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
            $audioReplacementOk = $audioReplacementOk -and $qaG4AudioObservationOk
        }
    }
    if ($saveLoadedOk -and $AutoExerciseHatchAnimalVoice) {
        $hatchVoiceReadyOk = Wait-ForLogLine -LogPath $logPath -Pattern 'AudioReplacement local WAV ready owner=DTMAPI.HatchAssets' -TimeoutSeconds 30 -AbortOnFatalInstanceWindow
        if ($hatchVoiceReadyOk) {
            $hatchVoiceSmokeOk = if ($qaG4HatchVoiceEnabled) {
                Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.HatchAnimalVoice = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            }
            else {
                Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise HatchAnimalVoice OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            }
            $hatchVoiceChildOk = Wait-ForLogLine -LogPath $logPath -Pattern 'AudioReplacement event owner=DTMAPI.HatchAssets replacement=hatch-pet-child event=PLAY_ANIMAL_PET_CHICKEN_CHILD played=True suppressed=True' -TimeoutSeconds 20 -AbortOnFatalInstanceWindow
            $hatchVoiceAdultOk = Wait-ForLogLine -LogPath $logPath -Pattern 'AudioReplacement event owner=DTMAPI.HatchAssets replacement=hatch-pet-adult event=PLAY_ANIMAL_PET_CHICKEN played=True suppressed=True' -TimeoutSeconds 20 -AbortOnFatalInstanceWindow
            $hatchAnimalVoiceOk = $hatchVoiceSmokeOk -and $hatchVoiceChildOk -and $hatchVoiceAdultOk
            "HatchAnimalVoiceEvidence=$(Get-Date -Format o);ready=$hatchVoiceReadyOk;smoke=$hatchVoiceSmokeOk;child=$hatchVoiceChildOk;adult=$hatchVoiceAdultOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        }
        else {
            $hatchAnimalVoiceOk = $false
            "HatchAnimalVoiceReadyTimeout=$(Get-Date -Format o)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        }
    }
    if ($saveLoadedOk -and $autoFishingMovementRequested) {
        $autoFishingInputLogOk = if ($autoFishingPerformanceBaseline -and $AutoFishingPerformanceProfile -eq 'EnabledNoRod') { $true } else { Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise AutoFishingHotkey OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow }
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
            Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.PauseMenuLayout = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        else {
            Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise PauseMenuLayout OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
    }
    if ($saveLoadedOk -and $AutoExerciseInstantSave) {
        $instantSaveOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise InstantSave OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $debugConsoleSaveAcceptanceRequested) {
        $debugConsoleSaveAcceptanceOk =
            Wait-ForLogLine `
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
                $externalPlayerInputReadyOk = Wait-ForLogLine -LogPath $logPath -Pattern $readyPattern -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
                "ExternalPlayerInputReady=$(Get-Date -Format o);token=$externalPlayerInputHandshakeToken;ok=$externalPlayerInputReadyOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
            }

            $debugConsoleInGameHotkeyOk = if ($RequireExternalPlayerInputGate -or $qaG4DebugConsoleEnabled -or $AssertNoQaUiEvidence) {
                $false
            }
            else {
                Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugConsoleHotkey OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
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
                $qaG4DebugObservationOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.DebugConsoleHotkey = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
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
            $externalPlayerInputCleanupOk = Wait-ForLogLine -LogPath $logPath -Pattern $cleanupPattern -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
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
                (Wait-ForLogLine `
                    -LogPath $logPath `
                    -Pattern 'Smoke exercise TitleButtonLifecycle OK' `
                    -TimeoutSeconds $titleLifecycleWaitSeconds `
                    -AbortOnFatalInstanceWindow)
        }
        else {
            $titleLifecycleOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise TitleButtonLifecycle OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
    }
    if ($saveLoadedOk -and $AssertAdvancedProductOwnerDeactivation) {
        $advancedProductOwnerDeactivationOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise AdvancedProductOwnerDeactivation OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
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
            Write-Host 'NO-QA UI GATE: Y console input is complete. The runner will now open EquipmentSlots with one foreground SendInput B, perform a move-only hover sweep, and close it with Escape. Afterwards, use ordinary player controls in the third save to open a configured animal viewer and switch animals until two new progress-render receipts appear.'
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
        $debugInventoryOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugInventory OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugWeather) {
        $debugWeatherOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugWeather OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugTeleport) {
        $debugTeleportCsvOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugTeleportCsv OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $debugTeleportOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugTeleport OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugTime) {
        $debugTimeOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugTime OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugMovement) {
        $debugMovementOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugMovement OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseAdvancedDebug) {
        $advancedDebugOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise AdvancedDebug OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseZoom) {
        $zoomOk = if ($qaG4CameraPlayableEnabled) {
            Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.CameraPlayable = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        else {
            Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise CameraPlayable OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        if ($zoomOk -and -not $qaG4CameraPlayableEnabled) {
            $cameraDiagnosticsOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.DiagnosticsSnapshot = verified. scenario=Camera' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            if ($cameraDiagnosticsOk) {
                $diagnosticsReportExportPassedScenarios += 'Camera'
            }
        }
        elseif (-not $zoomOk) {
            $diagnosticsReportExportOk = $false
        }
    }
    if ($saveLoadedOk -and $AutoExerciseChestLocatorEnhancer) {
        $chestLocatorEnhancerOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ChestLocatorEnhancer OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseZoomProductNative) {
        $zoomProductNativeOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ZoomProductNative OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseMoreEquipmentSlots) {
        if ($ObserveMoreEquipmentSlotsInterruptedCandidateRecovery) {
            $moreEquipmentSlotsInterruptedCandidateRecoveryOk =
                Wait-ForLogLine `
                    -LogPath $logPath `
                    -Pattern 'Smoke exercise MoreEquipmentSlotsInterruptedCandidateRecovery OK .*persistedCleanupWrites=1.*journal=false.*candidate=false.*beforeGiveItem=true.*beforeNativeSave=true' `
                    -TimeoutSeconds $TimeoutSeconds `
                    -Regex `
                    -AbortOnFatalInstanceWindow
        }
        $moreEquipmentSlotsReadyOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise MoreEquipmentSlots OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $moreEquipmentSlotsProtectedTransactionOk = Wait-ForLogLine -LogPath $logPath -Pattern 'MoreEquipmentSlots protected transaction committed destination=(Backpack|Mail)' -TimeoutSeconds $TimeoutSeconds -Regex -AbortOnFatalInstanceWindow
        $moreEquipmentSlotsOk = $moreEquipmentSlotsReadyOk -and $moreEquipmentSlotsProtectedTransactionOk
    }
    if ($saveLoadedOk -and
        $SetupMoreEquipmentSlotsCommittedShield) {
        $moreEquipmentSlotsCommittedShieldSetupOk =
            Wait-ForLogLine `
                -LogPath $logPath `
                -Pattern 'Smoke exercise MoreEquipmentSlotsCommittedShieldSetup OK .*logicalShieldItems=1.*nativeShieldItems=0.*committedShieldItems=1.*journal=false.*candidate=false' `
                -TimeoutSeconds $TimeoutSeconds `
                -Regex `
                -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and
        $DamageMoreEquipmentSlotsCommittedShieldNoNativeSave) {
        $moreEquipmentSlotsCommittedShieldDamageOk =
            Wait-ForLogLine `
                -LogPath $logPath `
                -Pattern 'Smoke exercise MoreEquipmentSlotsCommittedShieldDamageNoNativeSave OK .*workingShieldDamage=.*logicalCommittedShieldItems=1.*cleanupBeforeTitle=false.*nativeSaveRequested=false' `
                -TimeoutSeconds $TimeoutSeconds `
                -Regex `
                -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and
        $BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave) {
        $moreEquipmentSlotsCommittedShieldBreakReplaceOk =
            Wait-ForLogLine `
                -LogPath $logPath `
                -Pattern 'Smoke exercise MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave OK .*workingShieldBroken=true.*replacement=true.*unequip=true.*logicalCommittedShieldItems=1.*cleanupBeforeTitle=false.*nativeSaveRequested=false' `
                -TimeoutSeconds $TimeoutSeconds `
                -Regex `
                -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and
        $AutoExerciseMoreEquipmentSlotsNoNativeSave) {
        $moreEquipmentSlotsNoNativeSaveOk =
            Wait-ForLogLine `
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
            Wait-ForLogLine `
                -LogPath $logPath `
                -Pattern 'Smoke exercise MoreEquipmentSlotsNoNativeSaveColdObserver OK .*workingMatchesCommitted=true.*workingDirty=false.*journal=false.*candidate=false.*nativeSaveRequested=false' `
                -TimeoutSeconds $TimeoutSeconds `
                -Regex `
                -AbortOnFatalInstanceWindow
        if ($moreEquipmentSlotsNoNativeSaveColdOk) {
            $moreEquipmentSlotsNoNativeSaveColdOk =
                Wait-ForLogLine `
                    -LogPath $logPath `
                    -Pattern $expectedMoreEquipmentSlotsColdItemPattern `
                    -TimeoutSeconds $TimeoutSeconds `
                    -Regex `
                    -AbortOnFatalInstanceWindow
        }
    }
    if ($saveLoadedOk -and
        $MoreEquipmentSlotsTransitionPhase -eq 'ColdCommit') {
        $moreEquipmentSlotsColdRecoveryDemandOk = Wait-ForLogLine -LogPath $logPath -Pattern 'EquipmentSlots cold compatibility demand detected source=(Sidecar|Journal)' -TimeoutSeconds $TimeoutSeconds -Regex -AbortOnFatalInstanceWindow
        $moreEquipmentSlotsColdRecoveryHostOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Hook status: Compatibility\.Host = resident\..*service=EquipmentSlots' -TimeoutSeconds $TimeoutSeconds -Regex -AbortOnFatalInstanceWindow
        $moreEquipmentSlotsColdRecoveryDestinationOk = Wait-ForLogLine -LogPath $logPath -Pattern 'EquipmentSlots orphan recovery OK owner=DTMAPI\.MoreEquipmentSlotsMod recovered=1 message=.*(native backpack|native item mail)' -TimeoutSeconds $TimeoutSeconds -Regex -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseMoreSavesOfficialSaveUi) {
        $moreSavesOfficialSaveUiOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.MoreSavesOfficialSaveUi = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $moreSavesOfficialSaveUiEvidenceOk = if ($qaG4SaveSlotsPagingEnabled) {
            $moreSavesOfficialSaveUiOk -and (
                -not $qaG4MoreSavesPostTitlePanelEnabled -or
                (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.MoreSavesPostTitleOfficialSaveUi = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
            )
        }
        else {
            $moreSavesOfficialSaveUiOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'MoreSaves official save UI evidence archiveFileCount=' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        }
    }
    if ($saveLoadedOk -and $AutoExerciseStrongPlantingGun) {
        $strongPlantingGunOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise StrongPlantingGun OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseCropHarvestingApi) {
        $cropHarvestingApiOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise CropHarvestingApi OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $cropHarvestingApiEvidenceOk = $cropHarvestingApiOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.CropHarvestingApi = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
    }
    if ($saveLoadedOk -and $AutoExerciseCustomEntityApis) {
        $customEntityApisOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise CustomEntityApis OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $qaFishRoeTooltipObservationEnabled) {
        $fishRoeTooltipOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.FishRoeTooltip = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $itemDisplayNameLifecycleRequested) {
        $itemDisplayNameLifecycleOk =
            (Wait-ForLogLine -LogPath $logPath -Pattern 'Shared item display-name consumer query succeeded owner=Yuuka.DTMAPI.FishBreedingAssistant ' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow) -and
            (Wait-ForLogLine -LogPath $logPath -Pattern 'Shared item display-name consumer query succeeded owner=Yuuka.DTMAPI.AnimalHusbandryProgress ' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow) -and
            (Wait-ForLogLine -LogPath $logPath -Pattern 'SharedNative.EnvironmentReset = experimental' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow) -and
            (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.ItemDisplayNameLifecycle = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
    }
    if ($saveLoadedOk -and $qaDiagnosticsSnapshotEnabled) {
        $qaDiagnosticsSnapshotOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.DiagnosticsSnapshot = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $QaObserveSaveLoaded) {
        $qaSaveLoadedObservationOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.QaLifecycle.SaveLoaded = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($QaObserveSaveSaved) {
        $qaSaveSavedObservationOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.QaLifecycle.SaveSaved = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($QaObserveWorkshopReloadCompleted) {
        $qaWorkshopReloadObservationOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.QaLifecycle.WorkshopReloadCompleted = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
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
$fatalWindows = @()
$fatalWindowDetectedAt = $null
$unityCrashFreshnessStatus = 'not-evaluated'
$unityCrashFreshnessSummary = ''
$unityCrashPostCloseFreshness = $null
$fatalProcessDumpStatus = 'Skipped'
while (Test-SmokeDeadlineHasBudget -Deadline $deadline) {
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
& "$PSScriptRoot\collect-logs.ps1" -CaseId 'GAME-SMOKE' -OutputDirectory $evidence -RuntimeEvidenceSinceUtc ($smokeRunStartedAt.ToUniversalTime()) | Tee-Object -FilePath (Join-Path $evidence 'collect-logs-output.txt')
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
    & "$PSScriptRoot\collect-logs.ps1" -CaseId 'GAME-SMOKE' -OutputDirectory $evidence -RuntimeEvidenceSinceUtc ($smokeRunStartedAt.ToUniversalTime()) | Tee-Object -FilePath (Join-Path $evidence 'collect-logs-after-close-output.txt')
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
}
catch {
    $smokeBodyFailure = [string]$_.Exception.ToString()
    $smokeBodyFailure |
        Set-Content -LiteralPath (Join-Path $evidence 'runner-body-failure.txt')
    throw
}
finally {
    $restoreErrors = New-Object 'System.Collections.Generic.List[string]'
    $recoveryExit = Wait-SmokeProcessExitBeforeRecovery -EvidencePath $evidence -TimeoutSeconds 15
    if (-not [bool]$recoveryExit.Exited) {
        $manualRecoveryItems = New-Object 'System.Collections.Generic.List[object]'
        if ($null -ne $officialModProfileSummary -and [bool]$officialModProfileSummary.Applied -and -not $officialModProfileRestored) {
            $manualRecoveryItems.Add([ordered]@{
                Kind = 'OfficialModEnablement'
                TargetPath = [string]$officialModProfileSummary.EnablementPath
                BackupPath = [string]$officialModProfileSummary.BackupPath
                ExistedBefore = $true
                ActionAfterExit = 'CopyBackupToTarget'
            }) | Out-Null
        }
        if ($null -ne $local11AuthorSourceSummary -and [bool]$local11AuthorSourceSummary.Applied -and -not $local11AuthorSourceRestored) {
            $manualRecoveryItems.Add([ordered]@{
                Kind = 'LocalProductAuthorSourceState'
                TargetPath = [string]$local11AuthorSourceSummary.StatePath
                BackupPath = [string]$local11AuthorSourceSummary.BackupPath
                ExistedBefore = [bool]$local11AuthorSourceSummary.OriginalExisted
                ActionAfterExit = if ([bool]$local11AuthorSourceSummary.OriginalExisted) { 'CopyBackupToTarget' } else { 'RemoveTargetIfPresent' }
                ExpectedLength = [int64]$local11AuthorSourceSummary.OriginalLength
                ExpectedSha256 = [string]$local11AuthorSourceSummary.OriginalSha256
            }) | Out-Null
        }
        if ($AutoExerciseStrongPlantingGun) {
            $manualRecoveryItems.Add([ordered]@{
                Kind = 'StrongPlantingGunExactConfig'
                TargetPath = [System.IO.Path]::GetFullPath($strongPlantingGunConfigPath)
                BackupPath = if ($strongPlantingGunConfigHadOriginal) {
                    [System.IO.Path]::GetFullPath($strongPlantingGunConfigBackup)
                }
                else {
                    ''
                }
                ExistedBefore = [bool]$strongPlantingGunConfigHadOriginal
                ActionAfterExit = if ($strongPlantingGunConfigHadOriginal) {
                    'CopyBackupToTarget'
                }
                else {
                    'RemoveTargetIfPresent'
                }
            }) | Out-Null
        }
        foreach ($config in @(
            [ordered]@{ Used = [bool]$usesOneActionConfigSmoke; Kind = 'OneActionConfig'; TargetPath = $oneActionConfigPath; BackupPath = $oneActionConfigBackup; ExistedBefore = [bool]$oneActionConfigHadOriginal },
            [ordered]@{ Used = [bool]$usesAutoFishingConfigSmoke; Kind = 'AutoFishingConfig'; TargetPath = $autoFishingConfigPath; BackupPath = $autoFishingConfigBackup; ExistedBefore = [bool]$autoFishingConfigHadOriginal },
            [ordered]@{ Used = [bool]$usesActionSpeedConfigSmoke; Kind = 'ActionSpeedConfig'; TargetPath = $actionSpeedConfigPath; BackupPath = $actionSpeedConfigBackup; ExistedBefore = [bool]$actionSpeedConfigHadOriginal }
        )) {
            if (-not [bool]$config.Used) {
                continue
            }
            $manualRecoveryItems.Add([ordered]@{
                Kind = [string]$config.Kind
                TargetPath = [System.IO.Path]::GetFullPath([string]$config.TargetPath)
                BackupPath = [System.IO.Path]::GetFullPath([string]$config.BackupPath)
                ExistedBefore = [bool]$config.ExistedBefore
                ActionAfterExit = if ([bool]$config.ExistedBefore) { 'CopyBackupToTarget' } else { 'RemoveTargetIfPresent' }
            }) | Out-Null
        }
        if ($Batch6AutoFishingManagerLifecycle) {
            $manualRecoveryItems.Add([ordered]@{
                Kind = 'Batch6AutoFishingManagerDisabledMarker'
                TargetPath = $batch6ManagerMarkerPath
                BackupPath = $batch6ManagerMarkerBackupPath
                ExistedBefore = $batch6ManagerMarkerExistedBefore
                ActionAfterExit = if ($batch6ManagerMarkerExistedBefore) { 'CopyBackupToTarget' } else { 'RemoveTargetIfPresent' }
                ExpectedLength = $batch6ManagerMarkerLengthBefore
                ExpectedSha256 = $batch6ManagerMarkerSha256Before
            }) | Out-Null
        }
        if ($null -ne $qaHostStage) {
            foreach ($artifact in @($qaHostStage.Artifacts)) {
                $manualRecoveryItems.Add([ordered]@{
                    Kind = 'QaHost' + [string]$artifact.Kind
                    TargetPath = [string]$artifact.Path
                    ActionAfterExit = 'RemoveOnlyIfLengthAndSha256Match'
                    ExpectedLength = [int64]$artifact.Length
                    ExpectedSha256 = [string]$artifact.Sha256
                }) | Out-Null
            }
            foreach ($directory in @([string]$qaHostStage.RunDir, [string]$qaHostStage.QaRoot)) {
                $manualRecoveryItems.Add([ordered]@{
                    Kind = 'QaHostDirectory'
                    TargetPath = $directory
                    ActionAfterExit = 'RemoveOnlyIfEmptyAfterExactArtifacts'
                }) | Out-Null
            }
        }

        $manualRecoveryReceiptPath = Join-Path $evidence 'manual-recovery-required.json'
        $manualRecoveryInstructionsPath = Join-Path $evidence 'MANUAL-RECOVERY-REQUIRED.txt'
        $manualRecoveryReceiptError = ''
        try {
            Write-SmokeJsonObject -Path $manualRecoveryReceiptPath -Value ([ordered]@{
                CreatedAt = (Get-Date).ToString('o')
                Reason = 'Stable DolocTown process absence was not proven during the bounded recovery wait. No player-owned save was backed up or written back; mod_infos.json, author source state, smoke setting, product config, and runner-owned QA host cleanup remain blocked.'
                ProcessExitEvidence = $recoveryExit
                RecoveryItems = @($manualRecoveryItems.ToArray())
                RequiredOrder = @(
                    'Verify Get-Process -Name DolocTown returns no process.',
                    'Apply each RecoveryItems action exactly as recorded.',
                    'Verify each non-save test asset against its recorded receipt before launching the game again.'
                )
            })
            @(
                'DTMAPI smoke restoration was intentionally blocked because stable DolocTown process absence was not proven.',
                'Do not edit or restore player save files; this runner created no routine archive backup.',
                "Use the exact target, backup, action, and expected hash entries in: $manualRecoveryReceiptPath",
                'After recovery, verify every changed non-save test asset byte-for-byte before starting DolocTown again.'
            ) | Set-Content -LiteralPath $manualRecoveryInstructionsPath
        }
        catch {
            $manualRecoveryReceiptError = [string]$_.Exception.Message
        }
        throw "Smoke cleanup blocked: stable DolocTown process absence was not proven during a 15-second bounded wait. No routine player archive backup or writeback exists. Manual recovery receipt: $manualRecoveryReceiptPath. receiptError=$manualRecoveryReceiptError"
    }
    if ($noNativeSaveMetadataRequested) {
        try {
            $playerSaveFamilyChecks = @($playerSaveSnapshots | ForEach-Object {
                Compare-DtmApiCurrentSaveArchiveFamilySnapshot -Baseline $_
            })
            $playerSaveUnchangedBeforeCleanupOk =
                @($playerSaveFamilyChecks | Where-Object { -not [bool]$_.Passed }).Count -eq 0
            Write-SmokeJsonObject -Path (Join-Path $evidence 'player-save-unchanged-before-cleanup.json') -Value ([ordered]@{
                SaveTestMode = $SaveTestMode
                ComparedBeforeRunnerOrExternalRestore = $true
                RoutineByteBackupCreated = $false
                PlayerArchiveWritebackPerformed = $false
                CurrentArchiveFamily = '<current>.data + <current>.data.prev[0-9]+ + <current>.data.bak'
                Families = @($playerSaveFamilyChecks)
                Passed = $playerSaveUnchangedBeforeCleanupOk
            })
            $committedSidecarsUnchangedBeforeCleanupOk = Compare-SmokeFileMetadataSnapshots `
                -Snapshots @($committedSidecarSnapshots) `
                -EvidencePath (Join-Path $evidence 'committed-sidecar-unchanged-before-cleanup.json') `
                -SaveTestMode $SaveTestMode
            $g5CommittedSidecarUnchangedOk =
                -not [bool]$qaG5AnyRequested -or
                $committedSidecarsUnchangedBeforeCleanupOk
        }
        catch {
            $playerSaveUnchangedBeforeCleanupOk = $false
            $committedSidecarsUnchangedBeforeCleanupOk = $false
            $g5CommittedSidecarUnchangedOk = $false
            $restoreErrors.Add("no-native-save-pre-cleanup-verification:$([string]$_.Exception.Message)") | Out-Null
        }
        if (-not $playerSaveUnchangedBeforeCleanupOk) {
            $restoreErrors.Add('player-save:changed-before-cleanup') | Out-Null
        }
        if (-not $committedSidecarsUnchangedBeforeCleanupOk) {
            $restoreErrors.Add('committed-sidecar:changed-before-config-cleanup') | Out-Null
        }
    }
    if ($AutoExerciseStrongPlantingGun) {
        try {
            if ($strongPlantingGunConfigHadOriginal) {
                Copy-Item -Force -LiteralPath $strongPlantingGunConfigBackup -Destination $strongPlantingGunConfigPath
            }
            elseif (Test-Path -LiteralPath $strongPlantingGunConfigPath -PathType Leaf) {
                Remove-Item -LiteralPath $strongPlantingGunConfigPath -Force
            }
            $strongPlantingGunConfigRestored =
                if ($strongPlantingGunConfigHadOriginal) {
                    (Test-Path -LiteralPath $strongPlantingGunConfigPath -PathType Leaf) -and
                    [string]::Equals(
                        (Get-SmokeFileSha256 -Path $strongPlantingGunConfigPath),
                        (Get-SmokeFileSha256 -Path $strongPlantingGunConfigBackup),
                        [System.StringComparison]::OrdinalIgnoreCase)
                }
                else {
                    -not (Test-Path -LiteralPath $strongPlantingGunConfigPath)
                }
            if (-not $strongPlantingGunConfigRestored) {
                $g5ConfigDirectoryRestored = $false
                $restoreErrors.Add('strong-planting-gun-config:verification-failed') | Out-Null
            }
        }
        catch {
            $g5ConfigDirectoryRestored = $false
            $restoreErrors.Add("strong-planting-gun-config:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($null -ne $officialModProfileSummary -and [bool]$officialModProfileSummary.Applied -and -not $officialModProfileRestored) {
        try {
            $officialModProfileRestored = Restore-SmokeOfficialModProfile -Summary $officialModProfileSummary -EvidencePath $evidence -Phase 'run-finally'
            "OfficialModProfileRestored=$officialModProfileRestored $(Get-Date -Format o)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        }
        catch {
            $officialModProfileRestored = $false
            $restoreErrors.Add("official-mod-profile:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($null -ne $local11AuthorSourceSummary -and [bool]$local11AuthorSourceSummary.Applied -and -not $local11AuthorSourceRestored) {
        try {
            $local11AuthorSourceRestored = Restore-SmokeRecoveryOnlyAuthorSourceState -Summary $local11AuthorSourceSummary -EvidencePath $evidence
            "LocalProductAuthorSourceStateRestored=$local11AuthorSourceRestored $(Get-Date -Format o)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        }
        catch {
            $local11AuthorSourceRestored = $false
            $restoreErrors.Add("local11-author-source-state:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($usesOneActionConfigSmoke) {
        try {
            if ($oneActionConfigHadOriginal) {
                Copy-Item -Force -LiteralPath $oneActionConfigBackup -Destination $oneActionConfigPath
            }
            elseif (Test-Path -LiteralPath $oneActionConfigPath) {
                Remove-Item -Force -LiteralPath $oneActionConfigPath
            }
        }
        catch {
            $restoreErrors.Add("one-action-config:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($usesAutoFishingConfigSmoke) {
        try {
            if ($autoFishingConfigHadOriginal) {
                Copy-Item -Force -LiteralPath $autoFishingConfigBackup -Destination $autoFishingConfigPath
            }
            elseif (Test-Path -LiteralPath $autoFishingConfigPath) {
                Remove-Item -Force -LiteralPath $autoFishingConfigPath
            }
        }
        catch {
            $restoreErrors.Add("auto-fishing-config:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($usesActionSpeedConfigSmoke) {
        try {
            if ($actionSpeedConfigHadOriginal) {
                Copy-Item -Force -LiteralPath $actionSpeedConfigBackup -Destination $actionSpeedConfigPath
            }
            elseif (Test-Path -LiteralPath $actionSpeedConfigPath) {
                Remove-Item -Force -LiteralPath $actionSpeedConfigPath
            }
        }
        catch {
            $restoreErrors.Add("action-speed-config:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($Batch6AutoFishingManagerLifecycle) {
        try {
            if ($batch6ManagerMarkerExistedBefore) {
                Copy-Item -Force -LiteralPath $batch6ManagerMarkerBackupPath -Destination $batch6ManagerMarkerPath
            }
            elseif (Test-Path -LiteralPath $batch6ManagerMarkerPath) {
                if (-not (Test-Path -LiteralPath $batch6ManagerMarkerPath -PathType Leaf)) {
                    throw 'Marker restore refused to remove a non-file dtmapi.disabled path.'
                }
                $markerShaBeforeRemove = Get-SmokeFileSha256 -Path $batch6ManagerMarkerPath
                if (-not [string]::Equals($markerShaBeforeRemove, $Batch6AutoFishingManagerMarkerSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
                    throw 'Marker restore refused to remove a dtmapi.disabled file not created by this lifecycle run.'
                }
                Remove-Item -Force -LiteralPath $batch6ManagerMarkerPath
            }
            $markerExistsAfterRestore = Test-Path -LiteralPath $batch6ManagerMarkerPath -PathType Leaf
            $markerLengthAfterRestore = if ($markerExistsAfterRestore) { [int64](Get-Item -LiteralPath $batch6ManagerMarkerPath).Length } else { [int64]0 }
            $markerShaAfterRestore = if ($markerExistsAfterRestore) { Get-SmokeFileSha256 -Path $batch6ManagerMarkerPath } else { '' }
            $batch6ManagerMarkerRestoreOk = $markerExistsAfterRestore -eq $batch6ManagerMarkerExistedBefore -and
                $markerLengthAfterRestore -eq $batch6ManagerMarkerLengthBefore -and
                [string]::Equals($markerShaAfterRestore, $batch6ManagerMarkerSha256Before, [System.StringComparison]::OrdinalIgnoreCase)
            Write-SmokeJsonObject -Path (Join-Path $evidence 'batch6-autofishing-manager-marker-restore.json') -Value ([ordered]@{
                Path = $batch6ManagerMarkerPath
                ExpectedExists = $batch6ManagerMarkerExistedBefore
                ActualExists = $markerExistsAfterRestore
                ExpectedLength = $batch6ManagerMarkerLengthBefore
                ActualLength = $markerLengthAfterRestore
                ExpectedSha256 = $batch6ManagerMarkerSha256Before
                ActualSha256 = $markerShaAfterRestore
                Passed = $batch6ManagerMarkerRestoreOk
            })
            if (-not $batch6ManagerMarkerRestoreOk) { $restoreErrors.Add('batch6-autofishing-manager-marker:verification-failed') | Out-Null }
        }
        catch {
            $batch6ManagerMarkerRestoreOk = $false
            $restoreErrors.Add("batch6-autofishing-manager-marker:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($null -ne $qaHostStage) {
        try {
            $qaHostCleanup = Remove-SmokeQaHostStage -Stage $qaHostStage -EvidencePath $evidence
            if (-not [bool]$qaHostCleanup.Cleaned) {
                $restoreErrors.Add("qa-host-cleanup:$([string]$qaHostCleanup.Reason)") | Out-Null
            }
        }
        catch {
            $restoreErrors.Add("qa-host-cleanup:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($restoreErrors.Count -gt 0) {
        throw "Smoke state restoration failed: $([string]::Join('; ', $restoreErrors.ToArray())). Original failure: $smokeBodyFailure Evidence: $evidence"
    }
}
$noQaUiEvidenceGateOk = -not [bool]$AssertNoQaUiEvidence
if ($AssertNoQaUiEvidence) {
    $noQaRuntimePostflight = Get-SmokeNoQaRuntimeReceipt -GameDir $gameDir -StateDir $dtmapiDir
    $noQaRuntimePostflightOk = [bool]$noQaRuntimePostflight.Passed
    $noQaLegacyEvidenceComparisons = @($noQaLegacyEvidenceBefore | ForEach-Object {
        $after = Get-SmokeDirectoryReceipt -Path ([string]$_.Path)
        Compare-SmokeDirectoryReceipt -Before $_ -After $after
    })
    $noQaLegacyEvidenceUnchangedOk = $noQaLegacyEvidenceComparisons.Count -eq 3 -and
        @($noQaLegacyEvidenceComparisons | Where-Object { -not [bool]$_.Passed }).Count -eq 0
    $noQaQaActivityAbsentOk = -not [bool](Select-String `
        -LiteralPath $logPath `
        -Pattern 'QA host lifecycle|InternalFixture\.QaHost|Optional QA participant|Hook status: Smoke\.(DebugConsole|AnimalViewer|AnimalPanelUi|EquipmentSlotsUiObservation|NewContentEquipmentSlots)' `
        -ErrorAction SilentlyContinue `
        | Select-Object -First 1)
    $noQaEquipmentSlotsBoundaryCleanupOk = [bool](Select-String `
        -LiteralPath $logPath `
        -Pattern 'EquipmentSlots UI lifecycle cleared reason=ReturnedToTitle, clones=[1-9]\d*, binders=[1-9]\d*, rendered=True\.' `
        -ErrorAction SilentlyContinue `
        | Select-Object -Last 1)
    $noQaEquipmentSlotsCloseOk = $noQaEquipmentSlotsCloseOk -and $noQaEquipmentSlotsBoundaryCleanupOk
    $noQaDebugConsoleOk = $debugConsoleOpenY1Ok -and $debugConsoleCloseEscapeOk -and
        $debugConsoleOpenY2Ok -and $debugConsoleCloseYOk -and
        $debugConsoleTenYShortTapsOk -and $debugConsoleHoldYNoFlickerOk -and
        $debugConsolePostHoldCloseOk
    $noQaCleanupInputLabel = if ($Issue011Acceptance) { 'Issue011InteractiveCleanupEscape' } else { 'YHoldCleanupEscape' }
    $noQaExpectedInputLabels = @('Escape','Y1','Y2','Y3','YHold',$noQaCleanupInputLabel) + @(1..10 | ForEach-Object { "YShort$_" }) | Sort-Object
    $noQaActualInputLabels = @($externalPlayerInputAttempts.ToArray() | ForEach-Object { [string]$_.Label } | Sort-Object)
    $noQaInputProvenanceOk = $noQaActualInputLabels.Count -eq $noQaExpectedInputLabels.Count -and
        [string]::Equals((@($noQaExpectedInputLabels) -join "`n"), (@($noQaActualInputLabels) -join "`n"), [System.StringComparison]::Ordinal) -and
        @($externalPlayerInputAttempts.ToArray() | Where-Object {
            $expectedHold = if ([string]$_.Label -eq 'YHold') { 1800 } else { $ExternalPlayerInputHoldMilliseconds }
            -not [bool]$_.Sent -or -not [bool]$_.MatchedExpectedLog -or -not [bool]$_.ForegroundMatched -or
                -not [bool]$_.SendInputSucceeded -or -not [bool]$_.CompletedBeforeDeadline -or [bool]$_.PostMessageFallbackUsed -or
                [int]$_.HoldMilliseconds -ne $expectedHold
        }).Count -eq 0 -and -not $externalPlayerInputNativeMenuLeakDetected
    $noQaActualAnimalInputLabels = @($animalViewerPlayerInputAttempts.ToArray() | ForEach-Object { [string]$_.Label })
    $noQaAnimalInputSequence = @($noQaActualAnimalInputLabels) -join "`n"
    $noQaExpectedAnimalInputSequences = @(Get-SmokeNoQaAnimalInputSequences -AutoDrive ([bool]$AutoDriveNoQaAnimalViewer))
    $noQaAnimalViewerInputProvenanceOk = @($noQaExpectedAnimalInputSequences | Where-Object {
            [string]::Equals([string]$_, $noQaAnimalInputSequence, [System.StringComparison]::Ordinal)
        }).Count -eq 1 -and
        @($animalViewerPlayerInputAttempts.ToArray() | Where-Object {
            -not [bool]$_.Sent -or -not [bool]$_.ForegroundMatched -or -not [bool]$_.SendInputSucceeded -or
                -not [bool]$_.CompletedBeforeDeadline -or [bool]$_.PostMessageFallbackUsed
        }).Count -eq 0
    if ($AutoDriveNoQaAnimalViewer) {
        $noQaAnimalViewerInputProvenanceOk = $noQaAnimalViewerInputProvenanceOk -and $noQaAnimalViewerAutoDriveProvenanceOk -and
            @($animalViewerPlayerInputAttempts.ToArray() | Where-Object {
                [string]$_.Label -eq 'NoQaAnimalSelectSecondRow' -and
                [string]$_.InputKind -eq 'NormalizedMouseSendInput' -and
                [bool]$_.SetCursorPosSucceeded -and
                [Math]::Abs([double]$_.NormalizedX - 0.357) -lt 0.0001 -and
                [Math]::Abs([double]$_.NormalizedY - 0.427) -lt 0.0001
            }).Count -eq 1
    }
    $noQaUiEvidenceGateOk = $noQaRuntimePreflightOk -and $noQaRuntimePostflightOk -and
        $noQaLegacyEvidenceUnchangedOk -and $noQaQaActivityAbsentOk -and
        $noQaDebugConsoleOk -and $noQaInputProvenanceOk -and $noQaAnimalViewerUiOk -and $noQaAnimalViewerCloseOk -and $noQaAnimalViewerInputProvenanceOk -and
        $noQaEquipmentSlotsRegisterOk -and $noQaEquipmentSlotsRenderOk -and
        $noQaEquipmentSlotsInteractionOk -and $noQaEquipmentSlotsCloseOk -and $noQaEquipmentSlotsRecoveryOk -and
        $noQaBehaviorCompletedBeforeDeadlineOk
    Write-SmokeJsonObject -Path (Join-Path $evidence 'no-qa-ui-evidence-gate.json') -Value ([ordered]@{
        Requested = $true
        RuntimePreflight = $noQaRuntimePreflight
        RuntimePostflight = $noQaRuntimePostflight
        LegacyEvidenceComparisons = @($noQaLegacyEvidenceComparisons)
        LegacyEvidenceUnchanged = $noQaLegacyEvidenceUnchangedOk
        QaActivityAndAffectedSmokeStatusesAbsent = $noQaQaActivityAbsentOk
        DebugConsoleOpenUseClose = $noQaDebugConsoleOk
        DebugConsoleRealInputProvenance = $noQaInputProvenanceOk
        DebugConsoleInputAttempts = @($externalPlayerInputAttempts.ToArray())
        DebugConsoleNativeMenuLeakDetected = $externalPlayerInputNativeMenuLeakDetected
        AutoDriveNoQaAnimalViewer = [bool]$AutoDriveNoQaAnimalViewer
        AnimalViewerTwoCausalRenders = $noQaAnimalViewerUiOk
        AnimalViewerRenderReceiptCount = $noQaAnimalViewerRenderReceiptCount
        AnimalViewerClosed = $noQaAnimalViewerCloseOk
        AnimalViewerInputProvenance = $noQaAnimalViewerInputProvenanceOk
        AnimalViewerAutoDriveInputProvenance = $noQaAnimalViewerAutoDriveProvenanceOk
        AnimalViewerPlayerInputAttempts = @($animalViewerPlayerInputAttempts.ToArray())
        EquipmentSlotsRegistered = $noQaEquipmentSlotsRegisterOk
        EquipmentSlotsRendered = $noQaEquipmentSlotsRenderOk
        EquipmentSlotsInteracted = $noQaEquipmentSlotsInteractionOk
        EquipmentSlotsCloseInput = $noQaEquipmentSlotsCloseInputOk
        EquipmentSlotsDownstreamInteraction = $noQaEquipmentSlotsDownstreamInteractionOk
        EquipmentSlotsBoundaryCleanup = $noQaEquipmentSlotsBoundaryCleanupOk
        EquipmentSlotsClosed = $noQaEquipmentSlotsCloseOk
        EquipmentSlotsPlayerInputAttempts = @($equipmentSlotsPlayerInputAttempts.ToArray())
        EquipmentSlotsRecoveryNoFailure = $noQaEquipmentSlotsRecoveryOk
        NoQaBehaviorCompletedAt = if ($null -ne $noQaBehaviorCompletedAt) { $noQaBehaviorCompletedAt.ToString('o') } else { '' }
        NoQaBehaviorDeadline = if ($null -ne $noQaUiDeadline) { $noQaUiDeadline.ToString('o') } else { '' }
        NoQaBehaviorRemainingMilliseconds = $noQaBehaviorRemainingMilliseconds
        NoQaBehaviorCompletedBeforeDeadline = $noQaBehaviorCompletedBeforeDeadlineOk
        Passed = $noQaUiEvidenceGateOk
    })
}
$qaHostLifecycleOk = -not [bool]$StageQaHost
$qaHostCleanupOk = -not [bool]$StageQaHost
$qaHostMarkerCounts = [ordered]@{
    Validated = 0
    Activated = 0
    Attached = 0
    Started = 0
    Updated = 0
    Closed = 0
    Failed = 0
}
if ($StageQaHost -and $null -ne $qaHostStage) {
    $qaRunId = [string]$qaHostStage.RunId
    $qaHostLifecycleGate = Test-SmokeQaHostLifecycleLog -LogPath $logPath -RunId $qaRunId
    $qaHostMarkerCounts = $qaHostLifecycleGate.MarkerCounts
    $qaHostLifecycleOk = [bool]$qaHostLifecycleGate.Passed
    $qaHostCleanupOk = $null -ne $qaHostCleanup -and [bool]$qaHostCleanup.Cleaned
    Write-SmokeJsonObject -Path (Join-Path $evidence 'qa-host-lifecycle-gate.json') -Value ([ordered]@{
        RunId = $qaRunId
        MarkerCounts = $qaHostMarkerCounts
        MarkerLineNumbers = $qaHostLifecycleGate.MarkerLineNumbers
        AllLifecycleMarkerCount = $qaHostLifecycleGate.AllLifecycleMarkerCount
        CurrentRunLifecycleMarkerCount = $qaHostLifecycleGate.CurrentRunLifecycleMarkerCount
        AllMarkersCurrentRun = $qaHostLifecycleGate.AllMarkersCurrentRun
        Ordered = $qaHostLifecycleGate.Ordered
        ClosedDetailsPassed = $qaHostLifecycleGate.ClosedDetailsPassed
        FailedStateAbsent = $qaHostLifecycleGate.FailedStateAbsent
        LifecyclePassed = $qaHostLifecycleOk
        CleanupPassed = $qaHostCleanupOk
        Passed = $qaHostLifecycleOk -and $qaHostCleanupOk
    })
}
$titleButtonScreenshotFileOk = -not $titleSettingsRequested
$titleMenuScreenshotFileOk = -not $titleSettingsRequested
$officialModUiScreenshotFileOk = -not [bool]$AutoOpenOfficialModUi
$pauseMenuLayoutScreenshotFileOk = -not $qaG4PauseMenuLayoutEnabled
$animalViewerUiScreenshotFileOk = -not $qaG4AnimalObservationEnabled
$equipmentSlotsUiScreenshotFileOk = -not $qaG4EquipmentSlotsObservationEnabled
$debugConsoleScreenshotFileOk = -not $qaG4DebugConsoleEnabled
$saveSlotsUiScreenshotFileOk = -not $qaG4SaveSlotsPagingEnabled
$cameraPlayableEvidenceFilesOk = -not $qaG4CameraPlayableEnabled
$newContentMineOfficialTechTreeUiScreenshotFileOk = -not [bool]$AutoExerciseMineContentApis
$smokeRootIsolationRuntimeLine = $null
$smokeRootIsolationSummaryLine = $null
$ownerRootTypeIsolationRuntimeLine = $null
$ownerRootTypeIsolationSummaryLine = ''
$nativeContinuationProbeRuntimeLine = $null
$nativeContinuationProbeSummaryLine = ''
$nativeContinuationLastLine = ''
$nativeContinuationProbeCleanupOk = $SmokeNativeLoadContinuationProbe -ne 'VersionPatcher'
$ownerLifetimeCloseCleanupOk = -not [bool]$StageQaHost
$ownerLifetimeCloseCleanupSummary = ''
if ($titleSettingsRequested -and (Test-Path $logPath)) {
    $buttonScreenshotLine = Select-String -Path $logPath -Pattern 'Title settings button screenshot OK screenshot=' | Select-Object -Last 1
    if ($buttonScreenshotLine) {
        $buttonScreenshotPath = $buttonScreenshotLine.Line -replace '^.*screenshot=', '' -replace '\.$', ''
        $titleButtonScreenshotFileOk = Test-Path -LiteralPath $buttonScreenshotPath
    }
    $menuScreenshotLine = Select-String -Path $logPath -Pattern 'Title settings menu screenshot OK screenshot=' | Select-Object -Last 1
    if ($menuScreenshotLine) {
        $menuScreenshotPath = $menuScreenshotLine.Line -replace '^.*screenshot=', '' -replace '\.$', ''
        $titleMenuScreenshotFileOk = Test-Path -LiteralPath $menuScreenshotPath
    }
    if ($managerStatusRequested) {
        $statusScreenshotLine = Select-String -Path $logPath -Pattern 'Manager Status page screenshot OK screenshot=' | Select-Object -Last 1
        if ($statusScreenshotLine) {
            $statusScreenshotPath = $statusScreenshotLine.Line -replace '^.*screenshot=', '' -replace '\.$', ''
            $managerStatusPageScreenshotFileOk = Test-Path -LiteralPath $statusScreenshotPath
        }
    }
    if ($managerMvpRequested) {
        $logsScreenshotLine = Select-String -Path $logPath -Pattern 'Manager Logs page screenshot OK screenshot=' | Select-Object -Last 1
        if ($logsScreenshotLine) {
            $logsScreenshotPath = $logsScreenshotLine.Line -replace '^.*screenshot=', '' -replace '\.$', ''
            $managerLogsPageScreenshotFileOk = Test-Path -LiteralPath $logsScreenshotPath
        }
    }
}
if ($AutoOpenOfficialModUi -and (Test-Path $logPath)) {
    $officialScreenshotLine = Select-String -Path $logPath -Pattern 'Official Mod UI evidence OK .*screenshot=' | Select-Object -Last 1
    if ($officialScreenshotLine) {
        $officialScreenshotPath = $officialScreenshotLine.Line -replace '^.*screenshot=', '' -replace '\.$', ''
        $officialModUiScreenshotFileOk = Test-Path -LiteralPath $officialScreenshotPath
    }
}
$qaG4ArchivedEvidenceRoot = Join-Path $evidence 'qa-host\g4'
if ($qaG4TitleSettingsUiEnabled) {
    $titleSettingsQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\title-settings.png'
    $titleSettingsQaScreenshotOk = (Test-Path -LiteralPath $titleSettingsQaScreenshot -PathType Leaf) -and (Get-Item -LiteralPath $titleSettingsQaScreenshot).Length -gt 0
    $titleButtonScreenshotFileOk = $titleSettingsQaScreenshotOk
    $titleMenuScreenshotFileOk = $titleSettingsQaScreenshotOk
}
if ($qaG4ManagerStatusUiEnabled -or $qaG4ManagerMvpUiEnabled) {
    $managerStatusQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\manager-status-page.png'
    $managerStatusQaScreenshotOk = (Test-Path -LiteralPath $managerStatusQaScreenshot -PathType Leaf) -and (Get-Item -LiteralPath $managerStatusQaScreenshot).Length -gt 0
    $titleButtonScreenshotFileOk = $managerStatusQaScreenshotOk
    $titleMenuScreenshotFileOk = $managerStatusQaScreenshotOk
    $managerStatusPageScreenshotFileOk = $managerStatusQaScreenshotOk
}
if ($qaG4ManagerMvpUiEnabled) {
    $managerModsQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\manager-mods-page.png'
    $managerModsPageScreenshotFileOk = (Test-Path -LiteralPath $managerModsQaScreenshot -PathType Leaf) -and (Get-Item -LiteralPath $managerModsQaScreenshot).Length -gt 0
    $managerAdvancedQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\manager-advanced-page.png'
    $managerAdvancedPageScreenshotFileOk = (Test-Path -LiteralPath $managerAdvancedQaScreenshot -PathType Leaf) -and (Get-Item -LiteralPath $managerAdvancedQaScreenshot).Length -gt 0
    $managerLogsQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\manager-logs-page.png'
    $managerLogsPageScreenshotFileOk = (Test-Path -LiteralPath $managerLogsQaScreenshot -PathType Leaf) -and (Get-Item -LiteralPath $managerLogsQaScreenshot).Length -gt 0
}
if ($qaG4OfficialModUiEnabled) {
    $officialQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\official-mod-ui.png'
    $officialModUiScreenshotFileOk = (Test-Path -LiteralPath $officialQaScreenshot -PathType Leaf) -and (Get-Item -LiteralPath $officialQaScreenshot).Length -gt 0
}
if ($qaG4PauseMenuLayoutEnabled) {
    $pauseQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\pause-menu-layout.png'
    $pauseMenuLayoutScreenshotFileOk = (Test-Path -LiteralPath $pauseQaScreenshot -PathType Leaf) -and (Get-Item -LiteralPath $pauseQaScreenshot).Length -gt 0
}
if ($qaG4SaveSlotsPagingEnabled) {
    $saveSlotsQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\official-save-ui.png'
    $saveSlotsPostTitleQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\official-save-ui-post-title.png'
    $saveSlotsUiScreenshotFileOk =
        (Test-Path -LiteralPath $saveSlotsQaScreenshot -PathType Leaf) -and
        (Get-Item -LiteralPath $saveSlotsQaScreenshot).Length -gt 0 -and (
            -not $qaG4MoreSavesPostTitlePanelEnabled -or (
                (Test-Path -LiteralPath $saveSlotsPostTitleQaScreenshot -PathType Leaf) -and
                (Get-Item -LiteralPath $saveSlotsPostTitleQaScreenshot).Length -gt 0
            )
        )
}
if ($qaG4AnimalObservationEnabled) {
    $animalQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\animal-panel.png'
    $animalViewerUiScreenshotFileOk = (Test-Path -LiteralPath $animalQaScreenshot -PathType Leaf) -and (Get-Item -LiteralPath $animalQaScreenshot).Length -gt 0
}
if ($qaG4EquipmentSlotsObservationEnabled) {
    $equipmentSlotsQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\equipment-slots.png'
    $equipmentSlotsUiScreenshotFileOk = (Test-Path -LiteralPath $equipmentSlotsQaScreenshot -PathType Leaf) -and (Get-Item -LiteralPath $equipmentSlotsQaScreenshot).Length -gt 0
}
if ($qaG4DebugConsoleEnabled) {
    $debugQaScreenshot = Join-Path $qaG4ArchivedEvidenceRoot 'ui\debug-console.png'
    $debugConsoleScreenshotFileOk = (Test-Path -LiteralPath $debugQaScreenshot -PathType Leaf) -and (Get-Item -LiteralPath $debugQaScreenshot).Length -gt 0
}
if ($qaG4CameraPlayableEnabled) {
    $cameraQaRoot = Join-Path $qaG4ArchivedEvidenceRoot 'camera-playable'
    $cameraExpectedFiles = @('before.png','scale-4x.png','movement-start.png','movement-mid.png','movement-end.png','fallback-2x.png','reset.png','telemetry.csv')
    $cameraPlayableEvidenceFilesOk = @($cameraExpectedFiles | Where-Object {
        $path = Join-Path $cameraQaRoot $_
        -not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -le 0
    }).Count -eq 0
}
if ($AutoExerciseMineContentApis -and (Test-Path $logPath)) {
    $mineTechTreeScreenshotLine = Select-String -Path $logPath -Pattern 'Mine official tech tree UI evidence OK .*screenshot=' | Select-Object -Last 1
    if ($mineTechTreeScreenshotLine) {
        $mineTechTreeScreenshotPath = $mineTechTreeScreenshotLine.Line -replace '^.*screenshot=', '' -replace ', close=.*$', '' -replace '\.$', ''
        $newContentMineOfficialTechTreeUiScreenshotFileOk = Test-Path -LiteralPath $mineTechTreeScreenshotPath
    }
}
$refactorFlagsSummary = ''
$refactorFlagsOk = $false
$refactorLifecycleObservationRequested = $true
$refactorShadowContentRegistryRequested = $true
$refactorLifecycleBoundaryContractRequested = $true
$resourceLifecycleLedgerRequested = $true
$resourceLifecycleCleanupRequested = $true
$saveLoadRequestCoordinatorRequested = $true
$hookInstallSchedulerRequested = $true
$hookReadinessLayersRequested = $true
$hookStatusQueueRequested = $true
$eventMainThreadBoundaryRequested = $true
$modOwnerLifecycleRequested = $true
$modLoadTransactionRequested = $true
$ownerBoundInputRequested = $true
$eventHandlerCleanupRequested = $true
$configPreviewAuditRequested = $true
$gameBridgeFeatureContractsRequested = $true
$gameBridgeFinalHealthSnapshotRequested = $true
$contentManifestRegistryRequested = $true
$refactorLifecycleObservationOk = $false
$refactorShadowContentRegistryOk = $false
$contentRegistryOk = $false
$manifestRegistryOk = $false
$dependencyCompatibilityOk = $false
$contentPackOwnershipOk = $false
$registryDiffsOk = $false
$refactorLifecycleBoundaryContractOk = $false
$refactorLifecycleBoundaryContractSummary = ''
$resourceLifecycleLedgerOk = $false
$resourceLifecycleCleanupOk = $false
$resourceLifecycleSummary = ''
$titleIdleResourceGrowthOk = $true
$saveLoadRequestCoordinatorOk = $false
$saveLoadRequestSummary = ''
$duplicateLoadRequestsOk = $true
$saveLoadBoundaryOk = $false
$titleReturnBoundaryRequested = $saveLoadedRequested -or [bool]$AutoExerciseTitleButtonLifecycle -or [bool]$AutoExerciseSaveLoadCycle
$titleReturnBoundaryOk = -not $titleReturnBoundaryRequested
$titleReturnBoundarySummary = ''
$saveLoadCycleObjectDeltaLedgerRequested = $titleReturnBoundaryRequested
$saveLoadCycleObjectDeltaLedgerOk = -not $saveLoadCycleObjectDeltaLedgerRequested
$saveLoadCycleObjectDeltaLedgerSummary = ''
$hookSchedulerOk = $false
$coreHookReadinessOk = $false
$featureHookReadinessOk = $false
$hookStatusQueueOk = $false
$offThreadHookRequestsOk = $false
$assemblyLoadSubscriptionOk = $false
$retryTimerAliveOk = $false
$modOwnerLifecycleOk = $false
$modOwnerLifecycleSummary = ''
$modLoadTransactionOk = $false
$ownerBoundInputOk = $false
$eventHandlerCleanupOk = $false
$configPreviewAuditOk = $false
$failedModRollbackOk = $true
$gameBridgeFeatureContractsOk = $false
$gameBridgeFinalHealthSnapshotOk = $false
$gameBridgeGeneralHealthOk = $false
$gameBridgeFinalHealthSummary = ''
$productOwnerRefreshRequested = [bool]$AssertProductOwnerRefresh
$productOwnerRefreshOk = -not $productOwnerRefreshRequested
$productOwnerRefreshCounts = [ordered]@{}
$productOwnerRefreshReloadDispatchCount = 0
$productOwnerRefreshHotStatusCount = 0
$productOwnerRefreshDependencyOk = $false
$productOwnerRefreshCleanupOk = $false
$productOwnerRefreshOrchestrationOk = $false
$advancedProductOwnerDeactivationRequested = [bool]$AssertAdvancedProductOwnerDeactivation
$advancedProductOwnerDeactivationSummary = ''
$publishedProductGateRequested = [bool]$AssertPublishedProductCombination -or [bool]$AssertPublishedProductsDisabled
$publishedProductGateMode = if ($AssertPublishedProductCombination) { 'Enabled11' } elseif ($AssertPublishedProductsDisabled) { 'Disabled11' } else { 'NotRequested' }
$publishedProductGateOk = -not $publishedProductGateRequested
$publishedProductOwnerCounts = [ordered]@{}
$publishedProductHotRefreshCount = 0
$publishedProductDependencyOk = -not $publishedProductGateRequested
$publishedProductCleanupOk = -not $publishedProductGateRequested
$publishedProductExactOwnerSetOk = -not $publishedProductGateRequested
$publishedProductObservedCodeOwners = @()
$publishedProductProvenanceOk = $true
$publishedProductOrderOk = -not $publishedProductGateRequested
$hookProbeAbsenceRequested = [bool]$RequireExternalPlayerInputGate -or $publishedProductGateRequested -or [bool]$AssertNoQaUiEvidence
$hookProbeAbsenceOk = -not $hookProbeAbsenceRequested
$externalPlayerInputGateOk = -not [bool]$RequireExternalPlayerInputGate
$externalPlayerInputAllAttemptsOk = -not [bool]$RequireExternalPlayerInputGate
$externalPlayerInputProvenanceOk = -not [bool]$RequireExternalPlayerInputGate
$externalPlayerInputOrderOk = -not [bool]$RequireExternalPlayerInputGate
$externalPlayerInputModalIsolationOk = -not [bool]$RequireExternalPlayerInputGate
$externalPlayerInputModalYCloseDispatchCount = 0
$externalPlayerInputTypedModalYCount = 0
$externalPlayerInputLegacyModalYCount = 0
$externalPlayerInputLegacyOwnerDispatchCount = 0
$externalPlayerInputLegacyModToggleCloseCount = 0
$externalPlayerInputModalEscapeCloseCount = 0
$missingFrameFallbackWarningCount = 0
$frameDriverStallWarningCount = 0
$inputSystemResubscribeCount = 0
$playerLoopLifecycleRefreshCount = 0
if (Test-Path $logPath) {
    $missingFrameFallbackWarningCount = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'DTMAPI fallback pump detected a missing frame callback;' -ErrorAction SilentlyContinue).Count
    $frameDriverStallWarningCount = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'DTMAPI Input System frame driver stalled;' -ErrorAction SilentlyContinue).Count
    $inputSystemResubscribeCount = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'DTMAPI per-frame driver subscribed to InputSystem.onAfterUpdate.' -ErrorAction SilentlyContinue).Count
    $playerLoopLifecycleRefreshCount = @(Select-String -LiteralPath $logPath -Pattern 'DTMAPI PlayerLoop frame driver refreshed at lifecycle boundary .* installed=True\.' -ErrorAction SilentlyContinue).Count
    $refactorFlagsLine = Select-String -Path $logPath -Pattern 'Refactor scaffold flags:' | Select-Object -Last 1
    if ($refactorFlagsLine) {
        $refactorFlagsOk = $true
        $refactorFlagsSummary = $refactorFlagsLine.Line
        $refactorLifecycleObservationRequested = -not ($refactorFlagsSummary -match 'LifecycleObservation=false')
        $refactorShadowContentRegistryRequested = -not ($refactorFlagsSummary -match 'ShadowContentRegistry=false')
        $refactorLifecycleBoundaryContractRequested = $refactorLifecycleObservationRequested
        $resourceLifecycleLedgerRequested = -not ($refactorFlagsSummary -match 'ResourceLifecycleLedger=false')
        $resourceLifecycleCleanupRequested = $resourceLifecycleLedgerRequested -and -not ($refactorFlagsSummary -match 'ResourceLifecycleCleanup=false')
        $saveLoadRequestCoordinatorRequested = -not ($refactorFlagsSummary -match 'SaveLoadRequestCoordinator=false')
        $hookInstallSchedulerRequested = -not ($refactorFlagsSummary -match 'HookInstallScheduler=false')
        $hookReadinessLayersRequested = -not ($refactorFlagsSummary -match 'HookReadinessLayers=false')
        $hookStatusQueueRequested = -not ($refactorFlagsSummary -match 'HookStatusQueue=false')
        $eventMainThreadBoundaryRequested = -not ($refactorFlagsSummary -match 'EventMainThreadBoundary=false')
        $modOwnerLifecycleRequested = -not ($refactorFlagsSummary -match 'ModOwnerLedger=false')
        $modLoadTransactionRequested = $modOwnerLifecycleRequested -and -not ($refactorFlagsSummary -match 'ModLoadTransaction=false')
        $ownerBoundInputRequested = -not ($refactorFlagsSummary -match 'OwnerBoundInput=false')
        $eventHandlerCleanupRequested = -not ($refactorFlagsSummary -match 'EventHandlerQuarantine=false')
        $configPreviewAuditRequested = $modOwnerLifecycleRequested -and -not ($refactorFlagsSummary -match 'ConfigPreviewAudit=false')
        $gameBridgeFeatureContractsRequested = -not ($refactorFlagsSummary -match 'GameBridgeFeatureContracts=false')
        $gameBridgeFinalHealthSnapshotRequested = -not ($refactorFlagsSummary -match 'GameBridgeFinalHealthSnapshot=false')
        $contentManifestRegistryRequested = -not ($refactorFlagsSummary -match 'ContentManifestRegistry=false')
    }
    $refactorLifecycleObservationOk = (-not $refactorLifecycleObservationRequested) -or [bool](Select-String -Path $logPath -Pattern 'Refactor lifecycle observation phase=Startup' | Select-Object -First 1)
    $refactorShadowContentRegistryOk = (-not $refactorShadowContentRegistryRequested) -or [bool](Select-String -Path $logPath -Pattern 'Refactor shadow content registry:' | Select-Object -First 1)
    $contentRegistryLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.ContentRegistry = ' | Select-Object -Last 1
    $contentRegistryOk = (-not $contentManifestRegistryRequested) -or ([bool]$contentRegistryLine -and -not ($contentRegistryLine.Line -match '= (warning|error|failed)'))
    $manifestRegistryLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.ManifestRegistry = ' | Select-Object -Last 1
    $manifestRegistryOk = (-not $contentManifestRegistryRequested) -or ([bool]$manifestRegistryLine -and -not ($manifestRegistryLine.Line -match '= (error|failed)'))
    $dependencyCompatibilityLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.DependencyCompatibility = ' | Select-Object -Last 1
    $dependencyCompatibilityOk = (-not $contentManifestRegistryRequested) -or ([bool]$dependencyCompatibilityLine -and -not ($dependencyCompatibilityLine.Line -match '= (error|failed)'))
    $contentPackOwnershipLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.ContentPackOwnership = ' | Select-Object -Last 1
    $contentPackOwnershipOk = (-not $contentManifestRegistryRequested) -or ([bool]$contentPackOwnershipLine -and -not ($contentPackOwnershipLine.Line -match '= (warning|error|failed)|ownerlessRows=[1-9]'))
    $registryDiffsLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.RegistryDiffs = ' | Select-Object -Last 1
    $registryDiffsOk = (-not $contentManifestRegistryRequested) -or ([bool]$registryDiffsLine -and $registryDiffsLine.Line -match '= ok' -and -not ($registryDiffsLine.Line -match 'diffs=[1-9]'))
    $refactorLifecycleBoundaryContractLine = Select-String -Path $logPath -Pattern 'Refactor lifecycle boundary contract status=' | Select-Object -Last 1
    if ($refactorLifecycleBoundaryContractLine) {
        $refactorLifecycleBoundaryContractSummary = $refactorLifecycleBoundaryContractLine.Line
    }
    $refactorLifecycleBoundaryContractHasError = [bool](Select-String -Path $logPath -Pattern 'Refactor lifecycle boundary contract status=error' | Select-Object -First 1)
    $refactorLifecycleBoundaryContractOk = (-not $refactorLifecycleBoundaryContractRequested) -or ([bool]$refactorLifecycleBoundaryContractLine -and -not $refactorLifecycleBoundaryContractHasError)
    $resourceLifecycleLedgerLine = Select-String -Path $logPath -Pattern 'Refactor resource lifecycle ledger status=' | Select-Object -Last 1
    if ($resourceLifecycleLedgerLine) {
        $resourceLifecycleSummary = $resourceLifecycleLedgerLine.Line
    }
    $resourceLifecycleLedgerHasError = [bool](Select-String -Path $logPath -Pattern 'Refactor resource lifecycle ledger status=error' | Select-Object -First 1)
    $resourceLifecycleLedgerOk = (-not $resourceLifecycleLedgerRequested) -or ([bool]$resourceLifecycleLedgerLine -and -not $resourceLifecycleLedgerHasError)
    $resourceLifecycleCleanupLine = Select-String -Path $logPath -Pattern 'Refactor resource lifecycle ledger status=.*operation=Cleanup:' | Select-Object -Last 1
    $resourceLifecycleCleanupOk = (-not $resourceLifecycleCleanupRequested) -or [bool]$resourceLifecycleCleanupLine
    $titleIdleResourceGrowthOk = -not [bool](Select-String -Path $logPath -Pattern 'Resource lifecycle ledger diagnostic.*Resource acquisition repeated during title idle|titleIdleGrowthWarnings=[1-9]' | Select-Object -First 1)
    $saveLoadRequestCoordinatorLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.SaveLoadRequestCoordinator = |Refactor save-load request coordinator status=' | Select-Object -Last 1
    $saveLoadRequestCoordinatorIdleLine = $null
    if (-not $saveLoadedRequested) {
        $saveLoadRequestCoordinatorIdleLine = Select-String -Path $logPath -Pattern 'GameBridgeFinalHealthSnapshot = ok\..*saveLoad=\{status=ok; requests=0; active=none; duplicateRequests=0;|GameBridge final health snapshot reason=.*saveLoad=\{status=ok; requests=0; active=none; duplicateRequests=0;' | Select-Object -Last 1
    }
    if ($saveLoadRequestCoordinatorLine) {
        $saveLoadRequestSummary = $saveLoadRequestCoordinatorLine.Line
    }
    elseif ($saveLoadRequestCoordinatorIdleLine) {
        $saveLoadRequestSummary = 'Idle coordinator verified by final-health snapshot: ' + $saveLoadRequestCoordinatorIdleLine.Line
    }
    $saveLoadCoordinatorWarning = [bool](Select-String -Path $logPath -Pattern 'Hook status: Refactor\.SaveLoadRequestCoordinator = warning|Refactor save-load request coordinator status=warning' | Select-Object -First 1)
    $saveLoadRequestCoordinatorOk = (-not $saveLoadRequestCoordinatorRequested) -or (([bool]$saveLoadRequestCoordinatorLine -or [bool]$saveLoadRequestCoordinatorIdleLine) -and -not $saveLoadCoordinatorWarning)
    $duplicateLoadWarning = [bool](Select-String -Path $logPath -Pattern 'Hook status: Refactor\.DuplicateLoadRequests = warning' | Select-Object -First 1)
    $duplicateLoadRequestsOk = (-not $saveLoadRequestCoordinatorRequested) -or -not $duplicateLoadWarning
    $saveLoadBoundaryLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.SaveLoadBoundary = ' | Select-Object -Last 1
    $saveLoadBoundaryOk = (-not $saveLoadRequestCoordinatorRequested) -or (-not $saveLoadedRequested) -or ([bool]$saveLoadBoundaryLine -and $saveLoadBoundaryLine.Line -match '= closed')
    $titleReturnBoundaryLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.TitleReturnBoundaryLedger = |TitleReturn boundary ledger operation=' | Select-Object -Last 1
    if ($titleReturnBoundaryLine) {
        $titleReturnBoundarySummary = $titleReturnBoundaryLine.Line
    }
    $titleReturnBoundaryOk = (-not $titleReturnBoundaryRequested) -or [bool]$titleReturnBoundaryLine
    $saveLoadCycleObjectDeltaLedgerLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.SaveLoadCycleObjectDeltaLedger = |SaveLoad cycle object delta ' | Select-Object -Last 1
    if ($saveLoadCycleObjectDeltaLedgerLine) {
        $saveLoadCycleObjectDeltaLedgerSummary = $saveLoadCycleObjectDeltaLedgerLine.Line
    }
    $saveLoadCycleObjectDeltaLedgerOk = (-not $saveLoadCycleObjectDeltaLedgerRequested) -or [bool]$saveLoadCycleObjectDeltaLedgerLine
    $preLoadGcProbeLine = Select-String -Path $logPath -Pattern 'Hook status: Smoke\.PreLoadForcedGCProbe = ' | Select-Object -Last 1
    if ($preLoadGcProbeLine) {
        $preLoadGcProbeSummary = $preLoadGcProbeLine.Line
    }
    $preLoadGcProbeOk = (-not $preLoadGcProbeRequested) -or ([bool]$preLoadGcProbeLine -and $preLoadGcProbeLine.Line -match '= verified')
    $ownerLifetimeLine = Select-String -Path $logPath -Pattern 'Hook status: Smoke\.OwnerLifetime = ' | Select-Object -Last 1
    $ownerLifetimeOk = (-not [bool]$AutoExerciseModOwnerLifetime) -or ([bool]$ownerLifetimeLine -and $ownerLifetimeLine.Line -match '= verified')
    $hookSchedulerLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.HookInstallScheduler = ' | Select-Object -Last 1
    $hookSchedulerOk = (-not $hookInstallSchedulerRequested) -or ([bool]$hookSchedulerLine -and -not ($hookSchedulerLine.Line -match '= (failed|error)|dropped=[1-9]'))
    $coreHookReadinessLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.CoreHookReadiness = ' | Select-Object -Last 1
    $coreHookReadinessOk = (-not $hookReadinessLayersRequested) -or ([bool]$coreHookReadinessLine -and $coreHookReadinessLine.Line -match '= ready')
    $featureHookReadinessLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.FeatureHookReadiness = ' | Select-Object -Last 1
    $featureHookReadinessOk = (-not $hookReadinessLayersRequested) -or ([bool]$featureHookReadinessLine -and $featureHookReadinessLine.Line -match '= (ready|partial)')
    $hookStatusQueueLine = Select-String -Path $logPath -Pattern 'Refactor hook status queue flushed|HookStatusQueue:' | Select-Object -Last 1
    $hookStatusQueueDropped = [bool](Select-String -Path $logPath -Pattern 'Hook status publication queue dropped entries|dropped=[1-9]' | Select-Object -First 1)
    $hookStatusQueueOk = (-not $hookStatusQueueRequested) -or ([bool]$hookStatusQueueLine -and -not $hookStatusQueueDropped)
    $offThreadHookRequestsLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.OffThreadHookRequests = ' | Select-Object -Last 1
    $offThreadDirectPatch = [bool](Select-String -Path $logPath -Pattern 'off-thread direct patch|direct patch from non-runtime thread' | Select-Object -First 1)
    $offThreadHookRequestsOk = (-not $hookInstallSchedulerRequested) -or ([bool]$offThreadHookRequestsLine -and -not $offThreadDirectPatch)
    $assemblyLoadSubscriptionLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.AssemblyLoadSubscription = ' | Select-Object -Last 1
    $assemblyLoadSubscriptionOk = (-not $hookInstallSchedulerRequested) -or ([bool]$assemblyLoadSubscriptionLine -and $assemblyLoadSubscriptionLine.Line -match '= released')
    $retryTimerAliveLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.RetryTimerAlive = ' | Select-Object -Last 1
    $retryTimerAliveOk = (-not $hookInstallSchedulerRequested) -or ([bool]$retryTimerAliveLine -and $retryTimerAliveLine.Line -match '= released')
    $modOwnerLifecycleLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.ModOwnerLifecycle = ' | Select-Object -Last 1
    if ($modOwnerLifecycleLine) {
        $modOwnerLifecycleSummary = $modOwnerLifecycleLine.Line
    }
    $modOwnerLifecycleOk = (-not $modOwnerLifecycleRequested) -or ([bool]$modOwnerLifecycleLine -and -not ($modOwnerLifecycleLine.Line -match '= (warning|error|failed)'))
    $modLoadTransactionLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.ModLoadTransaction = ' | Select-Object -Last 1
    $modLoadTransactionOk = (-not $modLoadTransactionRequested) -or ([bool]$modLoadTransactionLine -and $modLoadTransactionLine.Line -match '= closed')
    $ownerBoundInputLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.OwnerBoundInput = ' | Select-Object -Last 1
    $ownerBoundInputOk = (-not $ownerBoundInputRequested) -or ([bool]$ownerBoundInputLine -and $ownerBoundInputLine.Line -match '= ok')
    $eventHandlerCleanupLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.EventHandlerCleanup = ' | Select-Object -Last 1
    $eventHandlerCleanupOk = (-not $eventHandlerCleanupRequested) -or ([bool]$eventHandlerCleanupLine -and $eventHandlerCleanupLine.Line -match '= ok')
    $configPreviewAuditLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.ConfigPreviewAudit = ' | Select-Object -Last 1
    $configPreviewAuditOk = (-not $configPreviewAuditRequested) -or ([bool]$configPreviewAuditLine -and $configPreviewAuditLine.Line -match '= observing')
    $failedModRollbackLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.FailedModRollback = ' | Select-Object -Last 1
    $failedModRollbackOk = (-not $modOwnerLifecycleRequested) -or ([bool]$failedModRollbackLine -and -not ($failedModRollbackLine.Line -match '= warning|cleanupFailures=[1-9]'))
    $gameBridgeFeatureContractsLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.GameBridgeFeatureContracts = ' | Select-Object -Last 1
    $gameBridgeFeatureContractsOk = (-not $gameBridgeFeatureContractsRequested) -or ([bool]$gameBridgeFeatureContractsLine -and -not ($gameBridgeFeatureContractsLine.Line -match '= (warning|error|failed)'))
    # Prefer the unbounded runtime-monitor receipt. The HookStatus mirror is intentionally
    # length-bounded and can truncate the trailing disposeGraph fields used by the gates below.
    $gameBridgeFinalHealthLine = Select-String -Path $logPath -Pattern 'GameBridge final health snapshot reason=' | Select-Object -Last 1
    if (-not $gameBridgeFinalHealthLine) {
        $gameBridgeFinalHealthLine = Select-String -Path $logPath -Pattern 'Hook status: Refactor\.GameBridgeFinalHealthSnapshot = ' | Select-Object -Last 1
    }
    if ($gameBridgeFinalHealthLine) {
        $gameBridgeFinalHealthSummary = $gameBridgeFinalHealthLine.Line
    }
    $gameBridgeGeneralHealthOk = [bool]$gameBridgeFinalHealthLine -and
        -not ($gameBridgeFinalHealthLine.Line -match '= warning|needsRestart=[1-9]|cleanupFailures=[1-9]|resourceErrors=[1-9]')
    $gameBridgeFinalHealthSnapshotOk = (-not $gameBridgeFinalHealthSnapshotRequested) -or
        $gameBridgeGeneralHealthOk
    $smokeRootIsolationRuntimeLine = Select-String -Path $logPath -Pattern 'SmokeRootIsolationProfile=|Hook status: Smoke\.RootIsolation = ' | Select-Object -Last 1
    if ($smokeRootIsolationRuntimeLine) {
        $smokeRootIsolationSummaryLine = $smokeRootIsolationRuntimeLine.Line
        "RuntimeEvidence=$smokeRootIsolationSummaryLine" | Add-Content -LiteralPath $smokeRootIsolationSummaryPath
        "SmokeRootIsolationRuntimeEvidence=$smokeRootIsolationSummaryLine" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    }
    $ownerRootTypeIsolationRuntimeLine = Select-String -Path $logPath -Pattern 'SmokeOwnerRootIsolationProfile=|Hook status: Smoke\.OwnerRootIsolation = |Smoke owner root isolation profile=' | Select-Object -Last 1
    if ($ownerRootTypeIsolationRuntimeLine) {
        $ownerRootTypeIsolationSummaryLine = $ownerRootTypeIsolationRuntimeLine.Line
        "RuntimeEvidence=$ownerRootTypeIsolationSummaryLine" | Add-Content -LiteralPath $ownerRootTypeIsolationSummaryPath
        "SmokeOwnerRootIsolationRuntimeEvidence=$ownerRootTypeIsolationSummaryLine" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        if ($ownerRootTypeIsolationSummaryLine -match 'suppressionSucceeded=([^; .]+)') {
            "SuppressionSucceeded=$($Matches[1])" | Add-Content -LiteralPath $ownerRootTypeIsolationSummaryPath
        }
        if ($ownerRootTypeIsolationSummaryLine -match 'unexpectedRemainingSuppressedRoots=([^; .]+)') {
            "UnexpectedRemainingSuppressedRoots=$($Matches[1])" | Add-Content -LiteralPath $ownerRootTypeIsolationSummaryPath
        }
        if ($ownerRootTypeIsolationSummaryLine -match 'unexpectedDisabledCodeOwners=([^; .]+)') {
            "UnexpectedDisabledCodeOwners=$($Matches[1])" | Add-Content -LiteralPath $ownerRootTypeIsolationSummaryPath
        }
    }
    $nativeContinuationProbeRuntimeLine = Select-String -Path $logPath -Pattern 'SmokeNativeLoadContinuationProbe=|Hook status: Smoke\.NativeLoadContinuationProbe = ' | Select-Object -Last 1
    if ($nativeContinuationProbeRuntimeLine) {
        $nativeContinuationProbeSummaryLine = $nativeContinuationProbeRuntimeLine.Line
        "RuntimeEvidence=$nativeContinuationProbeSummaryLine" | Add-Content -LiteralPath $nativeContinuationProbeSummaryPath
        "SmokeNativeLoadContinuationRuntimeEvidence=$nativeContinuationProbeSummaryLine" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        if ($SmokeNativeLoadContinuationProbe -eq 'VersionPatcher' -and $null -ne $qaHostStage) {
            $expectedQaHarmonyOwner = 'dtmapi.gamebridge.doloctown.qa.native-continuation.' + [string]$qaHostStage.RunId
            $nativeContinuationProbeCleanupOk =
                $nativeContinuationProbeSummaryLine -match 'Hook status: Smoke\.NativeLoadContinuationProbe = closed\.' -and
                $nativeContinuationProbeSummaryLine -match ('ownerId=' + [regex]::Escape($expectedQaHarmonyOwner) + ';') -and
                $nativeContinuationProbeSummaryLine -match 'unpatchSucceeded=True;' -and
                $nativeContinuationProbeSummaryLine -match 'installedBefore=afterLoad=installed; loadAll=installed; loadBeyond=installed; mapInit=installed;'
            "CleanupPassed=$nativeContinuationProbeCleanupOk" | Add-Content -LiteralPath $nativeContinuationProbeSummaryPath
            "ExpectedQaHarmonyOwner=$expectedQaHarmonyOwner" | Add-Content -LiteralPath $nativeContinuationProbeSummaryPath
        }
    }
    $ownerLifetimeCloseCleanupLine = Select-String -Path $logPath -Pattern 'Hook status: Smoke\.OwnerLifetimeCleanup = ' | Select-Object -Last 1
    if ($ownerLifetimeCloseCleanupLine) {
        $ownerLifetimeCloseCleanupSummary = $ownerLifetimeCloseCleanupLine.Line
        if ($StageQaHost) {
            $ownerLifetimeCloseCleanupOk =
                $ownerLifetimeCloseCleanupSummary -match 'Hook status: Smoke\.OwnerLifetimeCleanup = verified\.' -and
                $ownerLifetimeCloseCleanupSummary -match 'controlRoots=0;' -and
                $ownerLifetimeCloseCleanupSummary -match 'cleanupRoots=0;' -and
                $ownerLifetimeCloseCleanupSummary -match 'controlInstance=False;' -and
                $ownerLifetimeCloseCleanupSummary -match 'cleanupInstance=False;' -and
                $ownerLifetimeCloseCleanupSummary -match 'controlCameraLeases=0;' -and
                $ownerLifetimeCloseCleanupSummary -match 'cleanupCameraLeases=0;' -and
                $ownerLifetimeCloseCleanupSummary -match 'failures=none;'
        }
    }
    $nativeContinuationLastMatch = Select-String -Path $logPath -Pattern 'NativeContinuation\.Step=' | Select-Object -Last 1
    if ($nativeContinuationLastMatch) {
        $nativeContinuationLastLine = $nativeContinuationLastMatch.Line
        "LastNativeContinuationLine=$nativeContinuationLastLine" | Add-Content -LiteralPath $nativeContinuationProbeSummaryPath
        "LastNativeContinuationLine=$nativeContinuationLastLine" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        if ($nativeContinuationLastLine -match 'NativeContinuation\.Step=([^ ]+)') {
            "LastNativeContinuationStep=$($Matches[1])" | Add-Content -LiteralPath $nativeContinuationProbeSummaryPath
        }
        if ($nativeContinuationLastLine -match 'method=([^ ]+)') {
            "LastNativeContinuationMethod=$($Matches[1])" | Add-Content -LiteralPath $nativeContinuationProbeSummaryPath
        }
        if ($nativeContinuationLastLine -match 'phase=([^ ]+)') {
            "LastNativeContinuationPhase=$($Matches[1])" | Add-Content -LiteralPath $nativeContinuationProbeSummaryPath
        }
        if ($nativeContinuationLastLine -match 'elapsedMs=([0-9]+)') {
            "LastNativeContinuationElapsedMs=$($Matches[1])" | Add-Content -LiteralPath $nativeContinuationProbeSummaryPath
        }
    }
}
if ($saveLoadedOk -and $AutoExerciseLegacyFishingCompatibility -and (Test-Path $logPath)) {
    $legacyFishingCompatibilityOk = Wait-ForAutoFishingLogLine -LogPath $logPath -Pattern 'Smoke.LegacyFishingAutomationCompatibility = verified' -TimeoutSeconds $TimeoutSeconds
}
if ($AutoExerciseAutoFishingPhase -and (Test-Path $logPath)) {
    $autoFishingLifecyclePattern = if ($AutoFishingPerformance) {
        'Hook status: Smoke\.AutoFishingPerformance = (verified|blocked)\.'
    }
    elseif ($AutoFishingMonoGate) {
        'Hook status: Smoke\.AutoFishingMonoGate = verified\.'
    }
    else {
        'Hook status: Smoke\.AutoFishingLifecycle = verified\.'
    }
    $autoFishingLifecycleLine = Select-String -Path $logPath -Pattern $autoFishingLifecyclePattern | Select-Object -Last 1
    if ($autoFishingLifecycleLine) {
        $autoFishingLifecycleSummary = $autoFishingLifecycleLine.Line
        if ($autoFishingLifecycleLine.Line -match 'Smoke\.AutoFishingPerformance = blocked\.') {
            $autoFishingPerformanceCounterBlocked = $true
        }
    }
    $autoFishingLifecycleWarning = [bool](Select-String -Path $logPath -Pattern 'Hook status: Fishing\.Automation\.Lifecycle = (warning|error)|Fishing automation lifecycle boundary status=(warning|error)|Hook status: Smoke\.AutoFishingMonoGate = (warning|error)' | Select-Object -First 1)
    $autoFishingLifecycleOk = [bool]$autoFishingLifecycleLine -and -not $autoFishingLifecycleWarning
    if ($AutoFishingPerformance) {
        $allocationCounterLine = Select-String -Path $logPath -Pattern 'Hook status: Smoke\.AutoFishingAllocationCounter = (verified|blocked)\.' | Select-Object -Last 1
        $autoFishingPerformanceCounterBlocked = [bool]$allocationCounterLine -and $allocationCounterLine.Line -match '= blocked\.'
    }
}
if ($productOwnerRefreshRequested -and (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    $productOwnerRefreshAllCountsOk = $true
    foreach ($owner in @(
        [ordered]@{ Id = 'Yuuka.DTMAPI.ActionSpeed'; CleanupPattern = 'Mod-owner cleanup participants owner=Yuuka\.DTMAPI\.ActionSpeed, reason=ReturnedToTitle, participants=[0-9]+, removed=[0-9]+, remaining=0, failures=0, results=.*DTMAPI\.GameBridge\.DolocTown\.OwnerResources:[0-9]+/0:ok' },
        [ordered]@{ Id = 'Yuuka.DTMAPI.AutoHarvest'; CleanupPattern = 'AutoHarvest ReturnedToTitle boundary OK\.' },
        [ordered]@{ Id = 'DTMAPI.CropHarvestingQaMod'; CleanupPattern = 'CropHarvesting QA ReturnedToTitle boundary OK\.' }
    )) {
        $ownerId = [string]$owner.Id
        $escapedOwnerId = [regex]::Escape($ownerId)
        $entryCount = @(Select-String -Path $logPath -Pattern ('\[' + $escapedOwnerId + '\] Mod Entry completed\.') -AllMatches).Count
        $beginCount = @(Select-String -Path $logPath -Pattern ('Refactor mod load transaction BeginTransaction owner=' + $escapedOwnerId + '\b') -AllMatches).Count
        $commitCount = @(Select-String -Path $logPath -Pattern ('Refactor mod load transaction CommitTransaction owner=' + $escapedOwnerId + '\b') -AllMatches).Count
        $failureCount = @(Select-String -Path $logPath -Pattern ('Skipping ' + $escapedOwnerId + '\b|Failed to load ' + $escapedOwnerId + '\b') -AllMatches).Count
        $cleanupCount = @(Select-String -Path $logPath -Pattern ([string]$owner.CleanupPattern) -AllMatches).Count
        $ownerPassed = $entryCount -eq 1 -and $beginCount -eq 1 -and $commitCount -eq 1 -and $failureCount -eq 0 -and $cleanupCount -ge 1
        $productOwnerRefreshAllCountsOk = $productOwnerRefreshAllCountsOk -and $ownerPassed
        $productOwnerRefreshCounts[$ownerId] = [ordered]@{
            EntryCompleted = $entryCount
            BeginTransaction = $beginCount
            CommitTransaction = $commitCount
            LoadFailures = $failureCount
            CleanupBoundaries = $cleanupCount
            Passed = $ownerPassed
        }
    }
    $productOwnerRefreshReloadDispatchCount = @(Select-String -Path $logPath -Pattern '\[DTMAPI\] Workshop ModListChanged hook dispatched\..*hotLoaded=0$' -AllMatches).Count
    $productOwnerRefreshHotStatusCount = @(Select-String -Path $logPath -Pattern '\[DTMAPI\] Refactor shadow content registry: reason=LoadMods hot loadedNow=0;' -AllMatches).Count
    $productOwnerRefreshOrchestrationOk = [bool](Select-String -Path $logPath -Pattern 'Smoke exercise ProductOwnerRefreshCleanup OK' | Select-Object -Last 1)
    $productOwnerRefreshDependencyOk = $dependencyCompatibilityOk -and [bool]$dependencyCompatibilityLine -and $dependencyCompatibilityLine.Line -match 'dependencyErrors=0'
    $productOwnerRefreshCleanupOk = $productOwnerRefreshAllCountsOk -and $resourceLifecycleCleanupOk -and $modOwnerLifecycleOk -and $modLoadTransactionOk -and $eventHandlerCleanupOk
    $productOwnerRefreshOk = $productOwnerRefreshAllCountsOk -and
        $productOwnerRefreshReloadDispatchCount -ge 1 -and
        $productOwnerRefreshHotStatusCount -ge 1 -and
        $productOwnerRefreshOrchestrationOk -and
        $productOwnerRefreshDependencyOk -and
        $productOwnerRefreshCleanupOk
}
if ($advancedProductOwnerDeactivationRequested -and (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    $advancedProductOwnerDeactivationLine = Select-String -Path $logPath -Pattern 'Hook status: Smoke\.AdvancedProductOwnerDeactivation = verified\.' | Select-Object -Last 1
    $advancedProductOwnerDeactivationLog = Select-String -Path $logPath -Pattern 'Smoke exercise AdvancedProductOwnerDeactivation OK' | Select-Object -Last 1
    $advancedProductOwnerDeactivationOk = [bool]$advancedProductOwnerDeactivationLine -and [bool]$advancedProductOwnerDeactivationLog
    if ($advancedProductOwnerDeactivationLine) {
        $advancedProductOwnerDeactivationSummary = $advancedProductOwnerDeactivationLine.Line
    }
}
if ($moreEquipmentSlotsColdRecoveryTransitionRequested -and
    (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    $moreEquipmentSlotsColdProductEntry = Select-String -LiteralPath $logPath -Pattern 'MoreEquipmentSlots product-owned Hook set installed|MoreEquipmentSlots ProductNative ready|Mod Entry completed owner=DTMAPI\.MoreEquipmentSlotsMod' -ErrorAction SilentlyContinue | Select-Object -First 1
    $moreEquipmentSlotsColdFailure = Select-String -LiteralPath $logPath -Pattern 'EquipmentSlots orphan recovery failed|Player\.EquipmentSlotsApi = orphan-recovery-failed|Compatibility\.Host = failed-closed' -ErrorAction SilentlyContinue | Select-Object -First 1
    $moreEquipmentSlotsColdRecoveryOk =
        $moreEquipmentSlotsColdOfficialPackagePreflightOk -and
        $moreEquipmentSlotsColdOfficialDisabledOk -and
        $moreEquipmentSlotsColdRecoveryDemandOk -and
        $moreEquipmentSlotsColdRecoveryHostOk -and
        $moreEquipmentSlotsColdRecoveryDestinationOk -and
        -not [bool]$moreEquipmentSlotsColdProductEntry -and
        -not [bool]$moreEquipmentSlotsColdFailure
    "MoreEquipmentSlotsColdRecovery=$moreEquipmentSlotsColdRecoveryOk;phase=$MoreEquipmentSlotsTransitionPhase;officialPackage=$moreEquipmentSlotsColdOfficialPackagePreflightOk;officialDisabled=$moreEquipmentSlotsColdOfficialDisabledOk;demand=$moreEquipmentSlotsColdRecoveryDemandOk;host=$moreEquipmentSlotsColdRecoveryHostOk;destination=$moreEquipmentSlotsColdRecoveryDestinationOk;productEntry=$([bool]$moreEquipmentSlotsColdProductEntry);failure=$([bool]$moreEquipmentSlotsColdFailure)" |
        Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
}
if ($hookProbeAbsenceRequested -and (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    $hookProbeAbsenceOk = -not [bool](Select-String -LiteralPath $logPath -Pattern 'HookProbe|DTMAPI\.HookProbeMod' -ErrorAction SilentlyContinue | Select-Object -First 1)
}
if ($AssertNoQaUiEvidence -and (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    $noQaProducts = @(Get-SmokePublishedProductDefinitions)
    $noQaExpectedOwners = @($noQaProducts | ForEach-Object { [string]$_.uniqueId } | Sort-Object -Unique)
    $noQaOwnerCountsOk = $noQaExpectedOwners.Count -eq 11
    $noQaUsesLocalProducts = $OfficialModProfile -eq 'Local11'
    $noQaProductSource = if ($noQaUsesLocalProducts) { 'Local' } else { 'Workshop' }
    $noQaProductProvenanceOk = $true
    $noQaLocalModsRoot = if ($noQaUsesLocalProducts) { [System.IO.Path]::GetFullPath((Join-Path (Get-DolocTownPersistentRootForSmoke) 'MODS')) } else { '' }
    $noQaOwnerCounts = [ordered]@{}
    foreach ($product in $noQaProducts) {
        $ownerId = [string]$product.uniqueId
        $escapedOwnerId = [regex]::Escape($ownerId)
        $entryCount = @(Select-String -LiteralPath $logPath -Pattern ('\[' + $escapedOwnerId + '\] Mod Entry completed\.') -AllMatches).Count
        $beginCount = @(Select-String -LiteralPath $logPath -Pattern ('Refactor mod load transaction BeginTransaction owner=' + $escapedOwnerId + '\b') -AllMatches).Count
        $commitCount = @(Select-String -LiteralPath $logPath -Pattern ('Refactor mod load transaction CommitTransaction owner=' + $escapedOwnerId + '\b') -AllMatches).Count
        $failureCount = @(Select-String -LiteralPath $logPath -Pattern ('Failed to load ' + $escapedOwnerId + '\b|\[' + $escapedOwnerId + '\].*Mod Entry (failed|threw)') -AllMatches).Count
        $expectedProductRoot = if ($noQaUsesLocalProducts) {
            [System.IO.Path]::GetFullPath((Join-Path $noQaLocalModsRoot ([string]$product.officialFolder)))
        }
        else {
            [System.IO.Path]::GetFullPath((Join-Path $workshopContentRoot ([string]$product.workshopId)))
        }
        $duplicateSelectionMatches = @(Select-String -LiteralPath $logPath -Pattern ('Duplicate UniqueID ' + $escapedOwnerId + ' discovered; using ') -ErrorAction SilentlyContinue)
        $expectedSelectionPattern = 'Duplicate UniqueID ' + $escapedOwnerId + ' discovered; using ' + $noQaProductSource + ' enabled=true priority=-?\d+ root=' + [regex]::Escape($expectedProductRoot) + '(?:[.;|]|$)'
        $provenancePassed = $duplicateSelectionMatches.Count -eq 0 -or
            @($duplicateSelectionMatches | Where-Object { $_.Line -notmatch $expectedSelectionPattern }).Count -eq 0
        $loadSourceMatches = @()
        if ($noQaUsesLocalProducts) {
            $expectedProductDll = [System.IO.Path]::GetFullPath((Join-Path $expectedProductRoot ('Content\DTMAPI\' + [string]$product.packageDll)))
            $loadSourceMatches = @(Select-String -LiteralPath $logPath -Pattern ('Code mod load-source owner=' + $escapedOwnerId + ';') -ErrorAction SilentlyContinue)
            $expectedLoadSourcePattern = 'Code mod load-source owner=' + $escapedOwnerId + ';.*; source=Local; workshopId=none; root=' +
                [regex]::Escape($expectedProductRoot) + '; dll=' + [regex]::Escape($expectedProductDll) + '\.\s*$'
            $provenancePassed = $provenancePassed -and $loadSourceMatches.Count -eq 1 -and $loadSourceMatches[0].Line -match $expectedLoadSourcePattern
        }
        $ownerPassed = $entryCount -eq 1 -and $beginCount -eq 1 -and $commitCount -eq 1 -and $failureCount -eq 0 -and $provenancePassed
        $noQaOwnerCountsOk = $noQaOwnerCountsOk -and $ownerPassed
        $noQaProductProvenanceOk = $noQaProductProvenanceOk -and $provenancePassed
        $noQaOwnerCounts[$ownerId] = [ordered]@{
            WorkshopId = [string]$product.workshopId
            OfficialFolder = [string]$product.officialFolder
            ExpectedSource = $noQaProductSource
            ExpectedRoot = $expectedProductRoot
            EntryCompleted = $entryCount
            BeginTransaction = $beginCount
            CommitTransaction = $commitCount
            LoadFailures = $failureCount
            DuplicateSelectionEvidenceCount = $duplicateSelectionMatches.Count
            LoadSourceEvidenceCount = $loadSourceMatches.Count
            ProvenancePassed = $provenancePassed
            Passed = $ownerPassed
        }
    }
    $noQaObservedOwners = @(Select-String -LiteralPath $logPath -Pattern '\[(?<Owner>[A-Za-z0-9_.-]+)\] Mod Entry completed\.' -AllMatches -ErrorAction SilentlyContinue | ForEach-Object {
        foreach ($match in $_.Matches) { $match.Groups['Owner'].Value }
    } | Sort-Object -Unique)
    $noQaExactOwnerSetOk = [string]::Equals(
        (@($noQaExpectedOwners) -join "`n"),
        (@($noQaObservedOwners) -join "`n"),
        [System.StringComparison]::Ordinal)
    $noQaPublishedOwnerSetOk = $noQaOwnerCountsOk -and $noQaExactOwnerSetOk -and $noQaProductProvenanceOk
    Write-SmokeJsonObject -Path (Join-Path $evidence 'no-qa-published-owner-gate.json') -Value ([ordered]@{
        Profile = $OfficialModProfile
        ProductSource = $noQaProductSource
        ExpectedOwners = @($noQaExpectedOwners)
        ObservedOwners = @($noQaObservedOwners)
        Counts = $noQaOwnerCounts
        ExactOwnerSet = $noQaExactOwnerSetOk
        ProductProvenance = $noQaProductProvenanceOk
        WorkshopProvenance = (-not $noQaUsesLocalProducts -and $noQaProductProvenanceOk)
        LocalProvenance = ($noQaUsesLocalProducts -and $noQaProductProvenanceOk)
        Passed = $noQaPublishedOwnerSetOk
    })
}
if ($publishedProductGateRequested -and (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    $expectedEntryCount = if ($AssertPublishedProductCombination) { 1 } else { 0 }
    $publishedProductCountsOk = $true
    foreach ($product in @(Get-SmokePublishedProductDefinitions)) {
        $ownerId = [string]$product.uniqueId
        $escapedOwnerId = [regex]::Escape($ownerId)
        $entryCount = @(Select-String -LiteralPath $logPath -Pattern ('\[' + $escapedOwnerId + '\] Mod Entry completed\.') -AllMatches).Count
        $beginCount = @(Select-String -LiteralPath $logPath -Pattern ('Refactor mod load transaction BeginTransaction owner=' + $escapedOwnerId + '\b') -AllMatches).Count
        $commitCount = @(Select-String -LiteralPath $logPath -Pattern ('Refactor mod load transaction CommitTransaction owner=' + $escapedOwnerId + '\b') -AllMatches).Count
        $failureCount = @(Select-String -LiteralPath $logPath -Pattern ('Failed to load ' + $escapedOwnerId + '\b|\[' + $escapedOwnerId + '\].*Mod Entry (failed|threw)') -AllMatches).Count
        $expectedWorkshopRoot = [System.IO.Path]::GetFullPath((Join-Path $workshopContentRoot ([string]$product.workshopId)))
        $duplicateSelectionMatches = @(Select-String -LiteralPath $logPath -Pattern ('Duplicate UniqueID ' + $escapedOwnerId + ' discovered; using ') -ErrorAction SilentlyContinue)
        $expectedWorkshopSelectionPattern = 'Duplicate UniqueID ' + $escapedOwnerId + ' discovered; using Workshop enabled=true root=' + [regex]::Escape($expectedWorkshopRoot) + '(?:[.;|]|$)'
        $provenancePassed = -not [bool]$AssertPublishedProductCombination -or
            $duplicateSelectionMatches.Count -eq 0 -or
            @($duplicateSelectionMatches | Where-Object { $_.Line -notmatch $expectedWorkshopSelectionPattern }).Count -eq 0
        $ownerPassed = $entryCount -eq $expectedEntryCount -and
            $beginCount -eq $expectedEntryCount -and
            $commitCount -eq $expectedEntryCount -and
            $failureCount -eq 0 -and
            $provenancePassed
        $publishedProductCountsOk = $publishedProductCountsOk -and $ownerPassed
        $publishedProductProvenanceOk = $publishedProductProvenanceOk -and $provenancePassed
        $publishedProductOwnerCounts[$ownerId] = [ordered]@{
            WorkshopId = [string]$product.workshopId
            ExpectedWorkshopRoot = $expectedWorkshopRoot
            ExpectedEntryCount = $expectedEntryCount
            EntryCompleted = $entryCount
            BeginTransaction = $beginCount
            CommitTransaction = $commitCount
            LoadFailures = $failureCount
            DuplicateSelectionEvidenceCount = $duplicateSelectionMatches.Count
            ProvenancePassed = $provenancePassed
            Passed = $ownerPassed
        }
    }
    $publishedProductObservedCodeOwners = @(Select-String -LiteralPath $logPath -Pattern '\[(?<Owner>[A-Za-z0-9_.-]+)\] Mod Entry completed\.' -AllMatches -ErrorAction SilentlyContinue | ForEach-Object {
        foreach ($match in $_.Matches) {
            $match.Groups['Owner'].Value
        }
    } | Sort-Object -Unique)
    $publishedProductExpectedCodeOwners = if ($AssertPublishedProductCombination) {
        @(Get-SmokePublishedProductDefinitions | ForEach-Object { [string]$_.uniqueId } | Sort-Object -Unique)
    }
    else {
        @()
    }
    $publishedProductExactOwnerSetOk = [string]::Equals(
        (@($publishedProductExpectedCodeOwners) -join "`n"),
        (@($publishedProductObservedCodeOwners) -join "`n"),
        [System.StringComparison]::Ordinal)
    $publishedProductHotRefreshCount = @(Select-String -LiteralPath $logPath -Pattern '\[DTMAPI\] Refactor shadow content registry: reason=LoadMods hot loadedNow=0;' -AllMatches).Count
    $publishedProductDependencyOk = $dependencyCompatibilityOk -and [bool]$dependencyCompatibilityLine -and $dependencyCompatibilityLine.Line -match 'dependencyErrors=0'
    $publishedProductCleanupOk = $titleReturnBoundaryOk -and $resourceLifecycleCleanupOk -and $modOwnerLifecycleOk -and $modLoadTransactionOk -and $eventHandlerCleanupOk -and $gameBridgeFinalHealthSnapshotOk
    $refreshOk = (-not $AssertPublishedProductCombination) -or $publishedProductHotRefreshCount -ge 1
    $saveLoadedOrderLine = Select-String -LiteralPath $logPath -Pattern 'SaveLoaded hook dispatched\.' | Select-Object -First 1
    if ($AssertPublishedProductCombination) {
        $hotRefreshOrderLine = Select-String -LiteralPath $logPath -Pattern 'Refactor shadow content registry: reason=LoadMods hot loadedNow=0;' | Select-Object -First 1
        $modListChangedOrderLine = Select-String -LiteralPath $logPath -Pattern 'Workshop ModListChanged hook dispatched\..*hotLoaded=0$' | Where-Object { $hotRefreshOrderLine -and $_.LineNumber -gt $hotRefreshOrderLine.LineNumber } | Select-Object -First 1
        $reloadCompletionOrderLine = Select-String -LiteralPath $logPath -Pattern 'Smoke auto-reload completion observed after Workshop ModListChanged dispatch\.' | Where-Object { $modListChangedOrderLine -and $_.LineNumber -gt $modListChangedOrderLine.LineNumber } | Select-Object -First 1
        $saveLoadedOrderLine = Select-String -LiteralPath $logPath -Pattern 'SaveLoaded hook dispatched\.' | Where-Object { $reloadCompletionOrderLine -and $_.LineNumber -gt $reloadCompletionOrderLine.LineNumber } | Select-Object -First 1
        $refreshOrderOk = [bool]$hotRefreshOrderLine -and [bool]$modListChangedOrderLine -and [bool]$reloadCompletionOrderLine -and [bool]$saveLoadedOrderLine
    }
    else {
        $refreshOrderOk = [bool]$saveLoadedOrderLine
    }
    $returnedToTitleOrderLine = Select-String -LiteralPath $logPath -Pattern 'ReturnedToTitle hook dispatched\.' | Where-Object { $saveLoadedOrderLine -and $_.LineNumber -gt $saveLoadedOrderLine.LineNumber } | Select-Object -First 1
    $finalHealthOrderLine = Select-String -LiteralPath $logPath -Pattern 'GameBridge final health snapshot reason=ReturnedToTitle' | Where-Object { $saveLoadedOrderLine -and $_.LineNumber -gt $saveLoadedOrderLine.LineNumber } | Select-Object -First 1
    $cleanupBoundaryMaxLine = if ($returnedToTitleOrderLine -and $finalHealthOrderLine) { [Math]::Max($returnedToTitleOrderLine.LineNumber, $finalHealthOrderLine.LineNumber) } else { 0 }
    $titleLifecycleOrderLine = Select-String -LiteralPath $logPath -Pattern 'Smoke exercise TitleButtonLifecycle OK' | Where-Object { $_.LineNumber -gt $cleanupBoundaryMaxLine } | Select-Object -First 1
    $quitOrderLine = Select-String -LiteralPath $logPath -Pattern 'Optional QA participant requesting game quit:' | Where-Object { $titleLifecycleOrderLine -and $_.LineNumber -gt $titleLifecycleOrderLine.LineNumber } | Select-Object -First 1
    $publishedProductOrderOk = $refreshOrderOk -and [bool]$returnedToTitleOrderLine -and [bool]$finalHealthOrderLine -and [bool]$titleLifecycleOrderLine -and [bool]$quitOrderLine
    $publishedProductGateOk = $publishedProductArtifactsOk -and $publishedProductSelectionOk -and $publishedProductCountsOk -and $publishedProductExactOwnerSetOk -and $publishedProductProvenanceOk -and $refreshOk -and $publishedProductOrderOk -and $publishedProductDependencyOk -and $publishedProductCleanupOk -and $hookProbeAbsenceOk
}
$externalShortTapAttempts = @($externalPlayerInputAttempts.ToArray() | Where-Object { [string]$_.Label -match '^YShort([1-9]|10)$' })
if ($RequireExternalPlayerInputGate) {
    $externalShortTapAttemptsOk = $externalShortTapAttempts.Count -eq 10 -and @($externalShortTapAttempts | Where-Object {
        -not [bool]$_.Sent -or -not [bool]$_.MatchedExpectedLog -or -not [bool]$_.ForegroundMatched -or
            -not [bool]$_.SendInputSucceeded -or [bool]$_.PostMessageFallbackUsed -or
            [int]$_.HoldMilliseconds -ne $ExternalPlayerInputHoldMilliseconds
    }).Count -eq 0
    $expectedExternalAttemptLabels = @('Escape','Y1','Y2','Y3','YHold','YHoldCleanupEscape') + @(1..10 | ForEach-Object { "YShort$_" }) | Sort-Object
    $actualExternalAttemptLabels = @($externalPlayerInputAttempts.ToArray() | ForEach-Object { [string]$_.Label } | Sort-Object)
    $externalPlayerInputAllAttemptsOk = $actualExternalAttemptLabels.Count -eq $expectedExternalAttemptLabels.Count -and
        [string]::Equals((@($expectedExternalAttemptLabels) -join "`n"), (@($actualExternalAttemptLabels) -join "`n"), [System.StringComparison]::Ordinal) -and
        @($externalPlayerInputAttempts.ToArray() | Where-Object {
            $expectedHold = if ([string]$_.Label -eq 'YHold') { 1800 } else { $ExternalPlayerInputHoldMilliseconds }
            -not [bool]$_.Sent -or -not [bool]$_.MatchedExpectedLog -or -not [bool]$_.ForegroundMatched -or
                -not [bool]$_.SendInputSucceeded -or [bool]$_.PostMessageFallbackUsed -or
                [int]$_.HoldMilliseconds -ne $expectedHold
        }).Count -eq 0

    $yConsoleDuplicateMatches = @(Select-String -LiteralPath $logPath -Pattern 'Duplicate UniqueID DTMAPI\.DebugConsoleMod discovered; using ' -ErrorAction SilentlyContinue)
    $expectedYConsoleSelectionPattern = 'Duplicate UniqueID DTMAPI\.DebugConsoleMod discovered; using Workshop enabled=true root=' + [regex]::Escape($expectedYConsoleWorkshopRoot) + '(?:[.;|]|$)'
    $externalPlayerInputDuplicateSelectionEvidenceAvailable = $yConsoleDuplicateMatches.Count -gt 0
    $externalPlayerInputDuplicateSelectionCount = $yConsoleDuplicateMatches.Count
    $externalPlayerInputDuplicateSelectionLines = @($yConsoleDuplicateMatches | ForEach-Object { [string]$_.Line })
    $externalPlayerInputDuplicateSelectionOk = $yConsoleDuplicateMatches.Count -eq 0 -or
        @($yConsoleDuplicateMatches | Where-Object { $_.Line -notmatch $expectedYConsoleSelectionPattern }).Count -eq 0
    $yConsoleLoadSourceMatches = @(Select-String -LiteralPath $logPath -Pattern 'Code mod load-source owner=DTMAPI\.DebugConsoleMod;' -ErrorAction SilentlyContinue)
    $expectedYConsoleLoadSourcePattern = 'Code mod load-source owner=DTMAPI\.DebugConsoleMod;[^\r\n]*; source=Workshop; workshopId=3742714442; root=' +
        [regex]::Escape($expectedYConsoleWorkshopRoot) + '; dll=' + [regex]::Escape($expectedYConsoleWorkshopDll) + '\.\s*$'
    $externalPlayerInputLoadSourceCount = $yConsoleLoadSourceMatches.Count
    $externalPlayerInputLoadSourceLines = @($yConsoleLoadSourceMatches | ForEach-Object { [string]$_.Line })
    $externalPlayerInputLoadSourceOk = $yConsoleLoadSourceMatches.Count -eq 1 -and
        $yConsoleLoadSourceMatches[0].Line -match $expectedYConsoleLoadSourcePattern
    $externalPlayerInputProvenanceOk = $externalPlayerInputArtifactOk -and
        $externalPlayerInputLoadSourceOk -and
        $externalPlayerInputDuplicateSelectionOk

    $readyOrderPattern = if ($qaG4ExternalPlayerInputObservationEnabled) {
        'Smoke\.ExternalPlayerInput = ready.*token=' + [regex]::Escape($externalPlayerInputHandshakeToken)
    }
    else {
        'Smoke external player input READY token=' + [regex]::Escape($externalPlayerInputHandshakeToken) + '\.'
    }
    $markerOrderPattern = if ($qaG4ExternalPlayerInputObservationEnabled) {
        'Smoke external player input marker token=' + [regex]::Escape($externalPlayerInputHandshakeToken) + '; passed=true\.'
    }
    else {
        'Smoke external player input terminal marker observed token=' + [regex]::Escape($externalPlayerInputHandshakeToken) + ' passed=True\.'
    }
    $cleanupOrderPattern = 'Smoke exercise ExternalPlayerInputCleanup OK token=' + [regex]::Escape($externalPlayerInputHandshakeToken) + ';'
    $readyOrderLine = Select-String -LiteralPath $logPath -Pattern $readyOrderPattern | Select-Object -First 1
    $firstInputOrderLine = Select-String -LiteralPath $logPath -Pattern 'Debug console opened owner=DTMAPI\.DebugConsoleMod reason=hotkey Y\.' | Where-Object { $readyOrderLine -and $_.LineNumber -gt $readyOrderLine.LineNumber } | Select-Object -First 1
    $markerOrderLine = Select-String -LiteralPath $logPath -Pattern $markerOrderPattern | Where-Object { $firstInputOrderLine -and $_.LineNumber -gt $firstInputOrderLine.LineNumber } | Select-Object -First 1
    $lastInputOrderLine = Select-String -LiteralPath $logPath -Pattern 'Debug console closed reason=Escape owner=DTMAPI\.DebugConsoleMod\.' | Where-Object { $readyOrderLine -and $markerOrderLine -and $_.LineNumber -gt $readyOrderLine.LineNumber -and $_.LineNumber -lt $markerOrderLine.LineNumber } | Select-Object -Last 1
    $externalReturnedOrderLine = Select-String -LiteralPath $logPath -Pattern 'ReturnedToTitle hook dispatched\.' | Where-Object { $markerOrderLine -and $_.LineNumber -gt $markerOrderLine.LineNumber } | Select-Object -First 1
    $externalFinalHealthOrderLine = Select-String -LiteralPath $logPath -Pattern 'GameBridge final health snapshot reason=ReturnedToTitle' | Where-Object { $markerOrderLine -and $_.LineNumber -gt $markerOrderLine.LineNumber } | Select-Object -First 1
    $externalCleanupBoundaryMaxLine = if ($externalReturnedOrderLine -and $externalFinalHealthOrderLine) { [Math]::Max($externalReturnedOrderLine.LineNumber, $externalFinalHealthOrderLine.LineNumber) } else { 0 }
    $externalCleanupOrderLine = Select-String -LiteralPath $logPath -Pattern $cleanupOrderPattern | Where-Object { $_.LineNumber -gt $externalCleanupBoundaryMaxLine } | Select-Object -First 1
    $externalQuitOrderLine = Select-String -LiteralPath $logPath -Pattern 'Optional QA participant requesting game quit: optional QA participant reached its terminal state' | Where-Object { $externalCleanupOrderLine -and $_.LineNumber -gt $externalCleanupOrderLine.LineNumber } | Select-Object -First 1
    $externalPlayerInputOrderOk = [bool]$readyOrderLine -and [bool]$firstInputOrderLine -and [bool]$lastInputOrderLine -and [bool]$markerOrderLine -and
        $lastInputOrderLine.LineNumber -lt $markerOrderLine.LineNumber -and [bool]$externalReturnedOrderLine -and [bool]$externalFinalHealthOrderLine -and [bool]$externalCleanupOrderLine -and [bool]$externalQuitOrderLine

    $externalPlayerInputModalYCloseDispatchCount = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console close boundary reason=hotkey Y modalOpen=True activeMenu=DTMAPI.DebugConsole.' -ErrorAction SilentlyContinue).Count
    $externalPlayerInputTypedModalYCount = @(Select-String -LiteralPath $logPath -Pattern 'Keybind DTMAPI\.DebugConsoleMod/debug-console\.toggle pressed trigger=Y context=' -ErrorAction SilentlyContinue).Count
    $externalPlayerInputLegacyModalYCount = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console legacy RegisterButton Y close compatibility dispatched owner=DTMAPI.DebugConsoleMod.' -ErrorAction SilentlyContinue).Count
    $externalPlayerInputLegacyOwnerDispatchCount = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Legacy modal input ButtonPressed dispatched owner=DTMAPI.DebugConsoleMod menu=DTMAPI.DebugConsole button=Y handlers=1.' -ErrorAction SilentlyContinue).Count
    $externalPlayerInputLegacyModToggleCloseCount = @(Select-String -LiteralPath $logPath -Pattern '\[DTMAPI\.DebugConsoleMod\].*open=False\s*$' -ErrorAction SilentlyContinue).Count
    $externalPlayerInputModalEscapeCloseCount = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console close boundary reason=Escape modalOpen=True activeMenu=DTMAPI.DebugConsole.' -ErrorAction SilentlyContinue).Count
    $externalPlayerInputModalIsolationOk = -not $externalPlayerInputNativeMenuLeakDetected -and
        $externalPlayerInputModalYCloseDispatchCount -eq 6 -and
        ($externalPlayerInputTypedModalYCount + $externalPlayerInputLegacyModalYCount) -eq 6 -and
        $externalPlayerInputLegacyOwnerDispatchCount -eq $externalPlayerInputLegacyModalYCount -and
        $externalPlayerInputLegacyModToggleCloseCount -eq $externalPlayerInputLegacyModalYCount -and
        $externalPlayerInputModalEscapeCloseCount -eq 2

    $externalPlayerInputGateOk = -not $internalDebugConsoleSmokeEnabled -and $externalPlayerInputWorkshopSelectionOk -and $externalPlayerInputProvenanceOk -and $hookProbeAbsenceOk -and $externalPlayerInputReadyOk -and $externalPlayerInputMarkerWrittenOk -and $externalPlayerInputCleanupOk -and $debugConsoleOpenY1Ok -and $debugConsoleCloseEscapeOk -and $debugConsoleOpenY2Ok -and $debugConsoleCloseYOk -and $debugConsoleTenYShortTapsOk -and $debugConsoleHoldYNoFlickerOk -and $debugConsolePostHoldCloseOk -and $externalShortTapAttemptsOk -and $externalPlayerInputAllAttemptsOk -and $externalPlayerInputOrderOk -and $externalPlayerInputModalIsolationOk
}
Write-SmokeJsonObject -Path (Join-Path $evidence 'external-input-gate.json') -Value ([ordered]@{
    Requested = [bool]$RequireExternalPlayerInputGate
    LaunchMode = if ($launchViaSteam) { 'Steam' } else { 'DirectExe' }
    HookProbeAbsent = $hookProbeAbsenceOk
    WorkshopYConsoleSelected = $externalPlayerInputWorkshopSelectionOk
    ExpectedWorkshopRoot = if ($RequireExternalPlayerInputGate) { $expectedYConsoleWorkshopRoot } else { '' }
    WorkshopYConsoleDll = if ($RequireExternalPlayerInputGate) { $expectedYConsoleWorkshopDll } else { '' }
    WorkshopYConsoleDllSha256 = if ($RequireExternalPlayerInputGate) { $actualYConsoleWorkshopDllSha256 } else { '' }
    ExpectedWorkshopYConsoleDllSha256 = if ($RequireExternalPlayerInputGate) { $expectedYConsoleWorkshopDllSha256 } else { '' }
    ActualWorkshopYConsoleDllSha256 = if ($RequireExternalPlayerInputGate) { $actualYConsoleWorkshopDllSha256 } else { '' }
    WorkshopYConsoleDllExists = $externalPlayerInputWorkshopDllExists
    ExpectedWorkshopYConsoleDllLength = $expectedYConsoleWorkshopDllLength
    WorkshopYConsoleDllLength = $externalPlayerInputWorkshopDllLength
    WorkshopYConsoleDllNonEmpty = $externalPlayerInputWorkshopDllNonEmpty
    WorkshopYConsoleDllLengthPassed = $externalPlayerInputWorkshopDllLengthOk
    WorkshopYConsoleDllHashPassed = $externalPlayerInputWorkshopDllHashOk
    WorkshopArtifactPassed = $externalPlayerInputArtifactOk
    WorkshopArtifactSnapshot = $externalPlayerInputWorkshopArtifactSnapshot
    DuplicateSelectionEvidenceAvailable = $externalPlayerInputDuplicateSelectionEvidenceAvailable
    DuplicateSelectionEvidenceCount = $externalPlayerInputDuplicateSelectionCount
    DuplicateSelectionPassed = $externalPlayerInputDuplicateSelectionOk
    DuplicateSelectionLines = @($externalPlayerInputDuplicateSelectionLines)
    LoadSourceEvidenceCount = $externalPlayerInputLoadSourceCount
    LoadSourcePassed = $externalPlayerInputLoadSourceOk
    LoadSourceLines = @($externalPlayerInputLoadSourceLines)
    LoadedSourceRootAssertionMode = 'CodeModLoadSourceLog'
    LoadedSourceRootAsserted = $externalPlayerInputLoadSourceOk
    WorkshopProvenancePassed = $externalPlayerInputProvenanceOk
    InternalSyntheticExerciseEnabled = [bool]$internalDebugConsoleSmokeEnabled
    ReadyObserved = $externalPlayerInputReadyOk
    TerminalMarkerWritten = $externalPlayerInputMarkerWrittenOk
    ReturnHomeCleanupPassed = $externalPlayerInputCleanupOk
    OrderedLifecyclePassed = $externalPlayerInputOrderOk
    ModalInputIsolationPassed = $externalPlayerInputModalIsolationOk
    NativeMenuLeakDetected = $externalPlayerInputNativeMenuLeakDetected
    ModalYCloseDispatchCount = $externalPlayerInputModalYCloseDispatchCount
    TypedModalYDispatchCount = $externalPlayerInputTypedModalYCount
    LegacyModalYCompatibilityCount = $externalPlayerInputLegacyModalYCount
    LegacyModalYOwnerDispatchCount = $externalPlayerInputLegacyOwnerDispatchCount
    LegacyModToggleCloseCount = $externalPlayerInputLegacyModToggleCloseCount
    ModalEscapeCloseCount = $externalPlayerInputModalEscapeCloseCount
    ExpectedShortTapCount = if ($RequireExternalPlayerInputGate) { 10 } else { 0 }
    ActualShortTapCount = $externalShortTapAttempts.Count
    HoldMilliseconds = $ExternalPlayerInputHoldMilliseconds
    Attempts = @($externalPlayerInputAttempts.ToArray())
    AllAttemptEvidencePassed = $externalPlayerInputAllAttemptsOk
    MissingFrameFallbackWarningCount = $missingFrameFallbackWarningCount
    FrameDriverStallWarningCount = $frameDriverStallWarningCount
    InputSystemResubscribeCount = $inputSystemResubscribeCount
    PlayerLoopLifecycleRefreshCount = $playerLoopLifecycleRefreshCount
    Passed = $externalPlayerInputGateOk
})
Write-SmokeJsonObject -Path (Join-Path $evidence 'public-product-combination-gate.json') -Value ([ordered]@{
    Requested = $publishedProductGateRequested
    Mode = $publishedProductGateMode
    Profile = $OfficialModProfile
    ArtifactGatePassed = $publishedProductArtifactsOk
    ExactSelectionPassed = $publishedProductSelectionOk
    HookProbeAbsent = $hookProbeAbsenceOk
    Owners = $publishedProductOwnerCounts
    ExpectedCodeOwners = if ($publishedProductGateRequested) { @($publishedProductExpectedCodeOwners) } else { @() }
    ObservedCodeOwners = @($publishedProductObservedCodeOwners)
    ExactCodeOwnerSetPassed = $publishedProductExactOwnerSetOk
    WorkshopProvenancePassed = $publishedProductProvenanceOk
    HotRefreshLoadedNowZeroCount = $publishedProductHotRefreshCount
    OrderedLifecyclePassed = $publishedProductOrderOk
    ArtifactsUnchangedDuringRun = $publishedProductArtifactsUnchangedDuringRun
    DependencyCompatibilityPassed = $publishedProductDependencyOk
    CleanupPassed = $publishedProductCleanupOk
    Passed = $publishedProductGateOk
})
Write-SmokeJsonObject -Path (Join-Path $evidence 'product-owner-refresh-gate.json') -Value ([ordered]@{
    Requested = $productOwnerRefreshRequested
    Profile = $OfficialModProfile
    ExtraEnabledIds = @($OfficialModProfileExtraEnabledIds)
    IsolateAllOfficialMods = [bool]$IsolateAllOfficialMods
    Owners = $productOwnerRefreshCounts
    ReloadDispatchCount = $productOwnerRefreshReloadDispatchCount
    HotRefreshStatusCount = $productOwnerRefreshHotStatusCount
    ReturnHomeOrchestrationPassed = $productOwnerRefreshOrchestrationOk
    DependencyCompatibilityPassed = $productOwnerRefreshDependencyOk
    CleanupPassed = $productOwnerRefreshCleanupOk
    Passed = $productOwnerRefreshOk
})
$officialModProfileRestoredOk = [bool]$officialModProfileRestored
$officialModProfileGateOk = $officialModProfileSelectionOk -and $officialModProfileRestoredOk
$local11AuthorSourceGateOk = -not $localAuthorSourceRequested -or ($local11AuthorSourceAppliedOk -and $local11AuthorSourceRestored)
Write-SmokeJsonObject -Path (Join-Path $evidence 'official-mod-profile-gate.json') -Value ([ordered]@{
    Profile = $OfficialModProfile
    IsolateAllOfficialMods = [bool]$IsolateAllOfficialMods
    AppliedOk = $officialModProfileAppliedOk
    ExtraEnabledOk = $officialModProfileExtraEnabledOk
    OilDisabledOk = $officialModProfileOilDisabledOk
    PublishedProductSelectionOk = $publishedProductSelectionOk
    ExternalPlayerInputWorkshopSelectionOk = $externalPlayerInputWorkshopSelectionOk
    Restored = $officialModProfileRestoredOk
    Passed = $officialModProfileGateOk
    RequestedExtraEnabledIds = @($requestedOfficialModProfileExtraIds)
    ActualEnabledIds = @($officialModProfileSummary.EnabledIds)
    ActualDisabledIds = @($officialModProfileSummary.DisabledIds)
})
Write-SmokeJsonObject -Path (Join-Path $evidence 'local11-author-source-state-gate.json') -Value ([ordered]@{
    Requested = $localAuthorSourceRequested
    RequestedLocalIds = @($localAuthorSourceRequestedIds)
    Applied = $local11AuthorSourceAppliedOk
    SelectionCount = if ($null -ne $local11AuthorSourceSummary) { @($local11AuthorSourceSummary.Selections).Count } else { 0 }
    Restored = $local11AuthorSourceRestored
    StatePath = if ($null -ne $local11AuthorSourceSummary) { [string]$local11AuthorSourceSummary.StatePath } else { '' }
    Algorithm = 'DTMAPI-FileTree-SHA256-v1'
    Passed = $local11AuthorSourceGateOk
})
if ($debugConsoleTitleUiCleanupRequested) {
    $zeroUiGraph =
        'Canvas=0; EventSystem=0; Button=0; InputField=0; ScrollRect=0; UnityEventListeners=0; DynamicBinders=0; rootAlive=0'
    if ($RequireExternalPlayerInputGate) {
        $compatibilityCleanupLine =
            Select-String -LiteralPath $logPath `
                -Pattern ('TitleReturn object graph snapshot .*AfterReturnedToTitleComplete.*DTMAPI\.DebugConsoleMod=\{' +
                    [regex]::Escape($zeroUiGraph) + '\}') `
                -ErrorAction SilentlyContinue |
            Select-Object -Last 1
        $compatibilityOwnerObserved =
            [bool](Select-String -LiteralPath $logPath `
                -Pattern 'nativeOwner=Compatibility' `
                -ErrorAction SilentlyContinue |
                Select-Object -First 1)
        $debugConsoleTitleUiCleanupOk =
            $compatibilityOwnerObserved -and
            [bool]$compatibilityCleanupLine
        $debugConsoleTitleUiCleanupSummary =
            if ($compatibilityCleanupLine) {
                $compatibilityCleanupLine.Line
            }
            else {
                'Compatibility owner or exact zero UI graph was not observed after ReturnedToTitle.'
            }
    }
    else {
        $productCleanupLine =
            Select-String -LiteralPath $logPath `
                -Pattern ('DebugConsole ReturnedToTitle cleanup UI=\{DTMAPI\.DebugConsoleMod=\{' +
                    [regex]::Escape($zeroUiGraph) + '\}\}') `
                -ErrorAction SilentlyContinue |
            Select-Object -Last 1
        $productLoadSourceObserved =
            [bool](Select-String -LiteralPath $logPath `
                -Pattern ('Code mod load-source owner=DTMAPI\.DebugConsoleMod; ' +
                    'identity=Advanced CodeMod; provenance=sdk-reference-receipt-verified;.*' +
                    'expectedHarmonyOwner=dtmapi\.mod\.dtmapi\.debugconsolemod;') `
                -ErrorAction SilentlyContinue |
                Select-Object -First 1)
        $productHostObserved =
            [bool](Select-String -LiteralPath $logPath `
                -Pattern '\[DTMAPI\.DebugConsoleMod\] DebugConsole status UI\.DebugConsoleHost=product-native' `
                -ErrorAction SilentlyContinue |
                Select-Object -First 1)
        $productOwnerObserved = $productLoadSourceObserved -and $productHostObserved
        $debugConsoleTitleUiCleanupOk =
            $productOwnerObserved -and
            [bool]$productCleanupLine
        $debugConsoleTitleUiCleanupSummary =
            if ($productOwnerObserved -and $productCleanupLine) {
                $productCleanupLine.Line
            }
            else {
                'Exact Advanced ProductNative load-source/host ownership or zero UI graph was not observed after ReturnedToTitle.'
            }
    }
}
$issue011AcceptanceProjection = if ($Issue011Acceptance) {
    New-SmokeIssue011AcceptanceProjection `
        -EvidencePath $evidence `
        -GameDirectory $gameDir `
        -RunStartedAt $smokeRunStartedAt `
        -LastGiveBaseline $issue011LastGiveBaseline `
        -CrashFreshness ([pscustomobject]@{
            Status = $unityCrashFreshnessStatus
            Reason = $unityCrashFreshnessSummary
            FreshDirectories = if ($null -ne $unityCrashPostCloseFreshness) { @($unityCrashPostCloseFreshness.FreshDirectories) } else { @($unityCrashLiveFreshness.FreshDirectories) }
        }) `
        -NoQaGatePassed ([bool]$noQaUiEvidenceGateOk) `
        -NoQaPublishedOwnerSetPassed ([bool]$noQaPublishedOwnerSetOk) `
        -SaveLoadedPassed ([bool]$saveLoadedOk) `
        -InteractiveEvidenceObserved ([bool]$issue011InteractiveEvidenceObserved) `
        -FatalWindowCount $fatalWindows.Count `
        -ProcessExited (-not [bool]$leftover) `
        -ForcedClose ([bool]$forcedClose)
}
else { $null }
$issue011AcceptanceOk = (-not [bool]$Issue011Acceptance) -or
    ($null -ne $issue011AcceptanceProjection -and [bool]$issue011AcceptanceProjection.Passed)
$runAborted = ($fatalWindows.Count -gt 0) -or $forcedClose -or [bool]$leftover
$diagnosticsReportExportOk = (-not $diagnosticsReportExportRequested) -or ($diagnosticsReportExportPassedScenarios.Count -eq $diagnosticsReportExportScenarios.Count)
$runFailed = (-not $startupOk -or -not $gameLaunchedOk -or -not $probeOk -or -not $saveLoadedOk -or -not $ownerLifetimeOk -or -not $refactorFlagsOk -or -not $refactorLifecycleObservationOk -or -not $refactorLifecycleBoundaryContractOk -or -not $refactorShadowContentRegistryOk -or -not $contentRegistryOk -or -not $manifestRegistryOk -or -not $dependencyCompatibilityOk -or -not $contentPackOwnershipOk -or -not $registryDiffsOk -or -not $resourceLifecycleLedgerOk -or -not $resourceLifecycleCleanupOk -or -not $titleIdleResourceGrowthOk -or -not $saveLoadRequestCoordinatorOk -or -not $duplicateLoadRequestsOk -or -not $saveLoadBoundaryOk -or -not $titleReturnBoundaryOk -or -not $saveLoadCycleObjectDeltaLedgerOk -or -not $preLoadGcProbeOk -or -not $hookSchedulerOk -or -not $coreHookReadinessOk -or -not $featureHookReadinessOk -or -not $hookStatusQueueOk -or -not $offThreadHookRequestsOk -or -not $assemblyLoadSubscriptionOk -or -not $retryTimerAliveOk -or -not $modOwnerLifecycleOk -or -not $modLoadTransactionOk -or -not $ownerBoundInputOk -or -not $eventHandlerCleanupOk -or -not $configPreviewAuditOk -or -not $failedModRollbackOk -or -not $gameBridgeFeatureContractsOk -or -not $gameBridgeFinalHealthSnapshotOk -or -not $titleButtonOk -or -not $titleButtonScreenshotOk -or -not $titleButtonScreenshotFileOk -or -not $titleLifecycleOk -or -not $saveLoadCycleOk -or -not $saveLoadCyclePendingPressureOk -or -not $titleMenuOk -or -not $titleMenuScreenshotOk -or -not $titleMenuScreenshotFileOk -or -not $managerStatusPageOk -or -not $managerStatusPageScreenshotOk -or -not $managerStatusPageScreenshotFileOk -or -not $managerStatusSummaryTextOk -or -not $managerStatusSummaryCopyOk -or -not $managerModsPageOk -or -not $managerModsPageScreenshotFileOk -or -not $managerPlayerInteractionsOk -or -not $managerErrorsPageOk -or -not $managerHooksPageOk -or -not $managerFeaturesPageOk -or -not $managerAdvancedPageScreenshotFileOk -or -not $managerLogsPageOk -or -not $managerLogsExportButtonOk -or -not $managerLogsExportStateTextOk -or -not $managerLogsPageScreenshotOk -or -not $managerLogsPageScreenshotFileOk -or -not $officialModUiOk -or -not $officialModUiScreenshotFileOk -or -not $animalViewerUiOk -or -not $experimentalHooksOk -or -not $actionSpeedToolOk -or -not $actionSpeedConfigApplyOk -or -not $actionSpeedInteractionOk -or -not $oneActionResourceHitOk -or -not $oneActionWrongToolOk -or -not $oneActionFuelFeedOk -or -not $oneActionVegetationOk -or -not $autoFishingInputLogOk -or -not $autoFishingHotkeyOk -or -not $autoFishingMovementCancelOk -or -not $autoFishingPhaseOk -or -not $autoFishingMonoGateOk -or -not $autoFishingInstantBiteOk -or -not $autoFishingMiniGameSkipOk -or -not $autoFishingMiniGameCompleteOk -or -not $autoFishingAnimationSpeedOk -or -not $autoFishingCastChargeOk -or -not $autoFishingLifecycleOk -or -not $autoFishingSoakOk -or -not $autoFishingReportExportOk -or -not $pauseMenuLayoutOk -or -not $diagnosticsReportExportOk -or -not $instantSaveOk -or -not $debugConsoleSaveAcceptanceOk -or -not $debugConsoleOpenY1Ok -or -not $debugConsoleMouseGiveOk -or -not $debugConsoleCloseEscapeOk -or -not $debugConsoleOpenY2Ok -or -not $debugConsoleCloseYOk -or -not $debugConsoleTenYShortTapsOk -or -not $debugConsoleHoldYNoFlickerOk -or -not $debugInventoryOk -or -not $debugWeatherOk -or -not $debugTeleportCsvOk -or -not $debugTeleportOk -or -not $debugTimeOk -or -not $debugMovementOk -or -not $advancedDebugOk -or -not $vehicleSecondMotorOk -or -not $zoomOk -or -not $zoomProductNativeOk -or -not $chestLocatorEnhancerOk -or -not $moreSavesOfficialSaveUiOk -or -not $moreSavesOfficialSaveUiEvidenceOk -or -not $strongPlantingGunOk -or -not $cropHarvestingApiOk -or -not $cropHarvestingApiEvidenceOk -or -not $customEntityApisOk -or -not $audioReplacementOk -or -not $hatchAnimalVoiceOk -or -not $newContentApisOk -or -not $newContentMineApisOk -or -not $newContentOilAbsentOk -or -not $newContentOilOnlyOk -or -not $newContentOilItemMetadataOk -or -not $newContentOilCoalDropOk -or -not $newContentMineOfficialJsonOk -or -not $newContentMineOfficialTechTreeUiOk -or -not $newContentMineOfficialTechTreeUiScreenshotFileOk -or -not $newContentEquipmentSlotsOk -or -not $newContentMineProductionOk -or $runAborted)
$runFailed = $runFailed -or -not $oneActionMenuKeyOk
$runFailed = $runFailed -or -not $oneActionPartialEnergyOk -or -not $oneActionConfigReloadOk
$runFailed = $runFailed -or -not $advancedProductOwnerDeactivationOk
$runFailed = $runFailed -or -not $moreEquipmentSlotsOk
$runFailed = $runFailed -or -not $moreEquipmentSlotsCommittedShieldSetupOk
$runFailed = $runFailed -or -not $moreEquipmentSlotsCommittedShieldDamageOk
$runFailed = $runFailed -or -not $moreEquipmentSlotsCommittedShieldBreakReplaceOk
$runFailed = $runFailed -or -not $moreEquipmentSlotsInterruptedCandidateRecoveryOk
$runFailed = $runFailed -or -not $moreEquipmentSlotsNoNativeSaveOk
$runFailed = $runFailed -or -not $moreEquipmentSlotsNoNativeSaveColdOk
$runFailed = $runFailed -or -not $moreEquipmentSlotsTransitionOk
$runFailed = $runFailed -or -not $moreSavesFixed12LifecycleOk
$runFailed =
    $runFailed -or
    ($MoreEquipmentSlotsTransitionPhase -eq 'U1' -and
        -not $moreEquipmentSlotsRetainedWorkshopPreflightOk)
$runFailed = $runFailed -or -not $moreEquipmentSlotsColdRecoveryOk
$runFailed = $runFailed -or -not $officialModProfileGateOk -or -not $productOwnerRefreshOk -or -not $publishedProductGateOk -or -not $externalPlayerInputGateOk -or -not $hookProbeAbsenceOk
$runFailed = $runFailed -or -not $qaHostLifecycleOk -or -not $qaHostCleanupOk
$runFailed = $runFailed -or -not $batch6AutoFishingHandshakeOk -or -not $batch6AutoFishingInputOk -or -not $batch6AutoFishingPilotOk -or -not $batch6AutoFishingCleanupOk -or
    -not $batch6AutoFishingManagerHandshakeOk -or -not $batch6AutoFishingManagerActionOk -or
    -not $batch6AutoFishingManagerLifecycleOk -or -not $batch6AutoFishingManagerCleanupOk -or -not $batch6ManagerMarkerRestoreOk
$runFailed = $runFailed -or -not $local11AuthorSourceGateOk
$runFailed = $runFailed -or -not $noQaUiEvidenceGateOk -or -not $noQaPublishedOwnerSetOk
$runFailed = $runFailed -or -not $issue011AcceptanceOk
$runFailed = $runFailed -or ($publishedProductArtifactGateRequested -and -not $publishedProductArtifactsOk)
$runFailed = $runFailed -or -not $nativeContinuationProbeCleanupOk
$runFailed = $runFailed -or -not $ownerLifetimeCloseCleanupOk
$runFailed = $runFailed -or -not $saveFixtureIsolationOk -or -not $saveFixtureIsolationCleanupOk
$runFailed = $runFailed -or ($noNativeSaveMetadataRequested -and (-not $playerSaveUnchangedBeforeCleanupOk -or -not $committedSidecarsUnchangedBeforeCleanupOk))
$runFailed = $runFailed -or ([bool]$qaExternalStateProtectionRequested -and (-not $playerSaveUnchangedBeforeCleanupOk -or -not $g5CommittedSidecarUnchangedOk -or -not $g5ConfigDirectoryRestored))
$runFailed = $runFailed -or -not $fishRoeTooltipOk -or -not $itemDisplayNameLifecycleOk -or -not $qaDiagnosticsSnapshotOk -or -not $qaSaveLoadedObservationOk -or -not $qaSaveSavedObservationOk -or -not $qaWorkshopReloadObservationOk
$runFailed = $runFailed -or -not $contentMetadataObservationOk -or -not $qaG4DebugEvidenceReadyOk -or -not $debugConsolePostHoldCloseOk -or
    -not $debugConsoleTitleUiCleanupOk -or
    -not $pauseMenuLayoutScreenshotFileOk -or -not $animalViewerUiScreenshotFileOk -or -not $equipmentSlotsUiObservationOk -or
    -not $equipmentSlotsUiEvidenceCapturedOk -or -not $equipmentSlotsUiHoverOk -or -not $equipmentSlotsUiCloseInputOk -or -not $equipmentSlotsUiScreenshotFileOk -or
    -not $debugConsoleScreenshotFileOk -or -not $saveSlotsUiScreenshotFileOk -or -not $cameraPlayableEvidenceFilesOk
if ($disposableSaveFixtureCleanupRequested -and -not $runFailed) {
    try {
        Remove-SmokeDisposableSaveFixture `
            -Root $disposableSaveFixtureRootResolved `
            -ExpectedMarkerSha256 $disposableSaveFixtureMarkerSha256 `
            -Mode $SaveTestMode
        $disposableSaveFixtureCleanupOk = $true
    }
    catch {
        $disposableSaveFixtureCleanupOk = $false
        $runFailed = $true
        $_ | Out-String |
            Set-Content -LiteralPath (
                Join-Path $evidence 'disposable-save-fixture-cleanup-error.txt')
    }
}
elseif ($disposableSaveFixtureCleanupRequested) {
    $disposableSaveFixtureCleanupOk = $false
}
$runStatus = if ($runAborted) { 'Aborted' } elseif ($runFailed) { 'Failed' } else { 'Passed' }
$result = @{
    SchemaVersion = 2
    RunStatus = $runStatus
    OfficialModProfile = $OfficialModProfile
    IsolateAllOfficialMods = [bool]$IsolateAllOfficialMods
    OfficialModProfileApplied = [bool]$officialModProfileSummary.Applied
    OfficialModProfileRestored = [bool]$officialModProfileRestored
    OfficialModProfileGate = Get-SmokeAlwaysStatus -Passed $officialModProfileGateOk
    Local11AuthorSourceState = Get-SmokeStatus -Requested $localAuthorSourceRequested -Passed $local11AuthorSourceGateOk
    Local11AuthorSourceSelectionCount = if ($null -ne $local11AuthorSourceSummary) { @($local11AuthorSourceSummary.Selections).Count } else { 0 }
    Local11AuthorSourceStateRestored = [bool]$local11AuthorSourceRestored
    LocalAuthorSourceState = Get-SmokeStatus -Requested $localAuthorSourceRequested -Passed $local11AuthorSourceGateOk
    LocalAuthorSourceRequestedIds = @($localAuthorSourceRequestedIds)
    LocalAuthorSourceSelectionCount = if ($null -ne $local11AuthorSourceSummary) { @($local11AuthorSourceSummary.Selections).Count } else { 0 }
    LocalAuthorSourceStateRestored = [bool]$local11AuthorSourceRestored
    SaveTestMode = $SaveTestMode
    DisposableSaveFixtureRoot = $disposableSaveFixtureRootResolved
    SaveFixtureIsolation = Get-SmokeStatus -Requested $saveFixtureIsolationRequested -Passed $saveFixtureIsolationOk
    SaveFixtureIsolationCleanup = Get-SmokeStatus -Requested $saveFixtureIsolationRequested -Passed $saveFixtureIsolationCleanupOk
    DisposableSaveFixtureCleanup = if (-not $disposableSaveFixtureCleanupRequested) {
        if ($disposableSaveFixtureRequested -and
            $RetainDisposableSaveFixtureOnSuccess) {
            'RetainedForBoundedFollowUp'
        }
        else {
            'Skipped'
        }
    }
    elseif ($disposableSaveFixtureCleanupOk) {
        'Passed'
    }
    else {
        'RetainedOnFailure'
    }
    DisposableSaveFixtureRetentionRequested =
        [bool]$RetainDisposableSaveFixtureOnSuccess
    PlayerSaveRestored = Get-SmokeStatus -Requested ([bool]$RequirePlayerSaveRestore) -Passed $playerSaveRestoreOk
    PlayerSaveRestoreRequired = [bool]$RequirePlayerSaveRestore
    PlayerSaveUnchangedBeforeCleanup = Get-SmokeStatus -Requested $noNativeSaveMetadataRequested -Passed $playerSaveUnchangedBeforeCleanupOk
    CommittedSidecarsUnchangedBeforeCleanup = Get-SmokeStatus -Requested $noNativeSaveMetadataRequested -Passed $committedSidecarsUnchangedBeforeCleanupOk
    RoutinePlayerSaveByteBackupCreated = $false
    PlayerArchiveWritebackPerformed = $false
    QaG5WorldMutationCases = @($qaG5WorldMutationCases.ToArray())
    Batch5GcLadder = [ordered]@{
        Requested = [bool]$batch5GcLadderEnabled
        Domain = $Batch5GcLadderDomain
        Level = $Batch5GcLadderLevel
        Workload = $Batch5GcLadderWorkload
        Multiplier = $Batch5GcLadderMultiplier
        MeasureSeconds = $Batch5GcLadderMeasureSeconds
        SampleSeconds = $Batch5GcLadderSampleSeconds
        TargetUnits = $Batch5GcLadderTargetUnits
        SaveSlot = $SaveSlot
        ForcedGc = $false
    }
    Batch6AutoFishingPilot = Get-SmokeStatus -Requested ([bool]$Batch6AutoFishingPilot) -Passed $batch6AutoFishingPilotOk
    Batch6AutoFishingHandshake = Get-SmokeStatus -Requested ([bool]$Batch6AutoFishingPilot -and $Batch6AutoFishingLevel -ne 'L0') -Passed $batch6AutoFishingHandshakeOk
    Batch6AutoFishingInput = Get-SmokeStatus -Requested ([bool]$Batch6AutoFishingPilot -and $Batch6AutoFishingLevel -ne 'L0') -Passed $batch6AutoFishingInputOk
    Batch6AutoFishingCleanup = Get-SmokeStatus -Requested ([bool]$Batch6AutoFishingPilot) -Passed $batch6AutoFishingCleanupOk
    Batch6AutoFishing = [ordered]@{
        Requested = [bool]$Batch6AutoFishingPilot
        Level = $Batch6AutoFishingLevel
        Scenario = $Batch6AutoFishingScenario
        MeasureSeconds = $Batch6AutoFishingMeasureSeconds
        SampleSeconds = $Batch6AutoFishingSampleSeconds
        WarmupFish = $Batch6AutoFishingWarmupFish
        TargetFish = $Batch6AutoFishingTargetFish
        Formal = [bool]$Batch6AutoFishingFormal
        FormalContract = $Batch6AutoFishingFormalContract
        Multiplier = $Batch6AutoFishingMultiplier
        CastChargeRatio = $Batch6AutoFishingCastChargeRatio
        ManualMovementCancel = [bool]$Batch6AutoFishingManualMovementCancel
        ToggleKey = $autoFishingToggleKeyEffective
        ExpectedPackageSha256 = $Batch6AutoFishingExpectedPackageSha256
        ExpectedEntrySha256 = $Batch6AutoFishingExpectedEntrySha256
        ExpectedManifestSha256 = $Batch6AutoFishingExpectedManifestSha256
        ExpectedPolicySha256 = $Batch6AutoFishingExpectedPolicySha256
        SaveSlot = $SaveSlot
        ForcedGc = $false
        HandshakeReceipts = @($batch6AutoFishingHandshakeReceipts.ToArray())
        InputReceipts = @($batch6AutoFishingInputReceipts.ToArray())
    }
    Batch6AutoFishingManagerLifecycle = Get-SmokeStatus -Requested ([bool]$Batch6AutoFishingManagerLifecycle) -Passed $batch6AutoFishingManagerLifecycleOk
    Batch6AutoFishingManagerHandshake = Get-SmokeStatus -Requested ([bool]$Batch6AutoFishingManagerLifecycle -and $Batch6AutoFishingManagerMode -eq 'SameProcessDisable') -Passed $batch6AutoFishingManagerHandshakeOk
    Batch6AutoFishingManagerAction = Get-SmokeStatus -Requested ([bool]$Batch6AutoFishingManagerLifecycle -and $Batch6AutoFishingManagerMode -eq 'SameProcessDisable') -Passed $batch6AutoFishingManagerActionOk
    Batch6AutoFishingManagerCleanup = Get-SmokeStatus -Requested ([bool]$Batch6AutoFishingManagerLifecycle) -Passed $batch6AutoFishingManagerCleanupOk
    Batch6AutoFishingManagerMarkerRestored = Get-SmokeStatus -Requested ([bool]$Batch6AutoFishingManagerLifecycle) -Passed $batch6ManagerMarkerRestoreOk
    Batch6AutoFishingManager = [ordered]@{
        Requested = [bool]$Batch6AutoFishingManagerLifecycle
        Mode = $Batch6AutoFishingManagerMode
        ProductRoot = $Batch6AutoFishingManagerProductRoot
        MarkerSha256 = $Batch6AutoFishingManagerMarkerSha256
        ExpectedPackageSha256 = $Batch6AutoFishingManagerExpectedPackageSha256
        ExpectedEntrySha256 = $Batch6AutoFishingManagerExpectedEntrySha256
        ExpectedManifestSha256 = $Batch6AutoFishingManagerExpectedManifestSha256
        ExpectedPolicySha256 = $Batch6AutoFishingManagerExpectedPolicySha256
        SaveSlot = $SaveSlot
        ForcedGc = $false
        HandshakeReceipts = @($batch6AutoFishingManagerHandshakeReceipts.ToArray())
        InputReceipts = @($batch6AutoFishingManagerInputReceipts.ToArray())
        MarkerActionReceipts = @($batch6AutoFishingManagerMarkerActionReceipts.ToArray())
    }
    QaG5ExternalStateRestored = Get-SmokeStatus -Requested ([bool]$qaExternalStateProtectionRequested) -Passed ($playerSaveUnchangedBeforeCleanupOk -and $g5CommittedSidecarUnchangedOk -and $g5ConfigDirectoryRestored)
    QaG5CommittedSidecarUnchangedBeforeCleanup = Get-SmokeStatus -Requested ([bool]$qaG5AnyRequested -and $noNativeSaveMetadataRequested) -Passed $g5CommittedSidecarUnchangedOk
    QaG5ConfigDirectoryRestored = Get-SmokeStatus -Requested ([bool]$qaExternalStateProtectionRequested) -Passed $g5ConfigDirectoryRestored
    OfficialModProfileAppliedGate = Get-SmokeAlwaysStatus -Passed $officialModProfileAppliedOk
    OfficialModProfileExtraEnabledGate = Get-SmokeAlwaysStatus -Passed $officialModProfileExtraEnabledOk
    OfficialModProfileOilDisabledGate = Get-SmokeAlwaysStatus -Passed $officialModProfileOilDisabledOk
    OfficialModProfilePublishedProductSelectionGate = Get-SmokeAlwaysStatus -Passed $publishedProductSelectionOk
    OfficialModProfileEnabledIds = @($officialModProfileSummary.EnabledIds)
    OfficialModProfileDisabledIds = @($officialModProfileSummary.DisabledIds)
    StartupLog = Get-SmokeAlwaysStatus -Passed $startupOk
    GameLaunched = Get-SmokeAlwaysStatus -Passed $gameLaunchedOk
    HookProbe = Get-SmokeStatus -Requested ([bool]$IncludeHookProbe) -Passed $probeOk
    QaHostStaged = Get-SmokeStatus -Requested ([bool]$StageQaHost) -Passed ($null -ne $qaHostStage)
    QaHostRunId = if ($null -ne $qaHostStage) { [string]$qaHostStage.RunId } else { '' }
    QaHostLifecycle = Get-SmokeStatus -Requested ([bool]$StageQaHost) -Passed $qaHostLifecycleOk
    QaHostCleanup = Get-SmokeStatus -Requested ([bool]$StageQaHost) -Passed $qaHostCleanupOk
    NoQaOrdinaryPlayerHandshake = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaOrdinaryPlayerHandshakeShown
    NoQaRunnerBudgetSeconds = $noQaRunnerBudgetSeconds
    NoQaRunnerBudgetSource = $noQaRunnerBudgetSource
    NoQaRunnerDeadline = if ($null -ne $noQaRunnerDeadline) { $noQaRunnerDeadline.ToString('o') } else { '' }
    NoQaSaveLoadedWaitSeconds = $noQaSaveLoadedWaitSeconds
    NoQaSaveLoadedDeadlineExpired = $noQaSaveLoadedDeadlineExpired
    NoQaProcessWaitSeconds = $noQaProcessWaitSeconds
    NoQaBehaviorCompletedAt = if ($null -ne $noQaBehaviorCompletedAt) { $noQaBehaviorCompletedAt.ToString('o') } else { '' }
    NoQaBehaviorRemainingMilliseconds = $noQaBehaviorRemainingMilliseconds
    NoQaBehaviorCompletedBeforeDeadline = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaBehaviorCompletedBeforeDeadlineOk
    NoQaUiEvidence = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaUiEvidenceGateOk
    Issue011Acceptance = Get-SmokeStatus -Requested ([bool]$Issue011Acceptance) -Passed $issue011AcceptanceOk
    Issue011AcceptanceReceiptPath = $issue011AcceptanceReceiptPath
    AutoDriveNoQaAnimalViewer = [bool]$AutoDriveNoQaAnimalViewer
    NoQaPublishedOwnerSet = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaPublishedOwnerSetOk
    NoQaLegacyEvidenceUnchanged = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaLegacyEvidenceUnchangedOk
    NoQaRuntimePreflight = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaRuntimePreflightOk
    NoQaRuntimePostflight = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaRuntimePostflightOk
    NoQaPublishedArtifacts = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence -and $OfficialModProfile -eq 'Published11') -Passed $publishedProductArtifactsOk
    NoQaAnimalViewerTwoCausalRenders = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaAnimalViewerUiOk
    NoQaAnimalViewerRenderReceiptCount = $noQaAnimalViewerRenderReceiptCount
    NoQaAnimalViewerNativeClose = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaAnimalViewerNativeCloseOk
    NoQaAnimalViewerOverlayClear = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaAnimalViewerOverlayClearOk
    NoQaAnimalViewerCloseOrder = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaAnimalViewerCloseOrderOk
    NoQaAnimalViewerClose = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaAnimalViewerCloseOk
    NoQaAnimalViewerInputProvenance = Get-SmokeStatus -Requested ([bool]$AssertNoQaUiEvidence) -Passed $noQaAnimalViewerInputProvenanceOk
    NoQaAnimalViewerAutoDriveInputProvenance = Get-SmokeStatus -Requested ([bool]$AutoDriveNoQaAnimalViewer) -Passed $noQaAnimalViewerAutoDriveProvenanceOk
    OwnerLifetimeCloseCleanup = Get-SmokeStatus -Requested ([bool]$StageQaHost) -Passed $ownerLifetimeCloseCleanupOk
    OwnerLifetimeCloseCleanupSummary = $ownerLifetimeCloseCleanupSummary
    QaHostMarkerCounts = $qaHostMarkerCounts
    QaFishRoeTooltip = Get-SmokeStatus -Requested $qaFishRoeTooltipObservationEnabled -Passed $fishRoeTooltipOk
    ItemDisplayNameLifecycle = Get-SmokeStatus -Requested $itemDisplayNameLifecycleRequested -Passed $itemDisplayNameLifecycleOk
    QaDiagnosticsSnapshot = Get-SmokeStatus -Requested $qaDiagnosticsSnapshotEnabled -Passed $qaDiagnosticsSnapshotOk
    QaSaveLoadedObservation = Get-SmokeStatus -Requested ([bool]$QaObserveSaveLoaded) -Passed $qaSaveLoadedObservationOk
    QaSaveSavedObservation = Get-SmokeStatus -Requested ([bool]$QaObserveSaveSaved) -Passed $qaSaveSavedObservationOk
    QaWorkshopReloadCompletedObservation = Get-SmokeStatus -Requested ([bool]$QaObserveWorkshopReloadCompleted) -Passed $qaWorkshopReloadObservationOk
    SaveLoaded = Get-SmokeStatus -Requested $saveLoadedRequested -Passed $saveLoadedOk
    LifecycleObservation = Get-SmokeStatus -Requested $refactorLifecycleObservationRequested -Passed $refactorLifecycleObservationOk
    LifecycleBoundaryContract = Get-SmokeStatus -Requested $refactorLifecycleBoundaryContractRequested -Passed $refactorLifecycleBoundaryContractOk
    LifecycleBoundaryContractSummary = $refactorLifecycleBoundaryContractSummary
    ShadowContentRegistry = Get-SmokeStatus -Requested $refactorShadowContentRegistryRequested -Passed $refactorShadowContentRegistryOk
    ContentRegistry = Get-SmokeStatus -Requested $contentManifestRegistryRequested -Passed $contentRegistryOk
    ManifestRegistry = Get-SmokeStatus -Requested $contentManifestRegistryRequested -Passed $manifestRegistryOk
    DependencyCompatibility = Get-SmokeStatus -Requested $contentManifestRegistryRequested -Passed $dependencyCompatibilityOk
    PublishedProductCombination = Get-SmokeStatus -Requested $publishedProductGateRequested -Passed $publishedProductGateOk
    PublishedProductCombinationMode = $publishedProductGateMode
    PublishedProductOwnerCounts = $publishedProductOwnerCounts
    PublishedProductHotRefreshLoadedNowZeroCount = $publishedProductHotRefreshCount
    PublishedProductArtifactGate = Get-SmokeStatus -Requested $publishedProductGateRequested -Passed $publishedProductArtifactsOk
    HookProbeAbsent = Get-SmokeStatus -Requested $hookProbeAbsenceRequested -Passed $hookProbeAbsenceOk
    ExternalPlayerInput = Get-SmokeStatus -Requested ([bool]$RequireExternalPlayerInputGate) -Passed $externalPlayerInputGateOk
    MissingFrameFallbackWarningCount = $missingFrameFallbackWarningCount
    FrameDriverStallWarningCount = $frameDriverStallWarningCount
    InputSystemResubscribeCount = $inputSystemResubscribeCount
    PlayerLoopLifecycleRefreshCount = $playerLoopLifecycleRefreshCount
    ProductOwnerRefresh = Get-SmokeStatus -Requested $productOwnerRefreshRequested -Passed $productOwnerRefreshOk
    ProductOwnerRefreshOwners = $productOwnerRefreshCounts
    ProductOwnerRefreshReloadDispatchCount = $productOwnerRefreshReloadDispatchCount
    ProductOwnerRefreshHotStatusCount = $productOwnerRefreshHotStatusCount
    ProductOwnerRefreshReturnHomeOrchestration = Get-SmokeStatus -Requested $productOwnerRefreshRequested -Passed $productOwnerRefreshOrchestrationOk
    ProductOwnerRefreshDependencyCompatibility = Get-SmokeStatus -Requested $productOwnerRefreshRequested -Passed $productOwnerRefreshDependencyOk
    ProductOwnerRefreshCleanup = Get-SmokeStatus -Requested $productOwnerRefreshRequested -Passed $productOwnerRefreshCleanupOk
    AdvancedProductOwnerDeactivation = Get-SmokeStatus -Requested $advancedProductOwnerDeactivationRequested -Passed $advancedProductOwnerDeactivationOk
    AdvancedProductOwnerDeactivationSummary = $advancedProductOwnerDeactivationSummary
    ContentPackOwnership = Get-SmokeStatus -Requested $contentManifestRegistryRequested -Passed $contentPackOwnershipOk
    RegistryDiffs = Get-SmokeStatus -Requested $contentManifestRegistryRequested -Passed $registryDiffsOk
    RefactorScaffoldFlags = Get-SmokeAlwaysStatus -Passed $refactorFlagsOk
    RefactorScaffoldFlagsSummary = $refactorFlagsSummary
    ResourceLifecycleLedger = Get-SmokeStatus -Requested $resourceLifecycleLedgerRequested -Passed $resourceLifecycleLedgerOk
    ResourceLifecycleCleanup = Get-SmokeStatus -Requested $resourceLifecycleCleanupRequested -Passed $resourceLifecycleCleanupOk
    ResourceLifecycleSummary = $resourceLifecycleSummary
    TitleIdleResourceGrowth = Get-SmokeStatus -Requested $resourceLifecycleLedgerRequested -Passed $titleIdleResourceGrowthOk
    SaveLoadRequestCoordinator = Get-SmokeStatus -Requested $saveLoadRequestCoordinatorRequested -Passed $saveLoadRequestCoordinatorOk
    SaveLoadRequestSummary = $saveLoadRequestSummary
    DuplicateLoadRequests = Get-SmokeStatus -Requested $saveLoadRequestCoordinatorRequested -Passed $duplicateLoadRequestsOk
    SaveLoadBoundary = Get-SmokeStatus -Requested ($saveLoadRequestCoordinatorRequested -and $saveLoadedRequested) -Passed $saveLoadBoundaryOk
    TitleReturnBoundaryLedger = Get-SmokeStatus -Requested $titleReturnBoundaryRequested -Passed $titleReturnBoundaryOk
    TitleReturnBoundarySummary = $titleReturnBoundarySummary
    SaveLoadCycleObjectDeltaLedger = Get-SmokeStatus -Requested $saveLoadCycleObjectDeltaLedgerRequested -Passed $saveLoadCycleObjectDeltaLedgerOk
    SaveLoadCycleObjectDeltaSummary = $saveLoadCycleObjectDeltaLedgerSummary
    HookScheduler = Get-SmokeStatus -Requested $hookInstallSchedulerRequested -Passed $hookSchedulerOk
    CoreHookReadiness = Get-SmokeStatus -Requested $hookReadinessLayersRequested -Passed $coreHookReadinessOk
    FeatureHookReadiness = Get-SmokeStatus -Requested $hookReadinessLayersRequested -Passed $featureHookReadinessOk
    HookStatusQueue = Get-SmokeStatus -Requested $hookStatusQueueRequested -Passed $hookStatusQueueOk
    OffThreadHookRequests = Get-SmokeStatus -Requested $hookInstallSchedulerRequested -Passed $offThreadHookRequestsOk
    AssemblyLoadSubscription = Get-SmokeStatus -Requested $hookInstallSchedulerRequested -Passed $assemblyLoadSubscriptionOk
    RetryTimerAlive = Get-SmokeStatus -Requested $hookInstallSchedulerRequested -Passed $retryTimerAliveOk
    ModOwnerLifecycle = Get-SmokeStatus -Requested $modOwnerLifecycleRequested -Passed $modOwnerLifecycleOk
    ModOwnerLifecycleSummary = $modOwnerLifecycleSummary
    ModLoadTransaction = Get-SmokeStatus -Requested $modLoadTransactionRequested -Passed $modLoadTransactionOk
    OwnerBoundInput = Get-SmokeStatus -Requested $ownerBoundInputRequested -Passed $ownerBoundInputOk
    EventHandlerCleanup = Get-SmokeStatus -Requested $eventHandlerCleanupRequested -Passed $eventHandlerCleanupOk
    ConfigPreviewAudit = Get-SmokeStatus -Requested $configPreviewAuditRequested -Passed $configPreviewAuditOk
    FailedModRollback = Get-SmokeStatus -Requested $modOwnerLifecycleRequested -Passed $failedModRollbackOk
    GameBridgeFeatureContracts = Get-SmokeStatus -Requested $gameBridgeFeatureContractsRequested -Passed $gameBridgeFeatureContractsOk
    GameBridgeFinalHealthSnapshot = Get-SmokeStatus -Requested $gameBridgeFinalHealthSnapshotRequested -Passed $gameBridgeFinalHealthSnapshotOk
    GameBridgeFinalHealthSummary = $gameBridgeFinalHealthSummary
    LongTitleIdleBeforeSave = Get-SmokeStatus -Requested ($TitleIdleBeforeSaveSeconds -gt 0) -Passed ($saveLoadedOk -and $fatalWindows.Count -eq 0)
    TitleIdleBeforeSaveSeconds = $TitleIdleBeforeSaveSeconds
    TitleSettingsButton = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleButtonOk
    TitleSettingsButtonScreenshot = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleButtonScreenshotOk
    TitleSettingsButtonScreenshotFile = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleButtonScreenshotFileOk
    TitleButtonLifecycle = Get-SmokeStatus -Requested ([bool]$AutoExerciseTitleButtonLifecycle) -Passed $titleLifecycleOk
    OwnerLifetime = Get-SmokeStatus -Requested ([bool]$AutoExerciseModOwnerLifetime) -Passed $ownerLifetimeOk
    OwnerLifetimeSummary = if ($ownerLifetimeLine) { $ownerLifetimeLine.Line } else { '' }
    SaveLoadCycle = Get-SmokeStatus -Requested ([bool]$AutoExerciseSaveLoadCycle) -Passed $saveLoadCycleOk
    SaveLoadCycleSummary = $saveLoadCycleSummary
    SaveLoadObjectSnapshotMode = $SaveLoadObjectSnapshotMode
    SmokeRootIsolationProfile = $SmokeRootIsolationProfile
    SmokeRootIsolationDisabled = $smokeRootIsolationDisabled
    SmokeRootIsolationUnsupported = $smokeRootIsolationUnsupported
    SmokeRootIsolationActive = $smokeRootIsolationActive
    SmokeRootIsolationSummary = $smokeRootIsolationSummaryLine
    SmokeOwnerRootIsolationProfile = $SmokeOwnerRootIsolationProfile
    SmokeOwnerRootIsolationSummary = $ownerRootTypeIsolationSummaryLine
    SmokeNativeLoadContinuationProbe = $SmokeNativeLoadContinuationProbe
    SmokeNativeLoadContinuationCleanup = Get-SmokeStatus -Requested ($SmokeNativeLoadContinuationProbe -eq 'VersionPatcher') -Passed $nativeContinuationProbeCleanupOk
    SmokeNativeLoadContinuationSummary = $nativeContinuationProbeSummaryLine
    LastNativeContinuationLine = $nativeContinuationLastLine
    CrashBaseline = Get-SmokeAlwaysStatus -Passed (Test-Path -LiteralPath (Join-Path $evidence 'crash-baseline.txt') -PathType Leaf)
    FatalWindowProcessDumpMode = $FatalWindowProcessDumpMode
    FatalWindowProcessDump = $fatalProcessDumpStatus
    FatalWindowPostCloseCrashDumpWaitSeconds = $FatalWindowPostCloseCrashDumpWaitSeconds
    UnityCrashFreshness = $unityCrashFreshnessStatus
    UnityCrashFreshnessSummary = $unityCrashFreshnessSummary
    PreLoadForcedGCProbe = Get-SmokeStatus -Requested $preLoadGcProbeRequested -Passed $preLoadGcProbeOk
    PreLoadForcedGCProbeSummary = $preLoadGcProbeSummary
    SaveLoadCyclePendingPressure = Get-SmokeStatus -Requested ([bool]$AutoExerciseSaveLoadCyclePendingPressure) -Passed $saveLoadCyclePendingPressureOk
    SaveLoadCyclePendingPressureSummary = $saveLoadCyclePendingPressureSummary
    TitleSettingsMenu = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleMenuOk
    TitleSettingsMenuScreenshot = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleMenuScreenshotOk
    TitleSettingsMenuScreenshotFile = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleMenuScreenshotFileOk
    ManagerStatusPage = Get-SmokeStatus -Requested $managerStatusRequested -Passed $managerStatusPageOk
    ManagerStatusPageScreenshot = Get-SmokeStatus -Requested $managerStatusRequested -Passed $managerStatusPageScreenshotOk
    ManagerStatusPageScreenshotFile = Get-SmokeStatus -Requested $managerStatusRequested -Passed $managerStatusPageScreenshotFileOk
    ManagerStatusSummaryText = Get-SmokeStatus -Requested $managerStatusRequested -Passed $managerStatusSummaryTextOk
    ManagerStatusSummaryCopy = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerStatusSummaryCopyOk
    ManagerModsPage = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerModsPageOk
    ManagerModsPageScreenshotFile = Get-SmokeStatus -Requested $qaG4ManagerMvpUiEnabled -Passed $managerModsPageScreenshotFileOk
    ManagerPlayerInteractions = Get-SmokeStatus -Requested $qaG4ManagerMvpUiEnabled -Passed $managerPlayerInteractionsOk
    ManagerErrorsPage = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerErrorsPageOk
    ManagerHooksPage = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerHooksPageOk
    ManagerFeaturesPage = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerFeaturesPageOk
    ManagerAdvancedPageScreenshotFile = Get-SmokeStatus -Requested $qaG4ManagerMvpUiEnabled -Passed $managerAdvancedPageScreenshotFileOk
    ManagerLogsPage = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerLogsPageOk
    ManagerLogsExportButton = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerLogsExportButtonOk
    ManagerLogsExportStateText = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerLogsExportStateTextOk
    ManagerLogsPageScreenshot = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerLogsPageScreenshotOk
    ManagerLogsPageScreenshotFile = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerLogsPageScreenshotFileOk
    OfficialModUi = Get-SmokeStatus -Requested ([bool]$AutoOpenOfficialModUi) -Passed $officialModUiOk
    OfficialModUiScreenshotFile = Get-SmokeStatus -Requested ([bool]$AutoOpenOfficialModUi) -Passed $officialModUiScreenshotFileOk
    AnimalViewerUi = Get-SmokeStatus -Requested ([bool]$AutoOpenAnimalPanel) -Passed $animalViewerUiOk
    AnimalViewerUiScreenshotFile = Get-SmokeStatus -Requested $qaG4AnimalObservationEnabled -Passed $animalViewerUiScreenshotFileOk
    EquipmentSlotsUiScreenshotFile = Get-SmokeStatus -Requested $qaG4EquipmentSlotsObservationEnabled -Passed $equipmentSlotsUiScreenshotFileOk
    EquipmentSlotsUiObservation = Get-SmokeStatus -Requested $qaG4EquipmentSlotsObservationEnabled -Passed $equipmentSlotsUiObservationOk
    EquipmentSlotsUiEvidenceCaptured = Get-SmokeStatus -Requested $qaG4EquipmentSlotsObservationEnabled -Passed $equipmentSlotsUiEvidenceCapturedOk
    EquipmentSlotsUiHover = Get-SmokeStatus -Requested $qaG4EquipmentSlotsObservationEnabled -Passed $equipmentSlotsUiHoverOk
    EquipmentSlotsUiCloseInput = Get-SmokeStatus -Requested $qaG4EquipmentSlotsObservationEnabled -Passed $equipmentSlotsUiCloseInputOk
    EquipmentSlotsPlayerInputAttempts = @($equipmentSlotsPlayerInputAttempts.ToArray())
    AnimalViewerPlayerInputAttempts = @($animalViewerPlayerInputAttempts.ToArray())
    ExperimentalHooks = Get-SmokeStatus -Requested ([bool]$AutoExerciseExperimentalHooks) -Passed $experimentalHooksOk
    ActionSpeedTool = Get-SmokeStatus -Requested ([bool]$AutoExerciseActionSpeedTool) -Passed $actionSpeedToolOk
    ActionSpeedConfigApply = Get-SmokeStatus -Requested ([bool]$AutoExerciseActionSpeedConfigApply) -Passed $actionSpeedConfigApplyOk
    ActionSpeedInteraction = Get-SmokeStatus -Requested ([bool]$AutoExerciseActionSpeedInteraction) -Passed $actionSpeedInteractionOk
    OneActionResourceHit = Get-SmokeStatus -Requested ([bool]$AutoExerciseOneActionResourceHit) -Passed $oneActionResourceHitOk
    OneActionPartialEnergy = Get-SmokeStatus -Requested ([bool]$AutoExerciseOneActionResourceHit) -Passed $oneActionPartialEnergyOk
    OneActionConfigReload = Get-SmokeStatus -Requested ([bool]$AutoExerciseOneActionResourceHit) -Passed $oneActionConfigReloadOk
    OneActionWrongTool = Get-SmokeStatus -Requested ([bool]$AutoExerciseOneActionWrongTool) -Passed $oneActionWrongToolOk
    OneActionFuelFeed = Get-SmokeStatus -Requested ([bool]$AutoExerciseOneActionFuelFeed) -Passed $oneActionFuelFeedOk
    OneActionVegetation = Get-SmokeStatus -Requested ([bool]$AutoExerciseOneActionVegetation) -Passed $oneActionVegetationOk
    OneActionMenuKey = Get-SmokeStatus -Requested ([bool]$AutoPressOneActionMenuKey) -Passed $oneActionMenuKeyOk
    OneActionMenuInputEvidence = $oneActionMenuInputEvidence
    AutoFishingInputLog = Get-SmokeStatus -Requested ([bool]$AutoPressAutoFishingHotkey) -Passed $autoFishingInputLogOk
    AutoFishingHotkey = Get-SmokeStatus -Requested ([bool]$AutoPressAutoFishingHotkey -or ([bool]$AutoExerciseAutoFishingPhase -and -not ($autoFishingPerformanceBaseline -and $AutoFishingPerformanceProfile -eq 'InactiveNoConsumer'))) -Passed $autoFishingHotkeyOk
    AutoFishingMovementCancel = Get-SmokeStatus -Requested $autoFishingMovementRequested -Passed $autoFishingMovementCancelOk
    LegacyFishingAutomationCompatibility = Get-SmokeStatus -Requested ([bool]$AutoExerciseLegacyFishingCompatibility) -Passed $legacyFishingCompatibilityOk
    AutoFishingPhase = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase) -Passed $autoFishingPhaseOk
    AutoFishingPerformanceCounter = Get-SmokeStatus -Requested ([bool]$AutoFishingPerformance) -Passed ($autoFishingPhaseOk -and -not $autoFishingPerformanceCounterBlocked) -Blocked $autoFishingPerformanceCounterBlocked
    AutoFishingMonoGate = Get-SmokeStatus -Requested ([bool]$AutoFishingMonoGate) -Passed $autoFishingMonoGateOk
    AutoFishingScenario = $autoFishingScenarioEffective
    AutoFishingPerformanceProfile = $AutoFishingPerformanceProfile
    AutoFishingInstantBite = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase -and $autoFishingScenarioRequestsInstantBite) -Passed $autoFishingInstantBiteOk
    AutoFishingMiniGameSkip = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase -and $autoFishingScenarioRequestsSkip) -Passed $autoFishingMiniGameSkipOk
    AutoFishingMiniGameComplete = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase -and $autoFishingScenarioRequestsComplete) -Passed $autoFishingMiniGameCompleteOk
    AutoFishingAnimationSpeed = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase -and $autoFishingScenarioRequestsAnimationSpeed) -Passed $autoFishingAnimationSpeedOk
    AutoFishingCastCharge = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase -and $autoFishingScenarioRequestsCastCharge) -Passed $autoFishingCastChargeOk
    AutoFishingLifecycle = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase) -Passed $autoFishingLifecycleOk
    AutoFishingLifecycleSummary = $autoFishingLifecycleSummary
    AutoFishingSoak = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase -and $AutoFishingSoakLoops -gt 0) -Passed $autoFishingSoakOk
    AutoFishingReportExport = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase) -Passed $autoFishingReportExportOk
    PauseMenuLayout = Get-SmokeStatus -Requested ([bool]$AutoExercisePauseMenuLayout) -Passed $pauseMenuLayoutOk
    PauseMenuLayoutScreenshotFile = Get-SmokeStatus -Requested $qaG4PauseMenuLayoutEnabled -Passed $pauseMenuLayoutScreenshotFileOk
    DiagnosticsReportExport = Get-SmokeStatus -Requested $diagnosticsReportExportRequested -Passed $diagnosticsReportExportOk
    InstantSave = Get-SmokeStatus -Requested ([bool]$AutoExerciseInstantSave) -Passed $instantSaveOk
    DebugConsoleSaveAcceptance =
        Get-SmokeStatus `
            -Requested ([bool]$debugConsoleSaveAcceptanceRequested) `
            -Passed $debugConsoleSaveAcceptanceOk
    DebugConsoleSaveAcceptancePhase =
        $DebugConsoleSaveAcceptancePhase
    ExpectedDebugConsoleMoney =
        $ExpectedDebugConsoleMoney
    DebugConsoleOpenY1 = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleOpenY1Ok
    DebugConsoleMouseGive = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugConsoleMouseGive) -Passed $debugConsoleMouseGiveOk
    DebugConsoleCloseEscape = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleCloseEscapeOk
    DebugConsoleOpenY2 = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleOpenY2Ok
    DebugConsoleCloseY = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleCloseYOk
    DebugConsoleTenYShortTaps = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleTenYShortTapsOk
    DebugConsoleHoldYNoFlicker = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleHoldYNoFlickerOk
    DebugConsoleQaEvidenceReady = Get-SmokeStatus -Requested $qaG4DebugConsoleEnabled -Passed $qaG4DebugEvidenceReadyOk
    DebugConsolePostHoldClose = Get-SmokeStatus -Requested ([bool]$RequireExternalPlayerInputGate -or $qaG4DebugConsoleEnabled -or [bool]$AssertNoQaUiEvidence) -Passed $debugConsolePostHoldCloseOk
    DebugConsoleTitleUiCleanup = Get-SmokeStatus -Requested $debugConsoleTitleUiCleanupRequested -Passed $debugConsoleTitleUiCleanupOk
    DebugConsoleTitleUiCleanupSummary = $debugConsoleTitleUiCleanupSummary
    DebugConsoleScreenshotFile = Get-SmokeStatus -Requested $qaG4DebugConsoleEnabled -Passed $debugConsoleScreenshotFileOk
    DebugInventory = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugInventory) -Passed $debugInventoryOk
    DebugWeather = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugWeather) -Passed $debugWeatherOk
    DebugTeleportCsv = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugTeleport) -Passed $debugTeleportCsvOk
    DebugTeleport = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugTeleport) -Passed $debugTeleportOk
    DebugTime = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugTime) -Passed $debugTimeOk
    DebugMovement = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugMovement) -Passed $debugMovementOk
    AdvancedDebug = Get-SmokeStatus -Requested ([bool]$AutoExerciseAdvancedDebug) -Passed $advancedDebugOk
    VehicleSecondMotor = Get-SmokeStatus -Requested $false -Passed $vehicleSecondMotorOk
    Zoom = Get-SmokeStatus -Requested ([bool]$AutoExerciseZoom) -Passed $zoomOk
    ZoomProductNative = Get-SmokeStatus -Requested ([bool]$AutoExerciseZoomProductNative) -Passed $zoomProductNativeOk
    CameraPlayableEvidenceFiles = Get-SmokeStatus -Requested $qaG4CameraPlayableEnabled -Passed $cameraPlayableEvidenceFilesOk
    ChestLocatorEnhancer = Get-SmokeStatus -Requested ([bool]$AutoExerciseChestLocatorEnhancer) -Passed $chestLocatorEnhancerOk
    MoreEquipmentSlots = Get-SmokeStatus -Requested ([bool]$AutoExerciseMoreEquipmentSlots) -Passed $moreEquipmentSlotsOk
    MoreEquipmentSlotsProtectedTransaction = Get-SmokeStatus -Requested ([bool]$AutoExerciseMoreEquipmentSlots) -Passed $moreEquipmentSlotsProtectedTransactionOk
    MoreEquipmentSlotsCommittedShieldSetup = Get-SmokeStatus -Requested ([bool]$SetupMoreEquipmentSlotsCommittedShield) -Passed $moreEquipmentSlotsCommittedShieldSetupOk
    MoreEquipmentSlotsCommittedShieldDamageNoNativeSave = Get-SmokeStatus -Requested ([bool]$DamageMoreEquipmentSlotsCommittedShieldNoNativeSave) -Passed $moreEquipmentSlotsCommittedShieldDamageOk
    MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave = Get-SmokeStatus -Requested ([bool]$BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave) -Passed $moreEquipmentSlotsCommittedShieldBreakReplaceOk
    MoreEquipmentSlotsInterruptedCandidateRecovery = Get-SmokeStatus -Requested ([bool]$ObserveMoreEquipmentSlotsInterruptedCandidateRecovery) -Passed $moreEquipmentSlotsInterruptedCandidateRecoveryOk
    MoreEquipmentSlotsNoNativeSave = Get-SmokeStatus -Requested ([bool]$AutoExerciseMoreEquipmentSlotsNoNativeSave) -Passed $moreEquipmentSlotsNoNativeSaveOk
    MoreEquipmentSlotsNoNativeSaveColdObserver = Get-SmokeStatus -Requested ([bool]$ObserveMoreEquipmentSlotsNoNativeSaveCold) -Passed $moreEquipmentSlotsNoNativeSaveColdOk
    MoreEquipmentSlotsTransition =
        Get-SmokeStatus `
            -Requested ([bool]$moreEquipmentSlotsTransitionRequested) `
            -Passed $moreEquipmentSlotsTransitionOk
    MoreEquipmentSlotsTransitionPhase =
        $MoreEquipmentSlotsTransitionPhase
    MoreEquipmentSlotsTransitionSleepInput =
        $moreEquipmentSlotsTransitionSleepInput
    MoreEquipmentSlotsRetainedWorkshopPreflight =
        Get-SmokeStatus `
            -Requested ($MoreEquipmentSlotsTransitionPhase -eq 'U1') `
            -Passed $moreEquipmentSlotsRetainedWorkshopPreflightOk
    MoreEquipmentSlotsRetainedWorkshopArtifact =
        $moreEquipmentSlotsRetainedWorkshopArtifactSnapshot
    MoreEquipmentSlotsColdRecovery = Get-SmokeStatus -Requested ([bool]$moreEquipmentSlotsColdRecoveryTransitionRequested) -Passed $moreEquipmentSlotsColdRecoveryOk
    MoreEquipmentSlotsColdOfficialPackage = Get-SmokeStatus -Requested ([bool]$moreEquipmentSlotsColdRecoveryTransitionRequested) -Passed $moreEquipmentSlotsColdOfficialPackagePreflightOk
    MoreEquipmentSlotsColdOfficialDisabled = Get-SmokeStatus -Requested ([bool]$moreEquipmentSlotsColdRecoveryTransitionRequested) -Passed $moreEquipmentSlotsColdOfficialDisabledOk
    MoreEquipmentSlotsColdRecoveryDemand = Get-SmokeStatus -Requested ($MoreEquipmentSlotsTransitionPhase -eq 'ColdCommit') -Passed $moreEquipmentSlotsColdRecoveryDemandOk
    MoreEquipmentSlotsColdRecoveryHost = Get-SmokeStatus -Requested ($MoreEquipmentSlotsTransitionPhase -eq 'ColdCommit') -Passed $moreEquipmentSlotsColdRecoveryHostOk
    MoreEquipmentSlotsColdRecoveryDestination = Get-SmokeStatus -Requested ($MoreEquipmentSlotsTransitionPhase -eq 'ColdCommit') -Passed $moreEquipmentSlotsColdRecoveryDestinationOk
    MoreSavesOfficialSaveUi = Get-SmokeStatus -Requested ([bool]$AutoExerciseMoreSavesOfficialSaveUi) -Passed $moreSavesOfficialSaveUiOk
    MoreSavesOfficialSaveUiEvidence = Get-SmokeStatus -Requested ([bool]$AutoExerciseMoreSavesOfficialSaveUi) -Passed $moreSavesOfficialSaveUiEvidenceOk
    MoreSavesOfficialSaveUiScreenshotFile = Get-SmokeStatus -Requested $qaG4SaveSlotsPagingEnabled -Passed $saveSlotsUiScreenshotFileOk
    MoreSavesFixed12AcceptancePhase = $MoreSavesFixed12AcceptancePhase
    MoreSavesFixed12OfficialLocalRootMode = $moreSavesFixed12OfficialLocalRootMode
    MoreSavesFixed12OfficialLocalProductRoot = $moreSavesFixed12OfficialLocalProductRoot
    MoreSavesFixed12Lifecycle = Get-SmokeStatus -Requested ([bool]$moreSavesFixed12AcceptanceRequested) -Passed $moreSavesFixed12LifecycleOk
    StrongPlantingGun = Get-SmokeStatus -Requested ([bool]$AutoExerciseStrongPlantingGun) -Passed $strongPlantingGunOk
    CropHarvestingApi = Get-SmokeStatus -Requested ([bool]$AutoExerciseCropHarvestingApi) -Passed $cropHarvestingApiOk
    CropHarvestingApiEvidence = Get-SmokeStatus -Requested ([bool]$AutoExerciseCropHarvestingApi) -Passed $cropHarvestingApiEvidenceOk
    CustomEntityApis = Get-SmokeStatus -Requested ([bool]$AutoExerciseCustomEntityApis) -Passed $customEntityApisOk
    AudioReplacement = Get-SmokeStatus -Requested ([bool]$AutoExerciseAudioReplacement) -Passed $audioReplacementOk
    HatchAnimalVoice = Get-SmokeStatus -Requested ([bool]$AutoExerciseHatchAnimalVoice) -Passed $hatchAnimalVoiceOk
    ContentMetadataObservation = Get-SmokeStatus -Requested $qaG4ContentMetadataObservationEnabled -Passed $contentMetadataObservationOk
    NewContentApis = Get-SmokeStatus -Requested ([bool]$AutoExerciseNewContentApis) -Passed $newContentApisOk
    NewContentMineApis = Get-SmokeStatus -Requested ([bool]$AutoExerciseMineContentApis) -Passed $newContentMineApisOk
    NewContentOilAbsent = Get-SmokeStatus -Requested ([bool]$ExpectOilAbsent) -Passed $newContentOilAbsentOk
    NewContentOilOnly = Get-SmokeStatus -Requested ([bool]$OilOnly) -Passed $newContentOilOnlyOk
    NewContentOilItemMetadata = Get-SmokeStatus -Requested $positiveNewContentRequested -Passed $newContentOilItemMetadataOk
    NewContentOilCoalDrop = Get-SmokeStatus -Requested $positiveNewContentRequested -Passed $newContentOilCoalDropOk
    NewContentMineOfficialJson = Get-SmokeStatus -Requested ($fullNewContentRequested -or [bool]$AutoExerciseMineContentApis) -Passed $newContentMineOfficialJsonOk
    NewContentMineOfficialTechTreeUi = Get-SmokeStatus -Requested ([bool]$AutoExerciseMineContentApis) -Passed $newContentMineOfficialTechTreeUiOk
    NewContentMineOfficialTechTreeUiScreenshotFile = Get-SmokeStatus -Requested ([bool]$AutoExerciseMineContentApis) -Passed $newContentMineOfficialTechTreeUiScreenshotFileOk
    NewContentEquipmentSlots = Get-SmokeStatus -Requested $fullNewContentRequested -Passed $newContentEquipmentSlotsOk
    NewContentMineProduction = Get-SmokeStatus -Requested ($fullNewContentRequested -or [bool]$AutoExerciseMineContentApis) -Passed $newContentMineProductionOk
    NoFatalInstanceWindow = Get-SmokeAlwaysStatus -Passed ($fatalWindows.Count -eq 0)
    ProcessExited = Get-SmokeAlwaysStatus -Passed (-not [bool]$leftover)
    ForcedClose = Get-SmokeAlwaysStatus -Passed (-not $forcedClose)
    Completed = Get-Date -Format o
}
Write-SmokeJsonObject -Path (Join-Path $evidence 'result.json') -Value $result
if ($Issue011Acceptance) {
    $issue011AcceptanceProjection['SmokeResult'] = Get-SmokeIssue011FileReceipt -Path (Join-Path $evidence 'result.json')
    Write-SmokeJsonObject -Path $issue011AcceptanceReceiptPath -Value $issue011AcceptanceProjection
}
try {
    & "$PSScriptRoot\analyze-startup-evidence.ps1" -EvidencePath $evidence -OutputDirectory $evidence -Quiet
}
catch {
    $_ | Out-String | Set-Content -LiteralPath (Join-Path $evidence 'startup-analysis-error.txt')
}
$machineEvidencePath = [System.IO.Path]::GetFullPath($evidence)
Write-Output ('DTMAPI_SMOKE_EVIDENCE_PATH=' + $machineEvidencePath)
if ($Issue011Acceptance) {
    Write-Output ('DTMAPI_ISSUE011_ACCEPTANCE_RECEIPT=' + [System.IO.Path]::GetFullPath($issue011AcceptanceReceiptPath))
    $issue011ReceiptPathBase64 = [Convert]::ToBase64String(
        (New-Object System.Text.UTF8Encoding($false)).GetBytes([System.IO.Path]::GetFullPath($issue011AcceptanceReceiptPath)))
    Write-Output ('DTMAPI_ISSUE011_ACCEPTANCE_RECEIPT_BASE64=' + $issue011ReceiptPathBase64)
}

if ($runFailed) {
    Write-Error "Game smoke failed or left DolocTown.exe running. Evidence: $evidence" -ErrorAction Continue
    exit 1
}

if ($runStatus -eq 'Blocked') {
    Write-Warning "Game smoke completed with a blocked capability and clean shutdown. Evidence: $evidence"
    exit 0
}

Write-Host "Game smoke passed. Evidence: $evidence"
}
finally {
    if ($smokeSaveEnvironmentScopeApplied) {
        try {
            [Environment]::SetEnvironmentVariable(
                'DTMAPI_DOLOC_PERSISTENT_ROOT',
                $previousDtmApiPersistentRoot,
                [EnvironmentVariableTarget]::Process)
        }
        finally {
            [Environment]::SetEnvironmentVariable(
                'DTMAPI_STATE_DIR',
                $previousDtmApiStateDir,
                [EnvironmentVariableTarget]::Process)
        }
    }
}
