namespace DTMAPI.Zoom
{
    internal enum ZoomInstallDecision
    {
        Install,
        RejectCompatibilityOwner,
        RejectDuplicateProductOwner
    }

    internal static class ZoomHookOwnership
    {
        internal static ZoomInstallDecision DecideInstall(
            bool compatibilityOwnerPresent,
            bool productOwnerPresent)
        {
            if (compatibilityOwnerPresent)
                return ZoomInstallDecision
                    .RejectCompatibilityOwner;
            return productOwnerPresent
                ? ZoomInstallDecision
                    .RejectDuplicateProductOwner
                : ZoomInstallDecision.Install;
        }
    }
}
