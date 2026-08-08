namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal enum Batch6AutoFishingNativeDriverOutcome
    {
        PendingSaveLoaded,
        FailedProductPresent,
        BlockedNativeDriverMissing,
        ContinueWithNativeDriver
    }

    internal static class Batch6AutoFishingFailClosedPolicy
    {
        internal static Batch6AutoFishingNativeDriverOutcome EvaluateProductAbsentL0(
            bool fifthSaveLoaded,
            bool coreProductInstancePresent,
            int loadedProductCount,
            bool safeIndependentNativeDriverAvailable)
        {
            if (coreProductInstancePresent || loadedProductCount != 0)
                return Batch6AutoFishingNativeDriverOutcome.FailedProductPresent;
            if (!fifthSaveLoaded)
                return Batch6AutoFishingNativeDriverOutcome.PendingSaveLoaded;
            return safeIndependentNativeDriverAvailable
                ? Batch6AutoFishingNativeDriverOutcome.ContinueWithNativeDriver
                : Batch6AutoFishingNativeDriverOutcome.BlockedNativeDriverMissing;
        }
    }
}
