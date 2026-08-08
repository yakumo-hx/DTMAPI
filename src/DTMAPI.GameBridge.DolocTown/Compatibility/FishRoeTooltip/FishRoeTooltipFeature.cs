#pragma warning disable CS0618 // This file registers the frozen IItemTooltipApi compatibility island.
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class FishRoeTooltipFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public FishRoeTooltipFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new FishRoeTooltipService(runtime);
            HookBridge = new FishRoeTooltipHookBridge(runtime, Service);
        }

        public string Id => "FishRoeTooltip";

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "FishRoeTooltip",
            requiresSave: true,
            allowsTitleScreen: false,
            requiresNativeScene: true,
            requiresUi: true,
            environmentResetSensitive: false,
            hasSaveLifetimeState: false,
            hasTitleLifetimeState: false,
            canAutoPauseAfterFailure: false);

        internal FishRoeTooltipService Service { get; }

        internal FishRoeTooltipHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IItemTooltipApi>(manifest, Service, OwnerBoundGameBridgeApis.ForItemTooltip(Service));
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
