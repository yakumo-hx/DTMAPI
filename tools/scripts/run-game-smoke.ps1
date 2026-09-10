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
    [switch] $WaitForManualExit,
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

$SmokeRunnerExitCode = $null
$SmokeRunnerLaunchAttempted = $false
$SmokeRunnerScenarioDeadline = [datetime]::MaxValue
$SmokeRunnerActivePhase = 'preflight'
$smokeSaveEnvironmentScopeApplied = $false
$evidence = ''
$SmokeRunnerScriptsRoot = $PSScriptRoot
$SmokeRunnerBoundParameters = @{}
foreach ($entry in $PSBoundParameters.GetEnumerator()) { $SmokeRunnerBoundParameters[$entry.Key] = $entry.Value }
$SmokeRunnerModuleRoot = Join-Path $PSScriptRoot 'game-smoke'
. (Join-Path $SmokeRunnerModuleRoot 'load-modules.ps1')
try {
    . (Join-Path $SmokeRunnerModuleRoot 'phases/preflight.ps1')
    $SmokeRunnerActivePhase = 'enter-save-environment'
    . (Join-Path $SmokeRunnerModuleRoot 'phases/enter-save-environment.ps1')
    if ($null -ne $SmokeRunnerExitCode) { exit $SmokeRunnerExitCode }
    if ($ValidateQaG4EvidenceCleanupOnly) {
        $selfTest = Invoke-SmokeQaG4EvidenceCleanupSelfTest
        $selfTest | ConvertTo-Json -Depth 6
        if (-not [bool]$selfTest.Passed) { exit 1 }
        exit 0
    }
    $SmokeRunnerActivePhase = 'routing'
    . (Join-Path $SmokeRunnerModuleRoot 'phases/routing.ps1')
    if ($null -ne $SmokeRunnerExitCode) { exit $SmokeRunnerExitCode }
    $SmokeRunnerActivePhase = 'prepare-session'
    . (Join-Path $SmokeRunnerModuleRoot 'phases/prepare-session.ps1')
    if ($null -ne $SmokeRunnerExitCode) { exit $SmokeRunnerExitCode }
    $SmokeRunnerActivePhase = 'run-session'
    . (Join-Path $SmokeRunnerModuleRoot 'phases/run-session.ps1')
    if ($null -ne $SmokeRunnerExitCode) { exit $SmokeRunnerExitCode }
    $SmokeRunnerActivePhase = 'assess-evidence'
    . (Join-Path $SmokeRunnerModuleRoot 'phases/assess-evidence.ps1')
    if ($null -ne $SmokeRunnerExitCode) { exit $SmokeRunnerExitCode }
    $SmokeRunnerActivePhase = 'publish-result'
    . (Join-Path $SmokeRunnerModuleRoot 'phases/publish-result.ps1')
    if ($null -ne $SmokeRunnerExitCode) { exit $SmokeRunnerExitCode }
}
catch {
    $runnerFailure = $_
    if (-not [string]::IsNullOrWhiteSpace($evidence)) {
        try {
            Write-SmokeRunnerFailure -EvidencePath $evidence -Phase $SmokeRunnerActivePhase `
                -Reason ([string]$runnerFailure.Exception.Message) -SaveTestMode $SaveTestMode `
                -LaunchAttempted $SmokeRunnerLaunchAttempted
        }
        catch { Write-Warning ('Failure receipt could not be completed: ' + $_.Exception.Message) }
    }
    throw $runnerFailure
}
finally {
    . (Join-Path $SmokeRunnerModuleRoot 'phases/leave-save-environment.ps1')
}
