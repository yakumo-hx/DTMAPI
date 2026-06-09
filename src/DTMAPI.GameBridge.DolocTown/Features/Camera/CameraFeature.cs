using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

#pragma warning disable CS0618

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CameraFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;
        private readonly CameraDiagnosticsService diagnostics;
        private readonly CameraViewService viewService;
        private readonly CameraZoomCompatibilityService zoomCompatibilityService;
        private bool cameraViewSetEnvCameraPatched;

        public CameraFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            diagnostics = new CameraDiagnosticsService(runtime);
            viewService = new CameraViewService(runtime, diagnostics);
            zoomCompatibilityService = new CameraZoomCompatibilityService(runtime, viewService);
            viewService.ViewApplied += zoomCompatibilityService.SynchronizeFromView;
        }

        public ICameraViewApi ViewApi => viewService;

        public ICameraZoomApi ZoomApi => zoomCompatibilityService;

        public string Id => "Camera";

        internal bool CameraViewSetEnvCameraPatched => cameraViewSetEnvCameraPatched;

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<ICameraViewApi>(manifest, viewService);
            runtime.RegisterRuntimeApi<ICameraZoomApi>(manifest, zoomCompatibilityService);
        }

        public void PublishHookStatuses()
        {
            diagnostics.PublishHookStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (cameraViewSetEnvCameraPatched)
                return;

            cameraViewSetEnvCameraPatched = patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "SetEnvCamera", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.DolocApiSetEnvCameraPostfix), BindingFlags.Public | BindingFlags.Static), 5);
            diagnostics.SetEnvironmentLifecyclePatched(cameraViewSetEnvCameraPatched);
        }

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
    }
}

#pragma warning restore CS0618
