using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class StrongPlantingGunHookBridge
    {
        private readonly DtmApiRuntime runtime;
        private readonly StrongPlantingGunService service;

        public StrongPlantingGunHookBridge(DtmApiRuntime runtime, StrongPlantingGunService service)
        {
            this.runtime = runtime;
            this.service = service;
        }

        internal bool CtorPatched { get; private set; }

        internal bool ToolPatched { get; private set; }

        internal bool UiPlacePatched { get; private set; }

        internal bool UiSwapOnePatched { get; private set; }

        internal bool ToolHookReady => ToolPatched;

        internal bool UiHooksReady => UiPlacePatched && UiSwapOnePatched;

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus(
                "Farming.StrongPlantingGun",
                "contract",
                "DTMAPI.GameBridge.DolocTown API",
                "0.3.0 experimental strong planting gun contract expands the official farming gun inventory and routes multi-slot use through native farming gun interaction checks.");
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (!CtorPatched)
            {
                MethodInfo? ctorPostfix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ItemFarmingGunCtorPostfix), BindingFlags.Public | BindingFlags.Static);
                CtorPatched =
                    patcher.TryPatchConstructorPostfix("DolocTown.ItemFarmingGun, Assembly-CSharp", ctorPostfix, 2) |
                    patcher.TryPatchConstructorPostfix("DolocTown.ItemFarmingGun, Assembly-CSharp", ctorPostfix, 3);
            }

            if (!ToolPatched)
            {
                ToolPatched = patcher.TryPatchPrefix(
                    "DolocTown.ItemFarmingGun, Assembly-CSharp",
                    "OnUseAsTool",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ItemFarmingGunOnUseAsToolPrefix), BindingFlags.Public | BindingFlags.Static),
                    0);
            }

            if (!UiPlacePatched)
            {
                UiPlacePatched = patcher.TryPatchPrefix(
                    "DolocTown.FarmingGunUiState, Assembly-CSharp",
                    "HandlePlaceToOtherSide",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FarmingGunUiStateHandlePlaceToOtherSidePrefix), BindingFlags.Public | BindingFlags.Static),
                    1);
            }

            if (!UiSwapOnePatched)
            {
                UiSwapOnePatched = patcher.TryPatchPrefix(
                    "DolocTown.FarmingGunUiState, Assembly-CSharp",
                    "HandleSwapOneItem",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FarmingGunUiStateHandleSwapOneItemPrefix), BindingFlags.Public | BindingFlags.Static),
                    1);
            }

            service.SetHooksInstalled(ToolHookReady, UiHooksReady, CtorPatched);
            runtime.SetHookStatus(
                "Farming.StrongPlantingGun",
                (ToolHookReady && UiHooksReady) ? "experimental" : "pending",
                "Harmony Prefix/Postfix: ItemFarmingGun + FarmingGunUiState",
                (ToolHookReady && UiHooksReady)
                    ? "Patched official farming gun construction, use, and UI transfer paths so registered DTMAPI policies can expose multi-slot seed/film/fertilizer behavior while delegating plant checks to official methods."
                    : "Waiting for ItemFarmingGun/FarmingGunUiState targets to become patchable.");
        }
    }
}
