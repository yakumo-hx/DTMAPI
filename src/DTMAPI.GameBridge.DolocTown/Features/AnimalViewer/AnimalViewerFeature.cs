using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class AnimalViewerFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public AnimalViewerFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new AnimalViewerService(runtime);
            HookBridge = new AnimalViewerHookBridge(runtime, Service);
        }

        public string Id => "AnimalViewer";

        internal AnimalViewerService Service { get; }

        internal AnimalViewerHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IAnimalViewerApi>(manifest, Service);
        }

        public void PublishHookStatuses()
        {
            HookBridge.PublishHookStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            HookBridge.InstallHooks(patcher);
        }

        public void Update()
        {
            Service.RefreshAnimalProgressOverlayTexts(force: false);
        }

        public void SaveLoaded(bool isNewGame)
        {
        }

        public void ReturnedToTitle()
        {
        }

        public void EnvironmentReset(string reason)
        {
        }
    }
}
