using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class Batch5GcLadderOrchestrator
    {
        private readonly QaHostSettings settings;
        private readonly GameBridgeFixtureAccess access;
        private readonly UnityRuntimeMemoryMetricsProvider platformMetrics = new UnityRuntimeMemoryMetricsProvider();
        private RuntimeMemoryTrendProbe? trend;
        private bool saveLoaded;
        private bool measurementCompleted;
        private bool terminal;
        private string resultPath = string.Empty;

        internal Batch5GcLadderOrchestrator(QaHostSettings settings, GameBridgeFixtureAccess access)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            Result = new Batch5GcLadderStageResult
            {
                SchemaVersion = 2,
                RunId = access.RunId,
                Domain = settings.Batch5GcLadderDomain,
                Level = settings.Batch5GcLadderLevel,
                Workload = settings.Batch5GcLadderWorkload,
                Multiplier = settings.Batch5GcLadderMultiplier,
                MeasureSeconds = settings.Batch5GcLadderMeasureSeconds,
                SampleSeconds = settings.Batch5GcLadderSampleSeconds,
                TargetUnits = settings.Batch5GcLadderTargetUnits,
                RequiredActiveDurationSeconds = settings.Batch5GcLadderMeasureSeconds,
                SaveSlot = settings.SaveSlot,
                ForcedGc = false,
                Status = "pending-save-loaded"
            };
        }

        internal bool Enabled => settings.Batch5GcLadderEnabled &&
            settings.Batch5GcLadderDomain.Equals("ActionSpeed", StringComparison.OrdinalIgnoreCase);

        internal bool RequiresTitleCycle => settings.Batch5GcLadderLevel.Equals("L5", StringComparison.Ordinal);

        internal bool ShouldRequestReturnHome => Enabled && measurementCompleted && RequiresTitleCycle && !Result.TitleCycleObserved;

        internal bool TerminalSucceeded { get; private set; }

        internal Batch5GcLadderStageResult Result { get; }

        internal Batch5GcLadderStageResult ResultForTests => Result;

        internal void OnSaveLoaded()
        {
            if (!Enabled || terminal || saveLoaded)
                return;
            saveLoaded = true;
            Result.Status = "waiting-active-workload";
            access.SetHookStatus(
                "Smoke.Batch5GcLadder.Performance",
                "pending",
                "optional QA active-workload performance sampler",
                FormatDetails("waiting-active-workload"));
        }

        internal void Update(ActionSpeedGcLadderProgress progress)
        {
            if (!Enabled || terminal || !saveLoaded)
                return;

            if (progress == null)
            {
                Fail("ActionSpeed GC ladder progress receipt was null.");
                return;
            }
            ValidateProgress(progress);
            SynchronizeProgress(progress);

            if (progress.CompletedUnits == 0)
                return;

            if (trend == null)
            {
                trend = new RuntimeMemoryTrendProbe(
                    TimeSpan.FromSeconds(Math.Max(1, settings.Batch5GcLadderSampleSeconds)),
                    platformMetrics.Capture,
                    CaptureDomainSnapshot);
                trend.Start(progress.FirstUnitAtUtc!.Value);
                Result.StartedAtUtc = progress.FirstUnitAtUtc.Value;
                Result.RuntimeMemoryTrend = trend.Result;
                Result.Status = "measuring-active-workload";
                access.SetHookStatus(
                    "Smoke.Batch5GcLadder.Performance",
                    "pending",
                    "optional QA active-workload performance sampler",
                    FormatDetails("measuring-active-workload"));
            }
            trend.Observe(progress.ObservedAtUtc);

            if (measurementCompleted || !progress.FixtureCompleted)
                return;

            ValidateCompletedFixture(progress);
            trend.Complete(progress.ObservedAtUtc);
            measurementCompleted = true;
            Result.MeasurementCompletedAtUtc = progress.ObservedAtUtc;
            Result.WorkloadCompleted = true;
            Result.MetricsAvailability = BuildAvailability(trend.Result);
            if (RequiresTitleCycle)
            {
                Result.Status = "waiting-title-cycle";
                WriteResult();
                access.SetHookStatus("Smoke.Batch5GcLadder.Performance", "pending", "optional QA active-workload performance sampler", FormatDetails("waiting-title-cycle"));
                return;
            }

            Result.BehaviorVerified = true;
            Result.Status = "completed";
            Result.CompletedAtUtc = progress.ObservedAtUtc;
            TerminalSucceeded = true;
            terminal = true;
            WriteResult();
            access.SetHookStatus("Smoke.Batch5GcLadder.Performance", "verified", "optional QA active-workload performance sampler", FormatDetails("completed"));
            access.Log("Batch 5 ActionSpeed GC ladder stage completed. " + FormatDetails("completed") + ".");
        }

        internal void OnReturnedToTitle()
        {
            if (!Enabled || terminal || !RequiresTitleCycle)
                return;
            if (!measurementCompleted)
                throw new InvalidOperationException("Batch 5 ActionSpeed title cycle arrived before measurement completed.");
            Result.TitleCycleObserved = true;
            Result.BehaviorVerified = true;
            Result.Status = "completed";
            Result.CompletedAtUtc = DateTimeOffset.UtcNow;
            TerminalSucceeded = true;
            terminal = true;
            WriteResult();
            access.SetHookStatus("Smoke.Batch5GcLadder.Performance", "verified", "optional QA active-workload performance sampler + ReturnedToTitle", FormatDetails("completed"));
            access.Log("Batch 5 ActionSpeed GC ladder title-cycle stage completed. " + FormatDetails("completed") + ".");
        }

        internal void Close(string reason)
        {
            if (!Enabled || terminal)
                return;
            if (trend != null && !measurementCompleted)
            {
                trend.Complete(DateTimeOffset.UtcNow);
                Result.RuntimeMemoryTrend = trend.Result;
                Result.MetricsAvailability = BuildAvailability(trend.Result);
            }
            Result.Status = "failed-host-closed-before-terminal";
            Result.FailureReason = "QA host closed before the GC ladder stage reached its terminal. reason=" + (reason ?? string.Empty) + ".";
            Result.CompletedAtUtc = DateTimeOffset.UtcNow;
            terminal = true;
            TerminalSucceeded = false;
            WriteResult();
            throw new InvalidOperationException(Result.FailureReason);
        }

        private void ValidateProgress(ActionSpeedGcLadderProgress progress)
        {
            if (!string.Equals(progress.Level, settings.Batch5GcLadderLevel, StringComparison.Ordinal))
                Fail("ActionSpeed GC ladder level receipt mismatch. expected=" + settings.Batch5GcLadderLevel + "; actual=" + progress.Level + ".");
            if (!string.Equals(progress.Workload, settings.Batch5GcLadderWorkload, StringComparison.Ordinal))
                Fail("ActionSpeed GC ladder workload receipt mismatch. expected=" + settings.Batch5GcLadderWorkload + "; actual=" + progress.Workload + ".");
            if (progress.ObservedAtUtc == default)
                Fail("ActionSpeed GC ladder observation timestamp was missing.");
            if (progress.CompletedUnits < Result.CompletedUnits)
                Fail("ActionSpeed GC ladder completed-unit receipts regressed.");
            if (progress.ActiveDurationSeconds + 0.001d < Result.ActiveDurationSeconds)
                Fail("ActionSpeed GC ladder active-duration receipts regressed.");
            if (progress.RecoveryUnits < Result.RecoveryUnits)
                Fail("ActionSpeed GC ladder recovery-unit receipts regressed.");
            if (progress.CompletedUnits == 0)
            {
                if (progress.FirstUnitAtUtc.HasValue || progress.LastUnitAtUtc.HasValue || progress.ActiveDurationSeconds > 0d || progress.ActiveWindowSatisfied)
                    Fail("ActionSpeed GC ladder empty progress carried active-window evidence.");
                if (progress.FixtureCompleted)
                    Fail("ActionSpeed GC ladder fixture completed before its first active workload unit.");
                return;
            }

            if (!progress.FirstUnitAtUtc.HasValue || !progress.LastUnitAtUtc.HasValue)
                Fail("ActionSpeed GC ladder active workload receipt omitted first/last timestamps.");
            DateTimeOffset first = progress.FirstUnitAtUtc!.Value;
            DateTimeOffset last = progress.LastUnitAtUtc!.Value;
            if (last < first || progress.ObservedAtUtc < last)
                Fail("ActionSpeed GC ladder active workload timestamps were not monotonic.");
            double timestampDuration = (last - first).TotalSeconds;
            if (Math.Abs(timestampDuration - progress.ActiveDurationSeconds) > 0.01d)
                Fail("ActionSpeed GC ladder active-duration receipt did not match its first/last timestamps.");
            if (progress.ActiveWindowSatisfied &&
                (progress.CompletedUnits < settings.Batch5GcLadderTargetUnits ||
                 progress.ActiveDurationSeconds + 0.001d < settings.Batch5GcLadderMeasureSeconds))
            {
                Fail("ActionSpeed GC ladder claimed an active window before satisfying its duration/unit contract.");
            }
            if (progress.RecoveryVerified != (progress.RecoveryUnits > 0))
                Fail("ActionSpeed GC ladder recovery boolean/unit receipts disagreed.");
            if (!settings.Batch5GcLadderLevel.Equals("L4", StringComparison.Ordinal) && progress.RecoveryUnits != 0)
                Fail("Only ActionSpeed L4 may publish native recovery units.");
            if (!progress.FixtureCompleted && !string.IsNullOrWhiteSpace(progress.BehaviorReceiptKind))
                Fail("ActionSpeed GC ladder published terminal behavior semantics before fixture completion.");
        }

        private void ValidateCompletedFixture(ActionSpeedGcLadderProgress progress)
        {
            if (!progress.ActiveWindowSatisfied)
                Fail("ActionSpeed GC ladder fixture completed without a satisfied active window.");
            if (progress.CompletedUnits < settings.Batch5GcLadderTargetUnits)
                Fail("ActionSpeed GC ladder fixture completed below its target unit count.");
            if (progress.ActiveDurationSeconds + 0.001d < settings.Batch5GcLadderMeasureSeconds)
                Fail("ActionSpeed GC ladder fixture completed before the full measurement duration elapsed.");

            bool requiresRecovery = settings.Batch5GcLadderLevel.Equals("L4", StringComparison.Ordinal);
            if (requiresRecovery && (!progress.RecoveryVerified || progress.RecoveryUnits < 1))
                Fail("ActionSpeed L4 completed without a native-control recovery unit.");
            if (!requiresRecovery && (progress.RecoveryVerified || progress.RecoveryUnits != 0))
                Fail("A non-L4 ActionSpeed stage published a recovery receipt.");

            string expectedBehavior = Batch5GcLadderBehavior.ExpectedActionSpeedReceiptKind(settings.Batch5GcLadderLevel);
            if (!string.Equals(progress.BehaviorReceiptKind, expectedBehavior, StringComparison.Ordinal))
                Fail("ActionSpeed GC ladder behavior receipt mismatch. expected=" + expectedBehavior + "; actual=" + progress.BehaviorReceiptKind + ".");
            Result.BehaviorReceiptKind = progress.BehaviorReceiptKind;
        }

        private void SynchronizeProgress(ActionSpeedGcLadderProgress progress)
        {
            Result.CompletedUnits = progress.CompletedUnits;
            Result.FirstUnitAtUtc = progress.FirstUnitAtUtc;
            Result.LastUnitAtUtc = progress.LastUnitAtUtc;
            Result.ActiveDurationSeconds = progress.ActiveDurationSeconds;
            Result.ActiveWindowSatisfied = progress.ActiveWindowSatisfied;
            Result.RecoveryVerified = progress.RecoveryVerified;
            Result.RecoveryUnits = progress.RecoveryUnits;
            if (!string.IsNullOrWhiteSpace(progress.BehaviorReceiptKind))
                Result.BehaviorReceiptKind = progress.BehaviorReceiptKind;
        }

        private void Fail(string failureReason)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (trend != null && !measurementCompleted)
            {
                trend.Complete(now);
                Result.RuntimeMemoryTrend = trend.Result;
                Result.MetricsAvailability = BuildAvailability(trend.Result);
            }
            Result.Status = "failed";
            Result.FailureReason = failureReason;
            Result.CompletedAtUtc = now;
            TerminalSucceeded = false;
            terminal = true;
            WriteResult();
            access.SetHookStatus("Smoke.Batch5GcLadder.Performance", "failed", "optional QA active-workload performance sampler", FormatDetails("failed") + "; failure=" + failureReason);
            throw new InvalidOperationException(failureReason);
        }

        private RuntimeMemoryDomainSnapshot CaptureDomainSnapshot()
        {
            DtmApiRuntime runtime = access.Runtime;
            InputOwnerSnapshot input = runtime.Input.GetOwnerSnapshot();
            EventHandlerCleanupSnapshot events = runtime.Events.GetHandlerCleanupSnapshot();
            RuntimeDemandSnapshot demand = runtime.RuntimeDemandSnapshot;
            return new RuntimeMemoryDomainSnapshot(
                runtime.RuntimeMemoryRecordCount,
                runtime.RuntimeMemoryOwnerRootCount,
                0,
                0,
                0,
                input.OwnerCount,
                input.ButtonCount,
                input.OwnerRegistrations,
                events.ActiveHandlers,
                events.DispatchableHandlers,
                events.QuarantinedHandlers,
                runtime.ModRegistry.TotalRootCount,
                runtime.RuntimeMemoryRecordCount,
                runtime.RuntimeMemoryResourceSnapshotBuildCount,
                runtime.Diagnostics.GetHookStatuses().Count,
                demand.DemandEntryCount,
                demand.TotalDemand);
        }

        private static Batch5GcMetricAvailability BuildAvailability(RuntimeMemoryTrendResult value)
        {
            return new Batch5GcMetricAvailability
            {
                Mono = Availability(value.MonoUsed.Start.HasValue || value.MonoHeap.Start.HasValue),
                Unity = Availability(value.UnityAllocated.Start.HasValue || value.UnityReserved.Start.HasValue),
                WindowsProcess = Availability(value.ProcessPrivate.Start.HasValue || value.ProcessWorkingSet.Start.HasValue),
                GcCollections = Availability(value.ProcessGen0Collections.Start.HasValue && value.ProcessGen2Collections.Start.HasValue),
                Owner = Availability(value.OwnerRootCount.Start.HasValue),
                Event = Availability(value.EventActiveHandlers.Start.HasValue),
                Input = Availability(value.InputOwnerRegistrations.Start.HasValue),
                Api = Availability(value.ApiRootCount.Start.HasValue),
                Resource = Availability(value.ResourceRecordCount.Start.HasValue),
                Hook = Availability(value.HookStatusCount.Start.HasValue),
                Demand = Availability(value.DemandEntryCount.Start.HasValue)
            };
        }

        private static string Availability(bool available) => available ? "available" : "unavailable";

        private string FormatDetails(string status) =>
            "domain=ActionSpeed; level=" + settings.Batch5GcLadderLevel +
            "; workload=" + settings.Batch5GcLadderWorkload +
            "; multiplier=" + settings.Batch5GcLadderMultiplier.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
            "; targetUnits=" + settings.Batch5GcLadderTargetUnits +
            "; measureSeconds=" + settings.Batch5GcLadderMeasureSeconds +
            "; completedUnits=" + Result.CompletedUnits +
            "; activeSeconds=" + Result.ActiveDurationSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
            "; activeWindow=" + Result.ActiveWindowSatisfied.ToString().ToLowerInvariant() +
            "; recoveryUnits=" + Result.RecoveryUnits +
            "; behavior=" + (string.IsNullOrWhiteSpace(Result.BehaviorReceiptKind) ? "pending" : Result.BehaviorReceiptKind) +
            "; titleCycle=" + Result.TitleCycleObserved.ToString().ToLowerInvariant() +
            "; status=" + status +
            "; forcedGc=false";

        private void WriteResult()
        {
            if (string.IsNullOrWhiteSpace(resultPath))
            {
                string root = Path.Combine(access.RuntimeEvidenceRoot, "BATCH5-GC-LADDER", access.RunId, "ActionSpeed", settings.Batch5GcLadderLevel, settings.Batch5GcLadderWorkload);
                Directory.CreateDirectory(root);
                resultPath = Path.Combine(root, "batch5-gc-ladder-stage.json");
            }
            using (var stream = new FileStream(resultPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var serializer = new DataContractJsonSerializer(typeof(Batch5GcLadderStageResult));
                serializer.WriteObject(stream, Result);
            }
        }
    }

    [DataContract]
    internal sealed class Batch5GcLadderStageResult
    {
        [DataMember] public int SchemaVersion { get; set; }
        [DataMember] public string RunId { get; set; } = string.Empty;
        [DataMember] public string Domain { get; set; } = string.Empty;
        [DataMember] public string Level { get; set; } = string.Empty;
        [DataMember] public string Workload { get; set; } = string.Empty;
        [DataMember] public double Multiplier { get; set; }
        [DataMember] public int MeasureSeconds { get; set; }
        [DataMember] public int SampleSeconds { get; set; }
        [DataMember] public int TargetUnits { get; set; }
        [DataMember] public int RequiredActiveDurationSeconds { get; set; }
        [DataMember] public int SaveSlot { get; set; }
        [DataMember] public bool ForcedGc { get; set; }
        [DataMember] public bool WorkloadCompleted { get; set; }
        [DataMember] public int CompletedUnits { get; set; }
        [DataMember] public DateTimeOffset? FirstUnitAtUtc { get; set; }
        [DataMember] public DateTimeOffset? LastUnitAtUtc { get; set; }
        [DataMember] public double ActiveDurationSeconds { get; set; }
        [DataMember] public bool ActiveWindowSatisfied { get; set; }
        [DataMember] public bool RecoveryVerified { get; set; }
        [DataMember] public int RecoveryUnits { get; set; }
        [DataMember] public string BehaviorReceiptKind { get; set; } = string.Empty;
        [DataMember] public bool BehaviorVerified { get; set; }
        [DataMember] public bool TitleCycleObserved { get; set; }
        [DataMember] public string Status { get; set; } = string.Empty;
        [DataMember] public string FailureReason { get; set; } = string.Empty;
        [DataMember] public DateTimeOffset StartedAtUtc { get; set; }
        [DataMember] public DateTimeOffset MeasurementCompletedAtUtc { get; set; }
        [DataMember] public DateTimeOffset CompletedAtUtc { get; set; }
        [DataMember] public Batch5GcMetricAvailability MetricsAvailability { get; set; } = new Batch5GcMetricAvailability();
        [DataMember] public RuntimeMemoryTrendResult RuntimeMemoryTrend { get; set; } = new RuntimeMemoryTrendResult();
    }

    [DataContract]
    internal sealed class Batch5GcMetricAvailability
    {
        [DataMember] public string Mono { get; set; } = "unavailable";
        [DataMember] public string Unity { get; set; } = "unavailable";
        [DataMember] public string WindowsProcess { get; set; } = "unavailable";
        [DataMember] public string GcCollections { get; set; } = "unavailable";
        [DataMember] public string Owner { get; set; } = "unavailable";
        [DataMember] public string Event { get; set; } = "unavailable";
        [DataMember] public string Input { get; set; } = "unavailable";
        [DataMember] public string Api { get; set; } = "unavailable";
        [DataMember] public string Resource { get; set; } = "unavailable";
        [DataMember] public string Hook { get; set; } = "unavailable";
        [DataMember] public string Demand { get; set; } = "unavailable";
    }
}
