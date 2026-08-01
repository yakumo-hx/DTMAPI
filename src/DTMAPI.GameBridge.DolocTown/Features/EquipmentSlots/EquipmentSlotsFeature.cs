#pragma warning disable CS0618 // Frozen IEquipmentSlotsApi registration.
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class EquipmentSlotsFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        internal EquipmentSlotsFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new EquipmentSlotsService(runtime);
        }

        public string Id => "EquipmentSlots";

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "EquipmentSlots",
            requiresSave: true,
            allowsTitleScreen: false,
            requiresNativeScene: true,
            requiresUi: true,
            environmentResetSensitive: true,
            hasSaveLifetimeState: false,
            hasTitleLifetimeState: false,
            canAutoPauseAfterFailure: false);

        internal EquipmentSlotsService Service { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IEquipmentSlotsApi>(
                manifest,
                Service,
                OwnerBoundGameBridgeApis.ForEquipmentSlots(Service));
        }

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus(
                "Player.EquipmentSlotsApi",
                "contract",
                "DTMAPI.GameBridge.DolocTown frozen IEquipmentSlotsApi proxy",
                "Deprecated/Frozen compatibility activates the dormant Host only for an old ABI consumer or a protected legacy sidecar.");
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            // The mandatory bridge owns no EquipmentSlots gameplay Hook.
        }

        public void Update()
        {
            // Frozen compatibility UI refresh is callback-driven after the Host owns all four Hooks.
        }

        public void SaveLoaded(bool isNewGame)
        {
            Service.SaveLoaded(isNewGame);
        }

        public void ReturnedToTitle()
        {
            Service.ReturnedToTitle();
        }

        public void EnvironmentReset(string reason)
        {
            Service.EnvironmentReset(reason);
        }
    }
}
