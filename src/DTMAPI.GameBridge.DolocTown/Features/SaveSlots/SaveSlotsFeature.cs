#pragma warning disable CS0618 // Frozen ISaveSlotsApi registration.
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class SaveSlotsFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public SaveSlotsFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new SaveSlotsService(runtime);
        }

        public string Id => "SaveSlots";

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "SaveSlots",
            requiresSave: false,
            allowsTitleScreen: true,
            requiresNativeScene: false,
            requiresUi: false,
            environmentResetSensitive: false,
            hasSaveLifetimeState: false,
            hasTitleLifetimeState: false,
            canAutoPauseAfterFailure: false);

        internal SaveSlotsService Service { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<ISaveSlotsApi>(manifest, Service, OwnerBoundGameBridgeApis.ForSaveSlots(Service));
        }

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus(
                "Save.MoreSlotsApi",
                "contract",
                "DTMAPI.GameBridge.DolocTown frozen ISaveSlotsApi proxy",
                "Deprecated/Frozen compatibility activates the dormant Host only when an old ABI consumer calls it; the admitted MoreSaves product owns current fixed-six/twelve behavior.");
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            runtime.SetHookStatus(
                "Save.MoreSlotsUiPaging",
                "not-required",
                "Doloc Town official save UI",
                "MoreSaves and the frozen fixed-six/twelve compatibility executor install zero save UI Hooks; GameDataUiState and GameDataPanel remain native owners.");
        }

        public void Update()
        {
            Service.RefreshSaveSlotExpansionForRuntime(force: false, reason: "runtime refresh");
        }

        public void SaveLoaded(bool isNewGame)
        {
            Service.RefreshSaveSlotExpansionForRuntime(force: true, reason: "SaveLoaded");
        }

        public void ReturnedToTitle()
        {
            Service.RefreshSaveSlotExpansionForRuntime(force: true, reason: "ReturnedToTitle");
        }

        public void EnvironmentReset(string reason)
        {
            // No product-owned save UI or native scene state exists in mandatory Runtime.
        }
    }
}
