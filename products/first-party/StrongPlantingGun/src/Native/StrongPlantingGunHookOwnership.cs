using System;

namespace DTMAPI.StrongPlantingGun
{
    internal enum StrongPlantingGunInstallDecision
    {
        Install,
        RejectCompatibilityOwner,
        RejectDuplicateProductOwner,
        RejectPartialOwnerSet
    }

    internal readonly struct StrongPlantingGunObservedHookState
    {
        internal StrongPlantingGunObservedHookState(
            bool isInstalled,
            int installedPatchCount,
            bool hasPartialOwnerSet)
        {
            IsInstalled = isInstalled;
            InstalledPatchCount = installedPatchCount;
            HasPartialOwnerSet = hasPartialOwnerSet;
        }

        internal bool IsInstalled { get; }

        internal int InstalledPatchCount { get; }

        internal bool HasPartialOwnerSet { get; }
    }

    internal static class StrongPlantingGunHookOwnership
    {
        internal static StrongPlantingGunInstallDecision DecideInstall(
            bool[] compatibilityOwners,
            bool[] productOwners)
        {
            Validate(compatibilityOwners, productOwners);
            int compatibilityCount = CountTrue(compatibilityOwners);
            int productCount = CountTrue(productOwners);
            if (compatibilityCount > 0)
                return StrongPlantingGunInstallDecision
                    .RejectCompatibilityOwner;
            if (productCount ==
                StrongPlantingGunProductContract.ExpectedHookCount)
            {
                return StrongPlantingGunInstallDecision
                    .RejectDuplicateProductOwner;
            }
            return productCount == 0
                ? StrongPlantingGunInstallDecision.Install
                : StrongPlantingGunInstallDecision
                    .RejectPartialOwnerSet;
        }

        internal static StrongPlantingGunObservedHookState Observe(
            bool[] owners)
        {
            if (owners == null)
                throw new ArgumentNullException(nameof(owners));
            if (owners.Length !=
                StrongPlantingGunProductContract.ExpectedHookCount)
            {
                throw new ArgumentException(
                    "StrongPlantingGun owner observations must contain exactly five targets.",
                    nameof(owners));
            }

            int count = CountTrue(owners);
            return new StrongPlantingGunObservedHookState(
                count ==
                    StrongPlantingGunProductContract
                        .ExpectedHookCount,
                count,
                count > 0 &&
                    count <
                    StrongPlantingGunProductContract
                        .ExpectedHookCount);
        }

        private static void Validate(
            bool[] compatibilityOwners,
            bool[] productOwners)
        {
            if (compatibilityOwners == null)
            {
                throw new ArgumentNullException(
                    nameof(compatibilityOwners));
            }
            if (productOwners == null)
                throw new ArgumentNullException(nameof(productOwners));
            if (compatibilityOwners.Length !=
                    StrongPlantingGunProductContract
                        .ExpectedHookCount ||
                productOwners.Length !=
                    StrongPlantingGunProductContract
                        .ExpectedHookCount)
            {
                throw new ArgumentException(
                    "StrongPlantingGun install decisions require exactly five target observations.");
            }
        }

        private static int CountTrue(bool[] values)
        {
            int result = 0;
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index])
                    result++;
            }
            return result;
        }
    }
}
