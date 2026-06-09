using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class ActionSpeedFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public ActionSpeedFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new ActionSpeedService(runtime);
            HookBridge = new ActionSpeedHookBridge(runtime, Service);
        }

        public string Id => "ActionSpeed";

        internal ActionSpeedService Service { get; }

        internal ActionSpeedHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IActionSpeedApi>(manifest, Service);
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
            Service.RestoreActionSpeed("SaveLoaded");
        }

        public void ReturnedToTitle()
        {
            Service.RestoreActionSpeed("ReturnedToTitle");
        }

        public void EnvironmentReset(string reason)
        {
        }
    }
}
