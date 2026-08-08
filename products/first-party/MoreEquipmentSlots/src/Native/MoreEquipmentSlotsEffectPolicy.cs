using System;
using System.Collections.Generic;

namespace DTMAPI.MoreEquipmentSlots
{
    internal static class MoreEquipmentSlotsEffectPolicy
    {
        internal static bool ShouldCreateNativeFunction(
            bool isShield,
            string skillId) =>
            !isShield &&
            !string.IsNullOrWhiteSpace(skillId);

        internal static int FindTailShieldIndex(
            IReadOnlyList<EquipmentSlotStorageEntry> slots)
        {
            if (slots == null)
                throw new ArgumentNullException(nameof(slots));
            for (int index = slots.Count - 1;
                 index >= 0;
                 index--)
            {
                EquipmentSlotStorageEntry slot = slots[index];
                if (slot != null &&
                    slot.IsOccupied &&
                    slot.IsShield &&
                    slot.ShieldValue > 0)
                {
                    return index;
                }
            }
            return -1;
        }

        internal static int SumHatDefense(
            IReadOnlyList<EquipmentSlotStorageEntry> slots)
        {
            if (slots == null)
                throw new ArgumentNullException(nameof(slots));
            int total = 0;
            foreach (EquipmentSlotStorageEntry slot in slots)
            {
                if (slot != null && slot.IsOccupied)
                    total += Math.Max(0, slot.DefenseBonus);
            }
            return total;
        }

        internal static bool PreservesNativeHatVisual => true;
    }
}
