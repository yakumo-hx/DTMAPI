using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

#pragma warning disable CS0618

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CameraFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;
        private readonly CameraViewService viewService;
        private readonly CameraZoomCompatibilityService zoomCompatibilityService;

        public CameraFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            viewService = new CameraViewService(
                runtime,
                CameraCompatibilityHookBridge
                    .IsManagedProductOwnerPresent);
            zoomCompatibilityService =
                new CameraZoomCompatibilityService(
                    runtime,
                    CameraCompatibilityHookBridge
                        .IsManagedProductOwnerPresent);
            HookBridge =
                new CameraCompatibilityHookBridge(
                    runtime,
                    viewService);
            viewService.ConfigureCompatibilityOwnerClaim(
                HookBridge.EnsureCompatibilityOwnerClaim);
            zoomCompatibilityService.ConfigureCompatibilityOwnerClaim(
                HookBridge.EnsureCompatibilityOwnerClaim);
        }

        public ICameraViewApi ViewApi => viewService;

        public ICameraZoomApi ZoomApi => zoomCompatibilityService;

        public string Id => "Camera";

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "Camera",
            requiresSave: true,
            allowsTitleScreen: false,
            requiresNativeScene: true,
            requiresUi: false,
            environmentResetSensitive: true,
            hasSaveLifetimeState: true,
            hasTitleLifetimeState: false,
            canAutoPauseAfterFailure: false);

        internal bool HasEnvironmentResetDemand => viewService.HasEnvironmentResetDemand;

        internal CameraCompatibilityHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<ICameraViewApi>(manifest, viewService, OwnerBoundGameBridgeApis.ForCameraView(viewService));
            runtime.RegisterRuntimeApi<ICameraZoomApi>(manifest, zoomCompatibilityService, OwnerBoundGameBridgeApis.ForCameraZoom(zoomCompatibilityService));
        }

        public void PublishHookStatuses()
        {
            HookBridge.PublishHookStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            HookBridge.InstallHooks();
        }

        internal void ReconcileManagedProductOwnerBeforeHookInstall() =>
            viewService.ReconcileManagedProductOwnerBeforeHookInstall();

        public void Update()
        {
            viewService.RefreshForRuntime();
        }

        public void SaveLoaded(bool isNewGame)
        {
            viewService.ResetForLifecycleBoundary("SaveLoaded");
        }

        public void ReturnedToTitle()
        {
            viewService.ResetForLifecycleBoundary("ReturnedToTitle");
        }

        public void EnvironmentReset(string reason)
        {
            viewService.NotifyEnvironmentReset(reason);
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            int removed = viewService.RemoveOwner(ownerId, reason);
            if (!viewService.HasEnvironmentResetDemand)
                HookBridge.RemoveOwnedHook();
            return removed;
        }

        internal int CountOwnerResources(string ownerId) =>
            viewService.CountOwnerResources(ownerId);
    }
}

#pragma warning restore CS0618
