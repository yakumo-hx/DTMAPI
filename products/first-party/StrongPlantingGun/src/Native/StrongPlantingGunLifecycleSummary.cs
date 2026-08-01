namespace DTMAPI.StrongPlantingGun
{
    internal static class StrongPlantingGunLifecycleSummary
    {
        internal static string Format(
            int callbacks,
            int hooks,
            int cachedMembers,
            int capacitySnapshots) =>
            "listeners=0;callbacks=" +
            callbacks +
            ";hooks=" +
            hooks +
            ";cachedObjects=0;cachedMembers=" +
            cachedMembers +
            ";capacitySnapshots=" +
            capacitySnapshots +
            ";roots=" +
            ((callbacks > 0 || hooks > 0 ? 1 : 0) +
             capacitySnapshots);
    }
}
