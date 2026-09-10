using System;

namespace DTMAPI.MoreEquipmentSlots
{
    internal enum MoreEquipmentSlotsItemKind
    {
        Rejected = 0,
        PassiveEquipment = 1,
        OrdinaryHat = 2,
        ShieldHat = 3
    }

    internal readonly struct MoreEquipmentSlotsItemAdmission
    {
        internal MoreEquipmentSlotsItemAdmission(
            MoreEquipmentSlotsItemKind kind,
            string reason)
        {
            Kind = kind;
            Reason = reason ?? string.Empty;
        }

        internal MoreEquipmentSlotsItemKind Kind { get; }

        internal string Reason { get; }

        internal bool Accepted =>
            Kind != MoreEquipmentSlotsItemKind.Rejected;
    }

    internal static class MoreEquipmentSlotsItemAdmissionPolicy
    {
        private const string PassiveFunction =
            "DolocTown.Config.Item.ItemFunctionPassive";
        private const string HerbPackageFunction =
            "DolocTown.Config.Item.ItemFunctionHerbPackage";

        internal static MoreEquipmentSlotsItemAdmission Decide(
            bool isNativePassiveItem,
            bool isNativeHatItem,
            string itemFunctionType,
            string skillId,
            bool isShieldHat,
            int maxShieldValue)
        {
            if (isNativePassiveItem)
            {
                bool acceptedFunction =
                    string.Equals(
                        itemFunctionType,
                        PassiveFunction,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        itemFunctionType,
                        HerbPackageFunction,
                        StringComparison.Ordinal);
                if (!acceptedFunction)
                {
                    return Reject(
                        "Passive equipment must use ItemFunctionPassive or ItemFunctionHerbPackage.");
                }
                if (string.IsNullOrWhiteSpace(skillId))
                {
                    return Reject(
                        "Passive equipment must expose a non-empty native skill.");
                }
                return new MoreEquipmentSlotsItemAdmission(
                    MoreEquipmentSlotsItemKind.PassiveEquipment,
                    "Accepted native passive equipment.");
            }

            if (!isNativeHatItem)
            {
                return Reject(
                    "Only registered native passive equipment or hats are accepted.");
            }
            if (!isShieldHat)
            {
                return new MoreEquipmentSlotsItemAdmission(
                    MoreEquipmentSlotsItemKind.OrdinaryHat,
                    "Accepted registered ordinary hat; defense and skill may both be empty.");
            }
            if (maxShieldValue <= 0)
            {
                return Reject(
                    "A shield hat must expose a positive MaxShieldValue.");
            }
            return new MoreEquipmentSlotsItemAdmission(
                MoreEquipmentSlotsItemKind.ShieldHat,
                "Accepted registered shield hat.");
        }

        private static MoreEquipmentSlotsItemAdmission Reject(
            string reason) =>
            new MoreEquipmentSlotsItemAdmission(
                MoreEquipmentSlotsItemKind.Rejected,
                reason);
    }
}
