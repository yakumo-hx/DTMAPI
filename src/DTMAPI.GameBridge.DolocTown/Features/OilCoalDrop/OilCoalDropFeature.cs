using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class OilCoalDropFeature : IGameBridgeFeature
    {
        private readonly System.Func<bool> isToolColliderRouteReady;

        public OilCoalDropFeature(DtmApiRuntime runtime, System.Func<bool> isToolColliderRouteReady)
        {
            this.isToolColliderRouteReady = isToolColliderRouteReady;
            Service = new OilCoalDropService(runtime);
        }

        public string Id => "OilCoalDrop";

        internal OilCoalDropService Service { get; }

        public void RegisterApis(IManifest manifest)
        {
        }

        public void PublishHookStatuses()
        {
            Service.PublishHookStatuses(isToolColliderRouteReady());
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            Service.PublishHookStatuses(isToolColliderRouteReady());
        }

        public void Update()
        {
        }

        public void SaveLoaded(bool isNewGame)
        {
            Service.ClearPendingOilResourceHits("SaveLoaded");
        }

        public void ReturnedToTitle()
        {
            Service.ClearPendingOilResourceHits("ReturnedToTitle");
        }

        public void EnvironmentReset(string reason)
        {
            Service.ClearPendingOilResourceHits("EnvironmentReset:" + reason);
        }
    }
}
