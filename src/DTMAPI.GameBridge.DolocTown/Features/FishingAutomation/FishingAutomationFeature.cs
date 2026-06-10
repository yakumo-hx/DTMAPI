using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class FishingAutomationFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public FishingAutomationFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new FishingAutomationService(runtime);
            HookBridge = new FishingAutomationHookBridge(runtime, Service);
        }

        public string Id => "FishingAutomation";

        internal FishingAutomationService Service { get; }

        internal FishingAutomationHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IFishingAutomationApi>(manifest, Service);
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
            Service.Update();
        }

        public void SaveLoaded(bool isNewGame)
        {
            Service.ResetFishingRuntimeState("SaveLoaded");
        }

        public void ReturnedToTitle()
        {
            Service.ResetFishingRuntimeState("ReturnedToTitle");
        }

        public void EnvironmentReset(string reason)
        {
            Service.ResetFishingRuntimeState(reason);
        }
    }
}
