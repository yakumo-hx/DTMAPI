using System;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class EquipmentSlotsCompatibilityService
    {
        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            int retained = CountOwnerResources(ownerId);
            if (retained == 0) return 0;
            if (!blockedEquipmentSlotStorageOwners.Contains(ownerId) && reason != "RuntimeShutdown")
            {
                if (!RecoverEquipmentSlotEntries(ownerId, "owner cleanup " + reason, DTMAPI.MoreEquipmentSlots.EquipmentSlotTransactionOrigin.OwnerRecovery, false, out var recovery))
                    throw new InvalidOperationException(recovery.Message);
                if (dirtyEquipmentSlotStorageOwners.Contains(ownerId) && !PersistEquipmentSlotStorage(ownerId, "owner cleanup " + reason))
                    throw new InvalidOperationException("Could not persist recovery authority for " + ownerId);
            }
            equipmentSlotEntries.Remove(ownerId);
            loadedEquipmentSlotStorageOwners.Remove(ownerId);
            blockedEquipmentSlotStorageOwners.Remove(ownerId);
            dirtyEquipmentSlotStorageOwners.Remove(ownerId);
            migratedLegacyEquipmentSlotStorageOwners.Remove(ownerId);
            equipmentSlotJournals.Remove(ownerId);
            committedEquipmentSlotEntries.Remove(ownerId);
            equipmentSlotGameplayCandidates.Remove(ownerId);
            equipmentSlotWorkingPlacementGuards.Remove(ownerId);
            equipmentSlotStorageGenerations.Remove(ownerId);
            return retained;
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId ??= string.Empty;
            return (equipmentSlotEntries.TryGetValue(ownerId, out var entries) ? 1 + entries.Count : 0) +
                (loadedEquipmentSlotStorageOwners.Contains(ownerId) ? 1 : 0) +
                (blockedEquipmentSlotStorageOwners.Contains(ownerId) ? 1 : 0) +
                (dirtyEquipmentSlotStorageOwners.Contains(ownerId) ? 1 : 0) +
                (migratedLegacyEquipmentSlotStorageOwners.Contains(ownerId) ? 1 : 0) +
                (equipmentSlotJournals.ContainsKey(ownerId) ? 1 : 0) +
                (committedEquipmentSlotEntries.ContainsKey(ownerId) ? 1 : 0) +
                (equipmentSlotGameplayCandidates.ContainsKey(ownerId) ? 1 : 0) +
                (equipmentSlotWorkingPlacementGuards.ContainsKey(ownerId) ? 1 : 0) +
                (equipmentSlotStorageGenerations.ContainsKey(ownerId) ? 1 : 0);
        }
    }
}
