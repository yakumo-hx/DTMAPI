using System;

namespace DTMAPI.DebugConsole
{
    public static class DebugConsoleInputHooks
    {
        public static bool UseToolPrefix()
        {
            return !DebugConsoleInputGate.ShouldSuppress;
        }

        public static bool UseItemPrefix()
        {
            return !DebugConsoleInputGate.ShouldSuppress;
        }

        public static bool EnterUiCheckPrefix(ref bool __result)
        {
            if (!DebugConsoleInputGate.ShouldSuppress)
                return true;
            __result = true;
            return false;
        }
    }
}
