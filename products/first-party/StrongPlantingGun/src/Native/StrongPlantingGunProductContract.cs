namespace DTMAPI.StrongPlantingGun
{
    internal static class StrongPlantingGunProductContract
    {
        internal const string UniqueId =
            "DTMAPI.StrongPlantingGunMod";
        internal const string HarmonyOwner =
            "dtmapi.mod.dtmapi.strongplantinggunmod";
        internal const string CompatibilityOwner =
            "dtmapi.gamebridge.doloctown";
        internal const int FixedSlotCount = 3;
        internal const int ExpectedHookCount = 5;

        internal static int GetTargetInventoryCapacity(
            int currentCapacity) =>
            currentCapacity < FixedSlotCount
                ? FixedSlotCount
                : currentCapacity;
    }
}
