using System;

namespace DTMAPI.GameBridge.DolocTown
{
    internal static class EquipmentSlotShieldPolicy
    {
        public static bool IsShieldSkill(string skillId, string itemFunctionTypeName, string skillFunctionTypeName)
        {
            return string.Equals(skillId?.Trim(), "shield", StringComparison.OrdinalIgnoreCase) ||
                ContainsTypeName(itemFunctionTypeName, "ItemFunctionHatShield") ||
                ContainsTypeName(skillFunctionTypeName, "AgentEquipmentFuncProtoShield");
        }

        public static int NormalizeShieldValue(int value, int maxValue)
        {
            if (maxValue <= 0)
                return 0;
            if (value <= 0)
                return maxValue;
            return Math.Min(value, maxValue);
        }

        public static EquipmentSlotShieldBlockResult Block(int incomingDamage, int shieldValue, int shieldDefend)
        {
            int normalizedDamage = Math.Max(0, incomingDamage);
            int normalizedShield = Math.Max(0, shieldValue);
            int normalizedDefend = Math.Max(0, shieldDefend);
            if (normalizedDamage <= 0)
                return new EquipmentSlotShieldBlockResult(true, false, 0, normalizedShield, 0);

            int damageAfterDefend = normalizedDamage - normalizedDefend;
            if (damageAfterDefend <= 0)
                return new EquipmentSlotShieldBlockResult(true, false, 0, normalizedShield, damageAfterDefend);

            if (normalizedShield > damageAfterDefend)
                return new EquipmentSlotShieldBlockResult(true, false, damageAfterDefend, normalizedShield - damageAfterDefend, damageAfterDefend);

            return new EquipmentSlotShieldBlockResult(false, true, normalizedShield, 0, damageAfterDefend);
        }

        private static bool ContainsTypeName(string value, string expected)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    internal readonly struct EquipmentSlotShieldBlockResult
    {
        public EquipmentSlotShieldBlockResult(bool fullyBlocked, bool broken, int blockedDamage, int remainingShieldValue, int damageAfterDefend)
        {
            FullyBlocked = fullyBlocked;
            Broken = broken;
            BlockedDamage = blockedDamage;
            RemainingShieldValue = remainingShieldValue;
            DamageAfterDefend = damageAfterDefend;
        }

        public bool FullyBlocked { get; }
        public bool Broken { get; }
        public int BlockedDamage { get; }
        public int RemainingShieldValue { get; }
        public int DamageAfterDefend { get; }
    }
}
