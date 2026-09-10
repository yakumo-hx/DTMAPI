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
    MoreEquipmentSlots100UiAcceptance = Get-SmokeStatus -Requested $moreEquipmentSlots100UiAcceptanceRequested -Passed ($equipmentSlotsUiObservationOk -and $equipmentSlotsUiEvidenceCapturedOk -and $equipmentSlotsUiHoverOk -and $equipmentSlotsUiCloseInputOk -and $equipmentSlotsUiScreenshotFileOk)
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
    DebugTeleportCsv = Get-SmokeStatus -Requested $false -Passed $debugTeleportCsvOk
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
    WaitForManualExit = [bool]$WaitForManualExit
    ManualExit = Get-SmokeStatus -Requested ([bool]$WaitForManualExit) -Passed ($null -ne $manualExitObservation -and [bool]$manualExitObservation.Passed)
    ManualExitReason = if($null -ne $manualExitObservation){[string]$manualExitObservation.Reason}else{''}
    ManualExitTimeout = $null -ne $manualExitObservation -and [bool]$manualExitObservation.TimedOut
    Completed = Get-Date -Format o
}
Write-SmokeJsonObject -Path (Join-Path $evidence 'result.json') -Value $result
if ($Issue011Acceptance) {
    $issue011AcceptanceProjection['SmokeResult'] = Get-SmokeIssue011FileReceipt -Path (Join-Path $evidence 'result.json')
    Write-SmokeJsonObject -Path $issue011AcceptanceReceiptPath -Value $issue011AcceptanceProjection
}
try {
    & "$SmokeRunnerScriptsRoot\analyze-startup-evidence.ps1" -EvidencePath $evidence -OutputDirectory $evidence -Quiet
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
    $SmokeRunnerExitCode = [int](1); return
}

if ($runStatus -eq 'Blocked') {
    Write-Warning "Game smoke completed with a blocked capability and clean shutdown. Evidence: $evidence"
    $SmokeRunnerExitCode = [int](0); return
}

Write-Host "Game smoke passed. Evidence: $evidence"
