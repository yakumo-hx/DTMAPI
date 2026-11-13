using System;

namespace DTMAPI.DebugConsole
{
    internal static class DebugConsoleModeHooks
    {
        internal static DebugConsoleNativeActions? Actions;
        internal static Func<bool>? EndSessionBeforeLoad;

        public static bool HealthCostPrefix(object __0, ref int __result)
        {
            if (Actions == null || !Actions.ProtectArchive(__0, out int health))
                return true;
            __result = health;
            return false;
        }

        public static bool EnergyCostPrefix(object __0, ref bool __result)
        {
            if (Actions?.ProtectArchive(__0, out _) != true)
                return true;
            __result = true;
            return false;
        }

        public static bool EnergyCostNoProtectPrefix(object __0) => Actions?.ProtectArchive(__0, out _) != true;
        public static bool SpiritTickPrefix(object __instance) => Actions?.ProtectSpirit(__instance) != true;
        public static void SpiritChangedPostfix(object __instance) => Actions?.ProtectSpirit(__instance);
        public static void TimeScaleWritePrefix(object __instance) => Actions?.ObserveNativeTimeScaleWrite(__instance);

        public static bool LoadGamePrefix(ref bool __result)
        {
            if (EndSessionBeforeLoad?.Invoke() != false)
                return true;
            __result = false;
            return false;
        }
        public static bool NewGamePrefix() => EndSessionBeforeLoad?.Invoke() != false;

        internal static void Reset()
        {
            Actions = null;
            EndSessionBeforeLoad = null;
        }
    }
}
