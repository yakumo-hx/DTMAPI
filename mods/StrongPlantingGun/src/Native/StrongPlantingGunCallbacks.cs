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

        public static bool ItemFarmingGunRefreshCellTipPrefix(object __instance) =>
            runtime?.RefreshMaterialPreview(__instance) ?? true;

        public static void ItemFarmingGunOpenContainerUiPrefix(object __instance) =>
            runtime?.PrepareGun(__instance, "ItemFarmingGun.OpenContainerUI");

        public static bool ItemFarmingGunInitCellTipPrefix(object __instance) =>
            runtime?.CanInitializePreview(__instance) ?? true;

        public static object FarmingGunArea(object function) =>
            runtime?.GetPreviewArea(function) ?? function.GetType().GetProperty("Area")!.GetValue(function, null)!;
    }
}
