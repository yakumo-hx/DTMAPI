using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CustomAnimalAnimatorBridgeFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;
        private HarmonyReflectionPatcher? patcher;

        public CustomAnimalAnimatorBridgeFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new CustomAnimalAnimatorBridgeService(runtime);
            HookBridge = new CustomAnimalAnimatorBridgeHookBridge(runtime, Service);
        }

        public string Id => "CustomAnimalAnimatorBridge";

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "CustomAnimalAnimatorBridge",
            requiresSave: false,
            allowsTitleScreen: true,
            requiresNativeScene: false,
            requiresUi: false,
            environmentResetSensitive: true,
            hasSaveLifetimeState: true,
            hasTitleLifetimeState: true,
            canAutoPauseAfterFailure: false);

        internal CustomAnimalAnimatorBridgeService Service { get; }

        internal CustomAnimalAnimatorBridgeHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
        }

        public void PublishHookStatuses()
        {
            Service.RefreshDefinitions("PublishHookStatuses", force: false);
            HookBridge.PublishHookStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            this.patcher = patcher;
            Service.RefreshDefinitions("InstallHooks", force: false);
            TryInstallHooks("InstallHooks", force: false);
        }

        public void Update()
        {
            if (Service.RefreshDefinitions("Update", force: false))
                TryInstallHooks("Update generation committed", force: false);
        }

        public void SaveLoaded(bool isNewGame)
        {
            Service.ClearSaveLifetimeState("SaveLoaded isNewGame=" + isNewGame);
            if (Service.RefreshDefinitions("SaveLoaded isNewGame=" + isNewGame, force: false))
                TryInstallHooks("SaveLoaded dirty generation", force: false);
        }

        public void ReturnedToTitle()
        {
            Service.ClearSaveLifetimeState("ReturnedToTitle");
            if (Service.RefreshDefinitions("ReturnedToTitle", force: false))
                TryInstallHooks("ReturnedToTitle dirty generation", force: false);
        }

        public void EnvironmentReset(string reason)
        {
            if (Service.RefreshDefinitions("EnvironmentReset " + (reason ?? string.Empty), force: false))
                TryInstallHooks("EnvironmentReset dirty generation", force: false);
        }

        private void TryInstallHooks(string reason, bool force)
        {
            if (HookBridge.HooksReady)
            {
                HookBridge.PublishHookStatuses();
                return;
            }

            if (patcher == null ||
                (Service.RegisteredKeyCount <= 0 &&
                    Service.RegisteredAiTemplateCount <= 0 &&
                    Service.RegisteredPngSpriteOverrideSpeciesCount <= 0 &&
                    Service.RegisteredCustomAnimalSpeciesCount <= 0))
            {
                HookBridge.PublishHookStatuses();
                return;
            }

            runtime.RuntimeMonitor.Log(
                "CustomAnimals.AnimatorBridge installing custom animal hooks reason=" + (reason ?? string.Empty) +
                " context=" + runtime.UI.InputContext +
                " registeredKeys=" + Service.RegisteredKeySummary +
                " aiTemplates=" + Service.RegisteredAiTemplateSummary +
                " pngSpriteOverrides=" + Service.RegisteredPngSpriteOverrideSummary +
                " customSpecies=" + Service.RegisteredCustomAnimalSpeciesSummary + ".");
            HookBridge.InstallHooks(patcher);
        }
    }
}
