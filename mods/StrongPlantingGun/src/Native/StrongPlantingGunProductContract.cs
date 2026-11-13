namespace DTMAPI.StrongPlantingGun
{
    internal static class StrongPlantingGunProductContract
    {
        internal const string UniqueId =
            "DTMAPI.StrongPlantingGunMod";
        internal const string HarmonyOwner =
            "DTMAPI.StrongPlantingGunMod.Native";
        internal const string LegacyProductOwner =
            "dtmapi.mod.dtmapi.strongplantinggunmod";
        internal const string CompatibilityOwner =
            "dtmapi.gamebridge.doloctown";
        internal const int FixedSlotCount = 4;
        internal const int ExpectedHookCount = 15;

        internal static int GetTargetInventoryCapacity(
            int currentCapacity) =>
            currentCapacity < FixedSlotCount
                ? FixedSlotCount
                : currentCapacity;
    }
}
