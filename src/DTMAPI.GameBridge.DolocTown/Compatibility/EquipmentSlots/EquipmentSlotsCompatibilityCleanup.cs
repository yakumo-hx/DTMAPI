using System;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class EquipmentSlotsCompatibilityService
    {
        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            reason ??= string.Empty;
            int removed = 0;
            Exception? failure = null;
            bool hasEquipmentState =
                equipmentSlotOptions.ContainsKey(ownerId) ||
                equipmentSlotStates.ContainsKey(ownerId) ||
                equipmentSlotEntries.ContainsKey(ownerId) ||
                loadedEquipmentSlotStorageOwners.Contains(ownerId) ||
                blockedEquipmentSlotStorageOwners.Contains(ownerId) ||
                dirtyEquipmentSlotStorageOwners.Contains(ownerId) ||
                migratedLegacyEquipmentSlotStorageOwners.Contains(ownerId) ||
                equipmentSlotJournals.ContainsKey(ownerId) ||
                committedEquipmentSlotEntries.ContainsKey(ownerId) ||
                equipmentSlotGameplayCandidates.ContainsKey(ownerId) ||
                equipmentSlotWorkingPlacementGuards.ContainsKey(
                    ownerId) ||
                equipmentSlotStorageGenerations.ContainsKey(ownerId);
            bool storageBlocked =
                blockedEquipmentSlotStorageOwners.Contains(
                    ownerId);
            bool runtimeShutdown =
                string.Equals(
                    reason,
                    "RuntimeShutdown",
                    StringComparison.Ordinal);
            if (hasEquipmentState &&
                !storageBlocked &&
                !runtimeShutdown)
            {
                if (!RecoverEquipmentSlotEntries(
                        ownerId,
                        "owner cleanup " + reason,
                        DTMAPI.MoreEquipmentSlots
                            .EquipmentSlotTransactionOrigin
                            .OwnerRecovery,
                        saveAfterRecovery: false,
                        out var recovery))
                {
                    throw new InvalidOperationException(
                        "Equipment-slot owner recovery failed for " +
                        ownerId +
                        ": " +
                        recovery.Message);
                }
                if (dirtyEquipmentSlotStorageOwners.Contains(
                        ownerId) &&
                    !PersistEquipmentSlotStorage(
                        ownerId,
                        "owner cleanup " + reason))
                {
                    throw new InvalidOperationException(
                        "Equipment-slot owner cleanup could not persist its exact recovery authority for " +
                        ownerId +
                        ".");
                }
            }
            try
            {
                if (hasEquipmentState && equipmentSlotEntries.TryGetValue(ownerId, out var entries))
                {
                    object? manager = GetNativeAgentEquipmentManager();
                    if (manager != null)
                    {
                        foreach (var entry in entries)
                            RemoveEquipmentSlotFunction(manager, entry);
                        InvokeNativeReloadParams(manager);
                    }
                    removed += entries.Count;
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                if (equipmentSlotOptions.Remove(ownerId))
                    removed++;
                if (equipmentSlotStates.Remove(ownerId))
                    removed++;
                if (equipmentSlotEntries.Remove(ownerId))
                    removed++;
                if (loadedEquipmentSlotStorageOwners.Remove(ownerId))
                    removed++;
                if (blockedEquipmentSlotStorageOwners.Remove(ownerId))
                    removed++;
                if (dirtyEquipmentSlotStorageOwners.Remove(ownerId))
                    removed++;
                if (migratedLegacyEquipmentSlotStorageOwners.Remove(ownerId))
                    removed++;
                if (committedEquipmentSlotEntries.Remove(ownerId))
                    removed++;
                if (equipmentSlotGameplayCandidates.Remove(ownerId))
                    removed++;
                if (equipmentSlotWorkingPlacementGuards.Remove(ownerId))
                    removed++;
                if (equipmentSlotJournals.ContainsKey(ownerId))
                {
                    if (equipmentSlotJournals.Remove(ownerId))
                        removed++;
                }
                if (equipmentSlotStorageGenerations.Remove(ownerId))
                    removed++;
                ClearEquipmentSlotsUiLifecycle("owner cleanup " + reason);
                if (hasEquipmentState &&
                    !ReleaseCompatibilityHooksIfUnused() &&
                    failure == null)
                {
                    failure = new InvalidOperationException(
                        "EquipmentSlots owner cleanup could not prove exact compatibility-owner Hook removal.");
                }
            }

            if (failure != null)
                throw failure;
            return removed;
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId ??= string.Empty;
            int entries = equipmentSlotEntries.TryGetValue(ownerId, out var values)
                ? 1 + values.Count
                : 0;
            bool hasOwner = equipmentSlotOptions.ContainsKey(ownerId) ||
                equipmentSlotStates.ContainsKey(ownerId) ||
                entries != 0 ||
                loadedEquipmentSlotStorageOwners.Contains(ownerId) ||
                blockedEquipmentSlotStorageOwners.Contains(ownerId) ||
                dirtyEquipmentSlotStorageOwners.Contains(ownerId) ||
                migratedLegacyEquipmentSlotStorageOwners.Contains(ownerId) ||
                equipmentSlotJournals.ContainsKey(ownerId) ||
                committedEquipmentSlotEntries.ContainsKey(ownerId) ||
                equipmentSlotGameplayCandidates.ContainsKey(ownerId) ||
                equipmentSlotWorkingPlacementGuards.ContainsKey(
                    ownerId) ||
                equipmentSlotStorageGenerations.ContainsKey(ownerId);
            int uiRoots = hasOwner
                ? activeEquipmentSlotUiObjects.Count + equipmentSlotUiEventBinders.Count
                : 0;
            return (equipmentSlotOptions.ContainsKey(ownerId) ? 1 : 0) +
                (equipmentSlotStates.ContainsKey(ownerId) ? 1 : 0) +
                entries +
                (loadedEquipmentSlotStorageOwners.Contains(ownerId) ? 1 : 0) +
                (blockedEquipmentSlotStorageOwners.Contains(ownerId) ? 1 : 0) +
                (dirtyEquipmentSlotStorageOwners.Contains(ownerId) ? 1 : 0) +
                (migratedLegacyEquipmentSlotStorageOwners.Contains(ownerId) ? 1 : 0) +
                (equipmentSlotJournals.ContainsKey(ownerId) ? 1 : 0) +
                (committedEquipmentSlotEntries.ContainsKey(ownerId) ? 1 : 0) +
                (equipmentSlotGameplayCandidates.ContainsKey(ownerId) ? 1 : 0) +
                (equipmentSlotWorkingPlacementGuards.ContainsKey(ownerId) ? 1 : 0) +
                (equipmentSlotStorageGenerations.ContainsKey(ownerId) ? 1 : 0) +
                uiRoots;
        }
    }
}
