using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CropHarvestingFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public CropHarvestingFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new CropHarvestingService(runtime);
        }

        public string Id => "CropHarvesting";

        internal CropHarvestingService Service { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<ICropHarvestingApi>(manifest, Service, OwnerBoundGameBridgeApis.ForCropHarvesting(Service));
        }

        public void PublishHookStatuses()
        {
            Service.PublishHookStatus();
        }

        public void Update()
        {
        }

        public void SaveLoaded(bool isNewGame)
        {
            Service.ResetRuntimeState("SaveLoaded");
        }

        public void ReturnedToTitle()
        {
            Service.ResetRuntimeState("ReturnedToTitle");
        }

    }
}
