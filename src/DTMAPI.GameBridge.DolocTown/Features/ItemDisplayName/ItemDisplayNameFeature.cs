using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Reads the current native item title for managed products.</summary>
    internal sealed class ItemDisplayNameFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        internal ItemDisplayNameFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new ItemDisplayNameService(runtime);
        }

        public string Id => "ItemDisplayName";
        internal ItemDisplayNameService Service { get; }

        public void RegisterApis(IManifest manifest) =>
            runtime.RegisterRuntimeApi<IItemDisplayNameApi>(manifest, Service, OwnerBoundGameBridgeApis.ForItemDisplayName(Service));

        public void PublishHookStatuses() { }
        public void Update() { }
        public void SaveLoaded(bool isNewGame) { }
        public void ReturnedToTitle() { }
    }
}
