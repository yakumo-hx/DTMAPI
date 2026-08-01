using System;

namespace DTMAPI.MoreEquipmentSlots
{
    internal enum EquipmentSlotsInstallDecision
    {
        Install = 0,
        RejectCompatibilityOwner = 1,
        RejectDuplicateProductOwner = 2,
        RejectPartialProductOwner = 3
    }

    internal readonly struct EquipmentSlotsObservedHookState
    {
        internal EquipmentSlotsObservedHookState(
            bool isInstalled,
            int installedPatchCount,
            bool hasPartialOwner)
        {
            IsInstalled = isInstalled;
            InstalledPatchCount = installedPatchCount;
            HasPartialOwner = hasPartialOwner;
        }

        internal bool IsInstalled { get; }

        internal int InstalledPatchCount { get; }

        internal bool HasPartialOwner { get; }
    }

    internal static class MoreEquipmentSlotsHookOwnership
    {
        internal static EquipmentSlotsInstallDecision DecideInstall(
            bool[] compatibilityOwners,
            bool[] productOwners)
        {
            Validate(compatibilityOwners, nameof(compatibilityOwners));
            Validate(productOwners, nameof(productOwners));
            for (int index = 0; index < compatibilityOwners.Length; index++)
            {
                if (compatibilityOwners[index])
                {
                    return EquipmentSlotsInstallDecision
                        .RejectCompatibilityOwner;
                }
            }

            int productCount = Count(productOwners);
            if (productCount == 0)
                return EquipmentSlotsInstallDecision.Install;
            return productCount ==
                MoreEquipmentSlotsProductContract.ExpectedHookCount
                    ? EquipmentSlotsInstallDecision
                        .RejectDuplicateProductOwner
                    : EquipmentSlotsInstallDecision
                        .RejectPartialProductOwner;
        }

        internal static EquipmentSlotsObservedHookState Observe(
            bool[] productOwners)
        {
            Validate(productOwners, nameof(productOwners));
            int count = Count(productOwners);
            int expected =
                MoreEquipmentSlotsProductContract.ExpectedHookCount;
            return new EquipmentSlotsObservedHookState(
                count == expected,
                count,
                count > 0 && count != expected);
        }

        private static int Count(bool[] values)
        {
            int count = 0;
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index])
                    count++;
            }

            return count;
        }

        private static void Validate(
            bool[] values,
            string parameterName)
        {
            if (values == null)
                throw new ArgumentNullException(parameterName);
            if (values.Length !=
                MoreEquipmentSlotsProductContract.ExpectedHookCount)
            {
                throw new ArgumentException(
                    "EquipmentSlots ownership requires exactly four native targets.",
                    parameterName);
            }
        }
    }
}
