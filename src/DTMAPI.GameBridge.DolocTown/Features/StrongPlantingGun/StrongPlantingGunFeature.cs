using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class StrongPlantingGunFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public StrongPlantingGunFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new StrongPlantingGunService(runtime);
            HookBridge = new StrongPlantingGunHookBridge(runtime, Service);
        }

        public string Id => "StrongPlantingGun";

        internal StrongPlantingGunService Service { get; }

        internal StrongPlantingGunHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IStrongPlantingGunApi>(manifest, Service);
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
