#pragma warning disable CS0618 // This feature registers the frozen IChestLocatorEnhancerApi compatibility shell.
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class ChestLocatorEnhancerFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public ChestLocatorEnhancerFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new ChestLocatorEnhancerService(runtime);
            HookBridge = new ChestLocatorEnhancerHookBridge(runtime, Service);
        }

        public string Id => "ChestLocatorEnhancer";

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "ChestLocatorEnhancer",
            requiresSave: true,
            allowsTitleScreen: false,
            requiresNativeScene: true,
            requiresUi: false,
            environmentResetSensitive: false,
            hasSaveLifetimeState: false,
            hasTitleLifetimeState: false,
            canAutoPauseAfterFailure: false);

        internal ChestLocatorEnhancerService Service { get; }

        internal ChestLocatorEnhancerHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IChestLocatorEnhancerApi>(manifest, Service, OwnerBoundGameBridgeApis.ForChestLocatorEnhancer(Service));
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
