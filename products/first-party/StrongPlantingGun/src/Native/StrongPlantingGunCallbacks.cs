namespace DTMAPI.StrongPlantingGun
{
    internal static class StrongPlantingGunCallbacks
    {
        private static StrongPlantingGunNativeRuntime? runtime;

        public static string LastLifecycleSummary { get; private set; } =
            "listeners=0;callbacks=0;hooks=0;cachedObjects=0;cachedMembers=0;capacitySnapshots=0;roots=0";

        internal static void Attach(
            StrongPlantingGunNativeRuntime owner)
        {
            runtime = owner;
            owner.SetCallbackAttached(true);
            PublishLifecycleSummary(
                owner.BuildLifecycleSummary(
                    callbacksOverride: 1));
        }

        internal static void Detach(
            StrongPlantingGunNativeRuntime owner,
            string finalSummary)
        {
            if (ReferenceEquals(runtime, owner))
                runtime = null;
            owner.SetCallbackAttached(false);
            LastLifecycleSummary = finalSummary ??
                "listeners=0;callbacks=0;hooks=0;cachedObjects=0;cachedMembers=0;capacitySnapshots=0;roots=0";
        }

        internal static void PublishLifecycleSummary(string summary) =>
            LastLifecycleSummary = summary ?? string.Empty;

        public static void ItemFarmingGunCtorPostfix(
            object __instance) =>
            runtime?.PrepareGun(
                __instance,
                "ItemFarmingGun.ctor");

        public static bool ItemFarmingGunOnUseAsToolPrefix(
            object __instance) =>
            runtime?.HandleToolUse(__instance) ?? true;

        public static bool FarmingGunUiStateHandlePlaceToOtherSidePrefix(
            object __instance,
            int __0) =>
            runtime?.HandleUiPlaceToOtherSide(
                __instance,
                __0) ?? true;

        public static bool FarmingGunUiStateHandleSwapOneItemPrefix(
            object __instance,
            int __0) =>
            runtime?.HandleUiSwapOneItem(
                __instance,
                __0) ?? true;
    }
}
