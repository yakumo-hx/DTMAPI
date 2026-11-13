using System;

namespace DTMAPI.Modules.Monsters
{
    // Native event facts, shared by every template using that event. Audio itself
    // only receives the resolved recipe; it has no Monster template knowledge.
    internal static class HummingLoopDefaults
    {
        internal static bool? Resolve(string nativeEvent)
        {
            switch (nativeEvent?.ToUpperInvariant())
            {
                case "PLAY_DRONE_HUMMING": return true;
                case "PLAY_DRONE_BEE_HUMMING":
                case "":
                case null: return false;
                default: return null;
            }
        }
    }
}
