using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>SharedNative read-only item-title adapter used by independent managed products.</summary>
    internal sealed class ItemDisplayNameFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        internal ItemDisplayNameFeature(DtmApiRuntime runtime, System.Func<bool> isEnvironmentResetHookReady)
        {
            this.runtime = runtime;
            Service = new ItemDisplayNameService(runtime, isEnvironmentResetHookReady);
        }

        public string Id => "ItemDisplayName";
        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "ItemDisplayName",
            requiresSave: false,
            allowsTitleScreen: true,
            requiresNativeScene: false,
            requiresUi: false,
            environmentResetSensitive: true,
            hasSaveLifetimeState: false,
            hasTitleLifetimeState: false,
            canAutoPauseAfterFailure: false);

        internal ItemDisplayNameService Service { get; }

        public void RegisterApis(IManifest manifest) =>
            runtime.RegisterRuntimeApi<IItemDisplayNameApi>(manifest, Service, OwnerBoundGameBridgeApis.ForItemDisplayName(Service));

        public void PublishHookStatuses() { }
        public void InstallHooks(HarmonyReflectionPatcher patcher) { }
        public void Update() { }
        public void SaveLoaded(bool isNewGame) => Service.Clear("SaveLoaded");
        public void ReturnedToTitle() => Service.Clear("ReturnedToTitle");
        public void EnvironmentReset(string reason) => Service.Clear("EnvironmentReset " + (reason ?? string.Empty));
    }
}
