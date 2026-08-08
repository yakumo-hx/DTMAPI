using System;
using System.Runtime.Serialization;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    [DataContract]
    internal sealed class Batch5NoDemandPerformanceReceipt
    {
        [DataMember] public int SchemaVersion { get; set; } = 1;
        [DataMember] public bool Passed { get; set; }
        [DataMember] public string FailureReason { get; set; } = string.Empty;
        [DataMember] public string FramePath { get; set; } = "Bootstrap Unity frame -> DolocTownGameBridge.Update -> ExplicitQa QaHost updater -> DtmApiRuntime.Update";
        [DataMember] public int WarmupFrameTarget { get; set; }
        [DataMember] public int WarmupFrameActual { get; set; }
        [DataMember] public int MeasurementFrameTarget { get; set; }
        [DataMember] public int MeasurementFrameActual { get; set; }
        [DataMember] public Batch5UnsignedCounterDelta CoreRuntimeUpdates { get; set; } = new Batch5UnsignedCounterDelta();
        [DataMember] public Batch5CounterDelta QaObserverUpdates { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta OptionalFeatureFileStatusCalls { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta OptionalDirectoryEnumerations { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta ActiveUpdaterMembershipSnapshotRebuilds { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta OptionalPerFeatureProjectionBuilds { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta OptionalReflectionObjectSearches { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta OptionalNativeUpdaterInvocations { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta OptionalRetainedCallbackWork { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta CustomAnimalsRetainedCallbackWork { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta AudioRetainedCallbackWork { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta OptionalHookInstallRequests { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta CameraEnvironmentResets { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta CustomAnimalDefinitionCandidateBuilds { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta ContentQueryCandidateBuilds { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta AudioPendingEntryVisits { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta MandatoryBaseUpdaterInvocations { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta QaUpdaterInvocations { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta EventQueueDiagnosticRevision { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta HookStatusQueueDiagnosticRevision { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta EventArgsCreated { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta EventSnapshotRebuilds { get; set; } = new Batch5CounterDelta();
        [DataMember] public Batch5CounterDelta EventZeroListenerBypasses { get; set; } = new Batch5CounterDelta();
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
        [DataMember] public Batch5MandatoryUpdaterCadenceReceipt[] MandatoryUpdaterCadence { get; set; } = Array.Empty<Batch5MandatoryUpdaterCadenceReceipt>();
    }

    [DataContract]
    internal sealed class Batch5MandatoryUpdaterCadenceReceipt
    {
        [DataMember] public string CapabilityId { get; set; } = string.Empty;
        [DataMember] public bool ActiveAtStart { get; set; }
        [DataMember] public bool ActiveAtEnd { get; set; }
        [DataMember] public Batch5CounterDelta Dispatches { get; set; } = new Batch5CounterDelta();
    }

    [DataContract]
    internal sealed class Batch5CounterDelta
    {
        internal Batch5CounterDelta()
        {
        }

        internal Batch5CounterDelta(long start, long end)
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
    internal sealed class Batch5UnsignedCounterDelta
    {
        internal Batch5UnsignedCounterDelta()
        {
        }

        internal Batch5UnsignedCounterDelta(ulong start, ulong end)
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
