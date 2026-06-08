using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

#pragma warning disable CS0618

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CameraFeature
    {
        private readonly DtmApiRuntime runtime;
        private readonly CameraDiagnosticsService diagnostics;
        private readonly CameraViewService viewService;
        private readonly CameraZoomCompatibilityService zoomCompatibilityService;

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

        public void RegisterRuntimeApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<ICameraViewApi>(manifest, viewService);
            runtime.RegisterRuntimeApi<ICameraZoomApi>(manifest, zoomCompatibilityService);
        }

        public void PublishHookStatuses()
        {
            diagnostics.PublishHookStatuses();
        }

        public void RefreshForRuntime()
        {
            viewService.RefreshForRuntime();
        }

        public void ResetForLifecycleBoundary(string reason)
        {
            viewService.ResetForLifecycleBoundary(reason);
        }

        public void NotifyEnvironmentReset(string reason)
        {
            viewService.NotifyEnvironmentReset(reason);
        }
    }
}

#pragma warning restore CS0618
