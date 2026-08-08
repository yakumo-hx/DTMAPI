namespace DTMAPI.DebugConsole
{
    public static class DebugConsoleCreativeHooks
    {
        internal static bool Enabled { get; private set; }
        internal static bool Installed { get; set; }

        internal static void Enable() => Enabled = true;

        internal static void Disable() => Enabled = false;

        internal static void Reset()
        {
            Enabled = false;
            Installed = false;
        }

        public static bool BoolTruePrefix(ref bool __result)
        {
            if (!Enabled)
                return true;
            __result = true;
            return false;
        }

        public static bool VoidSkipPrefix() => !Enabled;

        public static void RecipeTimePostfix(ref int __result)
        {
            if (Enabled)
                __result = 0;
        }
    }
}
