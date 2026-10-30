using System;

namespace DTMAPI.Mine
{
    internal static class MineProductContract
    {
        internal static readonly Type NativeReceiptAnchor =
            typeof(global::DolocAPI);
        internal const string UniqueId = "DTMAPI.MineMod";
        internal const string HarmonyOwner =
            "dtmapi.mod.dtmapi.minemod";
        internal const string MineMachineId = "dtmapi.mine";
        internal const string MineItemId = "dtmapi_mine";
        internal const string OilItemId = "crude_oil";
        internal const int FixedPowerCost = 10;
        internal const int ExpectedHookCount = 3;
    }
}
