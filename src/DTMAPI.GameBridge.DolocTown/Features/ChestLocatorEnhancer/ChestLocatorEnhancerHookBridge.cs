using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class ChestLocatorEnhancerHookBridge
    {
        private readonly DtmApiRuntime runtime;
        private readonly ChestLocatorEnhancerService service;

        public ChestLocatorEnhancerHookBridge(DtmApiRuntime runtime, ChestLocatorEnhancerService service)
        {
            this.runtime = runtime;
            this.service = service;
        }

        internal bool AvailableInventoriesPatched { get; private set; }

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus(
                "Inventory.ChestLocatorEnhancer",
                "contract",
                "DTMAPI.GameBridge.DolocTown API",
                "0.3.0 experimental chest locator enhancer contract appends only native LinearInventory instances from official ILocatable.IsShared containers to ArchiveDataHandle.GetAvailableInventories results.");
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (!AvailableInventoriesPatched)
            {
                AvailableInventoriesPatched = patcher.TryPatchArrayResultPostfix(
                    "DolocTown.GameData.ArchiveDataHandle, Assembly-CSharp",
                    "GetAvailableInventories",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ArchiveDataHandleGetAvailableInventoriesPostfix), BindingFlags.Public | BindingFlags.Static),
                    3);
            }

            service.SetInventoryHookInstalled(AvailableInventoriesPatched);
            runtime.SetHookStatus(
                "Inventory.ChestLocatorEnhancer",
                AvailableInventoriesPatched ? "experimental" : "pending",
                "Harmony Postfix: ArchiveDataHandle.GetAvailableInventories",
                AvailableInventoriesPatched
                    ? "Patched native inventory array enumeration so registered DTMAPI policies can append official shared container inventories without replacing CountItem/CostItem transaction logic."
                    : "Waiting for ArchiveDataHandle.GetAvailableInventories to become patchable.");
        }
    }
}
