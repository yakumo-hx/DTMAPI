using System;

namespace DTMAPI.Mine
{
    internal static class MineCallbacks
    {
        private static readonly object Sync = new object();
        private static MineNativeRuntime? runtime;

        internal static int AttachedCount
        {
            get
            {
                lock (Sync)
                    return runtime == null ? 0 : 1;
            }
        }

        internal static void Attach(MineNativeRuntime value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            lock (Sync)
            {
                if (runtime != null &&
                    !ReferenceEquals(runtime, value))
                {
                    throw new InvalidOperationException(
                        "Mine callbacks already have another active ProductNative owner.");
                }
                runtime = value;
            }
        }

        internal static void Detach(MineNativeRuntime value)
        {
            lock (Sync)
            {
                if (ReferenceEquals(runtime, value))
                    runtime = null;
            }
        }

        public static void EquipmentRendererOnReusePostfix(
            object __instance)
        {
            MineNativeRuntime? current;
            lock (Sync)
                current = runtime;
            current?.RestoreRendererOnReuse(__instance);
        }

        public static void EquipmentBuilderCreateIndicatorPostfix(
            object __instance)
        {
            MineNativeRuntime? current;
            lock (Sync)
                current = runtime;
            current?.ApplyMineBuilderPreviewScale(
                __instance,
                "EquipmentBuilder.CreateIndicator");
        }

        public static void EquipmentBuilderTurnIndicatorPostfix(
            object __instance)
        {
            MineNativeRuntime? current;
            lock (Sync)
                current = runtime;
            current?.ApplyMineBuilderPreviewScale(
                __instance,
                "EquipmentBuilder.TurnIndicator");
        }
    }
}
