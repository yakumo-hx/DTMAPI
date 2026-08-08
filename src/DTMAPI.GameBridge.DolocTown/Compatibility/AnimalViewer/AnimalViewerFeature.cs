#pragma warning disable CS0618 // This file registers the frozen IAnimalViewerApi compatibility island.
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Frozen old-ABI executor; new gameplay ownership lives in the admitted product.</summary>
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

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "AnimalViewer",
            requiresSave: true,
            allowsTitleScreen: false,
            requiresNativeScene: true,
            requiresUi: true,
            environmentResetSensitive: true,
            hasSaveLifetimeState: true,
            hasTitleLifetimeState: false,
            canAutoPauseAfterFailure: false);

        internal AnimalViewerService Service { get; }

        internal AnimalViewerHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IAnimalViewerApi>(manifest, Service, OwnerBoundGameBridgeApis.ForAnimalViewer(Service));
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
            Service.ResetAnimalViewerRuntimeState("SaveLoaded isNewGame=" + isNewGame);
        }

        public void ReturnedToTitle()
        {
            Service.ResetAnimalViewerRuntimeState("ReturnedToTitle");
        }

        public void EnvironmentReset(string reason)
        {
            Service.NotifyAnimalViewerEnvironmentReset(reason);
        }
    }
}
