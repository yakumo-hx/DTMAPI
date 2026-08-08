using System;

namespace DTMAPI.ChestLocatorEnhancer
{
    internal enum ChestLocatorInstallDecision
    {
        Install,
        RejectCompatibilityOwner,
        RejectDuplicateProductOwner
    }

    internal readonly struct ChestLocatorObservedHookState
    {
        internal ChestLocatorObservedHookState(bool isInstalled, int installedPatchCount)
        {
            IsInstalled = isInstalled;
            InstalledPatchCount = installedPatchCount;
        }

        internal bool IsInstalled { get; }

        internal int InstalledPatchCount { get; }
    }

    internal static class ChestLocatorHookOwnership
    {
        internal static void ApplyEnabledState(bool enabled, Action install, Action unpatch)
        {
            if (install == null)
                throw new ArgumentNullException(nameof(install));
            if (unpatch == null)
                throw new ArgumentNullException(nameof(unpatch));
            if (enabled)
                install();
            else
                unpatch();
        }

        internal static ChestLocatorInstallDecision DecideInstall(bool compatibilityOwnerPresent, bool productOwnerPresent)
        {
            if (compatibilityOwnerPresent)
                return ChestLocatorInstallDecision.RejectCompatibilityOwner;
            return productOwnerPresent
                ? ChestLocatorInstallDecision.RejectDuplicateProductOwner
                : ChestLocatorInstallDecision.Install;
        }

        internal static ChestLocatorObservedHookState FromExactOwnerObservation(bool productOwnerPresent) =>
            new ChestLocatorObservedHookState(productOwnerPresent, productOwnerPresent ? 1 : 0);
    }
}
