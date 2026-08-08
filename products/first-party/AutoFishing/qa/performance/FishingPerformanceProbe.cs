using System;
using System.Runtime.Serialization;
namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal enum FishingPerformanceProbeUpdate
    {
        None,
        MeasurementStarted,
        Completed,
        Blocked
    }

    internal sealed class FishingPerformanceProbe
    {
        private readonly RuntimeThreadAllocationProbe allocationProbe;
        private readonly RuntimeMemoryTrendProbe memoryTrendProbe;
        private readonly int targetFish;
        private readonly int warmupFish;
        private readonly TimeSpan zeroWarmup;
        private readonly TimeSpan zeroMeasure;
        private readonly TimeSpan positiveMinimumMeasure;
        private readonly int targetFrames;
        private readonly int warmupFrames;
        private readonly long initialPullExited;
        private readonly DateTimeOffset createdAtUtc;
        private bool measurementStarted;
        private bool completed;
        private DateTimeOffset measurementStartedAtUtc;
        private long measurementPullBaseline;
        private int observedWarmupFrames;
        private int measuredFrames;

        internal FishingPerformanceProbe(int targetFish, int warmupFish, TimeSpan zeroWarmup, TimeSpan zeroMeasure, long initialPullExited, DateTimeOffset nowUtc)
            : this("FishLoop", targetFish, warmupFish, zeroWarmup, zeroMeasure, initialPullExited, nowUtc)
        {
        }

        internal FishingPerformanceProbe(string profile, int targetFish, int warmupFish, TimeSpan zeroWarmup, TimeSpan zeroMeasure, long initialPullExited, DateTimeOffset nowUtc)
            : this(profile, targetFish, warmupFish, zeroWarmup, zeroMeasure, initialPullExited, nowUtc, RuntimeThreadAllocationProbe.ResolveAllocatedBytesGetter(), RuntimeThreadAllocationProbe.AllocateCalibrationBuffer)
        {
        }

        internal FishingPerformanceProbe(
            string profile,
            int targetFish,
            int warmupFish,
            TimeSpan zeroWarmup,
            TimeSpan zeroMeasure,
            long initialPullExited,
            DateTimeOffset nowUtc,
            Func<long>? allocatedBytesGetter,
            Action? calibrationAllocation,
            RuntimeMemoryTrendProbe? runtimeMemoryTrendProbe = null,
            TimeSpan? positiveMinimumMeasure = null,
            int targetFrames = 0,
            int warmupFrames = 0)
        {
            this.targetFish = Math.Max(0, targetFish);
            this.warmupFish = Math.Max(0, warmupFish);
            this.zeroWarmup = zeroWarmup < TimeSpan.Zero ? TimeSpan.Zero : zeroWarmup;
            this.zeroMeasure = zeroMeasure <= TimeSpan.Zero ? TimeSpan.FromMinutes(10) : zeroMeasure;
            this.positiveMinimumMeasure = positiveMinimumMeasure.GetValueOrDefault() < TimeSpan.Zero
                ? TimeSpan.Zero
                : positiveMinimumMeasure.GetValueOrDefault();
            this.targetFrames = Math.Max(0, targetFrames);
            this.warmupFrames = Math.Max(0, warmupFrames);
            this.initialPullExited = initialPullExited;
            createdAtUtc = nowUtc;
            allocationProbe = new RuntimeThreadAllocationProbe(allocatedBytesGetter, calibrationAllocation);
            memoryTrendProbe = runtimeMemoryTrendProbe ?? new RuntimeMemoryTrendProbe();
            RuntimeThreadAllocationResult allocation = allocationProbe.Result;
            Result = new FishingPerformanceResult
            {
                SchemaVersion = 2,
                Domain = "AutoFishing",
                Profile = string.IsNullOrWhiteSpace(profile) ? "FishLoop" : profile.Trim(),
                TargetFish = this.targetFish,
                WarmupFish = this.warmupFish,
                TargetFrames = this.targetFrames,
                WarmupFrames = this.warmupFrames,
                AllocationCounterAvailable = allocation.CounterAvailable,
                AllocationCounterStatus = allocation.CounterStatus,
                AllocationCalibrationPayloadBytes = allocation.CalibrationPayloadBytes,
                AllocationProbe = allocation,
                RuntimeMemoryTrend = memoryTrendProbe.Result,
                Status = "warming"
            };
        }

        internal FishingPerformanceResult Result { get; }

        internal FishingPerformanceProbeUpdate Observe(long pullExitedCount, DateTimeOffset nowUtc)
        {
            if (completed)
                return FishingPerformanceProbeUpdate.None;
            if (!measurementStarted)
            {
                bool warmed = targetFish == 0 && targetFrames > 0
                    ? observedWarmupFrames >= warmupFrames
                    : targetFish == 0
                    ? nowUtc - createdAtUtc >= zeroWarmup
                    : pullExitedCount - initialPullExited >= warmupFish;
                if (!warmed)
                {
                    if (targetFish == 0 && targetFrames > 0)
                        observedWarmupFrames++;
                    return FishingPerformanceProbeUpdate.None;
                }
                allocationProbe.Start();
                CopyAllocationResult();
                measurementStarted = true;
                measurementStartedAtUtc = nowUtc;
                measurementPullBaseline = pullExitedCount;
                memoryTrendProbe.Start(nowUtc);
                Result.Status = "measuring";
                Result.MeasurementStartedAtUtc = nowUtc;
                Result.WarmupFramesActual = observedWarmupFrames;
                return FishingPerformanceProbeUpdate.MeasurementStarted;
            }

            long measuredFish = pullExitedCount - measurementPullBaseline;
            if (targetFrames > 0)
                measuredFrames++;
            memoryTrendProbe.Observe(nowUtc);
            bool done = targetFish == 0
                ? targetFrames > 0
                    ? measuredFrames >= targetFrames
                    : nowUtc - measurementStartedAtUtc >= zeroMeasure
                : measuredFish >= targetFish && nowUtc - measurementStartedAtUtc >= positiveMinimumMeasure;
            if (!done)
                return FishingPerformanceProbeUpdate.None;

            allocationProbe.Complete(measuredFish);
            memoryTrendProbe.Complete(nowUtc);
            CopyAllocationResult();
            completed = true;
            Result.Status = targetFish == 0 && measuredFish != 0 ? "failed-nonzero-catch" : "completed";
            Result.CompletedAtUtc = nowUtc;
            Result.ElapsedSeconds = Math.Max(0d, (nowUtc - measurementStartedAtUtc).TotalSeconds);
            Result.MeasuredFish = measuredFish;
            Result.MeasuredFrames = measuredFrames;
            Result.ProcessGen0Collections = Result.RuntimeMemoryTrend.ProcessGen0Collections.Delta;
            Result.ProcessGen1Collections = Result.RuntimeMemoryTrend.ProcessGen1Collections.Delta;
            Result.ProcessGen2Collections = Result.RuntimeMemoryTrend.ProcessGen2Collections.Delta;
            Result.ProcessGen0CollectionsPerFish = measuredFish > 0 && Result.ProcessGen0Collections.HasValue ? (double)Result.ProcessGen0Collections.Value / measuredFish : null;
            return FishingPerformanceProbeUpdate.Completed;
        }

        internal void SetBaselineMetrics(long logBytes, int snapshotBuilds, int nativeTransient)
        {
            Result.LogBytesStart = logBytes;
            Result.SnapshotBuildsStart = snapshotBuilds;
            Result.NativeTransientStart = nativeTransient;
        }

        internal void SetCompletionMetrics(long logBytes, int snapshotBuilds, int nativeTransient)
        {
            Result.LogBytesEnd = logBytes;
            Result.TotalLogBytes = logBytes;
            Result.LogBytes = Math.Max(0L, logBytes - Result.LogBytesStart);
            Result.SnapshotBuildsEnd = snapshotBuilds;
            Result.SnapshotBuilds = Math.Max(0, snapshotBuilds - Result.SnapshotBuildsStart);
            Result.NativeTransientEnd = nativeTransient;
            Result.LogBytesPerFish = Result.MeasuredFish > 0 ? (double)Result.LogBytes / Result.MeasuredFish : 0d;
            Result.SnapshotBuildsPerFish = Result.MeasuredFish > 0 ? (double)Result.SnapshotBuilds / Result.MeasuredFish : 0d;
        }

        private void CopyAllocationResult()
        {
            RuntimeThreadAllocationResult allocation = allocationProbe.Result;
            Result.AllocationCounterAvailable = allocation.CounterAvailable;
            Result.AllocationCounterFunctional = allocation.CounterFunctional;
            Result.AllocationCounterStatus = allocation.CounterStatus;
            Result.AllocationProbeStatus = allocation.ProbeStatus;
            Result.AllocationFailureReason = allocation.FailureReason;
            Result.AllocationCalibrationPayloadBytes = allocation.CalibrationPayloadBytes;
            Result.AllocationCalibrationBefore = allocation.CalibrationBefore;
            Result.AllocationCalibrationAfter = allocation.CalibrationAfter;
            Result.AllocationCalibrationDelta = allocation.CalibrationDelta;
            Result.AllocatedBytes = allocation.AllocatedBytes;
            Result.AllocatedBytesPerFish = allocation.AllocatedBytesPerUnit;
        }
    }

    [DataContract]
    internal sealed class FishingPerformanceResult
    {
        internal void SetVisibleReelStart(long queued, long consumed, long nativeAccepted, long retries, long timeouts)
        {
            VisibleReelQueuedStart = queued;
            VisibleReelConsumedStart = consumed;
            VisibleReelNativeAcceptedStart = nativeAccepted;
            VisibleReelRetriesStart = retries;
            VisibleReelTimeoutsStart = timeouts;
        }

        internal void SetVisibleReelEnd(long queued, long consumed, long nativeAccepted, long retries, long timeouts)
        {
            VisibleReelQueuedEnd = queued;
            VisibleReelQueuedDelta = Math.Max(0L, queued - VisibleReelQueuedStart);
            VisibleReelConsumedEnd = consumed;
            VisibleReelConsumedDelta = Math.Max(0L, consumed - VisibleReelConsumedStart);
            VisibleReelNativeAcceptedEnd = nativeAccepted;
            VisibleReelNativeAcceptedDelta = Math.Max(0L, nativeAccepted - VisibleReelNativeAcceptedStart);
            VisibleReelRetriesEnd = retries;
            VisibleReelRetriesDelta = Math.Max(0L, retries - VisibleReelRetriesStart);
            VisibleReelTimeoutsEnd = timeouts;
            VisibleReelTimeoutsDelta = Math.Max(0L, timeouts - VisibleReelTimeoutsStart);
        }

        [DataMember] public int SchemaVersion { get; set; }
        [DataMember] public string QaHostRunId { get; set; } = string.Empty;
        [DataMember] public string Domain { get; set; } = string.Empty;
        [DataMember] public string Level { get; set; } = string.Empty;
        [DataMember] public string Workload { get; set; } = string.Empty;
        [DataMember] public string ProductState { get; set; } = string.Empty;
        [DataMember] public double Multiplier { get; set; }
        [DataMember] public int MeasureSeconds { get; set; }
        [DataMember] public int SampleSeconds { get; set; }
        [DataMember] public int TargetUnits { get; set; }
        [DataMember] public int RequiredActiveDurationSeconds { get; set; }
        [DataMember] public int SaveSlot { get; set; }
        [DataMember] public bool ForcedGc { get; set; }
        [DataMember] public string Scenario { get; set; } = string.Empty;
        [DataMember] public bool WorkloadCompleted { get; set; }
        [DataMember] public long CompletedUnits { get; set; }
        [DataMember] public bool ActiveWindowSatisfied { get; set; }
        [DataMember] public string BehaviorReceiptKind { get; set; } = string.Empty;
        [DataMember] public bool BehaviorVerified { get; set; }
        [DataMember] public bool TitleCycleObserved { get; set; }
        [DataMember] public string Profile { get; set; } = string.Empty;
        [DataMember] public Batch5NoDemandPerformanceReceipt? NoDemandProfile { get; set; }
        [DataMember] public string Status { get; set; } = string.Empty;
        [DataMember] public string FailureReason { get; set; } = string.Empty;
        [DataMember] public int TargetFish { get; set; }
        [DataMember] public int WarmupFish { get; set; }
        [DataMember] public long MeasuredFish { get; set; }
        [DataMember] public int TargetFrames { get; set; }
        [DataMember] public int WarmupFrames { get; set; }
        [DataMember] public int WarmupFramesActual { get; set; }
        [DataMember] public int MeasuredFrames { get; set; }
        [DataMember] public bool AllocationCounterAvailable { get; set; }
        [DataMember] public bool AllocationCounterFunctional { get; set; }
        [DataMember] public string AllocationCounterStatus { get; set; } = string.Empty;
        [DataMember] public string AllocationProbeStatus { get; set; } = string.Empty;
        [DataMember] public string AllocationFailureReason { get; set; } = string.Empty;
        [DataMember] public RuntimeThreadAllocationResult AllocationProbe { get; set; } = new RuntimeThreadAllocationResult();
        [DataMember] public RuntimeMemoryTrendResult RuntimeMemoryTrend { get; set; } = new RuntimeMemoryTrendResult();
        [DataMember] public int AllocationCalibrationPayloadBytes { get; set; }
        [DataMember] public long AllocationCalibrationBefore { get; set; }
        [DataMember] public long AllocationCalibrationAfter { get; set; }
        [DataMember] public long AllocationCalibrationDelta { get; set; }
        [DataMember] public DateTimeOffset MeasurementStartedAtUtc { get; set; }
        [DataMember] public DateTimeOffset CompletedAtUtc { get; set; }
        [DataMember] public double ElapsedSeconds { get; set; }
        [DataMember] public long? AllocatedBytes { get; set; }
        [DataMember] public double? AllocatedBytesPerFish { get; set; }
        [DataMember] public long? ProcessGen0Collections { get; set; }
        [DataMember] public long? ProcessGen1Collections { get; set; }
        [DataMember] public long? ProcessGen2Collections { get; set; }
        [DataMember] public double? ProcessGen0CollectionsPerFish { get; set; }
        [DataMember] public long LogBytesStart { get; set; }
        [DataMember] public long LogBytesEnd { get; set; }
        [DataMember] public long LogBytes { get; set; }
        [DataMember] public long TotalLogBytes { get; set; }
        [DataMember] public int FishingLogLinesStart { get; set; }
        [DataMember] public int FishingLogLinesEnd { get; set; }
        [DataMember] public int FishingLogLines { get; set; }
        [DataMember] public int FishingHotLogLinesStart { get; set; }
        [DataMember] public int FishingHotLogLinesEnd { get; set; }
        [DataMember] public int FishingHotLogLines { get; set; }
        [DataMember] public double LogBytesPerFish { get; set; }
        [DataMember] public int SnapshotBuildsStart { get; set; }
        [DataMember] public int SnapshotBuildsEnd { get; set; }
        [DataMember] public int SnapshotBuilds { get; set; }
        [DataMember] public double SnapshotBuildsPerFish { get; set; }
        [DataMember] public int NativeTransientStart { get; set; }
        [DataMember] public int NativeTransientEnd { get; set; }
        [DataMember] public long VisibleReelQueuedStart { get; set; }
        [DataMember] public long VisibleReelQueuedEnd { get; set; }
        [DataMember] public long VisibleReelQueuedDelta { get; set; }
        [DataMember] public long VisibleReelConsumedStart { get; set; }
        [DataMember] public long VisibleReelConsumedEnd { get; set; }
        [DataMember] public long VisibleReelConsumedDelta { get; set; }
        [DataMember] public long VisibleReelNativeAcceptedStart { get; set; }
        [DataMember] public long VisibleReelNativeAcceptedEnd { get; set; }
        [DataMember] public long VisibleReelNativeAcceptedDelta { get; set; }
        [DataMember] public long VisibleReelRetriesStart { get; set; }
        [DataMember] public long VisibleReelRetriesEnd { get; set; }
        [DataMember] public long VisibleReelRetriesDelta { get; set; }
        [DataMember] public long VisibleReelTimeoutsStart { get; set; }
        [DataMember] public long VisibleReelTimeoutsEnd { get; set; }
        [DataMember] public long VisibleReelTimeoutsDelta { get; set; }
        [DataMember] public int AccessorBuildsStart { get; set; }
        [DataMember] public int AccessorBuildsEnd { get; set; }
        [DataMember] public int AccessorBuildsDelta { get; set; }
        [DataMember] public int AccessorRebuildsStart { get; set; }
        [DataMember] public int AccessorRebuildsEnd { get; set; }
        [DataMember] public int AccessorRebuildsDelta { get; set; }
        [DataMember] public int AccessorBuildFailuresStart { get; set; }
        [DataMember] public int AccessorBuildFailuresEnd { get; set; }
        [DataMember] public int AccessorBuildFailuresDelta { get; set; }
        [DataMember] public int AccessorInvocationFailuresStart { get; set; }
        [DataMember] public int AccessorInvocationFailuresEnd { get; set; }
        [DataMember] public int AccessorInvocationFailuresDelta { get; set; }
        [DataMember] public long CastsStart { get; set; }
        [DataMember] public long CastsEnd { get; set; }
        [DataMember] public long Casts { get; set; }
        [DataMember] public long FishStart { get; set; }
        [DataMember] public long FishEnd { get; set; }
        [DataMember] public long Fish { get; set; }
        [DataMember] public int LegacyOptionsStart { get; set; }
        [DataMember] public int LegacyOptionsEnd { get; set; }
        [DataMember] public int LegacyStatesStart { get; set; }
        [DataMember] public int LegacyStatesEnd { get; set; }
        [DataMember] public int SessionStart { get; set; }
        [DataMember] public int SessionEnd { get; set; }
        [DataMember] public int InputLeaseStart { get; set; }
        [DataMember] public int InputLeaseEnd { get; set; }
        [DataMember] public int AnimationLeaseStart { get; set; }
        [DataMember] public int AnimationLeaseEnd { get; set; }
        [DataMember] public int HookRuntimeStart { get; set; }
        [DataMember] public int HookRuntimeEnd { get; set; }
        [DataMember] public int NativeReferencesStart { get; set; }
        [DataMember] public int NativeReferencesEnd { get; set; }
        [DataMember] public bool SelectedRodStart { get; set; }
        [DataMember] public bool SelectedRodEnd { get; set; }
        [DataMember] public bool NativeMovementAvailableStart { get; set; }
        [DataMember] public bool NativeMovementAvailableEnd { get; set; }
        [DataMember] public bool MovementCancellationVerified { get; set; }
        [DataMember] public string MovementCancellationSource { get; set; } = string.Empty;
        [DataMember] public bool SchedulerPendingEnd { get; set; }
        [DataMember] public int NativeTransientAfterTitle { get; set; }
        [DataMember] public int SessionAfterTitle { get; set; }
        [DataMember] public int InputLeaseAfterTitle { get; set; }
        [DataMember] public int AnimationLeaseAfterTitle { get; set; }
        [DataMember] public bool SchedulerPendingAfterTitle { get; set; }
        [DataMember] public int LegacyOptionsAfterTitle { get; set; }
        [DataMember] public int LegacyStatesAfterTitle { get; set; }
        [DataMember] public int HookRuntimeAfterTitle { get; set; }
        [DataMember] public int NativeReferencesAfterTitle { get; set; }
        [DataMember] public int VisibleReelPendingAfterTitle { get; set; }
        [DataMember] public string Batch5Level { get; set; } = string.Empty;
        [DataMember] public bool Batch5InitialTitleCleanupVerified { get; set; }
        [DataMember] public bool DisableRecoveryVerified { get; set; }
        [DataMember] public int NativeRecoveryUnits { get; set; }
        [DataMember] public string DisableRecoverySource { get; set; } = string.Empty;
        [DataMember] public bool TitleReloadCycleVerified { get; set; }
        [DataMember] public int TitleReloadSaveLoads { get; set; }
        [DataMember] public bool ReenabledAfterReload { get; set; }
        [DataMember] public bool DisabledAfterReload { get; set; }
        [DataMember] public string TitleReloadSource { get; set; } = string.Empty;
        [DataMember] public bool NativeVitalsReadbackVerified { get; set; }
        [DataMember] public bool NativeEnergyExhaustionObserved { get; set; }
        [DataMember] public bool NativeVitalsVerified { get; set; }
        [DataMember] public int NativeVitalsRequiredWorkloadReceipts { get; set; }
        [DataMember] public int NativeVitalsPrepareCount { get; set; }
        [DataMember] public int NativeVitalsWorkloadReceiptCount { get; set; }
        [DataMember] public int NativeVitalsMaintenanceReceiptCount { get; set; }
        [DataMember] public bool NativeVitalsMeasurementMaintenanceRequired { get; set; }
        [DataMember] public int NativeVitalsMeasurementMaintenanceReceiptCount { get; set; }
        [DataMember] public int NativeVitalsL4CheckpointReceiptCount { get; set; }
        [DataMember] public bool NativeVitalsL4CheckpointVerified { get; set; }
        [DataMember] public string NativeVitalsL4CheckpointContext { get; set; } = string.Empty;
        [DataMember] public bool NativeVitalsL4CheckpointEnergyCommandInvoked { get; set; }
        [DataMember] public bool NativeVitalsL4CheckpointSpiritCommandInvoked { get; set; }
        [DataMember] public int NativeVitalsInitialSavePrepareCount { get; set; }
        [DataMember] public int NativeVitalsPostReloadPrepareCount { get; set; }
        [DataMember] public int NativeVitalsInitialSavePrepareOrdinal { get; set; }
        [DataMember] public int NativeVitalsPostReloadPrepareOrdinal { get; set; }
        [DataMember] public int NativeVitalsEnergyCommandCount { get; set; }
        [DataMember] public int NativeVitalsSpiritCommandCount { get; set; }
        [DataMember] public int NativeVitalsFinalObservationCount { get; set; }
        [DataMember] public int NativeEnergyInsufficientObservations { get; set; }
        [DataMember] public int NativeFishingEnergyCost { get; set; }
        [DataMember] public int NativeVitalsMaintenanceCadenceMilliseconds { get; set; }
        [DataMember] public double NativeEnergyPercentBeforeLast { get; set; }
        [DataMember] public double NativeEnergyPercentAfterLast { get; set; }
        [DataMember] public double NativeSpiritPercentBeforeLast { get; set; }
        [DataMember] public double NativeSpiritPercentAfterLast { get; set; }
        [DataMember] public double NativeEnergyPercentAtStageStart { get; set; }
        [DataMember] public double NativeSpiritPercentAtStageStart { get; set; }
        [DataMember] public double NativeEnergyPercentAtMeasurementEnd { get; set; }
        [DataMember] public double NativeSpiritPercentAtMeasurementEnd { get; set; }
        [DataMember] public bool NativeVitalsFinalReadbackVerified { get; set; }
        [DataMember] public string NativeVitalsLastContext { get; set; } = string.Empty;
        [DataMember] public string NativeVitalsSource { get; set; } = string.Empty;
        [DataMember] public bool NativeVitalsDelegateIdentitiesVerified { get; set; }
        [DataMember] public string NativeVitalsComposeEnergyDelegateIdentity { get; set; } = string.Empty;
        [DataMember] public string NativeVitalsComposeSpiritDelegateIdentity { get; set; } = string.Empty;
        [DataMember] public string NativeVitalsGetEnergyPercentDelegateIdentity { get; set; } = string.Empty;
        [DataMember] public string NativeVitalsGetSpiritPercentDelegateIdentity { get; set; } = string.Empty;
        [DataMember] public long NativeTryCastEnergyGateUnavailableStart { get; set; }
        [DataMember] public long NativeTryCastEnergyGateUnavailableEnd { get; set; }
        [DataMember] public long NativeTryCastEnergyGateUnavailableDelta { get; set; }
        [DataMember] public long NativeTryCastInsufficientEnergyStart { get; set; }
        [DataMember] public long NativeTryCastInsufficientEnergyEnd { get; set; }
        [DataMember] public long NativeTryCastInsufficientEnergyDelta { get; set; }
        [DataMember] public bool TitleCleanupVerified { get; set; }
        [DataMember] public bool TitleCleanupPending { get; set; } = true;
    }
}
