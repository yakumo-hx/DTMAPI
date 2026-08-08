namespace DTMAPI.MoreEquipmentSlots
{
    public static class MoreEquipmentSlotsCallbacks
    {
        private static MoreEquipmentSlotsNativeRuntime? runtime;

        public static string LastLifecycleSummary { get; private set; } =
            "clones=0;listeners=0;functions=0;callbacks=0;hooks=0;roots=0";

        internal static void Attach(
            MoreEquipmentSlotsNativeRuntime owner)
        {
            runtime = owner;
            LastLifecycleSummary =
                "clones=0;listeners=0;functions=0;callbacks=1;hooks=0;roots=0";
        }

        internal static void Detach(
            MoreEquipmentSlotsNativeRuntime owner,
            string finalSummary)
        {
            if (ReferenceEquals(runtime, owner))
                runtime = null;
            LastLifecycleSummary =
                finalSummary ??
                "clones=0;listeners=0;functions=0;callbacks=0;hooks=0;roots=0";
        }

        internal static void PublishLifecycleSummary(string summary) =>
            LastLifecycleSummary = summary ?? string.Empty;

        public static void AgentEquipmentManagerReloadParamsPostfix(
            object __instance) =>
            runtime?.ApplyAfterNativeReload(__instance);

        public static void
            AgentEquipmentManagerTryGetShieldItemPostfix(
                ref DolocTown.IAgentEquipmentShieldItem __0,
                ref bool __result)
        {
            MoreEquipmentSlotsNativeRuntime? current = runtime;
            if (__result || current == null)
                return;
            DolocTown.IAgentEquipmentShieldItem? shield =
                current.GetNativeShieldAdapter();
            if (shield == null)
                return;
            __0 = shield;
            __result = true;
        }

        public static void AccessoriesBarInitPostfix(
            object __instance) =>
            runtime?.RenderAccessoriesBar(
                __instance,
                "AccessoriesBar.__Init");

        public static void AccessoriesBarStartShowPostfix(
            object __instance) =>
            runtime?.RenderAccessoriesBar(
                __instance,
                "AccessoriesBar.OnStartShow");
    }
}
