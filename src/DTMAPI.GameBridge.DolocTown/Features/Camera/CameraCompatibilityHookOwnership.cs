namespace DTMAPI.GameBridge.DolocTown
{
    internal enum CameraCompatibilityInstallDecision
    {
        Install,
        Dormant,
        RejectManagedProductOwner
    }

    internal static class CameraCompatibilityHookOwnership
    {
        internal static CameraCompatibilityInstallDecision DecideInstall(
            bool managedProductOwnerPresent,
            bool compatibilityDemandPresent)
        {
            if (managedProductOwnerPresent)
            {
                return CameraCompatibilityInstallDecision
                    .RejectManagedProductOwner;
            }
            return compatibilityDemandPresent
                ? CameraCompatibilityInstallDecision.Install
                : CameraCompatibilityInstallDecision.Dormant;
        }
    }
}
