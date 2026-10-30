using System;

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

        public static void AccessoriesBarRenderPassiveItemsPostfix(
            object __instance,
            object __0) =>
            runtime?.RenderAccessoriesBar(
                __instance,
                (__0 as Array)?.Length ?? 0,
                "AccessoriesBar.RenderPassiveItems");

        public static void AccessoriesBarAllSelectablesPostfix(
            object __instance,
            ref Array __result)
        {
            MoreEquipmentSlotsNativeRuntime? current = runtime;
            if (current == null)
                return;
            __result = current.ComposeAccessoriesSelectables(
                __instance,
                __result);
        }

        public static void AccessoriesBarClearCallBackPostfix(
            object __instance) =>
            runtime?.OnAccessoriesBarClear(__instance);
    }
}
