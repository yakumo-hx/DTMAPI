#pragma warning disable CS0618 // Frozen camera ABI executor.
using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Single dormant-host backend shared by the frozen CameraView and
    /// CameraZoom surfaces. ProductNative Zoom never enters this object graph.
    /// </summary>
    internal sealed class CameraCompatibilityBackend
    {
        private readonly CameraDiagnosticsService diagnostics;
        private readonly CameraViewService view;
        private readonly CameraZoomCompatibilityService zoom;

        internal CameraCompatibilityBackend(
            DtmApiRuntime runtime,
            Func<bool>? managedProductOwnerPresent,
            Func<bool>? compatibilityOwnerClaim)
        {
            diagnostics = new CameraDiagnosticsService(runtime);
            view = managedProductOwnerPresent == null
                ? new CameraViewService(runtime, diagnostics)
                : new CameraViewService(
                    runtime,
                    diagnostics,
                    managedProductOwnerPresent,
                    compatibilityOwnerClaim);
            zoom = new CameraZoomCompatibilityService(runtime, view);
            view.ViewApplied += zoom.SynchronizeFromView;
            view.ConfigureManagedProductConflictCleanup(
                zoom.AbandonForManagedProductOwner);
        }

        internal bool HasEnvironmentResetDemand =>
            view.HasEnvironmentResetDemand;

        internal ICameraViewLease ViewAcquireLease(
            IManifest owner,
            CameraViewRequest request) =>
            view.AcquireLease(owner, request);

        internal CameraViewState ViewGetState(string uniqueId) =>
            view.GetState(uniqueId);

        internal CameraViewState ViewGetSnapshot(string uniqueId) =>
            view.GetSnapshot(uniqueId);

        internal BridgeFeatureStatus ViewGetStatus(string uniqueId) =>
            view.GetStatus(uniqueId);

        internal void ConfigureNativeAccessForTests(
            Func<double?>? reader,
            Func<double, bool>? writer) =>
            view.ConfigureNativeAccessForTests(
                reader,
                writer);

        internal CameraZoomRegisterResult ZoomRegister(
            IManifest owner,
            CameraZoomOptions options) =>
            zoom.Register(owner, options);

        internal CameraZoomResult ZoomSetViewScale(
            IManifest owner,
            double viewScale,
            string reason) =>
            zoom.SetViewScale(owner, viewScale, reason);

        internal CameraZoomResult ZoomStepViewScale(
            IManifest owner,
            int direction,
            string reason) =>
            zoom.StepViewScale(owner, direction, reason);

        internal CameraZoomResult ZoomResetViewScale(
            IManifest owner,
            string reason) =>
            zoom.ResetViewScale(owner, reason);

        internal CameraZoomState ZoomGetState(string uniqueId) =>
            zoom.GetState(uniqueId);

        internal CameraZoomState ZoomGetSnapshot(string uniqueId) =>
            zoom.GetSnapshot(uniqueId);

        internal BridgeFeatureStatus ZoomGetStatus(string uniqueId) =>
            zoom.GetStatus(uniqueId);

        internal void PublishHookStatuses() =>
            diagnostics.PublishHookStatuses();

        internal void SetEnvironmentLifecyclePatched(bool patched) =>
            view.SetCompatibilityHookReady(patched);

        internal void Update() =>
            view.RefreshForRuntime();

        internal void ResetForLifecycleBoundary(string reason) =>
            view.ResetForLifecycleBoundary(reason);

        internal void NotifyEnvironmentReset(string reason) =>
            view.NotifyEnvironmentReset(reason);

        internal int ReconcileManagedProductOwnerBeforeHookInstall() =>
            view.ReconcileManagedProductOwnerBeforeHookInstall();

        internal int RemoveOwner(string ownerId, string reason)
        {
            int removed = zoom.RemoveOwner(ownerId, reason);
            removed += view.RemoveOwner(ownerId, reason);
            return removed;
        }

        internal int CountOwnerResources(string ownerId) =>
            zoom.CountOwnerResources(ownerId) +
            view.CountOwnerResources(ownerId);
    }
}

#pragma warning restore CS0618
