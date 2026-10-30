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

        public static void EquipmentInventoriesPostfix(object __0, object __result)
        {
            if (__result is Array inventories) runtime?.UiRefresh.RememberEquipmentQuery(__0, inventories);
        }
        public static void RecipeStartupPostfix(object __instance, object[] __args, bool __result) =>
            runtime?.UiRefresh.RecipeStarted(__instance, __args, __result);
        public static void EquipmentStartupPostfix(object __instance, object? __1, bool __result) =>
            runtime?.UiRefresh.EquipmentStarted(__instance, __1 as Array, __result);
        public static void BuildingStartupPostfix(object __instance) => runtime?.UiRefresh.BuildingStarted(__instance);
        public static bool UiUpdatePrefix(object __instance) => runtime?.UiRefresh.Tick(__instance) ?? true;
        public static void UiExitPostfix(object __instance) => runtime?.UiRefresh.Forget(__instance);
        public static bool CraftActionPrefix(object __instance) => runtime?.UiRefresh.BeforeAction(__instance) ?? true;
        public static void QuantityScopePrefix(object __instance, out object? __state) =>
            __state = runtime?.UiRefresh.BeginQuantityAction(__instance);
        public static bool QuantityActionPrefix(object __instance, out object? __state)
        {
            __state = runtime?.UiRefresh.BeginQuantityAction(__instance);
            return runtime?.UiRefresh.BeforeAction(__instance) ?? true;
        }
        public static Exception? QuantityScopeFinalizer(Exception? __exception, object? __state)
        {
            runtime?.UiRefresh.EndQuantityAction(__state);
            return __exception;
        }
        public static bool RecipeConfirmPrefix(object __instance, int __0) => runtime?.UiRefresh.BeforeConsume(__instance, null, __0) ?? true;
        public static bool CraftConsumePrefix(object __instance, object __0, int __1) => runtime?.UiRefresh.BeforeConsume(__instance, __0, __1) ?? true;
        public static Exception? CraftConsumeFinalizer(object __instance, Exception? __exception)
        {
            if (__exception != null) runtime?.UiRefresh.ConsumeFailed(__instance);
            return __exception;
        }
        public static void QuantityDataPostfix(object __instance, object __1, object __2, object? __3)
        {
            if (__2 is Array inventories) runtime?.UiRefresh.QuantityCreated(__instance, __1, inventories, __3);
        }
        public static void QuantityStartupPostfix(object __instance, object __0, bool __result) => runtime?.UiRefresh.QuantityStarted(__instance, __0, __result);
        public static bool QuantityConfirmPrefix(object __instance, int __0) => runtime?.UiRefresh.BeforeQuantityConfirm(__instance, __0) ?? true;

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
