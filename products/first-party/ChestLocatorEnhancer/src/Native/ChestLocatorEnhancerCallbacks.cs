using System;

namespace DTMAPI.ChestLocatorEnhancer
{
    public static class ChestLocatorEnhancerCallbacks
    {
        private static ChestLocatorEnhancerNativeRuntime? runtime;

        internal static void Attach(
            ChestLocatorEnhancerNativeRuntime owner) =>
            runtime = owner;

        internal static void Detach(
            ChestLocatorEnhancerNativeRuntime owner)
        {
            if (ReferenceEquals(runtime, owner))
                runtime = null;
        }

        public static void ArchiveDataHandleGetAvailableInventoriesPostfix<
            TInventory>(
            object __instance,
            bool __2,
            ref TInventory[] __result)
        {
            ChestLocatorEnhancerNativeRuntime? current = runtime;
            if (current == null || __result == null)
                return;

            Array next = current.ExtendAvailableInventories(
                __instance,
                __2,
                __result);
            if (next is TInventory[] typed)
                __result = typed;
        }
    }
}
