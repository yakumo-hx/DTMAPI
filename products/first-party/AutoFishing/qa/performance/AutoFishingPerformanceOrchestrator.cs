using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class AutoFishingPerformanceOrchestrator
    {
        private readonly AutoFishingQaSettings settings;
        private readonly GameBridgeFixtureAccess access;
        private readonly UnityRuntimeMemoryMetricsProvider runtimeMemoryMetricsProvider;
        private AutoFishingPerformanceFixtureSnapshot? latestSnapshot;
        private Batch5NoDemandRuntimeSnapshot? noDemandMeasurementStart;
        private FishingPerformanceProbe? probe;
        private FishingPerformanceResult? closedBeforeObservationResult;
        private bool measurementResultPrepared;
        private bool measurementStarted;
        private bool measurementCompleted;
        private bool resultWritten;
        private bool terminal;
        private bool batch5InitialTitleCleanupObserved;
        private string batch5PreCycleStatus = string.Empty;
        private string resultPath = string.Empty;
        private int nativeVitalsWorkloadReceiptCount;
        private int nativeVitalsMaintenanceReceiptCount;
        private int nativeVitalsMeasurementMaintenanceReceiptCount;
        private int nativeVitalsL4CheckpointReceiptCount;
        private bool nativeVitalsL4CheckpointEnergyCommandInvoked;
        private bool nativeVitalsL4CheckpointSpiritCommandInvoked;
        private string nativeVitalsL4CheckpointContext = string.Empty;
        private int nativeVitalsInitialSavePrepareCount;
        private int nativeVitalsPostReloadPrepareCount;
        private int nativeVitalsInitialSavePrepareOrdinal;
        private int nativeVitalsPostReloadPrepareOrdinal;
        private int nativeVitalsEnergyCommandCount;
        private int nativeVitalsSpiritCommandCount;
        private int nativeEnergyInsufficientObservations;
        private int nativeVitalsFinalObservationCount;
        private int nativeFishingEnergyCost;
        private float nativeEnergyPercentBeforeLast;
        private float nativeEnergyPercentAfterLast;
        private float nativeSpiritPercentBeforeLast;
        private float nativeSpiritPercentAfterLast;
        private string nativeVitalsLastContext = string.Empty;
        private string nativeVitalsSource = string.Empty;
        private string nativeVitalsComposeEnergyDelegateIdentity = string.Empty;
        private string nativeVitalsComposeSpiritDelegateIdentity = string.Empty;
        private string nativeVitalsGetEnergyPercentDelegateIdentity = string.Empty;
        private string nativeVitalsGetSpiritPercentDelegateIdentity = string.Empty;
        private float nativeEnergyPercentAtStageStart;
        private float nativeSpiritPercentAtStageStart;
        private float nativeEnergyPercentAtMeasurementEnd;
        private float nativeSpiritPercentAtMeasurementEnd;

        internal AutoFishingPerformanceOrchestrator(AutoFishingQaSettings settings, GameBridgeFixtureAccess access)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            runtimeMemoryMetricsProvider = new UnityRuntimeMemoryMetricsProvider();
        }

        internal bool Enabled => settings.AutoFishingPerformanceEnabled;

        internal string Profile => settings.AutoFishingPerformanceProfile;

        internal bool TerminalSucceeded { get; private set; }

        internal FishingPerformanceResult? ResultForTests => probe?.Result ?? closedBeforeObservationResult;

        internal AutoFishingPerformanceFixtureUpdate Observe(AutoFishingPerformanceFixtureSnapshot snapshot)
        {
            if (!Enabled)
                return AutoFishingPerformanceFixtureUpdate.None;
            if (terminal)
                return AutoFishingPerformanceFixtureUpdate.None;

            latestSnapshot = snapshot;
            EnsureProbe(snapshot);
            FishingPerformanceProbeUpdate update = probe!.Observe(snapshot.PullExited, snapshot.ObservedAtUtc);
            if (update == FishingPerformanceProbeUpdate.MeasurementStarted)
            {
                measurementStarted = true;
                ApplyStart(probe.Result, snapshot);
                if (IsFrameTargetNoDemandProfile)
                {
                    probe.Result.SchemaVersion = 3;
                    noDemandMeasurementStart = access.Bridge.CaptureBatch5NoDemandRuntimeSnapshot();
                }
                probe.SetBaselineMetrics(GetCurrentLogLength(), QaRuntimeMemorySnapshotCapture.GetResourceSnapshotBuildCount(access), snapshot.NativeTransient);
                probe.Result.FishingLogLinesStart = GetFishingLogLineCount(hotOnly: false);
                probe.Result.FishingHotLogLinesStart = GetFishingLogLineCount(hotOnly: true);
                // The frame-target receipt begins at this boundary. Publishing a pending
                // Hook status here would enqueue observer-owned work inside the measured
                // window and make the Hook-status revision gate fail for the observer itself.
                if (!IsFrameTargetNoDemandProfile)
                {
                    access.PublishAutoFishingPerformanceStatus(
                        "Smoke.AutoFishingPerformance",
                        "pending",
                        "optional QA performance orchestrator",
                        "profile=" + Profile + "; targetFish=" + settings.AutoFishingPerformanceTargetFish.ToString(CultureInfo.InvariantCulture) +
                        "; warmupFish=" + settings.AutoFishingPerformanceWarmupFish.ToString(CultureInfo.InvariantCulture) + "; owner=qa; fallback=false");
                }
                return AutoFishingPerformanceFixtureUpdate.MeasurementStarted;
            }

            if ((update == FishingPerformanceProbeUpdate.Completed || update == FishingPerformanceProbeUpdate.Blocked) && !measurementResultPrepared)
            {
                probe.SetCompletionMetrics(GetCurrentLogLength(), QaRuntimeMemorySnapshotCapture.GetResourceSnapshotBuildCount(access), snapshot.NativeTransient);
                ApplyEnd(probe.Result, snapshot);
                probe.Result.FishingLogLinesEnd = GetFishingLogLineCount(hotOnly: false);
                probe.Result.FishingLogLines = Math.Max(0, probe.Result.FishingLogLinesEnd - probe.Result.FishingLogLinesStart);
                probe.Result.FishingHotLogLinesEnd = GetFishingLogLineCount(hotOnly: true);
                probe.Result.FishingHotLogLines = Math.Max(0, probe.Result.FishingHotLogLinesEnd - probe.Result.FishingHotLogLinesStart);
                ValidateProfile(probe.Result);
                if (IsFrameTargetNoDemandProfile)
                    ApplyNoDemandProfileReceipt(probe.Result, access.Bridge.CaptureBatch5NoDemandRuntimeSnapshot());
                ApplyBatch5MeasurementReceipt(probe.Result);
                measurementResultPrepared = true;
            }

            if (measurementResultPrepared && !measurementCompleted)
            {
                WriteResult(probe.Result, overwrite: false);
                measurementCompleted = true;
                return AutoFishingPerformanceFixtureUpdate.MeasurementCompleted;
            }

            return AutoFishingPerformanceFixtureUpdate.None;
        }

        internal void MarkMovementCancellation(string source)
        {
            if (!Enabled || probe == null || !measurementCompleted)
                throw new InvalidOperationException("AutoFishing movement cancellation cannot be recorded before QA performance measurement completes.");
            probe.Result.MovementCancellationVerified = true;
            probe.Result.MovementCancellationSource = source ?? string.Empty;
        }

        internal void MarkBatch5DisableRecovery(int nativeRecoveryUnits, string source)
        {
            FishingPerformanceResult result = RequireBatch5PostMeasurement("L4", "disable recovery");
            if (nativeRecoveryUnits < 1)
                throw new InvalidOperationException("Batch 5 AutoFishing L4 requires at least one native-control recovery unit after product disablement.");
            if (!HasVerifiedL4RecoveryCheckpoint())
                throw new InvalidOperationException("Batch 5 AutoFishing L4 cannot publish recovery success without exactly one official energy+spirit recovery checkpoint in the product-disabled context.");
            result.DisableRecoveryVerified = true;
            result.NativeRecoveryUnits = nativeRecoveryUnits;
            result.DisableRecoverySource = source ?? string.Empty;
            result.BehaviorVerified = result.ActiveWindowSatisfied;
            if (!result.BehaviorVerified)
                throw new InvalidOperationException("Batch 5 AutoFishing L4 cannot publish recovery success without the complete target-and-duration active window.");
            WriteResult(result, overwrite: true);
        }

        internal void MarkBatch5TitleReloadCycle(int saveLoadCount, bool reenabledAfterReload, bool disabledAfterReload, string source)
        {
            FishingPerformanceResult result = RequireBatch5PostMeasurement("L5", "title reload cycle");
            if (!batch5InitialTitleCleanupObserved)
                throw new InvalidOperationException("Batch 5 AutoFishing L5 cannot record reload behavior before the first title cleanup receipt.");
            if (saveLoadCount < 2 || !reenabledAfterReload || !disabledAfterReload)
                throw new InvalidOperationException("Batch 5 AutoFishing L5 requires title -> configured-save reload -> product re-enable -> one loop -> product disable semantics.");
            if (nativeVitalsInitialSavePrepareCount != 1 || nativeVitalsInitialSavePrepareOrdinal != 1 ||
                nativeVitalsPostReloadPrepareCount != 1 || nativeVitalsPostReloadPrepareOrdinal != 2)
            {
                throw new InvalidOperationException("Batch 5 AutoFishing L5 cannot publish title-cycle success without distinct initial-save ordinal 1 and post-reload ordinal 2 workload preparations.");
            }
            result.TitleReloadCycleVerified = true;
            result.TitleReloadSaveLoads = saveLoadCount;
            result.ReenabledAfterReload = reenabledAfterReload;
            result.DisabledAfterReload = disabledAfterReload;
            result.TitleReloadSource = source ?? string.Empty;
            result.TitleCycleObserved = true;
            result.BehaviorVerified = result.ActiveWindowSatisfied;
            if (!result.BehaviorVerified)
                throw new InvalidOperationException("Batch 5 AutoFishing L5 cannot publish title-cycle success without the complete target-and-duration active window.");
            WriteResult(result, overwrite: true);
        }

        internal void RecordBatch5NativeVitals(AutoFishingNativeVitalsReceipt receipt)
        {
            if (!Enabled || !IsBatch5AutoFishing)
                throw new InvalidOperationException("Native vitals receipts are valid only for the Batch 5 AutoFishing performance fixture.");
            if (receipt == null)
                throw new ArgumentNullException(nameof(receipt));
            ValidateNativeVitalsReceipt(receipt);

            if (nativeFishingEnergyCost != 0 && nativeFishingEnergyCost != receipt.FishingEnergyCost)
                throw new InvalidOperationException("Batch 5 AutoFishing native FishingEnergyCost changed within one run.");
            nativeFishingEnergyCost = receipt.FishingEnergyCost;
            if (receipt.WorkloadStart)
            {
                if (receipt.WorkloadPhase.Equals(AutoFishingNativeVitalsReceipt.InitialSaveWorkloadPhase, StringComparison.Ordinal))
                {
                    if (receipt.SaveLoadOrdinal != 1 || nativeVitalsInitialSavePrepareCount != 0 || nativeVitalsPostReloadPrepareCount != 0)
                        throw new InvalidOperationException("Batch 5 AutoFishing initial-save workload receipt was duplicated or carried an invalid save-load ordinal/order.");
                    nativeVitalsInitialSavePrepareCount = 1;
                    nativeVitalsInitialSavePrepareOrdinal = receipt.SaveLoadOrdinal;
                }
                else
                {
                    if (!IsBatch5AutoFishingLevel("L5") || !batch5InitialTitleCleanupObserved ||
                        receipt.SaveLoadOrdinal != 2 || nativeVitalsInitialSavePrepareCount != 1 || nativeVitalsPostReloadPrepareCount != 0)
                    {
                        throw new InvalidOperationException("Batch 5 AutoFishing post-reload workload receipt was outside L5, before initial title cleanup, duplicated, or carried an invalid save-load ordinal/order.");
                    }
                    nativeVitalsPostReloadPrepareCount = 1;
                    nativeVitalsPostReloadPrepareOrdinal = receipt.SaveLoadOrdinal;
                }
                if (nativeVitalsWorkloadReceiptCount == 0)
                {
                    nativeEnergyPercentAtStageStart = receipt.EnergyPercentAfter;
                    nativeSpiritPercentAtStageStart = receipt.SpiritPercentAfter;
                }
                nativeVitalsWorkloadReceiptCount++;
            }
            if (receipt.MaintenanceRefill)
            {
                nativeVitalsMaintenanceReceiptCount++;
                if (measurementStarted && !measurementCompleted)
                    nativeVitalsMeasurementMaintenanceReceiptCount++;
            }
            if (receipt.L4RecoveryCheckpoint)
            {
                if (!IsBatch5AutoFishingLevel("L4") || nativeVitalsL4CheckpointReceiptCount != 0 ||
                    !receipt.Context.Equals(AutoFishingNativeVitalsReceipt.OfficialL4RecoveryCheckpointContext, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Batch 5 AutoFishing L4 recovery checkpoint was outside L4, duplicated, or carried the wrong context.");
                }
                nativeVitalsL4CheckpointReceiptCount = 1;
                nativeVitalsL4CheckpointContext = receipt.Context;
                nativeVitalsL4CheckpointEnergyCommandInvoked = receipt.EnergyCommandInvoked;
                nativeVitalsL4CheckpointSpiritCommandInvoked = receipt.SpiritCommandInvoked;
            }
            if (receipt.FinalObservation)
            {
                if (nativeVitalsFinalObservationCount != 0)
                    throw new InvalidOperationException("Batch 5 AutoFishing recorded more than one measurement-end native vitals observation.");
                nativeVitalsFinalObservationCount++;
                nativeEnergyPercentAtMeasurementEnd = receipt.EnergyPercentAfter;
                nativeSpiritPercentAtMeasurementEnd = receipt.SpiritPercentAfter;
            }
            if (receipt.EnergyCommandInvoked)
                nativeVitalsEnergyCommandCount++;
            if (receipt.SpiritCommandInvoked)
                nativeVitalsSpiritCommandCount++;
            if (receipt.NativeEnergyInsufficientObserved)
                nativeEnergyInsufficientObservations++;
            nativeEnergyPercentBeforeLast = receipt.EnergyPercentBefore;
            nativeEnergyPercentAfterLast = receipt.EnergyPercentAfter;
            nativeSpiritPercentBeforeLast = receipt.SpiritPercentBefore;
            nativeSpiritPercentAfterLast = receipt.SpiritPercentAfter;
            nativeVitalsLastContext = receipt.Context ?? string.Empty;
            nativeVitalsSource = receipt.Source ?? string.Empty;
            nativeVitalsComposeEnergyDelegateIdentity = receipt.ComposeEnergyDelegateIdentity;
            nativeVitalsComposeSpiritDelegateIdentity = receipt.ComposeSpiritDelegateIdentity;
            nativeVitalsGetEnergyPercentDelegateIdentity = receipt.GetEnergyPercentDelegateIdentity;
            nativeVitalsGetSpiritPercentDelegateIdentity = receipt.GetSpiritPercentDelegateIdentity;

            if (probe == null)
                return;
            ApplyNativeVitalsState(probe.Result);
            if (measurementCompleted)
                WriteResult(probe.Result, overwrite: true);
        }

        internal bool CompleteTitleCleanup(AutoFishingPerformanceTitleCleanupSnapshot snapshot)
        {
            if (!Enabled)
                return false;
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (terminal)
                return TerminalSucceeded;
            if (probe == null || !measurementCompleted)
                throw new InvalidOperationException("AutoFishing title cleanup arrived before QA performance measurement completed.");

            FishingPerformanceResult result = probe.Result;
            ApplyNativeTryCastEnergyRejectionEnd(result, snapshot.NativeTryCastEnergyGateUnavailable, snapshot.NativeTryCastInsufficientEnergy);
            ApplyNativeVitalsState(result);
            result.NativeTransientAfterTitle = snapshot.NativeTransient;
            result.SessionAfterTitle = snapshot.Sessions;
            result.InputLeaseAfterTitle = snapshot.InputLeases;
            result.AnimationLeaseAfterTitle = snapshot.AnimationLeases;
            result.SchedulerPendingAfterTitle = snapshot.SchedulerPending;
            result.LegacyOptionsAfterTitle = snapshot.LegacyOptions;
            result.LegacyStatesAfterTitle = snapshot.LegacyStates;
            result.HookRuntimeAfterTitle = snapshot.HookRuntimes;
            result.NativeReferencesAfterTitle = snapshot.NativeReferences;
            result.VisibleReelPendingAfterTitle = snapshot.VisibleReelPending;
            result.TitleCleanupPending = false;
            result.TitleCleanupVerified = snapshot.Sessions == 0 && snapshot.InputLeases == 0 && snapshot.AnimationLeases == 0 &&
                snapshot.HookRuntimes == 0 && snapshot.NativeTransient == 0 && snapshot.NativeReferences == 0 &&
                snapshot.LegacyOptions == 0 && snapshot.LegacyStates == 0 && !snapshot.SchedulerPending &&
                !snapshot.CallbackRuntime && snapshot.VisibleReelPending == 0;
            bool batch5L5 = IsBatch5AutoFishingLevel("L5");
            if (batch5L5 && !batch5InitialTitleCleanupObserved)
            {
                if (!HasVerifiedNativeVitalsWorkloads(1))
                {
                    result.TitleCleanupVerified = false;
                    result.Status = "failed-batch5-native-vitals";
                    result.FailureReason = "Batch 5 AutoFishing L5 did not record one verified official-command vitals receipt before its initial title cleanup.";
                }
                if (!result.TitleCleanupVerified)
                {
                    result.Status = "failed-batch5-initial-title-cleanup";
                    result.TitleCleanupPending = false;
                    WriteResult(result, overwrite: true);
                    TerminalSucceeded = false;
                    terminal = true;
                    return false;
                }
                batch5InitialTitleCleanupObserved = true;
                batch5PreCycleStatus = result.Status;
                result.Batch5InitialTitleCleanupVerified = true;
                result.TitleCleanupPending = true;
                result.Status = "waiting-batch5-title-reload-cycle";
                WriteResult(result, overwrite: true);
                return true;
            }
            if (batch5L5 && batch5InitialTitleCleanupObserved && !string.IsNullOrWhiteSpace(batch5PreCycleStatus))
                result.Status = batch5PreCycleStatus;
            if (Profile.Equals("EnabledNoRod", StringComparison.OrdinalIgnoreCase) && !result.MovementCancellationVerified)
            {
                result.TitleCleanupVerified = false;
                result.FailureReason = "EnabledNoRod did not record native movement cancellation before title cleanup.";
            }
            if (IsBatch5AutoFishingLevel("L4") && (!result.DisableRecoveryVerified || result.NativeRecoveryUnits < 1))
            {
                result.TitleCleanupVerified = false;
                result.FailureReason = "Batch 5 AutoFishing L4 did not prove one native-control recovery unit after product disablement.";
            }
            if (IsBatch5AutoFishingLevel("L4") && !result.NativeVitalsL4CheckpointVerified)
            {
                result.TitleCleanupVerified = false;
                result.FailureReason = "Batch 5 AutoFishing L4 did not prove exactly one official energy+spirit recovery checkpoint in the product-disabled context.";
            }
            if (batch5L5 && (!result.TitleReloadCycleVerified || result.TitleReloadSaveLoads < 2 || !result.ReenabledAfterReload || !result.DisabledAfterReload))
            {
                result.TitleCleanupVerified = false;
                result.FailureReason = "Batch 5 AutoFishing L5 did not prove title reload, re-enable, one loop, and disable semantics.";
            }
            if (batch5L5 && (result.NativeVitalsInitialSavePrepareCount != 1 || result.NativeVitalsInitialSavePrepareOrdinal != 1 ||
                result.NativeVitalsPostReloadPrepareCount != 1 || result.NativeVitalsPostReloadPrepareOrdinal != 2))
            {
                result.TitleCleanupVerified = false;
                result.FailureReason = "Batch 5 AutoFishing L5 did not bind distinct initial-save ordinal 1 and post-reload ordinal 2 native-vitals preparations.";
            }
            if (IsBatch5AutoFishing && !result.BehaviorVerified)
            {
                result.TitleCleanupVerified = false;
                result.FailureReason = "Batch 5 AutoFishing did not publish the exact level behavior receipt after a complete target-and-duration active window.";
            }
            if (IsBatch5AutoFishing && !result.NativeVitalsVerified)
            {
                result.TitleCleanupVerified = false;
                result.FailureReason =
                    "Batch 5 AutoFishing native vitals were not verified through the official command registry for every workload, or native fishing energy became insufficient.";
            }
            if (!result.TitleCleanupVerified &&
                (result.Status == "completed" || result.Status.StartsWith("blocked-allocation-counter-", StringComparison.Ordinal)))
            {
                result.Status = "failed-title-cleanup";
            }

            bool succeeded = result.TitleCleanupVerified &&
                (result.Status == "completed" || result.Status.StartsWith("blocked-allocation-counter-", StringComparison.Ordinal));
            WriteResult(result, overwrite: true);
            TerminalSucceeded = succeeded;
            terminal = true;
            return TerminalSucceeded;
        }

        private FishingPerformanceResult RequireBatch5PostMeasurement(string level, string operation)
        {
            if (!Enabled || probe == null || !measurementCompleted)
                throw new InvalidOperationException("Batch 5 AutoFishing " + operation + " cannot be recorded before QA performance measurement completes.");
            if (!IsBatch5AutoFishingLevel(level))
                throw new InvalidOperationException("Batch 5 AutoFishing " + operation + " is valid only for " + level + ".");
            return probe.Result;
        }

        private bool IsBatch5AutoFishingLevel(string level) =>
            IsBatch5AutoFishing &&
            settings.Batch5GcLadderLevel.Equals(level, StringComparison.Ordinal);

        private bool IsBatch5AutoFishing =>
            settings.Batch5GcLadderEnabled &&
            settings.Batch5GcLadderDomain.Equals("AutoFishing", StringComparison.OrdinalIgnoreCase);

        private void ApplyBatch5Identity(FishingPerformanceResult result)
        {
            if (!IsBatch5AutoFishing)
                return;

            result.SchemaVersion = 3;
            result.Domain = "AutoFishing";
            result.Level = settings.Batch5GcLadderLevel;
            result.Batch5Level = settings.Batch5GcLadderLevel;
            result.Workload = settings.Batch5GcLadderWorkload;
            result.ProductState = GetBatch5ProductState(settings.Batch5GcLadderLevel);
            result.Multiplier = settings.Batch5GcLadderMultiplier;
            result.MeasureSeconds = settings.Batch5GcLadderMeasureSeconds;
            result.SampleSeconds = settings.Batch5GcLadderSampleSeconds;
            result.TargetUnits = settings.Batch5GcLadderTargetUnits;
            result.RequiredActiveDurationSeconds = settings.Batch5GcLadderMeasureSeconds;
            result.SaveSlot = settings.SaveSlot;
            result.ForcedGc = false;
            result.Scenario = settings.G6AutoFishingScenario;
            result.BehaviorReceiptKind = GetBatch5BehaviorReceiptKind(settings.Batch5GcLadderLevel);
            result.NativeVitalsRequiredWorkloadReceipts = GetRequiredNativeVitalsWorkloadReceipts();
        }

        private int GetRequiredNativeVitalsWorkloadReceipts() => IsBatch5AutoFishingLevel("L5") ? 2 : 1;

        private bool HasVerifiedNativeVitalsWorkloads(int requiredWorkloads)
        {
            FishingPerformanceResult? result = probe?.Result;
            bool workloadIdentityVerified = requiredWorkloads == 1
                ? nativeVitalsInitialSavePrepareCount == 1 && nativeVitalsInitialSavePrepareOrdinal == 1 && nativeVitalsPostReloadPrepareCount == 0
                : IsBatch5AutoFishingLevel("L5") && requiredWorkloads == 2 &&
                    nativeVitalsInitialSavePrepareCount == 1 && nativeVitalsInitialSavePrepareOrdinal == 1 &&
                    nativeVitalsPostReloadPrepareCount == 1 && nativeVitalsPostReloadPrepareOrdinal == 2;
            bool checkpointVerified = IsBatch5AutoFishingLevel("L4")
                ? HasVerifiedL4RecoveryCheckpoint()
                : nativeVitalsL4CheckpointReceiptCount == 0;
            bool nativeTryCastEnergyVerified = result != null &&
                result.NativeTryCastEnergyGateUnavailableDelta == 0 &&
                result.NativeTryCastInsufficientEnergyDelta == 0;
            bool measurementMaintenanceVerified = !RequiresMeasurementMaintenance ||
                nativeVitalsMeasurementMaintenanceReceiptCount >= 1;
            return nativeVitalsWorkloadReceiptCount == requiredWorkloads &&
                workloadIdentityVerified &&
                measurementMaintenanceVerified &&
                checkpointVerified &&
                nativeVitalsFinalObservationCount == 1 &&
                nativeEnergyInsufficientObservations == 0 &&
                nativeTryCastEnergyVerified &&
                nativeFishingEnergyCost > 0 &&
                nativeVitalsSource.Equals(AutoFishingNativeVitalsReceipt.OfficialCommandSource, StringComparison.Ordinal) &&
                HasVerifiedDelegateIdentities();
        }

        private bool HasVerifiedL4RecoveryCheckpoint() =>
            nativeVitalsL4CheckpointReceiptCount == 1 &&
            nativeVitalsL4CheckpointEnergyCommandInvoked &&
            nativeVitalsL4CheckpointSpiritCommandInvoked &&
            nativeVitalsL4CheckpointContext.Equals(AutoFishingNativeVitalsReceipt.OfficialL4RecoveryCheckpointContext, StringComparison.Ordinal);

        private bool RequiresMeasurementMaintenance =>
            IsBatch5AutoFishing && settings.Batch5GcLadderMeasureSeconds >= 600;

        private bool HasVerifiedDelegateIdentities() =>
            nativeVitalsComposeEnergyDelegateIdentity.Equals(AutoFishingNativeVitalsReceipt.OfficialComposeEnergyDelegateIdentity, StringComparison.Ordinal) &&
            nativeVitalsComposeSpiritDelegateIdentity.Equals(AutoFishingNativeVitalsReceipt.OfficialComposeSpiritDelegateIdentity, StringComparison.Ordinal) &&
            nativeVitalsGetEnergyPercentDelegateIdentity.Equals(AutoFishingNativeVitalsReceipt.OfficialGetEnergyPercentDelegateIdentity, StringComparison.Ordinal) &&
            nativeVitalsGetSpiritPercentDelegateIdentity.Equals(AutoFishingNativeVitalsReceipt.OfficialGetSpiritPercentDelegateIdentity, StringComparison.Ordinal);

        private void ApplyNativeVitalsState(FishingPerformanceResult result)
        {
            if (!IsBatch5AutoFishing)
                return;

            result.NativeVitalsRequiredWorkloadReceipts = GetRequiredNativeVitalsWorkloadReceipts();
            result.NativeVitalsPrepareCount = nativeVitalsWorkloadReceiptCount;
            result.NativeVitalsWorkloadReceiptCount = nativeVitalsWorkloadReceiptCount;
            result.NativeVitalsMaintenanceReceiptCount = nativeVitalsMaintenanceReceiptCount;
            result.NativeVitalsMeasurementMaintenanceRequired = RequiresMeasurementMaintenance;
            result.NativeVitalsMeasurementMaintenanceReceiptCount = nativeVitalsMeasurementMaintenanceReceiptCount;
            result.NativeVitalsL4CheckpointReceiptCount = nativeVitalsL4CheckpointReceiptCount;
            result.NativeVitalsL4CheckpointVerified = IsBatch5AutoFishingLevel("L4") && HasVerifiedL4RecoveryCheckpoint();
            result.NativeVitalsL4CheckpointContext = nativeVitalsL4CheckpointContext;
            result.NativeVitalsL4CheckpointEnergyCommandInvoked = nativeVitalsL4CheckpointEnergyCommandInvoked;
            result.NativeVitalsL4CheckpointSpiritCommandInvoked = nativeVitalsL4CheckpointSpiritCommandInvoked;
            result.NativeVitalsInitialSavePrepareCount = nativeVitalsInitialSavePrepareCount;
            result.NativeVitalsPostReloadPrepareCount = nativeVitalsPostReloadPrepareCount;
            result.NativeVitalsInitialSavePrepareOrdinal = nativeVitalsInitialSavePrepareOrdinal;
            result.NativeVitalsPostReloadPrepareOrdinal = nativeVitalsPostReloadPrepareOrdinal;
            result.NativeVitalsEnergyCommandCount = nativeVitalsEnergyCommandCount;
            result.NativeVitalsSpiritCommandCount = nativeVitalsSpiritCommandCount;
            result.NativeVitalsFinalObservationCount = nativeVitalsFinalObservationCount;
            result.NativeEnergyInsufficientObservations = nativeEnergyInsufficientObservations;
            result.NativeEnergyExhaustionObserved = nativeEnergyInsufficientObservations > 0 ||
                result.NativeTryCastInsufficientEnergyDelta > 0;
            result.NativeFishingEnergyCost = nativeFishingEnergyCost;
            result.NativeEnergyPercentBeforeLast = nativeEnergyPercentBeforeLast;
            result.NativeEnergyPercentAfterLast = nativeEnergyPercentAfterLast;
            result.NativeSpiritPercentBeforeLast = nativeSpiritPercentBeforeLast;
            result.NativeSpiritPercentAfterLast = nativeSpiritPercentAfterLast;
            result.NativeVitalsLastContext = nativeVitalsLastContext;
            result.NativeVitalsSource = nativeVitalsSource;
            result.NativeVitalsComposeEnergyDelegateIdentity = nativeVitalsComposeEnergyDelegateIdentity;
            result.NativeVitalsComposeSpiritDelegateIdentity = nativeVitalsComposeSpiritDelegateIdentity;
            result.NativeVitalsGetEnergyPercentDelegateIdentity = nativeVitalsGetEnergyPercentDelegateIdentity;
            result.NativeVitalsGetSpiritPercentDelegateIdentity = nativeVitalsGetSpiritPercentDelegateIdentity;
            result.NativeVitalsDelegateIdentitiesVerified = HasVerifiedDelegateIdentities();
            result.NativeEnergyPercentAtStageStart = nativeEnergyPercentAtStageStart;
            result.NativeSpiritPercentAtStageStart = nativeSpiritPercentAtStageStart;
            result.NativeEnergyPercentAtMeasurementEnd = nativeEnergyPercentAtMeasurementEnd;
            result.NativeSpiritPercentAtMeasurementEnd = nativeSpiritPercentAtMeasurementEnd;
            result.NativeVitalsMaintenanceCadenceMilliseconds = 250;
            result.NativeVitalsFinalReadbackVerified =
                nativeVitalsFinalObservationCount == 1;
            result.NativeVitalsReadbackVerified =
                nativeVitalsWorkloadReceiptCount > 0 &&
                result.NativeVitalsFinalReadbackVerified &&
                nativeVitalsSource.Equals(AutoFishingNativeVitalsReceipt.OfficialCommandSource, StringComparison.Ordinal) &&
                result.NativeVitalsDelegateIdentitiesVerified;
            result.NativeVitalsVerified = HasVerifiedNativeVitalsWorkloads(result.NativeVitalsRequiredWorkloadReceipts);
        }

        private static void ValidateNativeVitalsReceipt(AutoFishingNativeVitalsReceipt receipt)
        {
            if (!receipt.Source.Equals(AutoFishingNativeVitalsReceipt.OfficialCommandSource, StringComparison.Ordinal))
                throw new InvalidOperationException("Batch 5 AutoFishing native vitals receipt did not use the approved official command source.");
            if (!receipt.ComposeEnergyDelegateIdentity.Equals(AutoFishingNativeVitalsReceipt.OfficialComposeEnergyDelegateIdentity, StringComparison.Ordinal) ||
                !receipt.ComposeSpiritDelegateIdentity.Equals(AutoFishingNativeVitalsReceipt.OfficialComposeSpiritDelegateIdentity, StringComparison.Ordinal) ||
                !receipt.GetEnergyPercentDelegateIdentity.Equals(AutoFishingNativeVitalsReceipt.OfficialGetEnergyPercentDelegateIdentity, StringComparison.Ordinal) ||
                !receipt.GetSpiritPercentDelegateIdentity.Equals(AutoFishingNativeVitalsReceipt.OfficialGetSpiritPercentDelegateIdentity, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Batch 5 AutoFishing native vitals receipt did not bind all four current official delegate Method.DeclaringType/Name identities.");
            }
            int receiptKindCount = (receipt.WorkloadStart ? 1 : 0) +
                (receipt.MaintenanceRefill ? 1 : 0) +
                (receipt.L4RecoveryCheckpoint ? 1 : 0) +
                (receipt.FinalObservation ? 1 : 0);
            if (receiptKindCount != 1)
                throw new InvalidOperationException("Batch 5 AutoFishing native vitals receipt must describe exactly one workload-start, periodic-maintenance, L4-checkpoint, or final-observation action.");
            if (receipt.WorkloadStart)
            {
                if (receipt.SaveLoadOrdinal <= 0 ||
                    (!receipt.WorkloadPhase.Equals(AutoFishingNativeVitalsReceipt.InitialSaveWorkloadPhase, StringComparison.Ordinal) &&
                        !receipt.WorkloadPhase.Equals(AutoFishingNativeVitalsReceipt.PostReloadWorkloadPhase, StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException("Batch 5 AutoFishing workload-start receipt did not bind a positive save-load ordinal and exact workload phase.");
                }
            }
            else if (receipt.SaveLoadOrdinal != 0 || !string.IsNullOrEmpty(receipt.WorkloadPhase))
                throw new InvalidOperationException("Only Batch 5 AutoFishing workload-start receipts may carry save-load ordinal/phase identity.");
            if (receipt.FishingEnergyCost <= 0)
                throw new InvalidOperationException("Batch 5 AutoFishing native vitals receipt did not include a positive FishingEnergyCost.");
            if (!AutoFishingNativeVitalsCommandAdapter.IsValidPercent(receipt.EnergyPercentBefore) ||
                !AutoFishingNativeVitalsCommandAdapter.IsValidPercent(receipt.EnergyPercentAfter) ||
                !AutoFishingNativeVitalsCommandAdapter.IsValidPercent(receipt.SpiritPercentBefore) ||
                !AutoFishingNativeVitalsCommandAdapter.IsValidPercent(receipt.SpiritPercentAfter))
            {
                throw new InvalidOperationException("Batch 5 AutoFishing native vitals receipt contained an invalid percent readback.");
            }
            if (!receipt.FinalObservation &&
                (!receipt.NativeEnergySufficientAfter || !receipt.NativeEnergyReserveSufficientAfter))
                throw new InvalidOperationException("Batch 5 AutoFishing native vitals receipt did not retain enough native energy after its command transaction.");
            if (receipt.WorkloadStart &&
                (!receipt.EnergyCommandInvoked || !receipt.SpiritCommandInvoked ||
                    !AutoFishingNativeVitalsCommandAdapter.IsFull(receipt.EnergyPercentAfter) ||
                    !AutoFishingNativeVitalsCommandAdapter.IsFull(receipt.SpiritPercentAfter)))
            {
                throw new InvalidOperationException("Batch 5 AutoFishing workload-start receipt did not prove both official commands and full percent readback.");
            }
            if (receipt.MaintenanceRefill && !receipt.EnergyCommandInvoked && !receipt.SpiritCommandInvoked)
                throw new InvalidOperationException("Batch 5 AutoFishing maintenance receipt did not invoke either approved command.");
            if (receipt.L4RecoveryCheckpoint &&
                (!receipt.EnergyCommandInvoked || !receipt.SpiritCommandInvoked ||
                    !AutoFishingNativeVitalsCommandAdapter.IsFull(receipt.EnergyPercentAfter) ||
                    !AutoFishingNativeVitalsCommandAdapter.IsFull(receipt.SpiritPercentAfter)))
            {
                throw new InvalidOperationException("Batch 5 AutoFishing L4 checkpoint receipt did not prove both official commands and full percent readback.");
            }
            if (receipt.FinalObservation && (receipt.EnergyCommandInvoked || receipt.SpiritCommandInvoked))
                throw new InvalidOperationException("Batch 5 AutoFishing final native vitals observation must be read-only.");
            if (receipt.EnergyCommandInvoked && !AutoFishingNativeVitalsCommandAdapter.IsFull(receipt.EnergyPercentAfter))
                throw new InvalidOperationException("Batch 5 AutoFishing energy command receipt did not prove full energy readback.");
            if (receipt.SpiritCommandInvoked && !AutoFishingNativeVitalsCommandAdapter.IsFull(receipt.SpiritPercentAfter))
                throw new InvalidOperationException("Batch 5 AutoFishing spirit command receipt did not prove full spirit readback.");
        }

        private void ApplyBatch5MeasurementReceipt(FishingPerformanceResult result)
        {
            if (!IsBatch5AutoFishing)
                return;

            if (result.NativeTryCastEnergyGateUnavailableDelta != 0 || result.NativeTryCastInsufficientEnergyDelta != 0)
            {
                result.Status = "failed-batch5-native-energy-cast-rejection";
                result.FailureReason = "Batch 5 AutoFishing observed production TryCast energy-gate-unavailable or insufficient-energy during the measured window.";
            }

            result.CompletedUnits = result.MeasuredFish;
            result.WorkloadCompleted = result.Status.Equals("completed", StringComparison.Ordinal) &&
                result.MeasuredFish >= result.TargetUnits;
            result.ActiveWindowSatisfied = result.WorkloadCompleted &&
                result.ElapsedSeconds + 0.001d >= result.RequiredActiveDurationSeconds;
            if (!IsBatch5AutoFishingLevel("L4") && !IsBatch5AutoFishingLevel("L5"))
                result.BehaviorVerified = result.ActiveWindowSatisfied;
        }

        private static string GetBatch5ProductState(string level)
        {
            if (level.Equals("L0", StringComparison.Ordinal))
                return "off";
            if (level.Equals("L4", StringComparison.Ordinal))
                return "enabled-then-disabled";
            return "enabled";
        }

        private static string GetBatch5BehaviorReceiptKind(string level)
        {
            switch (level)
            {
                case "L0": return "native-control-active-window";
                case "L1": return "enabled-1x-active-window";
                case "L2": return "common-multiplier-active-window";
                case "L3": return "high-multiplier-active-window";
                case "L4": return "disable-recovery-after-active-window";
                case "L5": return "title-reload-after-active-window";
                default: throw new InvalidOperationException("Unsupported Batch 5 AutoFishing level '" + level + "'.");
            }
        }

        internal void Close(string reason)
        {
            if (!Enabled)
                return;
            if (terminal)
            {
                if (!TerminalSucceeded)
                    throw new InvalidOperationException("Optional QA performance reached a failed terminal state before host close.");
                return;
            }

            FishingPerformanceResult result;
            if (probe != null)
            {
                result = probe.Result;
            }
            else
            {
                closedBeforeObservationResult = new FishingPerformanceResult
                {
                    Profile = Profile,
                    TargetFish = settings.AutoFishingPerformanceTargetFish,
                    WarmupFish = settings.AutoFishingPerformanceWarmupFish
                };
            ApplyBatch5Identity(closedBeforeObservationResult);
                ApplyNativeVitalsState(closedBeforeObservationResult);
                result = closedBeforeObservationResult;
            }

            result.Status = "failed-host-closed-before-title-cleanup";
            result.FailureReason = "Optional QA host closed before terminal performance cleanup. reason=" + (reason ?? string.Empty) + ".";
            result.TitleCleanupPending = false;
            result.TitleCleanupVerified = false;
            WriteResult(result, overwrite: true);
            TerminalSucceeded = false;
            terminal = true;
            throw new InvalidOperationException("Optional QA performance host closed without a durable successful terminal result.");
        }

        private bool IsFrameTargetNoDemandProfile =>
            Profile.Equals("InactiveNoConsumer", StringComparison.OrdinalIgnoreCase) &&
            settings.AutoFishingPerformanceTargetFrames > 0;

        private void ApplyNoDemandProfileReceipt(FishingPerformanceResult result, Batch5NoDemandRuntimeSnapshot end)
        {
            Batch5NoDemandRuntimeSnapshot start = noDemandMeasurementStart
                ?? throw new InvalidOperationException("InactiveNoConsumer frame measurement completed without a warmed Runtime counter baseline.");
            Batch5MandatoryUpdaterCadenceReceipt[] mandatoryCadence = BuildMandatoryUpdaterCadence(start, end);
            result.SchemaVersion = 3;
            result.Domain = "Batch5NoDemand";
            result.Workload = "WarmedNoOptionalDemand";
            result.SaveSlot = settings.SaveSlot;
            result.ForcedGc = false;
            var receipt = new Batch5NoDemandPerformanceReceipt
            {
                WarmupFrameTarget = settings.AutoFishingPerformanceWarmupFrames,
                WarmupFrameActual = result.WarmupFramesActual,
                MeasurementFrameTarget = settings.AutoFishingPerformanceTargetFrames,
                MeasurementFrameActual = result.MeasuredFrames,
                CoreRuntimeUpdates = new Batch5UnsignedCounterDelta(start.RuntimeUpdateTicks, end.RuntimeUpdateTicks),
                QaObserverUpdates = Delta(start.QaHostUpdateCount, end.QaHostUpdateCount),
                OptionalFeatureFileStatusCalls = Delta(start.OptionalFeatureFileStatusCalls, end.OptionalFeatureFileStatusCalls),
                OptionalDirectoryEnumerations = Delta(start.OptionalDirectoryEnumerations, end.OptionalDirectoryEnumerations),
                ActiveUpdaterMembershipSnapshotRebuilds = Delta(start.ActiveUpdaterMembershipSnapshotRebuilds, end.ActiveUpdaterMembershipSnapshotRebuilds),
                OptionalPerFeatureProjectionBuilds = Delta(start.OptionalPerFeatureProjectionBuilds, end.OptionalPerFeatureProjectionBuilds),
                OptionalReflectionObjectSearches = Delta(start.OptionalReflectionObjectSearches, end.OptionalReflectionObjectSearches),
                OptionalNativeUpdaterInvocations = Delta(start.OptionalNativeUpdaterInvocations, end.OptionalNativeUpdaterInvocations),
                OptionalRetainedCallbackWork = Delta(start.OptionalRetainedCallbackWork, end.OptionalRetainedCallbackWork),
                CustomAnimalsRetainedCallbackWork = Delta(start.CustomAnimalsRetainedCallbackWork, end.CustomAnimalsRetainedCallbackWork),
                AudioRetainedCallbackWork = Delta(start.AudioRetainedCallbackWork, end.AudioRetainedCallbackWork),
                OptionalHookInstallRequests = Delta(start.OptionalHookInstallRequests, end.OptionalHookInstallRequests),
                CameraEnvironmentResets = Delta(start.CameraEnvironmentResets, end.CameraEnvironmentResets),
                CustomAnimalDefinitionCandidateBuilds = Delta(start.CustomAnimalDefinitionCandidateBuilds, end.CustomAnimalDefinitionCandidateBuilds),
                ContentQueryCandidateBuilds = Delta(start.ContentQueryCandidateBuilds, end.ContentQueryCandidateBuilds),
                AudioPendingEntryVisits = Delta(start.AudioPendingEntryVisits, end.AudioPendingEntryVisits),
                MandatoryBaseUpdaterInvocations = Delta(start.MandatoryUpdaterInvocations, end.MandatoryUpdaterInvocations),
                QaUpdaterInvocations = Delta(start.QaObserverUpdaterInvocations, end.QaObserverUpdaterInvocations),
                EventQueueDiagnosticRevision = Delta(start.EventQueueDiagnosticRevision, end.EventQueueDiagnosticRevision),
                HookStatusQueueDiagnosticRevision = Delta(start.HookStatusQueueDiagnosticRevision, end.HookStatusQueueDiagnosticRevision),
                EventArgsCreated = Delta(start.EventArgsCreated, end.EventArgsCreated),
                EventSnapshotRebuilds = Delta(start.EventSnapshotRebuilds, end.EventSnapshotRebuilds),
                EventZeroListenerBypasses = Delta(start.EventZeroListenerBypasses, end.EventZeroListenerBypasses),
                EventQueuePendingAtStart = start.EventQueuePending,
                EventQueuePendingAtEnd = end.EventQueuePending,
                HookStatusQueuePendingAtStart = start.HookStatusQueuePending,
                HookStatusQueuePendingAtEnd = end.HookStatusQueuePending,
                QaExplicitDemandActiveAtStart = start.QaExplicitDemandActive,
                QaExplicitDemandActiveAtEnd = end.QaExplicitDemandActive,
                QaUpdaterActiveAtStart = start.QaUpdaterActive,
                QaUpdaterActiveAtEnd = end.QaUpdaterActive,
                ActiveOptionalDemandIdsAtStart = start.ActiveOptionalDemandIds,
                ActiveOptionalDemandIdsAtEnd = end.ActiveOptionalDemandIds,
                ActiveOptionalUpdaterIdsAtStart = start.ActiveOptionalUpdaterIds,
                ActiveOptionalUpdaterIdsAtEnd = end.ActiveOptionalUpdaterIds,
                ActiveMandatoryUpdaterIdsAtStart = start.ActiveMandatoryUpdaterIds,
                ActiveMandatoryUpdaterIdsAtEnd = end.ActiveMandatoryUpdaterIds,
                MandatoryUpdaterCadence = mandatoryCadence
            };

            var failures = new List<string>();
            if (receipt.WarmupFrameActual != receipt.WarmupFrameTarget)
                failures.Add("warmup frames actual != target");
            if (receipt.MeasurementFrameActual != receipt.MeasurementFrameTarget)
                failures.Add("measurement frames actual != target");
            if (!receipt.CoreRuntimeUpdates.Monotonic || receipt.CoreRuntimeUpdates.Delta != (ulong)receipt.MeasurementFrameTarget)
                failures.Add("Core DtmApiRuntime.Update delta != measurement target");
            if (receipt.QaObserverUpdates.Delta != receipt.MeasurementFrameTarget || receipt.QaUpdaterInvocations.Delta != receipt.MeasurementFrameTarget)
                failures.Add("ExplicitQa observer/update route delta != measurement target");
            RequireZero(failures, "optional feature file-status calls", receipt.OptionalFeatureFileStatusCalls);
            RequireZero(failures, "optional directory enumeration", receipt.OptionalDirectoryEnumerations);
            RequireZero(failures, "active updater membership snapshot rebuild", receipt.ActiveUpdaterMembershipSnapshotRebuilds);
            RequireZero(failures, "optional per-feature ToArray projection build", receipt.OptionalPerFeatureProjectionBuilds);
            RequireZero(failures, "optional reflection object search", receipt.OptionalReflectionObjectSearches);
            RequireZero(failures, "optional native updater invocation", receipt.OptionalNativeUpdaterInvocations);
            RequireZero(failures, "optional retained callback work", receipt.OptionalRetainedCallbackWork);
            RequireZero(failures, "CustomAnimals retained callback work", receipt.CustomAnimalsRetainedCallbackWork);
            RequireZero(failures, "Audio retained callback work", receipt.AudioRetainedCallbackWork);
            RequireZero(failures, "optional hook install request", receipt.OptionalHookInstallRequests);
            RequireZero(failures, "Camera EnvironmentReset fanout", receipt.CameraEnvironmentResets);
            RequireZero(failures, "CustomAnimals definition candidate build", receipt.CustomAnimalDefinitionCandidateBuilds);
            RequireZero(failures, "ContentQuery candidate build", receipt.ContentQueryCandidateBuilds);
            RequireZero(failures, "Audio pending entry visit", receipt.AudioPendingEntryVisits);
            RequireZero(failures, "event queue diagnostic revision", receipt.EventQueueDiagnosticRevision);
            RequireZero(failures, "Hook-status queue diagnostic revision", receipt.HookStatusQueueDiagnosticRevision);
            RequireZero(failures, "zero-listener EventArgs creation", receipt.EventArgsCreated);
            RequireZero(failures, "event membership snapshot rebuild", receipt.EventSnapshotRebuilds);
            if (receipt.EventQueuePendingAtStart || receipt.EventQueuePendingAtEnd || receipt.HookStatusQueuePendingAtStart || receipt.HookStatusQueuePendingAtEnd)
                failures.Add("Core event/Hook-status queue was pending at a measurement boundary");
            if (!receipt.QaExplicitDemandActiveAtStart || !receipt.QaExplicitDemandActiveAtEnd || !receipt.QaUpdaterActiveAtStart || !receipt.QaUpdaterActiveAtEnd)
                failures.Add("QA observer was not bound to the ExplicitQa demand/updater route at both boundaries");
            if (receipt.ActiveOptionalDemandIdsAtStart.Length != 0 || receipt.ActiveOptionalDemandIdsAtEnd.Length != 0)
                failures.Add("an optional product demand was active");
            if (receipt.ActiveOptionalUpdaterIdsAtStart.Length != 0 || receipt.ActiveOptionalUpdaterIdsAtEnd.Length != 0)
                failures.Add("an optional product updater was active");
            ValidateMandatoryCadence(failures, receipt);

            receipt.Passed = failures.Count == 0;
            receipt.FailureReason = string.Join("; ", failures.ToArray());
            result.NoDemandProfile = receipt;
            if (!receipt.Passed)
            {
                result.Status = "failed-batch5-no-demand-profile";
                result.FailureReason = receipt.FailureReason;
            }
        }

        private static Batch5CounterDelta Delta(long start, long end) => new Batch5CounterDelta(start, end);

        private static Batch5MandatoryUpdaterCadenceReceipt[] BuildMandatoryUpdaterCadence(
            Batch5NoDemandRuntimeSnapshot start,
            Batch5NoDemandRuntimeSnapshot end)
        {
            var byId = new SortedDictionary<string, Batch5MandatoryUpdaterCadenceReceipt>(StringComparer.OrdinalIgnoreCase);
            foreach (Batch5NoDemandMandatoryUpdaterSnapshot item in start.MandatoryUpdaterDispatches)
            {
                byId[item.CapabilityId] = new Batch5MandatoryUpdaterCadenceReceipt
                {
                    CapabilityId = item.CapabilityId,
                    ActiveAtStart = item.Active,
                    Dispatches = new Batch5CounterDelta(item.DispatchCount, item.DispatchCount)
                };
            }
            foreach (Batch5NoDemandMandatoryUpdaterSnapshot item in end.MandatoryUpdaterDispatches)
            {
                if (!byId.TryGetValue(item.CapabilityId, out Batch5MandatoryUpdaterCadenceReceipt? cadence))
                {
                    cadence = new Batch5MandatoryUpdaterCadenceReceipt
                    {
                        CapabilityId = item.CapabilityId,
                        Dispatches = new Batch5CounterDelta(0, item.DispatchCount)
                    };
                    byId[item.CapabilityId] = cadence;
                }
                else
                {
                    cadence.Dispatches = new Batch5CounterDelta(cadence.Dispatches.Start, item.DispatchCount);
                }
                cadence.ActiveAtEnd = item.Active;
            }

            var result = new Batch5MandatoryUpdaterCadenceReceipt[byId.Count];
            int index = 0;
            foreach (Batch5MandatoryUpdaterCadenceReceipt item in byId.Values)
                result[index++] = item;
            return result;
        }

        private static void ValidateMandatoryCadence(
            List<string> failures,
            Batch5NoDemandPerformanceReceipt receipt)
        {
            if (receipt.ActiveMandatoryUpdaterIdsAtStart.Length == 0 ||
                receipt.ActiveMandatoryUpdaterIdsAtEnd.Length == 0 ||
                receipt.MandatoryBaseUpdaterInvocations.Delta <= 0)
            {
                failures.Add("mandatory base cadence was not independently observed");
            }

            long composedDelta = 0;
            foreach (Batch5MandatoryUpdaterCadenceReceipt cadence in receipt.MandatoryUpdaterCadence)
            {
                composedDelta += cadence.Dispatches.Delta;
                if (cadence.Dispatches.Delta < 0)
                    failures.Add("mandatory updater counter regressed capability=" + cadence.CapabilityId);
            }
            if (composedDelta != receipt.MandatoryBaseUpdaterInvocations.Delta)
                failures.Add("mandatory updater per-capability deltas do not compose to the aggregate");

            RequireMandatoryCadence(
                failures,
                receipt,
                GameBridgeDemandRoutes.CoreUiContext,
                receipt.MeasurementFrameTarget,
                exactTarget: true);
            RequireMandatoryCadence(
                failures,
                receipt,
                GameBridgeDemandRoutes.ContentRefreshDrain,
                receipt.MeasurementFrameTarget,
                exactTarget: true);
            RequireMandatoryCadence(
                failures,
                receipt,
                GameBridgeDemandRoutes.NativeUiLayoutDiagnostics,
                receipt.MeasurementFrameTarget,
                exactTarget: false);
        }

        private static void RequireMandatoryCadence(
            List<string> failures,
            Batch5NoDemandPerformanceReceipt receipt,
            string capabilityId,
            int measurementFrameTarget,
            bool exactTarget)
        {
            Batch5MandatoryUpdaterCadenceReceipt? found = null;
            foreach (Batch5MandatoryUpdaterCadenceReceipt cadence in receipt.MandatoryUpdaterCadence)
            {
                if (cadence.CapabilityId.Equals(capabilityId, StringComparison.OrdinalIgnoreCase))
                {
                    found = cadence;
                    break;
                }
            }
            if (found == null)
            {
                failures.Add("mandatory updater cadence missing capability=" + capabilityId);
                return;
            }
            if (!found.ActiveAtStart || !found.ActiveAtEnd)
                failures.Add("mandatory updater was not active at both boundaries capability=" + capabilityId);
            if (exactTarget && found.Dispatches.Delta != measurementFrameTarget)
                failures.Add("mandatory every-frame updater delta != measurement target capability=" + capabilityId);
            if (!exactTarget && (found.Dispatches.Delta <= 0 || found.Dispatches.Delta > measurementFrameTarget))
                failures.Add("mandatory bounded-cadence updater delta is outside (0, measurement target] capability=" + capabilityId);
        }

        private static void RequireZero(List<string> failures, string label, Batch5CounterDelta counter)
        {
            if (counter.Delta != 0)
                failures.Add(label + " delta=" + counter.Delta.ToString(CultureInfo.InvariantCulture));
        }

        private void EnsureProbe(AutoFishingPerformanceFixtureSnapshot snapshot)
        {
            if (probe != null)
                return;

            var trend = new RuntimeMemoryTrendProbe(
                TimeSpan.FromSeconds(IsBatch5AutoFishing ? settings.Batch5GcLadderSampleSeconds : 30),
                runtimeMemoryMetricsProvider.Capture,
                CaptureDomainSnapshot);
            probe = new FishingPerformanceProbe(
                Profile,
                settings.AutoFishingPerformanceTargetFish,
                Profile.Equals("FishLoop", StringComparison.OrdinalIgnoreCase) ? settings.AutoFishingPerformanceWarmupFish : 0,
                TimeSpan.FromSeconds(Math.Max(0, settings.AutoFishingPerformanceZeroWarmupSeconds)),
                TimeSpan.FromSeconds(Math.Max(1, settings.AutoFishingPerformanceZeroMeasureSeconds)),
                snapshot.PullExited,
                snapshot.ObservedAtUtc,
                RuntimeThreadAllocationProbe.ResolveAllocatedBytesGetter(),
                RuntimeThreadAllocationProbe.AllocateCalibrationBuffer,
                trend,
                settings.Batch5GcLadderEnabled && settings.Batch5GcLadderDomain.Equals("AutoFishing", StringComparison.OrdinalIgnoreCase)
                    ? TimeSpan.FromSeconds(settings.Batch5GcLadderMeasureSeconds)
                    : TimeSpan.Zero,
                settings.AutoFishingPerformanceTargetFrames,
                settings.AutoFishingPerformanceWarmupFrames);
            probe.Result.QaHostRunId = access.RunId;
            ApplyBatch5Identity(probe.Result);
            ApplyNativeVitalsState(probe.Result);
        }

        private RuntimeMemoryDomainSnapshot CaptureDomainSnapshot()
        {
            AutoFishingPerformanceFixtureSnapshot snapshot = latestSnapshot
                ?? throw new InvalidOperationException("AutoFishing performance domain metrics were requested before an observation snapshot.");
            return QaRuntimeMemorySnapshotCapture.Capture(
                access,
                snapshot.AccessorBuilds,
                snapshot.VisibleReelConsumed,
                snapshot.NativeTransient);
        }

        private long GetCurrentLogLength()
        {
            string path = access.LatestLogPath;
            return string.IsNullOrWhiteSpace(path) || !File.Exists(path) ? 0L : new FileInfo(path).Length;
        }

        private int GetFishingLogLineCount(bool hotOnly)
        {
            string path = access.LatestLogPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return 0;
            int count = 0;
            foreach (string line in File.ReadLines(path))
            {
                if (hotOnly)
                {
                    if (line.IndexOf("AutoFishing action rejected", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("Fishing native state cache refresh failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("Fishing synthetic-input provider failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("First-party fishing CastHook animation adjustment failed", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        count++;
                    }
                }
                else if (line.IndexOf("Fishing", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    count++;
                }
            }
            return count;
        }

        private static void ApplyStart(FishingPerformanceResult result, AutoFishingPerformanceFixtureSnapshot value)
        {
            result.AccessorBuildsStart = value.AccessorBuilds;
            result.AccessorRebuildsStart = value.AccessorRebuilds;
            result.AccessorBuildFailuresStart = value.AccessorBuildFailures;
            result.AccessorInvocationFailuresStart = value.AccessorInvocationFailures;
            result.CastsStart = value.Casts;
            result.FishStart = value.Fish;
            result.LegacyOptionsStart = value.LegacyOptions;
            result.LegacyStatesStart = value.LegacyStates;
            result.SessionStart = value.Sessions;
            result.InputLeaseStart = value.InputLeases;
            result.AnimationLeaseStart = value.AnimationLeases;
            result.HookRuntimeStart = value.HookRuntimes;
            result.NativeReferencesStart = value.NativeReferences;
            result.SelectedRodStart = value.SelectedRod;
            result.HorizontalMoveFactorAvailableStart = value.HorizontalMoveFactorAvailable;
            result.NativeTryCastEnergyGateUnavailableStart = value.NativeTryCastEnergyGateUnavailable;
            result.NativeTryCastInsufficientEnergyStart = value.NativeTryCastInsufficientEnergy;
            result.SetVisibleReelStart(value.VisibleReelQueued, value.VisibleReelConsumed, value.VisibleReelNativeAccepted, value.VisibleReelRetries, value.VisibleReelTimeouts);
        }

        private static void ApplyEnd(FishingPerformanceResult result, AutoFishingPerformanceFixtureSnapshot value)
        {
            result.AccessorBuildsEnd = value.AccessorBuilds;
            result.AccessorBuildsDelta = Math.Max(0, value.AccessorBuilds - result.AccessorBuildsStart);
            result.AccessorRebuildsEnd = value.AccessorRebuilds;
            result.AccessorRebuildsDelta = Math.Max(0, value.AccessorRebuilds - result.AccessorRebuildsStart);
            result.AccessorBuildFailuresEnd = value.AccessorBuildFailures;
            result.AccessorBuildFailuresDelta = Math.Max(0, value.AccessorBuildFailures - result.AccessorBuildFailuresStart);
            result.AccessorInvocationFailuresEnd = value.AccessorInvocationFailures;
            result.AccessorInvocationFailuresDelta = Math.Max(0, value.AccessorInvocationFailures - result.AccessorInvocationFailuresStart);
            result.CastsEnd = value.Casts;
            result.Casts = Math.Max(0, value.Casts - result.CastsStart);
            result.FishEnd = value.Fish;
            result.Fish = Math.Max(0, value.Fish - result.FishStart);
            result.LegacyOptionsEnd = value.LegacyOptions;
            result.LegacyStatesEnd = value.LegacyStates;
            result.SessionEnd = value.Sessions;
            result.InputLeaseEnd = value.InputLeases;
            result.AnimationLeaseEnd = value.AnimationLeases;
            result.HookRuntimeEnd = value.HookRuntimes;
            result.NativeTransientEnd = value.NativeTransient;
            result.NativeReferencesEnd = value.NativeReferences;
            result.SelectedRodEnd = value.SelectedRod;
            result.HorizontalMoveFactorAvailableEnd = value.HorizontalMoveFactorAvailable;
            result.SchedulerPendingEnd = value.SchedulerPending;
            ApplyNativeTryCastEnergyRejectionEnd(result, value.NativeTryCastEnergyGateUnavailable, value.NativeTryCastInsufficientEnergy);
            result.SetVisibleReelEnd(value.VisibleReelQueued, value.VisibleReelConsumed, value.VisibleReelNativeAccepted, value.VisibleReelRetries, value.VisibleReelTimeouts);
        }

        private static void ApplyNativeTryCastEnergyRejectionEnd(FishingPerformanceResult result, long energyGateUnavailable, long insufficientEnergy)
        {
            result.NativeTryCastEnergyGateUnavailableEnd = energyGateUnavailable;
            result.NativeTryCastEnergyGateUnavailableDelta = Math.Max(0L, energyGateUnavailable - result.NativeTryCastEnergyGateUnavailableStart);
            result.NativeTryCastInsufficientEnergyEnd = insufficientEnergy;
            result.NativeTryCastInsufficientEnergyDelta = Math.Max(0L, insufficientEnergy - result.NativeTryCastInsufficientEnergyStart);
        }

        private static void ValidateProfile(FishingPerformanceResult result)
        {
            if (result.Profile.Equals("FishLoop", StringComparison.OrdinalIgnoreCase))
            {
                if (result.Batch5Level.Equals("L0", StringComparison.Ordinal) &&
                    !AutoFishingNativeControlFixturePolicy.HasSufficientVisibleReelUnits(
                        result.MeasuredFish,
                        result.VisibleReelConsumedDelta,
                        result.VisibleReelNativeAcceptedDelta))
                {
                    result.Status = "failed-batch5-l0-visible-reel-receipt";
                    result.FailureReason = "Batch 5 AutoFishing L0 measured PullExited units without an equal visible-reel consumed/native-accepted receipt.";
                }
                return;
            }

            string failure = string.Empty;
            if (result.MeasuredFish != 0 || result.Fish != 0 || result.Casts != 0)
                failure = "zero-fish profile observed cast or fish progress";
            else if (result.AccessorBuildsDelta != 0 || result.AccessorRebuildsDelta != 0 || result.AccessorBuildFailuresDelta != 0 || result.AccessorInvocationFailuresDelta != 0)
                failure = "accessor telemetry changed during measurement";
            else if (result.FishingHotLogLines != 0)
                failure = "repeated Fishing hot logs were emitted during measurement";
            else if (result.LegacyOptionsStart != 0 || result.LegacyOptionsEnd != 0 || result.LegacyStatesStart != 0 || result.LegacyStatesEnd != 0)
                failure = "legacy compatibility maps were populated";
            else if (result.Profile.Equals("InactiveNoConsumer", StringComparison.OrdinalIgnoreCase) &&
                (result.SessionStart != 0 || result.SessionEnd != 0 || result.HookRuntimeStart != 0 || result.HookRuntimeEnd != 0 ||
                    result.AccessorBuildsStart != 0 || result.AccessorBuildsEnd != 0 || result.NativeReferencesStart != 0 || result.NativeReferencesEnd != 0 ||
                    result.VisibleReelQueuedStart != 0 || result.VisibleReelQueuedEnd != 0 || result.VisibleReelConsumedStart != 0 || result.VisibleReelConsumedEnd != 0 ||
                    result.VisibleReelNativeAcceptedStart != 0 || result.VisibleReelNativeAcceptedEnd != 0 ||
                    result.VisibleReelRetriesStart != 0 || result.VisibleReelRetriesEnd != 0 || result.VisibleReelTimeoutsStart != 0 || result.VisibleReelTimeoutsEnd != 0))
                failure = "InactiveNoConsumer created runtime or native state";
            else if (result.Profile.Equals("EnabledNoRod", StringComparison.OrdinalIgnoreCase) && (result.SelectedRodStart || result.SelectedRodEnd))
                failure = "EnabledNoRod observed a selected rod during measurement";
            else if (result.Profile.Equals("EnabledNoRod", StringComparison.OrdinalIgnoreCase) && (!result.HorizontalMoveFactorAvailableStart || !result.HorizontalMoveFactorAvailableEnd))
                failure = "EnabledNoRod did not retain native HorizontalMoveFactor availability at both measurement boundaries";
            if (failure.Length == 0)
                return;
            result.Status = "failed-invariants";
            result.FailureReason = failure;
        }

        private void WriteResult(FishingPerformanceResult result, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(resultPath))
            {
                string directoryName = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + access.RunId.Substring(0, Math.Min(8, access.RunId.Length));
                string directory = Path.Combine(access.RuntimeEvidenceRoot, "AUTO-FISHING-PERF", directoryName);
                Directory.CreateDirectory(directory);
                resultPath = Path.Combine(directory, "auto-fishing-performance.json");
            }
            if (resultWritten && !overwrite)
                return;
            using (FileStream stream = File.Create(resultPath))
                new DataContractJsonSerializer(typeof(FishingPerformanceResult)).WriteObject(stream, result);
            bool allocationBlocked = result.AllocationProbeStatus.StartsWith("blocked-allocation-counter-", StringComparison.Ordinal);
            string performanceStatus = result.Status == "completed" && result.TitleCleanupVerified
                ? "verified"
                : result.TitleCleanupPending ? "pending" : "failed";
            string allocatedBytes = result.AllocatedBytes.HasValue ? result.AllocatedBytes.Value.ToString(CultureInfo.InvariantCulture) : "null";
            string gen0 = result.ProcessGen0Collections.HasValue ? result.ProcessGen0Collections.Value.ToString(CultureInfo.InvariantCulture) : "null";
            access.PublishAutoFishingPerformanceStatus(
                "Smoke.AutoFishingAllocationCounter",
                allocationBlocked ? "blocked" : result.AllocationCounterFunctional ? "verified" : "pending",
                "QA-owned same-thread allocation capability",
                "probeStatus=" + result.AllocationProbeStatus + "; counter=" + result.AllocationCounterStatus + "; allocatedBytes=" + allocatedBytes + "; owner=qa; fallback=false");
            access.PublishAutoFishingPerformanceStatus(
                "Smoke.AutoFishingPerformance",
                performanceStatus,
                "QA-owned runtime memory trend plus first-party fishing counters",
                "result=" + resultPath + "; probeStatus=" + result.Status + "; runtimeTrend=" + result.RuntimeMemoryTrend.Status +
                "; allocationProbe=" + result.AllocationProbeStatus + "; measuredFish=" + result.MeasuredFish.ToString(CultureInfo.InvariantCulture) +
                "; allocatedBytes=" + allocatedBytes + "; processGen0=" + gen0 + "; samples=" + result.RuntimeMemoryTrend.Samples.Count.ToString(CultureInfo.InvariantCulture) +
                "; titleCleanup=" + result.TitleCleanupVerified.ToString(CultureInfo.InvariantCulture) + "; owner=qa; fallback=false");
            resultWritten = true;
        }
    }
}
