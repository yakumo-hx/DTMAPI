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
            int removedForManagedProduct = service.ReconcileManagedProductOwnerBeforeHookInstall();
            if (removedForManagedProduct > 0 ||
                !DolocTownHookCallbacks.HasChestLocatorEnhancerRetainedCallbackDemand())
            {
                service.SetInventoryHookInstalled(false);
                runtime.SetHookStatus(
                    "Inventory.ChestLocatorEnhancer",
                    removedForManagedProduct > 0 ? "refused-managed-product-owner" : "dormant",
                    "ArchiveDataHandle.GetAvailableInventories exact-target ownership",
                    removedForManagedProduct > 0
                        ? "Frozen compatibility demand was removed before Hook installation because the managed ChestLocatorEnhancer product owns the exact native target."
                        : "No frozen IChestLocatorEnhancerApi owner currently demands the compatibility Postfix.");
                return;
            }

            if (!AvailableInventoriesPatched)
            {
                AvailableInventoriesPatched = patcher.TryPatchArrayResultPostfix(
                    "DolocTown.GameData.ArchiveDataHandle, Assembly-CSharp",
                    "GetAvailableInventories",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ArchiveDataHandleGetAvailableInventoriesPostfix), BindingFlags.Public | BindingFlags.Static),
                    3,
                    DolocTownHookCallbacks.HasChestLocatorEnhancerRetainedCallbackDemand);
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
