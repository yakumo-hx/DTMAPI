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

        public void RegisterApis(IManifest manifest) { }

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus("Player.EquipmentSlotsRecovery", "available", "Equipment cold storage reader", "Recovery retains supported storage and commits only after native save succeeds.");
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

        public void EnvironmentReset(string reason) { }
    }
}
