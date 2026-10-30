using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    /// <summary>Captures generic Core resource state for optional QA consumers.</summary>
    internal static class QaRuntimeMemorySnapshotCapture
    {
        internal static int GetResourceSnapshotBuildCount(GameBridgeFixtureAccess access) =>
            access.Runtime.RuntimeMemoryResourceSnapshotBuildCount;

        internal static RuntimeMemoryDomainSnapshot Capture(
            GameBridgeFixtureAccess access,
            int domainAccessorBuilds,
            long domainWorkUnits,
            int domainNativeTransient)
        {
            DtmApiRuntime runtime = access.Runtime;
            InputOwnerSnapshot input = runtime.Input.GetOwnerSnapshot();
            EventHandlerCleanupSnapshot events = runtime.Events.GetHandlerCleanupSnapshot();
            RuntimeDemandSnapshot demand = runtime.RuntimeDemandSnapshot;
            return new RuntimeMemoryDomainSnapshot(
                runtime.RuntimeMemoryRecordCount,
                runtime.RuntimeMemoryOwnerRootCount,
                domainAccessorBuilds,
                domainWorkUnits,
                domainNativeTransient,
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
    }
}
