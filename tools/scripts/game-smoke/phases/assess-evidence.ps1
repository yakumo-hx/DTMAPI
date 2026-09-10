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
    if ($moreEquipmentSlots100UiAcceptanceRequested) {
        $equipmentSlots100ExpectedScreenshots = @(
            'equipment-slots-1920x1080-dynamic-row.png',
            'equipment-slots-1024x768-dynamic-row.png',
            'equipment-slots-1024x768-reopen.png'
        )
        $equipmentSlotsUiScreenshotFileOk = $equipmentSlotsUiScreenshotFileOk -and
            @($equipmentSlots100ExpectedScreenshots | Where-Object {
                $candidate = Join-Path $qaG4ArchivedEvidenceRoot (Join-Path 'ui' $_)
                -not (Test-Path -LiteralPath $candidate -PathType Leaf) -or
                    (Get-Item -LiteralPath $candidate).Length -le 0
            }).Count -eq 0
    }
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
        -SaveSlot $SaveSlot `
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
$runFailed = $runFailed -or ([bool]$WaitForManualExit -and ($null -eq $manualExitObservation -or -not [bool]$manualExitObservation.Passed))
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
