using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class Batch5NoDemandProfileFixture
    {
        private readonly GameBridgeFixtureAccess access;
        private readonly int saveSlot;
        private readonly int warmupFrameTarget;
        private readonly int measurementFrameTarget;
        private Batch5NoDemandRuntimeSnapshot? measurementStart;
        private Batch5NoDemandProfileResult? result;
        private string resultPath = string.Empty;
        private int warmupFrames;
        private int measurementFrames;
        private bool terminal;

        internal Batch5NoDemandProfileFixture(
            GameBridgeFixtureAccess access,
            int saveSlot,
            int warmupFrameTarget,
            int measurementFrameTarget)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            this.saveSlot = saveSlot;
            this.warmupFrameTarget = warmupFrameTarget;
            this.measurementFrameTarget = measurementFrameTarget;
        }

        internal bool MeasurementCompleted => result != null;

        internal bool ShouldRequestReturnHome => result != null && !terminal;

        internal bool TerminalSucceeded { get; private set; }

        internal void Update()
        {
            if (terminal || result != null)
                return;

            if (measurementStart == null)
            {
                if (warmupFrames < warmupFrameTarget)
                {
                    warmupFrames++;
                    return;
                }

                measurementStart = access.Bridge.CaptureBatch5NoDemandRuntimeSnapshot();
                return;
            }

            measurementFrames++;
            if (measurementFrames < measurementFrameTarget)
                return;

            Batch5NoDemandRuntimeSnapshot end = access.Bridge.CaptureBatch5NoDemandRuntimeSnapshot();
            result = BuildResult(measurementStart, end);
            result.Status = result.NoDemandProfile.Passed
                ? "awaiting-title-cleanup"
                : "failed-batch5-no-demand-profile";
            result.FailureReason = result.NoDemandProfile.FailureReason;
            WriteResult();
            access.SetHookStatus(
                "Smoke.Batch5NoDemandProfile",
                result.NoDemandProfile.Passed ? "pending" : "failed",
                "QA-owned generic warmed no-optional-demand fixture",
                "result=" + resultPath + "; measuredFrames=" + measurementFrames.ToString(CultureInfo.InvariantCulture) +
                "; titleCleanup=false; owner=qa; productCounterSynthesized=false");
        }

        internal bool CompleteTitleCleanup()
        {
            if (terminal)
                return TerminalSucceeded;
            if (result == null)
                throw new InvalidOperationException("Batch 5 no-demand title cleanup arrived before measurement completed.");

            result.TitleCleanupVerified = true;
            result.Status = result.NoDemandProfile.Passed ? "completed" : "failed-batch5-no-demand-profile";
            WriteResult();
            TerminalSucceeded = result.NoDemandProfile.Passed;
            terminal = true;
            access.SetHookStatus(
                "Smoke.Batch5NoDemandProfile",
                TerminalSucceeded ? "verified" : "failed",
                "QA-owned generic warmed no-optional-demand fixture",
                "result=" + resultPath + "; measuredFrames=" + measurementFrames.ToString(CultureInfo.InvariantCulture) +
                "; titleCleanup=true; owner=qa; productCounterSynthesized=false");
            return TerminalSucceeded;
        }

        internal void Close(string reason)
        {
            if (terminal)
            {
                if (!TerminalSucceeded)
                    throw new InvalidOperationException("Batch 5 no-demand profile reached a failed terminal state.");
                return;
            }

            if (result == null)
            {
                result = CreateBaseResult();
                result.NoDemandProfile.FailureReason = "QA host closed before the warmed no-demand measurement completed.";
                result.NoDemandProfile.Passed = false;
            }
            result.Status = "failed-host-closed-before-title-cleanup";
            result.FailureReason = result.NoDemandProfile.FailureReason +
                " reason=" + (reason ?? string.Empty) + ".";
            result.TitleCleanupVerified = false;
            WriteResult();
            terminal = true;
            TerminalSucceeded = false;
            throw new InvalidOperationException("Batch 5 no-demand fixture closed without a durable successful terminal result.");
        }

        private Batch5NoDemandProfileResult BuildResult(
            Batch5NoDemandRuntimeSnapshot start,
            Batch5NoDemandRuntimeSnapshot end)
        {
            Batch5NoDemandProfileResult current = CreateBaseResult();
            CurrentBatch5NoDemandReceipt receipt = current.NoDemandProfile;
            receipt.WarmupFrameActual = warmupFrames;
            receipt.MeasurementFrameActual = measurementFrames;
            receipt.CoreRuntimeUpdates = new CurrentBatch5UnsignedCounterDelta(start.RuntimeUpdateTicks, end.RuntimeUpdateTicks);
            receipt.QaObserverUpdates = Delta(start.QaHostUpdateCount, end.QaHostUpdateCount);
            receipt.OptionalFeatureFileStatusCalls = Delta(start.OptionalFeatureFileStatusCalls, end.OptionalFeatureFileStatusCalls);
            receipt.OptionalDirectoryEnumerations = Delta(start.OptionalDirectoryEnumerations, end.OptionalDirectoryEnumerations);
            receipt.ActiveUpdaterMembershipSnapshotRebuilds = Delta(start.ActiveUpdaterMembershipSnapshotRebuilds, end.ActiveUpdaterMembershipSnapshotRebuilds);
            receipt.OptionalPerFeatureProjectionBuilds = Delta(start.OptionalPerFeatureProjectionBuilds, end.OptionalPerFeatureProjectionBuilds);
            receipt.OptionalReflectionObjectSearches = Delta(start.OptionalReflectionObjectSearches, end.OptionalReflectionObjectSearches);
            receipt.OptionalNativeUpdaterInvocations = Delta(start.OptionalNativeUpdaterInvocations, end.OptionalNativeUpdaterInvocations);
            receipt.OptionalRetainedCallbackWork = Delta(start.OptionalRetainedCallbackWork, end.OptionalRetainedCallbackWork);
            receipt.CustomAnimalsRetainedCallbackWork = Delta(start.CustomAnimalsRetainedCallbackWork, end.CustomAnimalsRetainedCallbackWork);
            receipt.AudioRetainedCallbackWork = Delta(start.AudioRetainedCallbackWork, end.AudioRetainedCallbackWork);
            receipt.OptionalHookInstallRequests = Delta(start.OptionalHookInstallRequests, end.OptionalHookInstallRequests);
            receipt.CameraEnvironmentResets = Delta(start.CameraEnvironmentResets, end.CameraEnvironmentResets);
            receipt.CustomAnimalDefinitionCandidateBuilds = Delta(start.CustomAnimalDefinitionCandidateBuilds, end.CustomAnimalDefinitionCandidateBuilds);
            receipt.ContentQueryCandidateBuilds = Delta(start.ContentQueryCandidateBuilds, end.ContentQueryCandidateBuilds);
            receipt.AudioPendingEntryVisits = Delta(start.AudioPendingEntryVisits, end.AudioPendingEntryVisits);
            receipt.MandatoryBaseUpdaterInvocations = Delta(start.MandatoryUpdaterInvocations, end.MandatoryUpdaterInvocations);
            receipt.QaUpdaterInvocations = Delta(start.QaObserverUpdaterInvocations, end.QaObserverUpdaterInvocations);
            receipt.EventQueueDiagnosticRevision = Delta(start.EventQueueDiagnosticRevision, end.EventQueueDiagnosticRevision);
            receipt.HookStatusQueueDiagnosticRevision = Delta(start.HookStatusQueueDiagnosticRevision, end.HookStatusQueueDiagnosticRevision);
            receipt.EventArgsCreated = Delta(start.EventArgsCreated, end.EventArgsCreated);
            receipt.EventSnapshotRebuilds = Delta(start.EventSnapshotRebuilds, end.EventSnapshotRebuilds);
            receipt.EventZeroListenerBypasses = Delta(start.EventZeroListenerBypasses, end.EventZeroListenerBypasses);
            receipt.EventQueuePendingAtStart = start.EventQueuePending;
            receipt.EventQueuePendingAtEnd = end.EventQueuePending;
            receipt.HookStatusQueuePendingAtStart = start.HookStatusQueuePending;
            receipt.HookStatusQueuePendingAtEnd = end.HookStatusQueuePending;
            receipt.QaExplicitDemandActiveAtStart = start.QaExplicitDemandActive;
            receipt.QaExplicitDemandActiveAtEnd = end.QaExplicitDemandActive;
            receipt.QaUpdaterActiveAtStart = start.QaUpdaterActive;
            receipt.QaUpdaterActiveAtEnd = end.QaUpdaterActive;
            receipt.ActiveOptionalDemandIdsAtStart = start.ActiveOptionalDemandIds;
            receipt.ActiveOptionalDemandIdsAtEnd = end.ActiveOptionalDemandIds;
            receipt.ActiveOptionalUpdaterIdsAtStart = start.ActiveOptionalUpdaterIds;
            receipt.ActiveOptionalUpdaterIdsAtEnd = end.ActiveOptionalUpdaterIds;
            receipt.ActiveMandatoryUpdaterIdsAtStart = start.ActiveMandatoryUpdaterIds;
            receipt.ActiveMandatoryUpdaterIdsAtEnd = end.ActiveMandatoryUpdaterIds;
            receipt.MandatoryUpdaterCadence = BuildMandatoryUpdaterCadence(start, end);

            var failures = new List<string>();
            if (receipt.WarmupFrameActual != receipt.WarmupFrameTarget)
                failures.Add("warmup frames actual != target");
            if (receipt.MeasurementFrameActual != receipt.MeasurementFrameTarget)
                failures.Add("measurement frames actual != target");
            if (!receipt.CoreRuntimeUpdates.Monotonic ||
                receipt.CoreRuntimeUpdates.Delta != (ulong)receipt.MeasurementFrameTarget)
            {
                failures.Add("Core DtmApiRuntime.Update delta != measurement target");
            }
            if (receipt.QaObserverUpdates.Delta != receipt.MeasurementFrameTarget ||
                receipt.QaUpdaterInvocations.Delta != receipt.MeasurementFrameTarget)
            {
                failures.Add("ExplicitQa observer/update route delta != measurement target");
            }
            RequireZero(failures, "optional feature file-status calls", receipt.OptionalFeatureFileStatusCalls);
            RequireZero(failures, "optional directory enumeration", receipt.OptionalDirectoryEnumerations);
            RequireZero(failures, "active updater membership snapshot rebuild", receipt.ActiveUpdaterMembershipSnapshotRebuilds);
            RequireZero(failures, "optional per-feature projection build", receipt.OptionalPerFeatureProjectionBuilds);
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
            if (receipt.EventQueuePendingAtStart || receipt.EventQueuePendingAtEnd ||
                receipt.HookStatusQueuePendingAtStart || receipt.HookStatusQueuePendingAtEnd)
            {
                failures.Add("Core event/Hook-status queue was pending at a measurement boundary");
            }
            if (!receipt.QaExplicitDemandActiveAtStart || !receipt.QaExplicitDemandActiveAtEnd ||
                !receipt.QaUpdaterActiveAtStart || !receipt.QaUpdaterActiveAtEnd)
            {
                failures.Add("QA observer was not bound to ExplicitQa demand/updater at both boundaries");
            }
            if (receipt.ActiveOptionalDemandIdsAtStart.Length != 0 || receipt.ActiveOptionalDemandIdsAtEnd.Length != 0)
                failures.Add("an optional product demand was active");
            if (receipt.ActiveOptionalUpdaterIdsAtStart.Length != 0 || receipt.ActiveOptionalUpdaterIdsAtEnd.Length != 0)
                failures.Add("an optional product updater was active");
            ValidateMandatoryCadence(failures, receipt);
            receipt.Passed = failures.Count == 0;
            receipt.FailureReason = string.Join("; ", failures.ToArray());
            return current;
        }

        private Batch5NoDemandProfileResult CreateBaseResult() => new Batch5NoDemandProfileResult
        {
            QaHostRunId = access.RunId,
            SaveSlot = saveSlot,
            TargetFrames = measurementFrameTarget,
            WarmupFrames = warmupFrameTarget,
            WarmupFramesActual = warmupFrames,
            MeasuredFrames = measurementFrames,
            NoDemandProfile = new CurrentBatch5NoDemandReceipt
            {
                WarmupFrameTarget = warmupFrameTarget,
                WarmupFrameActual = warmupFrames,
                MeasurementFrameTarget = measurementFrameTarget,
                MeasurementFrameActual = measurementFrames
            }
        };

        private static CurrentBatch5CounterDelta Delta(long start, long end) =>
            new CurrentBatch5CounterDelta(start, end);

        private static CurrentBatch5MandatoryUpdaterCadence[] BuildMandatoryUpdaterCadence(
            Batch5NoDemandRuntimeSnapshot start,
            Batch5NoDemandRuntimeSnapshot end)
        {
            var byId = new SortedDictionary<string, CurrentBatch5MandatoryUpdaterCadence>(StringComparer.OrdinalIgnoreCase);
            foreach (Batch5NoDemandMandatoryUpdaterSnapshot item in start.MandatoryUpdaterDispatches)
            {
                byId[item.CapabilityId] = new CurrentBatch5MandatoryUpdaterCadence
                {
                    CapabilityId = item.CapabilityId,
                    ActiveAtStart = item.Active,
                    Dispatches = Delta(item.DispatchCount, item.DispatchCount)
                };
            }
            foreach (Batch5NoDemandMandatoryUpdaterSnapshot item in end.MandatoryUpdaterDispatches)
            {
                if (!byId.TryGetValue(item.CapabilityId, out CurrentBatch5MandatoryUpdaterCadence? cadence))
                {
                    cadence = new CurrentBatch5MandatoryUpdaterCadence
                    {
                        CapabilityId = item.CapabilityId,
                        Dispatches = Delta(0, item.DispatchCount)
                    };
                    byId[item.CapabilityId] = cadence;
                }
                else
                {
                    cadence.Dispatches = Delta(cadence.Dispatches.Start, item.DispatchCount);
                }
                cadence.ActiveAtEnd = item.Active;
            }
            var result = new CurrentBatch5MandatoryUpdaterCadence[byId.Count];
            byId.Values.CopyTo(result, 0);
            return result;
        }

        private static void ValidateMandatoryCadence(
            List<string> failures,
            CurrentBatch5NoDemandReceipt receipt)
        {
            if (receipt.ActiveMandatoryUpdaterIdsAtStart.Length == 0 ||
                receipt.ActiveMandatoryUpdaterIdsAtEnd.Length == 0 ||
                receipt.MandatoryBaseUpdaterInvocations.Delta <= 0)
            {
                failures.Add("mandatory base cadence was not independently observed");
            }
            long composed = 0;
            foreach (CurrentBatch5MandatoryUpdaterCadence cadence in receipt.MandatoryUpdaterCadence)
            {
                composed += cadence.Dispatches.Delta;
                if (cadence.Dispatches.Delta < 0)
                    failures.Add("mandatory updater counter regressed capability=" + cadence.CapabilityId);
            }
            if (composed != receipt.MandatoryBaseUpdaterInvocations.Delta)
                failures.Add("mandatory updater per-capability deltas do not compose to aggregate");
            RequireMandatoryCadence(failures, receipt, "GameBridge.CoreUiContext", exactTarget: true);
            RequireMandatoryCadence(failures, receipt, "GameBridge.ContentRefreshDrain", exactTarget: true);
            RequireMandatoryCadence(failures, receipt, "NativeUiLayoutDiagnostics", exactTarget: false);
        }

        private static void RequireMandatoryCadence(
            List<string> failures,
            CurrentBatch5NoDemandReceipt receipt,
            string capabilityId,
            bool exactTarget)
        {
            CurrentBatch5MandatoryUpdaterCadence? found = null;
            foreach (CurrentBatch5MandatoryUpdaterCadence cadence in receipt.MandatoryUpdaterCadence)
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
            if (exactTarget && found.Dispatches.Delta != receipt.MeasurementFrameTarget)
                failures.Add("mandatory every-frame updater delta != measurement target capability=" + capabilityId);
            if (!exactTarget && (found.Dispatches.Delta <= 0 || found.Dispatches.Delta > receipt.MeasurementFrameTarget))
                failures.Add("mandatory bounded-cadence updater delta outside (0, target] capability=" + capabilityId);
        }

        private static void RequireZero(
            List<string> failures,
            string label,
            CurrentBatch5CounterDelta counter)
        {
            if (counter.Delta != 0)
                failures.Add(label + " delta=" + counter.Delta.ToString(CultureInfo.InvariantCulture));
        }

        private void WriteResult()
        {
            if (result == null)
                throw new InvalidOperationException("Batch 5 no-demand result is unavailable.");
            if (string.IsNullOrWhiteSpace(resultPath))
            {
                string directoryName = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) +
                    "-" + access.RunId.Substring(0, Math.Min(8, access.RunId.Length));
                string directory = Path.Combine(access.RuntimeEvidenceRoot, "BATCH5-NO-DEMAND", directoryName);
                Directory.CreateDirectory(directory);
                resultPath = Path.Combine(directory, "batch5-no-demand-profile.json");
            }
            using (FileStream stream = File.Create(resultPath))
                new DataContractJsonSerializer(typeof(Batch5NoDemandProfileResult)).WriteObject(stream, result);
        }
    }

    [DataContract]
    internal sealed class Batch5NoDemandProfileResult
    {
        [DataMember] public int SchemaVersion { get; set; } = 4;
        [DataMember] public string QaHostRunId { get; set; } = string.Empty;
        [DataMember] public string Domain { get; set; } = "Batch5NoDemand";
        [DataMember] public string Workload { get; set; } = "WarmedNoOptionalDemand";
        [DataMember] public int SaveSlot { get; set; }
        [DataMember] public bool ForcedGc { get; set; }
        [DataMember] public string Profile { get; set; } = "InactiveNoConsumer";
        [DataMember] public string Status { get; set; } = "warming";
        [DataMember] public string FailureReason { get; set; } = string.Empty;
        [DataMember] public bool TitleCleanupVerified { get; set; }
        [DataMember] public int TargetFrames { get; set; }
        [DataMember] public int WarmupFrames { get; set; }
        [DataMember] public int WarmupFramesActual { get; set; }
        [DataMember] public int MeasuredFrames { get; set; }
        [DataMember] public bool AllocationCounterAvailable { get; set; }
        [DataMember] public bool AllocationCounterFunctional { get; set; }
        [DataMember] public string AllocationProbeStatus { get; set; } = "not-measured-in-live-current-profile";
        [DataMember] public long? AllocatedBytes { get; set; }
        [DataMember] public CurrentBatch5NoDemandReceipt NoDemandProfile { get; set; } = new CurrentBatch5NoDemandReceipt();
    }

    [DataContract]
    internal sealed class CurrentBatch5NoDemandReceipt
    {
        [DataMember] public int SchemaVersion { get; set; } = 2;
        [DataMember] public bool Passed { get; set; }
        [DataMember] public string FailureReason { get; set; } = string.Empty;
        [DataMember] public string FramePath { get; set; } = "Bootstrap Unity frame -> DolocTownGameBridge.Update -> ExplicitQa QaHost updater -> DtmApiRuntime.Update";
        [DataMember] public int WarmupFrameTarget { get; set; }
        [DataMember] public int WarmupFrameActual { get; set; }
        [DataMember] public int MeasurementFrameTarget { get; set; }
        [DataMember] public int MeasurementFrameActual { get; set; }
        [DataMember] public CurrentBatch5UnsignedCounterDelta CoreRuntimeUpdates { get; set; } = new CurrentBatch5UnsignedCounterDelta();
        [DataMember] public CurrentBatch5CounterDelta QaObserverUpdates { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta OptionalFeatureFileStatusCalls { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta OptionalDirectoryEnumerations { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta ActiveUpdaterMembershipSnapshotRebuilds { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta OptionalPerFeatureProjectionBuilds { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta OptionalReflectionObjectSearches { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta OptionalNativeUpdaterInvocations { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta OptionalRetainedCallbackWork { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta CustomAnimalsRetainedCallbackWork { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta AudioRetainedCallbackWork { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta OptionalHookInstallRequests { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta CameraEnvironmentResets { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta CustomAnimalDefinitionCandidateBuilds { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta ContentQueryCandidateBuilds { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta AudioPendingEntryVisits { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta MandatoryBaseUpdaterInvocations { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta QaUpdaterInvocations { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta EventQueueDiagnosticRevision { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta HookStatusQueueDiagnosticRevision { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta EventArgsCreated { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta EventSnapshotRebuilds { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public CurrentBatch5CounterDelta EventZeroListenerBypasses { get; set; } = new CurrentBatch5CounterDelta();
        [DataMember] public bool EventQueuePendingAtStart { get; set; }
        [DataMember] public bool EventQueuePendingAtEnd { get; set; }
        [DataMember] public bool HookStatusQueuePendingAtStart { get; set; }
        [DataMember] public bool HookStatusQueuePendingAtEnd { get; set; }
        [DataMember] public bool QaExplicitDemandActiveAtStart { get; set; }
        [DataMember] public bool QaExplicitDemandActiveAtEnd { get; set; }
        [DataMember] public bool QaUpdaterActiveAtStart { get; set; }
        [DataMember] public bool QaUpdaterActiveAtEnd { get; set; }
        [DataMember] public string[] ActiveOptionalDemandIdsAtStart { get; set; } = Array.Empty<string>();
        [DataMember] public string[] ActiveOptionalDemandIdsAtEnd { get; set; } = Array.Empty<string>();
        [DataMember] public string[] ActiveOptionalUpdaterIdsAtStart { get; set; } = Array.Empty<string>();
        [DataMember] public string[] ActiveOptionalUpdaterIdsAtEnd { get; set; } = Array.Empty<string>();
        [DataMember] public string[] ActiveMandatoryUpdaterIdsAtStart { get; set; } = Array.Empty<string>();
        [DataMember] public string[] ActiveMandatoryUpdaterIdsAtEnd { get; set; } = Array.Empty<string>();
        [DataMember] public CurrentBatch5MandatoryUpdaterCadence[] MandatoryUpdaterCadence { get; set; } = Array.Empty<CurrentBatch5MandatoryUpdaterCadence>();
    }

    [DataContract]
    internal sealed class CurrentBatch5MandatoryUpdaterCadence
    {
        [DataMember] public string CapabilityId { get; set; } = string.Empty;
        [DataMember] public bool ActiveAtStart { get; set; }
        [DataMember] public bool ActiveAtEnd { get; set; }
        [DataMember] public CurrentBatch5CounterDelta Dispatches { get; set; } = new CurrentBatch5CounterDelta();
    }

    [DataContract]
    internal sealed class CurrentBatch5CounterDelta
    {
        internal CurrentBatch5CounterDelta()
        {
        }

        internal CurrentBatch5CounterDelta(long start, long end)
        {
            Start = start;
            End = end;
            Delta = end - start;
        }

        [DataMember] public long Start { get; set; }
        [DataMember] public long End { get; set; }
        [DataMember] public long Delta { get; set; }
    }

    [DataContract]
    internal sealed class CurrentBatch5UnsignedCounterDelta
    {
        internal CurrentBatch5UnsignedCounterDelta()
        {
        }

        internal CurrentBatch5UnsignedCounterDelta(ulong start, ulong end)
        {
            Start = start;
            End = end;
            Delta = end >= start ? end - start : 0;
            Monotonic = end >= start;
        }

        [DataMember] public ulong Start { get; set; }
        [DataMember] public ulong End { get; set; }
        [DataMember] public ulong Delta { get; set; }
        [DataMember] public bool Monotonic { get; set; }
    }
}
