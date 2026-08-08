using System;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class Batch5NoDemandRuntimeSnapshot
    {
        internal ulong RuntimeUpdateTicks { get; set; }
        internal int QaHostUpdateCount { get; set; }
        internal bool EventQueuePending { get; set; }
        internal long EventQueueDiagnosticRevision { get; set; }
        internal bool HookStatusQueuePending { get; set; }
        internal long HookStatusQueueDiagnosticRevision { get; set; }
        internal long EventArgsCreated { get; set; }
        internal long EventSnapshotRebuilds { get; set; }
        internal long EventZeroListenerBypasses { get; set; }
        internal long ActiveUpdaterMembershipSnapshotRebuilds { get; set; }
        internal long OptionalPerFeatureProjectionBuilds { get; set; }
        internal long OptionalNativeUpdaterInvocations { get; set; }
        internal long QaObserverUpdaterInvocations { get; set; }
        internal long MandatoryUpdaterInvocations { get; set; }
        internal long OptionalFeatureFileStatusCalls { get; set; }
        internal long OptionalDirectoryEnumerations { get; set; }
        internal long OptionalReflectionObjectSearches { get; set; }
        internal long OptionalRetainedCallbackWork { get; set; }
        internal long CustomAnimalsRetainedCallbackWork { get; set; }
        internal long AudioRetainedCallbackWork { get; set; }
        internal long OptionalHookInstallRequests { get; set; }
        internal long CameraEnvironmentResets { get; set; }
        internal int CustomAnimalDefinitionCandidateBuilds { get; set; }
        internal int ContentQueryCandidateBuilds { get; set; }
        internal long AudioPendingEntryVisits { get; set; }
        internal bool QaExplicitDemandActive { get; set; }
        internal bool QaUpdaterActive { get; set; }
        internal string[] ActiveOptionalDemandIds { get; set; } = Array.Empty<string>();
        internal string[] ActiveOptionalUpdaterIds { get; set; } = Array.Empty<string>();
        internal string[] ActiveMandatoryUpdaterIds { get; set; } = Array.Empty<string>();
        internal Batch5NoDemandMandatoryUpdaterSnapshot[] MandatoryUpdaterDispatches { get; set; } = Array.Empty<Batch5NoDemandMandatoryUpdaterSnapshot>();
    }

    internal sealed class Batch5NoDemandMandatoryUpdaterSnapshot
    {
        internal string CapabilityId { get; set; } = string.Empty;
        internal bool Active { get; set; }
        internal long DispatchCount { get; set; }
    }
}
