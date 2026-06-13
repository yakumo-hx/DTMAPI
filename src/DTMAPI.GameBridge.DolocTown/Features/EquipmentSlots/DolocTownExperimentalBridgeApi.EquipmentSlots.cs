using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class DolocTownExperimentalBridgeApi
    {
        internal void NotifyEquipmentSlotsSaveLoaded(bool isNewGame)
        {
            ResetEquipmentSlotSessionState("SaveLoaded isNewGame=" + isNewGame, discardDirty: true);
        }

        internal void NotifyEquipmentSlotsSaveSaved(int? slot)
        {
            if (dirtyEquipmentSlotStorageOwners.Count == 0)
                return;

            string[] owners = dirtyEquipmentSlotStorageOwners.ToArray();
            foreach (string ownerId in owners)
            {
                PersistEquipmentSlotStorage(ownerId, "SaveSaved slot=" + (slot?.ToString(CultureInfo.InvariantCulture) ?? "unknown"));
                dirtyEquipmentSlotStorageOwners.Remove(ownerId);
            }

            runtime.SetHookStatus("Player.EquipmentSlotsSaveTransaction", "verified", "SaveGame postfix -> DTMAPI equipment-slot storage flush", "Flushed " + owners.Length + " dirty equipment-slot owner(s) after native SaveGame completed.");
        }

        internal void NotifyEquipmentSlotsReturnedToTitle()
        {
            ResetEquipmentSlotSessionState("ReturnedToTitle", discardDirty: true);
        }

        private void ResetEquipmentSlotSessionState(string reason, bool discardDirty)
        {
            if (equipmentSlotEntries.Count == 0 && loadedEquipmentSlotStorageOwners.Count == 0 && dirtyEquipmentSlotStorageOwners.Count == 0 && migratedLegacyEquipmentSlotStorageOwners.Count == 0)
                return;

            object? manager = GetNativeAgentEquipmentManager();
            if (manager != null)
            {
                foreach (List<EquipmentSlotRuntimeEntry> entries in equipmentSlotEntries.Values)
                {
                    foreach (EquipmentSlotRuntimeEntry entry in entries)
                        RemoveEquipmentSlotFunction(manager, entry);
                }
                InvokeNativeReloadParams(manager);
            }

            int discardedDirty = dirtyEquipmentSlotStorageOwners.Count;
            equipmentSlotEntries.Clear();
            loadedEquipmentSlotStorageOwners.Clear();
            if (discardDirty)
                dirtyEquipmentSlotStorageOwners.Clear();
            migratedLegacyEquipmentSlotStorageOwners.Clear();
            equipmentSlotStates.Clear();
            equipmentSlotsOrphanRecoveryChecked = false;
            equipmentSlotsUiLastSummary = string.Empty;
            runtime.RuntimeMonitor.Log("EquipmentSlots session reset reason=" + (reason ?? string.Empty) + " discardedDirtyOwners=" + discardedDirty + ".");
            runtime.SetHookStatus("Player.EquipmentSlotsSaveTransaction", "session-reset", "SaveLoaded/ReturnedToTitle boundary", "Cleared in-memory equipment-slot state for " + (reason ?? string.Empty) + "; unsaved DTMAPI storage mutations are not written outside native SaveGame.");
        }

        internal void SetEquipmentSlotsRuntimeHooksInstalled(bool installed)
        {
            equipmentSlotsRuntimeHooksInstalled = installed;
            foreach (KeyValuePair<string, EquipmentSlotsState> entry in equipmentSlotStates.ToArray())
            {
                EquipmentSlotsState state = entry.Value;
                state.RuntimeStatsHookInstalled = installed;
                state.Status = installed && state.IsConfigured ? "configured-experimental-ui-storage-stats-hook" : state.Status;
                equipmentSlotStates[entry.Key] = state;
            }
        }

        internal void SetEquipmentSlotsUiHooksInstalled(bool installed)
        {
            equipmentSlotsUiHooksInstalled = installed;
            foreach (KeyValuePair<string, EquipmentSlotsState> entry in equipmentSlotStates.ToArray())
            {
                EquipmentSlotsState state = entry.Value;
                state.RuntimeUiHookInstalled = installed || equipmentSlotsUiRendered;
                state.Status = (installed || equipmentSlotsUiRendered) && state.IsConfigured && equipmentSlotsRuntimeHooksInstalled
                    ? "configured-experimental-player-ui-storage-stats-hook"
                    : state.Status;
                equipmentSlotStates[entry.Key] = state;
            }
        }

        public EquipmentSlotsRegisterResult RegisterSlots(IManifest owner, EquipmentSlotsOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            EquipmentSlotsOptions normalized = NormalizeEquipmentSlotsOptions(options);
            equipmentSlotOptions[owner.UniqueID] = normalized;
            EnsureEquipmentSlotStorageLoaded(owner.UniqueID);
            EnsureEquipmentSlotEntries(owner.UniqueID, normalized);

            if (!normalized.Enabled && normalized.SafeUnequipOnDisable)
                RecoverEquipmentSlotEntries(owner.UniqueID, "disabled registration", saveAfterRecovery: true, out _);
            else if (normalized.Enabled)
                TryApplyStoredEquipmentSlotFunctions(owner.UniqueID, "register");
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("register " + owner.UniqueID, force: true);

            EquipmentSlotsState state = BuildEquipmentSlotsState(owner.UniqueID, normalized);
            state.LastRecoveryMessage = FirstText(state.LastRecoveryMessage, "No runtime recovery has run in this session.");
            equipmentSlotStates[owner.UniqueID] = state;
            runtime.RuntimeMonitor.Log("Equipment slots definition registered owner=" + owner.UniqueID + " enabled=" + normalized.Enabled + " extraSlots=" + normalized.ExtraAttributeSlots + " preserveVanillaVisualSlots=" + normalized.PreserveVanillaVisualSlots + ".");
            runtime.SetHookStatus("Player.EquipmentSlotsApi", state.Status, "DTMAPI.GameBridge.DolocTown API", "Registered extra equipment-slot policy for " + owner.UniqueID + "; DTMAPI stores attribute-only slots, renders an interactive player equipment strip, preserves vanilla visual slots, and can recover stored items through native backpack placement.");

            return new EquipmentSlotsRegisterResult
            {
                Success = true,
                OwnerId = owner.UniqueID,
                ExtraAttributeSlots = normalized.Enabled ? normalized.ExtraAttributeSlots : 0,
                Message = state.Status
            };
        }

        public IReadOnlyList<EquipmentSlotInfo> GetSlots(string uniqueId)
        {
            string ownerId = uniqueId ?? string.Empty;
            EquipmentSlotsOptions options = GetEquipmentSlotsOptions(ownerId);
            EnsureEquipmentSlotStorageLoaded(ownerId);
            List<EquipmentSlotRuntimeEntry> entries = EnsureEquipmentSlotEntries(ownerId, options);
            return entries.Select(entry => ToEquipmentSlotInfo(ownerId, entry, options)).ToArray();
        }

        public EquipmentSlotEquipResult EquipExtraSlot(IManifest owner, string slotId, string itemId)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            string ownerId = owner.UniqueID;
            var result = new EquipmentSlotEquipResult
            {
                OwnerId = ownerId,
                SlotId = slotId ?? string.Empty,
                ItemId = itemId ?? string.Empty
            };

            EquipmentSlotsOptions options = GetEquipmentSlotsOptions(ownerId);
            if (!options.Enabled || options.ExtraAttributeSlots <= 0)
                return EquipmentSlotEquipFailed(result, "not-enabled", "Equipment slots are not enabled for " + ownerId + ".");

            EnsureEquipmentSlotStorageLoaded(ownerId);
            List<EquipmentSlotRuntimeEntry> entries = EnsureEquipmentSlotEntries(ownerId, options);
            EquipmentSlotRuntimeEntry? entry = FindEquipmentSlotEntry(entries, options, slotId ?? string.Empty);
            if (entry == null)
                return EquipmentSlotEquipFailed(result, "missing-slot", "Slot " + (slotId ?? string.Empty) + " is not available.");

            result.SlotId = entry.SlotId;
            string normalizedItemId = itemId?.Trim() ?? string.Empty;
            result.ItemId = normalizedItemId;
            if (string.IsNullOrWhiteSpace(normalizedItemId))
                return EquipmentSlotEquipFailed(result, "missing-item", "An item id is required.");

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null || ReadStaticMember(dolocApi, "archiveHandle") == null)
                return EquipmentSlotEquipFailed(result, "missing-archive", "No loaded archive is available.");

            result.BeforeBackpackCount = CountNativeBackpackItem(dolocApi, normalizedItemId);
            if (result.BeforeBackpackCount <= 0)
                return EquipmentSlotEquipFailed(result, "missing-backpack-item", "Backpack does not contain " + normalizedItemId + ".");

            if (!TryGenerateNativeItem(normalizedItemId, 1, out object? item, out string generateReason, out string generateMessage))
                return EquipmentSlotEquipFailed(result, generateReason, generateMessage);

            if (!TryValidateExtraEquipmentSlotItem(item, out string displayName, out string skillId, out string validationReason, out string validationMessage))
                return EquipmentSlotEquipFailed(result, validationReason, validationMessage);
            result.DisplayName = displayName;

            if (!RecoverEquipmentSlotEntry(ownerId, entry, "replace before equip", saveAfterRecovery: false, out string recoveryMessage, out int recovered))
                return EquipmentSlotEquipFailed(result, "recover-existing-failed", recoveryMessage);
            result.RecoveredCount = recovered;

            if (!TryCostNativeBackpackItem(dolocApi, normalizedItemId, 1))
                return EquipmentSlotEquipFailed(result, "consume-failed", "DolocAPI.CostItem failed for " + normalizedItemId + " x1.");

            entry.ItemId = normalizedItemId;
            entry.DisplayName = displayName;
            entry.SkillId = skillId;
            entry.LastMessage = "Equipped " + displayName + " as attribute-only extra slot item.";
            entry.Applied = false;
            entry.NativeItem = null;
            entry.NativeFunction = null;

            string applyMessage = TryApplyStoredEquipmentSlotFunctions(ownerId, "equip " + entry.SlotId)
                ? "Applied native AgentEquipmentFunction for " + normalizedItemId + "."
                : "Stored item; native AgentEquipmentFunction will apply after equipment manager is available.";
            entry.LastMessage = entry.LastMessage + " " + applyMessage;
            SaveEquipmentSlotStorage(ownerId);
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("equip " + entry.SlotId, force: true);

            EquipmentSlotsState state = BuildEquipmentSlotsState(ownerId, options);
            state.LastEquippedSlotId = entry.SlotId;
            state.LastEquippedItemId = normalizedItemId;
            state.LastRecoveryMessage = entry.LastMessage;
            equipmentSlotStates[ownerId] = state;

            result.AfterBackpackCount = CountNativeBackpackItem(dolocApi, normalizedItemId);
            result.Success = true;
            result.Message = entry.LastMessage + " backpack=" + result.BeforeBackpackCount + "->" + result.AfterBackpackCount + ".";
            runtime.RuntimeMonitor.Log("EquipmentSlots equip OK owner=" + ownerId + " slot=" + entry.SlotId + " item=" + normalizedItemId + " display=" + displayName + " backpack=" + result.BeforeBackpackCount + "->" + result.AfterBackpackCount + " applied=" + entry.Applied + ".");
            runtime.SetHookStatus("Player.EquipmentSlotsApi", state.Status, "IEquipmentSlotsApi.EquipExtraSlot -> AgentEquipmentFunction", result.Message);
            return result;
        }

        public EquipmentSlotEquipResult UnequipExtraSlot(IManifest owner, string slotId, string reason)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            string ownerId = owner.UniqueID;
            EquipmentSlotsOptions options = GetEquipmentSlotsOptions(ownerId);
            EnsureEquipmentSlotStorageLoaded(ownerId);
            List<EquipmentSlotRuntimeEntry> entries = EnsureEquipmentSlotEntries(ownerId, options);
            EquipmentSlotRuntimeEntry? entry = FindEquipmentSlotEntry(entries, options, slotId);
            var result = new EquipmentSlotEquipResult
            {
                OwnerId = ownerId,
                SlotId = slotId ?? string.Empty,
                ItemId = entry?.ItemId ?? string.Empty,
                DisplayName = entry?.DisplayName ?? string.Empty
            };
            if (entry == null)
                return EquipmentSlotEquipFailed(result, "missing-slot", "Slot " + (slotId ?? string.Empty) + " is not available.");

            result.SlotId = entry.SlotId;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (!string.IsNullOrWhiteSpace(entry.ItemId))
                result.BeforeBackpackCount = CountNativeBackpackItem(dolocApi, entry.ItemId);

            if (!RecoverEquipmentSlotEntry(ownerId, entry, reason ?? "manual unequip", saveAfterRecovery: true, out string message, out int recoveredCount))
                return EquipmentSlotEquipFailed(result, "recover-failed", message);

            result.RecoveredCount = recoveredCount;
            if (!string.IsNullOrWhiteSpace(result.ItemId))
                result.AfterBackpackCount = CountNativeBackpackItem(dolocApi, result.ItemId);

            EquipmentSlotsState state = BuildEquipmentSlotsState(ownerId, options);
            state.LastRecoveryMessage = message;
            equipmentSlotStates[ownerId] = state;

            result.Success = true;
            result.Message = message;
            runtime.RuntimeMonitor.Log("EquipmentSlots unequip OK owner=" + ownerId + " slot=" + entry.SlotId + " item=" + result.ItemId + " recovered=" + recoveredCount + " reason=" + (reason ?? string.Empty) + ".");
            runtime.SetHookStatus("Player.EquipmentSlotsApi", state.Status, "IEquipmentSlotsApi.UnequipExtraSlot -> DolocAPI.TryPlaceInBackpack", message);
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("unequip " + entry.SlotId, force: true);
            return result;
        }

        EquipmentSlotsState IEquipmentSlotsApi.GetState(string uniqueId)
        {
            string ownerId = uniqueId ?? string.Empty;
            if (equipmentSlotStates.TryGetValue(ownerId, out EquipmentSlotsState state))
            {
                EquipmentSlotsOptions options = GetEquipmentSlotsOptions(ownerId);
                EquipmentSlotsState fresh = BuildEquipmentSlotsState(ownerId, options);
                fresh.StatsRefreshCount = state.StatsRefreshCount;
                fresh.LastRecoveryMessage = FirstText(state.LastRecoveryMessage, fresh.LastRecoveryMessage);
                fresh.LastEquippedSlotId = state.LastEquippedSlotId;
                fresh.LastEquippedItemId = state.LastEquippedItemId;
                equipmentSlotStates[ownerId] = fresh;
                return fresh;
            }

            return new EquipmentSlotsState
            {
                OwnerId = ownerId,
                IsConfigured = false,
                Status = "not-configured",
                LastRecoveryMessage = "No equipment-slot policy registered."
            };
        }

        public EquipmentSlotsRecoveryResult RecoverExtraSlotItems(IManifest owner, string reason)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            EnsureEquipmentSlotStorageLoaded(owner.UniqueID);
            bool success = RecoverEquipmentSlotEntries(owner.UniqueID, reason ?? "manual recovery", saveAfterRecovery: true, out EquipmentSlotsRecoveryResult result);
            EquipmentSlotsState state = BuildEquipmentSlotsState(owner.UniqueID, GetEquipmentSlotsOptions(owner.UniqueID));
            state.LastRecoveryMessage = result.Message;
            equipmentSlotStates[owner.UniqueID] = state;
            runtime.RuntimeMonitor.Log("Equipment slots recovery requested owner=" + owner.UniqueID + " " + state.LastRecoveryMessage);
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("recover " + owner.UniqueID, force: true);
            result.Success = success;
            return result;
        }

        BridgeFeatureStatus IEquipmentSlotsApi.GetStatus(string uniqueId)
        {
            string ownerId = uniqueId ?? string.Empty;
            return equipmentSlotOptions.ContainsKey(ownerId)
                ? new BridgeFeatureStatus(((IEquipmentSlotsApi)this).GetState(ownerId).Status, "Extra attribute-slot policy is registered while preserving vanilla visual slots; DTMAPI owns extra-slot storage, interactive player equipment strip rendering, native AgentEquipmentFunction application, and safe recovery through backpack/mail overflow.")
                : new BridgeFeatureStatus("not-configured", "No equipment-slot policy was registered for this mod.");
        }

        private EquipmentSlotsOptions GetEquipmentSlotsOptions(string ownerId)
        {
            return equipmentSlotOptions.TryGetValue(ownerId ?? string.Empty, out EquipmentSlotsOptions? options)
                ? options
                : new EquipmentSlotsOptions
                {
                    Enabled = false,
                    ExtraAttributeSlots = equipmentSlotEntries.TryGetValue(ownerId ?? string.Empty, out List<EquipmentSlotRuntimeEntry>? entries) ? entries.Count : 0,
                    SlotIdPrefix = "dtmapi.extra",
                    PreserveVanillaVisualSlots = true,
                    ExtraSlotsAffectVisuals = false,
                    SafeUnequipOnDisable = true,
                    AutoRecoverOnMissingMod = true
                };
        }

        private List<EquipmentSlotRuntimeEntry> EnsureEquipmentSlotEntries(string ownerId, EquipmentSlotsOptions options)
        {
            ownerId ??= string.Empty;
            if (!equipmentSlotEntries.TryGetValue(ownerId, out List<EquipmentSlotRuntimeEntry>? entries))
            {
                entries = new List<EquipmentSlotRuntimeEntry>();
                equipmentSlotEntries[ownerId] = entries;
            }

            int targetCount = Math.Max(0, options.Enabled ? options.ExtraAttributeSlots : Math.Max(options.ExtraAttributeSlots, entries.Count));
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].Index = i;
                entries[i].OwnerId = ownerId;
                if (string.IsNullOrWhiteSpace(entries[i].SlotId))
                    entries[i].SlotId = BuildEquipmentSlotId(options, i);
            }

            while (entries.Count < targetCount)
            {
                int index = entries.Count;
                entries.Add(new EquipmentSlotRuntimeEntry
                {
                    OwnerId = ownerId,
                    Index = index,
                    SlotId = BuildEquipmentSlotId(options, index),
                    LastMessage = "Empty DTMAPI extra equipment slot."
                });
            }

            if (options.Enabled && entries.Count > targetCount)
            {
                for (int i = targetCount; i < entries.Count; i++)
                    entries[i].LastMessage = "Slot is outside the current enabled range and will be recovered when safe recovery runs.";
            }

            return entries;
        }

        private static string BuildEquipmentSlotId(EquipmentSlotsOptions options, int index)
        {
            return FirstText(options.SlotIdPrefix, "dtmapi.extra") + "." + (index + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static EquipmentSlotRuntimeEntry? FindEquipmentSlotEntry(List<EquipmentSlotRuntimeEntry> entries, EquipmentSlotsOptions options, string slotId)
        {
            slotId = (slotId ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(slotId))
            {
                EquipmentSlotRuntimeEntry? exact = entries.FirstOrDefault(entry => entry.SlotId.Equals(slotId, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;
                if (int.TryParse(slotId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int oneBased) && oneBased > 0)
                    return entries.FirstOrDefault(entry => entry.Index == oneBased - 1);
            }

            return entries
                .Where(entry => entry.Index >= 0 && entry.Index < Math.Max(0, options.ExtraAttributeSlots))
                .FirstOrDefault(entry => string.IsNullOrWhiteSpace(entry.ItemId)) ??
                entries.FirstOrDefault(entry => entry.Index >= 0 && entry.Index < Math.Max(0, options.ExtraAttributeSlots));
        }

        private EquipmentSlotsState BuildEquipmentSlotsState(string ownerId, EquipmentSlotsOptions options)
        {
            List<EquipmentSlotRuntimeEntry> entries = equipmentSlotEntries.TryGetValue(ownerId ?? string.Empty, out List<EquipmentSlotRuntimeEntry>? existing)
                ? existing
                : new List<EquipmentSlotRuntimeEntry>();
            int enabledCount = options.Enabled ? Math.Max(0, options.ExtraAttributeSlots) : 0;
            int storedCount = entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId));
            int appliedCount = entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId) && entry.Applied);
            int pendingRecovery = entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId) && (!options.Enabled || entry.Index >= enabledCount));
            string status = !options.Enabled
                ? (storedCount > 0 ? "disabled-recovery-pending" : "disabled")
                : equipmentSlotsRuntimeHooksInstalled
                    ? (equipmentSlotsUiHooksInstalled || equipmentSlotsUiRendered ? "configured-experimental-player-ui-storage-stats-hook" : "configured-experimental-storage-stats-hook")
                    : "configured-experimental-ui-storage";

            EquipmentSlotsState previous = equipmentSlotStates.TryGetValue(ownerId ?? string.Empty, out EquipmentSlotsState? oldState)
                ? oldState
                : new EquipmentSlotsState();
            return new EquipmentSlotsState
            {
                OwnerId = ownerId ?? string.Empty,
                IsConfigured = options.Enabled,
                RuntimeUiHookInstalled = options.Enabled && (equipmentSlotsUiHooksInstalled || equipmentSlotsUiRendered),
                RuntimeStatsHookInstalled = equipmentSlotsRuntimeHooksInstalled,
                ExtraAttributeSlots = enabledCount,
                StoredItemCount = storedCount,
                AppliedItemCount = appliedCount,
                PreserveVanillaVisualSlots = options.PreserveVanillaVisualSlots,
                ExtraSlotsAffectVisuals = false,
                SafeUnequipOnDisable = options.SafeUnequipOnDisable,
                StatsRefreshCount = previous.StatsRefreshCount,
                PendingRecoveryCount = pendingRecovery,
                LastEquippedSlotId = previous.LastEquippedSlotId,
                LastEquippedItemId = previous.LastEquippedItemId,
                Status = status,
                LastRecoveryMessage = FirstText(previous.LastRecoveryMessage, storedCount == 0 ? "No DTMAPI extra-slot items are stored." : "Stored extra-slot item count=" + storedCount + ", applied=" + appliedCount + ".")
            };
        }

        private static EquipmentSlotInfo ToEquipmentSlotInfo(string ownerId, EquipmentSlotRuntimeEntry entry, EquipmentSlotsOptions options)
        {
            return new EquipmentSlotInfo
            {
                OwnerId = ownerId ?? string.Empty,
                SlotId = entry.SlotId,
                Index = entry.Index,
                ItemId = entry.ItemId,
                DisplayName = entry.DisplayName,
                IsOccupied = !string.IsNullOrWhiteSpace(entry.ItemId),
                IsApplied = entry.Applied,
                IsRecoverable = true,
                AttributeOnly = true,
                AffectsVisuals = false,
                LastMessage = FirstText(entry.LastMessage, string.IsNullOrWhiteSpace(entry.ItemId) ? "Empty attribute-only slot." : "Stored attribute-only item.")
            };
        }

        private void EnsureEquipmentSlotStorageLoaded(string ownerId)
        {
            ownerId ??= string.Empty;
            if (string.IsNullOrWhiteSpace(ownerId) || loadedEquipmentSlotStorageOwners.Contains(ownerId))
                return;

            EquipmentSlotSaveScope scope = GetEquipmentSlotSaveScope();
            if (!scope.HasArchive)
            {
                runtime.SetHookStatus("Player.EquipmentSlotsSaveTransaction", "pending", "DTMAPI protected equipment-slot storage", "Waiting for a loaded archive before reading equipment-slot sidecar storage for " + ownerId + ".");
                return;
            }

            loadedEquipmentSlotStorageOwners.Add(ownerId);
            string scopedPath = GetEquipmentSlotStoragePath(ownerId, scope);
            if (TryLoadEquipmentSlotStorageDocument(ownerId, scopedPath, scope, isLegacyGlobalStorage: false))
                return;

            string legacyPath = GetLegacyEquipmentSlotStoragePath(ownerId);
            if (TryLoadEquipmentSlotStorageDocument(ownerId, legacyPath, scope, isLegacyGlobalStorage: true))
            {
                migratedLegacyEquipmentSlotStorageOwners.Add(ownerId);
                SaveEquipmentSlotStorage(ownerId);
                runtime.RuntimeMonitor.Log("EquipmentSlots adopted legacy global storage owner=" + ownerId + " legacyPath=" + legacyPath + " saveScope=" + scope.ScopeKey + "; it will be rewritten as per-save protected storage after the next native SaveGame.");
            }
        }

        private void SaveEquipmentSlotStorage(string ownerId)
        {
            ownerId ??= string.Empty;
            if (string.IsNullOrWhiteSpace(ownerId))
                return;

            dirtyEquipmentSlotStorageOwners.Add(ownerId);
            runtime.RuntimeMonitor.Log("EquipmentSlots storage marked dirty owner=" + ownerId + "; waiting for native SaveGame before writing DTMAPI sidecar storage.");
            runtime.SetHookStatus("Player.EquipmentSlotsSaveTransaction", "dirty", "runtime mutation -> SaveGame postfix", "Equipment-slot sidecar storage for " + ownerId + " is pending native SaveGame; unsaved exit will discard the sidecar mutation.");
        }

        private void PersistEquipmentSlotStorage(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            if (string.IsNullOrWhiteSpace(ownerId))
                return;

            EquipmentSlotSaveScope scope = GetEquipmentSlotSaveScope();
            if (!scope.HasArchive)
            {
                runtime.RuntimeMonitor.Log("EquipmentSlots storage save skipped owner=" + ownerId + " reason=" + (reason ?? string.Empty) + " because no loaded archive is available.", LogLevel.Warn);
                runtime.SetHookStatus("Player.EquipmentSlotsSaveTransaction", "blocked", "DTMAPI protected equipment-slot storage", "Could not persist equipment-slot sidecar storage for " + ownerId + " without a loaded archive.");
                return;
            }

            List<EquipmentSlotRuntimeEntry> entries = equipmentSlotEntries.TryGetValue(ownerId, out List<EquipmentSlotRuntimeEntry>? existing)
                ? existing
                : new List<EquipmentSlotRuntimeEntry>();
            int totalSlots = Math.Max(entries.Count, entries.Count == 0 ? 0 : entries.Max(entry => entry.Index) + 1);
            var document = new EquipmentSlotStorageDocument
            {
                SchemaVersion = EquipmentSlotProtectedStoragePolicy.SchemaVersion,
                OwnerId = ownerId,
                StorageScope = scope.ScopeKey,
                ArchiveIndex = scope.ArchiveIndex,
                PlayerName = scope.PlayerName,
                CustomPlayerName = scope.CustomPlayerName,
                CurrentScene = scope.CurrentScene,
                SavedTotalGameSeconds = scope.TotalGameSeconds,
                SavedAt = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture),
                Slots = entries.Select(entry => new EquipmentSlotStorageEntry
                {
                    Index = entry.Index,
                    TailIndexFromEnd = EquipmentSlotProtectedStoragePolicy.GetTailIndexFromEnd(entry.Index, totalSlots),
                    SlotId = entry.SlotId,
                    ItemId = entry.ItemId,
                    DisplayName = entry.DisplayName,
                    SkillId = entry.SkillId,
                    LastMessage = entry.LastMessage
                }).ToList()
            };

            string path = GetEquipmentSlotStoragePath(ownerId, scope);
            try
            {
                WriteJson(path, document);
                ArchiveMigratedLegacyEquipmentSlotStorage(ownerId);
                runtime.RuntimeMonitor.Log("EquipmentSlots protected storage persisted owner=" + ownerId + " reason=" + (reason ?? string.Empty) + " scope=" + scope.ScopeKey + " path=" + path + " storedItems=" + entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId)) + ".");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots storage save failed for " + ownerId + ".", ex.ToString());
                runtime.RuntimeMonitor.Log("EquipmentSlots storage save failed owner=" + ownerId + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
        }

        private bool TryLoadEquipmentSlotStorageDocument(string ownerId, string path, EquipmentSlotSaveScope scope, bool isLegacyGlobalStorage)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            try
            {
                EquipmentSlotStorageDocument? document = ReadJson<EquipmentSlotStorageDocument>(path);
                if (document?.Slots == null)
                    return false;

                string documentOwner = FirstText(document.OwnerId, ownerId);
                if (!documentOwner.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                {
                    runtime.RuntimeMonitor.Log("EquipmentSlots storage skipped path=" + path + " owner=" + ownerId + " documentOwner=" + documentOwner + ".", LogLevel.Warn);
                    return false;
                }

                if (!isLegacyGlobalStorage && !EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(
                    document.ArchiveIndex,
                    document.PlayerName,
                    document.CustomPlayerName,
                    document.SavedTotalGameSeconds,
                    scope.ArchiveIndex,
                    scope.PlayerName,
                    scope.CustomPlayerName,
                    scope.TotalGameSeconds,
                    out string compatibilityReason))
                {
                    runtime.RuntimeMonitor.Log("EquipmentSlots protected storage skipped owner=" + ownerId + " path=" + path + " reason=" + compatibilityReason + ".", LogLevel.Warn);
                    runtime.SetHookStatus("Player.EquipmentSlotsSaveTransaction", "blocked", "DTMAPI protected equipment-slot storage identity guard", "Skipped incompatible equipment-slot storage for " + ownerId + ": " + compatibilityReason);
                    return false;
                }

                var entries = new List<EquipmentSlotRuntimeEntry>();
                for (int i = 0; i < document.Slots.Count; i++)
                {
                    EquipmentSlotStorageEntry slot = document.Slots[i] ?? new EquipmentSlotStorageEntry();
                    entries.Add(new EquipmentSlotRuntimeEntry
                    {
                        OwnerId = ownerId,
                        Index = slot.Index >= 0 ? slot.Index : i,
                        SlotId = FirstText(slot.SlotId, "dtmapi.extra." + (i + 1).ToString(CultureInfo.InvariantCulture)),
                        ItemId = slot.ItemId ?? string.Empty,
                        DisplayName = slot.DisplayName ?? string.Empty,
                        SkillId = slot.SkillId ?? string.Empty,
                        LastMessage = FirstText(slot.LastMessage, isLegacyGlobalStorage ? "Loaded legacy DTMAPI extra-slot item." : "Loaded stored DTMAPI extra-slot item.")
                    });
                }

                equipmentSlotEntries[ownerId] = entries.OrderBy(entry => entry.Index).ToList();
                runtime.RuntimeMonitor.Log("EquipmentSlots " + (isLegacyGlobalStorage ? "legacy global" : "protected per-save") + " storage loaded owner=" + ownerId + " scope=" + scope.ScopeKey + " path=" + path + " slots=" + entries.Count + " storedItems=" + entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId)) + ".");
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots storage load failed for " + ownerId + ".", ex.ToString());
                runtime.RuntimeMonitor.Log("EquipmentSlots storage load failed owner=" + ownerId + " path=" + path + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                return false;
            }
        }

        private string GetEquipmentSlotStoragePath(string ownerId, EquipmentSlotSaveScope scope)
        {
            string scopeKey = EquipmentSlotProtectedStoragePolicy.MakeSafePathSegment(scope.ScopeKey);
            return Path.Combine(GetEquipmentSlotStorageDirectory(scope), "equipment-slots-" + MakeSafeFileName(ownerId ?? "unknown") + ".json");
        }

        private string GetEquipmentSlotStorageDirectory(EquipmentSlotSaveScope scope)
        {
            string scopeKey = EquipmentSlotProtectedStoragePolicy.MakeSafePathSegment(scope.ScopeKey);
            return Path.Combine(runtime.Paths.ConfigPath, "protected-items", "equipment-slots", scopeKey);
        }

        private string GetLegacyEquipmentSlotStoragePath(string ownerId)
        {
            return Path.Combine(runtime.Paths.ConfigPath, "equipment-slots-" + MakeSafeFileName(ownerId ?? "unknown") + ".json");
        }

        private void ArchiveMigratedLegacyEquipmentSlotStorage(string ownerId)
        {
            if (!migratedLegacyEquipmentSlotStorageOwners.Remove(ownerId ?? string.Empty))
                return;

            string legacyPath = GetLegacyEquipmentSlotStoragePath(ownerId ?? string.Empty);
            if (!File.Exists(legacyPath))
                return;

            try
            {
                string archivePath = legacyPath + ".migrated-" + DateTimeOffset.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                File.Move(legacyPath, archivePath);
                runtime.RuntimeMonitor.Log("EquipmentSlots legacy global storage archived owner=" + (ownerId ?? string.Empty) + " legacyPath=" + legacyPath + " archivePath=" + archivePath + ".");
            }
            catch (Exception ex)
            {
                runtime.RuntimeMonitor.Log("EquipmentSlots legacy global storage archive failed owner=" + (ownerId ?? string.Empty) + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
        }

        private EquipmentSlotSaveScope GetEquipmentSlotSaveScope()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? archive = dolocApi == null ? null : ReadStaticMember(dolocApi, "archiveHandle");
            if (archive == null)
                return EquipmentSlotSaveScope.Missing;

            int archiveIndex = ReadIntMember(archive, "archiveIndex", -1);
            object? farmData = ReadMember(archive, "farmData");
            object? agentData = farmData == null ? null : ReadMember(farmData, "agentData");
            object? baseDataOnLoad = ReadMember(archive, "baseDataOnLoad");
            string customPlayerName = FirstText(
                agentData == null ? string.Empty : ReadStringMember(agentData, "customPlayerName"),
                baseDataOnLoad == null ? string.Empty : ReadStringMember(baseDataOnLoad, "customPlayerName"));
            string playerName = FirstText(
                agentData == null ? string.Empty : ReadStringMember(agentData, "playerName"),
                customPlayerName);
            string currentScene = FirstText(
                baseDataOnLoad == null ? string.Empty : ReadStringMember(baseDataOnLoad, "currentScene"),
                farmData == null ? string.Empty : ReadStringMember(farmData, "currentSceneName"));
            long totalGameSeconds = baseDataOnLoad == null ? -1 : ReadLongMember(baseDataOnLoad, "totalGameSeconds", -1);
            return new EquipmentSlotSaveScope(
                archiveIndex,
                EquipmentSlotProtectedStoragePolicy.BuildSaveScopeKey(archiveIndex),
                playerName,
                customPlayerName,
                currentScene,
                totalGameSeconds);
        }

        private static long ReadLongMember(object instance, string name, long fallback)
        {
            object? value = ReadMember(instance, name);
            return value == null ? fallback : Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        private static string MakeSafeFileName(string value)
        {
            return EquipmentSlotProtectedStoragePolicy.MakeSafePathSegment(value);
        }

        private static T? ReadJson<T>(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                });
                object? value = serializer.ReadObject(stream);
                return value is T typed ? typed : default;
            }
        }

        private static void WriteJson<T>(string path, T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            using (FileStream stream = File.Create(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                });
                serializer.WriteObject(stream, value);
            }
        }

        private bool TryApplyStoredEquipmentSlotFunctions(string ownerId, string reason)
        {
            if (equipmentSlotsApplyingFunctions)
                return false;
            if (!equipmentSlotEntries.TryGetValue(ownerId ?? string.Empty, out List<EquipmentSlotRuntimeEntry>? entries))
                return false;

            object? manager = GetNativeAgentEquipmentManager();
            if (manager == null)
                return false;

            bool changed = false;
            bool appliedAny = false;
            equipmentSlotsApplyingFunctions = true;
            try
            {
                foreach (EquipmentSlotRuntimeEntry entry in entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.ItemId))
                        continue;
                    if (entry.Applied && entry.NativeItem != null && NativeEquipmentFunctionsContains(manager, entry.NativeItem))
                    {
                        appliedAny = true;
                        continue;
                    }

                    RemoveEquipmentSlotFunction(manager, entry);
                    if (TryApplyEquipmentSlotFunction(manager, entry, out string message))
                    {
                        entry.LastMessage = message;
                        changed = true;
                        appliedAny = true;
                    }
                    else
                    {
                        entry.LastMessage = message;
                        runtime.RuntimeMonitor.Log("EquipmentSlots apply skipped owner=" + ownerId + " slot=" + entry.SlotId + " item=" + entry.ItemId + " reason=" + message, LogLevel.Warn);
                    }
                }

                if (changed)
                {
                    InvokeNativeReloadParams(manager);
                    SaveEquipmentSlotStorage(ownerId ?? string.Empty);
                    runtime.RuntimeMonitor.Log("EquipmentSlots applied stored functions owner=" + ownerId + " reason=" + (reason ?? string.Empty) + " applied=" + entries.Count(entry => entry.Applied) + "/" + entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId)) + ".");
                }
            }
            finally
            {
                equipmentSlotsApplyingFunctions = false;
            }

            if (equipmentSlotOptions.TryGetValue(ownerId ?? string.Empty, out EquipmentSlotsOptions? options))
                equipmentSlotStates[ownerId ?? string.Empty] = BuildEquipmentSlotsState(ownerId ?? string.Empty, options);
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("apply " + (reason ?? string.Empty), force: changed);
            return appliedAny;
        }

        private void RecoverOrphanEquipmentSlotsIfNeeded()
        {
            if (equipmentSlotsOrphanRecoveryChecked)
                return;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null || ReadStaticMember(dolocApi, "archiveHandle") == null)
                return;

            EquipmentSlotSaveScope scope = GetEquipmentSlotSaveScope();
            if (!scope.HasArchive)
                return;

            equipmentSlotsOrphanRecoveryChecked = true;
            if (!Directory.Exists(runtime.Paths.ConfigPath))
                return;

            var scannedPaths = new List<string>();
            string scopedDirectory = GetEquipmentSlotStorageDirectory(scope);
            if (Directory.Exists(scopedDirectory))
                scannedPaths.AddRange(Directory.GetFiles(scopedDirectory, "equipment-slots-*.json", SearchOption.TopDirectoryOnly));
            scannedPaths.AddRange(Directory.GetFiles(runtime.Paths.ConfigPath, "equipment-slots-*.json", SearchOption.TopDirectoryOnly));

            var recoveredOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in scannedPaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    EquipmentSlotStorageDocument? document = ReadJson<EquipmentSlotStorageDocument>(path);
                    string ownerId = document?.OwnerId ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(ownerId) || equipmentSlotOptions.ContainsKey(ownerId) || recoveredOwners.Contains(ownerId))
                        continue;

                    loadedEquipmentSlotStorageOwners.Remove(ownerId);
                    bool isLegacyGlobalStorage = Path.GetDirectoryName(path)?.Equals(runtime.Paths.ConfigPath, StringComparison.OrdinalIgnoreCase) == true;
                    loadedEquipmentSlotStorageOwners.Add(ownerId);
                    if (!TryLoadEquipmentSlotStorageDocument(ownerId, path, scope, isLegacyGlobalStorage))
                    {
                        loadedEquipmentSlotStorageOwners.Remove(ownerId);
                        continue;
                    }

                    if (isLegacyGlobalStorage)
                        migratedLegacyEquipmentSlotStorageOwners.Add(ownerId);

                    if (RecoverEquipmentSlotEntries(ownerId, "orphan storage without registered mod", saveAfterRecovery: true, out EquipmentSlotsRecoveryResult result))
                    {
                        recoveredOwners.Add(ownerId);
                        runtime.RuntimeMonitor.Log("EquipmentSlots orphan recovery OK owner=" + ownerId + " recovered=" + result.RecoveredCount + " message=" + result.Message + ".");
                        runtime.SetHookStatus("Player.EquipmentSlotsApi", "orphan-recovery-ok", "DTMAPI protected equipment-slot storage scan", "Recovered missing-mod extra-slot storage for " + ownerId + " scope=" + scope.ScopeKey + ": " + result.Message);
                    }
                    else
                    {
                        runtime.RuntimeMonitor.Log("EquipmentSlots orphan recovery failed owner=" + ownerId + " message=" + result.Message + ".", LogLevel.Warn);
                        runtime.SetHookStatus("Player.EquipmentSlotsApi", "orphan-recovery-failed", "DTMAPI protected equipment-slot storage scan", result.Message);
                    }
                }
                catch (Exception ex)
                {
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots orphan recovery scan failed for " + path + ".", ex.ToString());
                }
            }
        }

        private static object? GetNativeAgentEquipmentManager()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? direct = ReadStaticMember(dolocApi, "AgentEquipmentManager");
            if (direct != null)
                return direct;

            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? farmData = archive == null ? null : ReadMember(archive, "farmData");
            object? agentData = farmData == null ? null : ReadMember(farmData, "agentData");
            return agentData == null ? null : ReadMember(agentData, "agentEquipment");
        }

        private bool TryApplyEquipmentSlotFunction(object manager, EquipmentSlotRuntimeEntry entry, out string message)
        {
            message = string.Empty;
            if (!TryGenerateNativeItem(entry.ItemId, 1, out object? item, out string reason, out string generateMessage))
            {
                message = reason + ": " + generateMessage;
                return false;
            }

            if (!TryValidateExtraEquipmentSlotItem(item, out string displayName, out string skillId, out string validationReason, out string validationMessage))
            {
                message = validationReason + ": " + validationMessage;
                return false;
            }

            Type? functionType = ResolveType("DolocTown.AgentEquipmentFunction, Assembly-CSharp");
            MethodInfo? create = functionType?.GetMethod("CreateAgentEquipmentFunction", BindingFlags.Public | BindingFlags.Static);
            if (create == null)
            {
                message = "missing-function-factory: AgentEquipmentFunction.CreateAgentEquipmentFunction was not found.";
                return false;
            }

            object?[] args = { item, manager, skillId, null };
            object? ok = create.Invoke(null, args);
            object? function = args[3];
            if (!(ok is bool success) || !success || function == null)
            {
                message = "function-create-failed: Could not create AgentEquipmentFunction for " + entry.ItemId + " skill=" + skillId + ".";
                return false;
            }

            IDictionary? functions = GetNativeEquipmentFunctions(manager);
            if (functions == null)
            {
                TryDisposeEquipmentFunction(function);
                message = "missing-functions-dictionary: AgentEquipmentManager.functions was not available.";
                return false;
            }

            functions[item] = function;
            entry.NativeItem = item;
            entry.NativeFunction = function;
            entry.DisplayName = displayName;
            entry.SkillId = skillId;
            entry.Applied = true;
            message = "Applied " + displayName + " (" + entry.ItemId + ") as attribute-only AgentEquipmentFunction skill=" + skillId + ".";
            return true;
        }

        private static bool NativeEquipmentFunctionsContains(object manager, object item)
        {
            IDictionary? functions = GetNativeEquipmentFunctions(manager);
            return functions != null && functions.Contains(item);
        }

        private static IDictionary? GetNativeEquipmentFunctions(object manager)
        {
            object? functions = ReadMember(manager, "functions");
            return functions as IDictionary;
        }

        private static void RemoveEquipmentSlotFunction(object manager, EquipmentSlotRuntimeEntry entry)
        {
            IDictionary? functions = GetNativeEquipmentFunctions(manager);
            if (functions != null && entry.NativeItem != null && functions.Contains(entry.NativeItem))
                functions.Remove(entry.NativeItem);
            if (entry.NativeFunction != null)
                TryDisposeEquipmentFunction(entry.NativeFunction);
            entry.NativeItem = null;
            entry.NativeFunction = null;
            entry.Applied = false;
        }

        private static void TryDisposeEquipmentFunction(object function)
        {
            try
            {
                FindMethodInHierarchy(function.GetType(), "Dispose", 0)?.Invoke(function, null);
            }
            catch
            {
            }
        }

        private static void InvokeNativeReloadParams(object manager)
        {
            try
            {
                FindMethodInHierarchy(manager.GetType(), "ReloadParams", 0)?.Invoke(manager, null);
            }
            catch
            {
            }
        }

        private static bool TryValidateExtraEquipmentSlotItem(object? item, out string displayName, out string skillId, out string reason, out string message)
        {
            displayName = string.Empty;
            skillId = string.Empty;
            reason = string.Empty;
            message = string.Empty;
            if (item == null)
            {
                reason = "missing-item";
                message = "Generated item is null.";
                return false;
            }

            Type itemType = item.GetType();
            object? proto = ReadMember(item, "proto");
            object? function = proto == null ? null : ReadMember(proto, "Function");
            string functionType = function == null ? string.Empty : function.GetType().FullName ?? function.GetType().Name;
            bool isPassive = IsTypeOrBase(itemType, "DolocTown.ItemPassive");
            bool isHat = IsTypeOrBase(itemType, "DolocTown.ItemHat");
            if (!isPassive && !isHat)
            {
                reason = "not-attribute-equipment";
                message = "Only passive attribute equipment or hats with equipment skills can be placed in DTMAPI extra slots; " + ReadStringMember(item, "name") + " is " + itemType.FullName + ".";
                return false;
            }

            if (isPassive && (function == null || (functionType.IndexOf("ItemFunctionPassive", StringComparison.OrdinalIgnoreCase) < 0 && functionType.IndexOf("ItemFunctionHerbPackage", StringComparison.OrdinalIgnoreCase) < 0)))
            {
                reason = "not-attribute-passive";
                message = "Extra slots are attribute-only and require ItemFunctionPassive/ItemFunctionHerbPackage; item=" + ReadStringMember(item, "name") + ".";
                return false;
            }

            if (isHat && (function == null || functionType.IndexOf("ItemFunctionHat", StringComparison.OrdinalIgnoreCase) < 0))
            {
                reason = "not-attribute-hat";
                message = "Extra hat slots require an ItemFunctionHat/ItemFunctionHatShield function; item=" + ReadStringMember(item, "name") + ".";
                return false;
            }

            object? hatInfo = isHat && function != null ? ReadMember(function, "HatId_Ref") : null;
            skillId = isHat && hatInfo != null ? ReadStringMember(hatInfo, "Skill") : ReadStringMember(function!, "Skill");
            if (string.IsNullOrWhiteSpace(skillId))
            {
                reason = "missing-skill";
                message = (isHat ? "Hat item " : "Passive item ") + ReadStringMember(item, "name") + " has no equipment skill.";
                return false;
            }

            displayName = FirstText(proto == null ? string.Empty : ReadStringMember(proto, "Title"), ReadStringMember(item, "name"));
            return true;
        }

        private bool RecoverEquipmentSlotEntries(string ownerId, string reason, bool saveAfterRecovery, out EquipmentSlotsRecoveryResult result)
        {
            result = new EquipmentSlotsRecoveryResult
            {
                OwnerId = ownerId ?? string.Empty
            };
            EnsureEquipmentSlotStorageLoaded(ownerId ?? string.Empty);
            if (!equipmentSlotEntries.TryGetValue(ownerId ?? string.Empty, out List<EquipmentSlotRuntimeEntry>? entries))
            {
                result.Success = true;
                result.Message = "No DTMAPI extra-slot storage exists for " + (ownerId ?? string.Empty) + ".";
                return true;
            }

            bool success = true;
            int recovered = 0;
            var messages = new List<string>();
            foreach (EquipmentSlotRuntimeEntry entry in EquipmentSlotProtectedStoragePolicy.OrderTailFirst(entries, entry => entry.Index))
            {
                if (string.IsNullOrWhiteSpace(entry.ItemId))
                    continue;
                if (RecoverEquipmentSlotEntry(ownerId ?? string.Empty, entry, reason, saveAfterRecovery: false, out string message, out int entryRecovered))
                {
                    recovered += entryRecovered;
                    messages.Add(message);
                }
                else
                {
                    success = false;
                    messages.Add(message);
                }
            }

            if (saveAfterRecovery)
                SaveEquipmentSlotStorage(ownerId ?? string.Empty);
            object? manager = GetNativeAgentEquipmentManager();
            if (manager != null && recovered > 0)
                InvokeNativeReloadParams(manager);

            result.Success = success;
            result.RecoveredCount = recovered;
            result.FailureReason = success ? string.Empty : "recover-partial-failed";
            result.Message = messages.Count == 0
                ? "Recovery requested reason=" + (reason ?? string.Empty) + "; no DTMAPI extra-slot items were stored."
                : string.Join(" | ", messages);
            return success;
        }

        private bool RecoverEquipmentSlotEntry(string ownerId, EquipmentSlotRuntimeEntry entry, string reason, bool saveAfterRecovery, out string message, out int recoveredCount)
        {
            recoveredCount = 0;
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
            {
                message = "Slot is already empty.";
                return true;
            }

            string itemId = entry.ItemId;
            string display = FirstText(entry.DisplayName, itemId);
            object? manager = GetNativeAgentEquipmentManager();
            if (manager != null)
                RemoveEquipmentSlotFunction(manager, entry);

            if (!TryPlaceNativeBackpackItem(itemId, 1, sendEmailOnOverflow: true, out string placeMessage))
            {
                entry.LastMessage = "Recovery failed for " + display + ": " + placeMessage;
                message = entry.LastMessage;
                return false;
            }

            entry.ItemId = string.Empty;
            entry.DisplayName = string.Empty;
            entry.SkillId = string.Empty;
            entry.LastMessage = "Recovered " + display + " from DTMAPI extra slot reason=" + (reason ?? string.Empty) + ". " + placeMessage;
            entry.Applied = false;
            entry.NativeItem = null;
            entry.NativeFunction = null;
            recoveredCount = 1;
            message = entry.LastMessage;

            if (saveAfterRecovery)
                SaveEquipmentSlotStorage(ownerId ?? string.Empty);
            if (manager != null)
                InvokeNativeReloadParams(manager);
            return true;
        }

        private static EquipmentSlotEquipResult EquipmentSlotEquipFailed(EquipmentSlotEquipResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static int CountNativeBackpackItem(Type? dolocApi, string itemId)
        {
            if (dolocApi == null || string.IsNullOrWhiteSpace(itemId))
                return 0;
            return InvokeInt(dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null), null, new object?[] { itemId, false }, 0);
        }

        private static bool TryCostNativeBackpackItem(Type dolocApi, string itemId, int count)
        {
            MethodInfo? cost = dolocApi.GetMethod("CostItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
            object? result = cost?.Invoke(null, new object?[] { itemId, Math.Max(1, count), false });
            return result is bool ok && ok;
        }

        private static bool TryPlaceNativeBackpackItem(string itemId, int count, bool sendEmailOnOverflow, out string message)
        {
            message = string.Empty;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? tryPlaceInBackpack = dolocApi?.GetMethod("TryPlaceInBackpack", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
            if (tryPlaceInBackpack == null)
            {
                message = "DolocAPI.TryPlaceInBackpack(string,int,bool) was not found.";
                return false;
            }

            object? placed = tryPlaceInBackpack.Invoke(null, new object?[] { itemId, Math.Max(1, count), sendEmailOnOverflow });
            if (!(placed is bool ok) || !ok)
            {
                message = "DolocAPI.TryPlaceInBackpack returned false for " + itemId + " x" + count + ".";
                return false;
            }

            message = "Returned " + itemId + " x" + count + " through native backpack placement" + (sendEmailOnOverflow ? " with email overflow enabled." : ".");
            return true;
        }

        internal string GetEquipmentSlotsStateSummaryForSmoke(string ownerId)
        {
            EquipmentSlotsState state = ((IEquipmentSlotsApi)this).GetState(ownerId ?? string.Empty);
            IReadOnlyList<EquipmentSlotInfo> slots = GetSlots(ownerId ?? string.Empty);
            return "status=" + state.Status +
                ", extra=" + state.ExtraAttributeSlots +
                ", stored=" + state.StoredItemCount +
                ", applied=" + state.AppliedItemCount +
                ", pendingRecovery=" + state.PendingRecoveryCount +
                ", uiRendered=" + equipmentSlotsUiRendered +
                ", ui={" + equipmentSlotsUiLastSummary + "}" +
                ", slots=" + string.Join(";", slots.Select(slot => slot.SlotId + "=" + (slot.IsOccupied ? slot.ItemId + (slot.IsApplied ? "[applied]" : "[stored]") : "empty")));
        }

        internal void ApplyEquipmentSlotsAfterReloadParams(object manager)
        {
            if (manager == null || equipmentSlotStates.Count == 0)
                return;

            foreach (KeyValuePair<string, EquipmentSlotsState> entry in equipmentSlotStates.ToArray())
            {
                EquipmentSlotsState state = entry.Value;
                if (!state.IsConfigured)
                    continue;
                state.RuntimeStatsHookInstalled = equipmentSlotsRuntimeHooksInstalled;
                state.StatsRefreshCount++;
                state.StoredItemCount = equipmentSlotEntries.TryGetValue(entry.Key, out List<EquipmentSlotRuntimeEntry>? entries)
                    ? entries.Count(slot => !string.IsNullOrWhiteSpace(slot.ItemId))
                    : 0;
                state.AppliedItemCount = equipmentSlotEntries.TryGetValue(entry.Key, out entries)
                    ? entries.Count(slot => !string.IsNullOrWhiteSpace(slot.ItemId) && slot.Applied)
                    : 0;
                state.Status = equipmentSlotsRuntimeHooksInstalled
                    ? (equipmentSlotsUiHooksInstalled || equipmentSlotsUiRendered ? "configured-experimental-player-ui-storage-stats-hook" : "configured-experimental-storage-stats-hook")
                    : state.Status;
                state.LastRecoveryMessage = "Native equipment params refreshed; extra slots are attribute-only and preserve vanilla visual slots. Stored extra-slot item count=" + state.StoredItemCount + ", applied=" + state.AppliedItemCount + ".";
                equipmentSlotStates[entry.Key] = state;
                if (state.StatsRefreshCount <= 1 || equipmentSlotOptions.TryGetValue(entry.Key, out EquipmentSlotsOptions? options) && options.VerboseLogging)
                    runtime.RuntimeMonitor.Log("EquipmentSlots stats refresh owner=" + entry.Key + " extraSlots=" + state.ExtraAttributeSlots + " stored=" + state.StoredItemCount + " applied=" + state.AppliedItemCount + " preserveVanillaVisualSlots=" + state.PreserveVanillaVisualSlots + " visualsFromExtraSlots=" + state.ExtraSlotsAffectVisuals + " refreshCount=" + state.StatsRefreshCount + ".");
            }

            runtime.SetHookStatus("Player.EquipmentSlotsApi", equipmentSlotsUiHooksInstalled || equipmentSlotsUiRendered ? "configured-experimental-player-ui-storage-stats-hook" : "configured-experimental-storage-stats-hook", "AgentEquipmentManager.ReloadParams postfix", "Native equipment params refresh observed for DTMAPI extra-slot policy; DTMAPI owns extra-slot storage and populated recovery.");
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("AgentEquipmentManager.ReloadParams", force: true);
        }

        internal bool RenderEquipmentSlotsUiForAccessoriesBar(object accessoriesBar, string reason)
        {
            return RenderEquipmentSlotsUi(accessoriesBar, reason, force: true);
        }

        internal bool RenderEquipmentSlotsUiForCurrentAccessoriesBar(string reason, bool force)
        {
            if (!HasEnabledEquipmentSlots())
                return false;
            if (!force && (DateTimeOffset.Now - lastEquipmentSlotsUiRefreshAt).TotalSeconds < 0.5)
                return equipmentSlotsUiRendered;

            object? accessoriesBar = FindUnityObjectOfType("DolocTown.UI.AccessoriesBar, Assembly-CSharp");
            if (accessoriesBar == null)
                return false;
            return RenderEquipmentSlotsUi(accessoriesBar, reason, force);
        }

        internal string CaptureEquipmentSlotsUiEvidenceForSmoke(string reason)
        {
            bool rendered = RenderEquipmentSlotsUiForCurrentAccessoriesBar(reason ?? "smoke", force: true);
            if (!rendered && !equipmentSlotsUiRendered)
                throw new InvalidOperationException("DTMAPI extra equipment slot UI surface was not rendered before smoke evidence capture.");

            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string evidenceDir = Path.Combine(runtime.Paths.EvidencePath, "EQUIPMENT-SLOTS-UI", timestamp);
            Directory.CreateDirectory(evidenceDir);
            string screenshotPath = Path.Combine(evidenceDir, "equipment-slots-ui.png");
            bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
            string summary = "rendered=" + equipmentSlotsUiRendered +
                ", " + equipmentSlotsUiLastSummary +
                ", screenshot=" + (screenshotRequested ? screenshotPath : "unavailable");
            File.WriteAllText(Path.Combine(evidenceDir, "summary.txt"),
                "Captured=" + DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture) + Environment.NewLine +
                "Reason=" + (reason ?? string.Empty) + Environment.NewLine +
                "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                "Screenshot=" + screenshotPath + Environment.NewLine +
                "Summary=" + summary + Environment.NewLine);
            equipmentSlotsUiEvidenceRecorded = true;
            runtime.RuntimeMonitor.Log("EquipmentSlots UI evidence " + (screenshotRequested ? "OK" : "unavailable") + " " + summary + ".");
            runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsUi", screenshotRequested ? "verified" : "pending", "AccessoriesBar cloned DTMAPI extra slots + UnityEngine.ScreenCapture", summary);
            return summary;
        }

        private bool RenderEquipmentSlotsUi(object accessoriesBar, string reason, bool force)
        {
            if (accessoriesBar == null || !HasEnabledEquipmentSlots())
                return false;
            if (!force && (DateTimeOffset.Now - lastEquipmentSlotsUiRefreshAt).TotalSeconds < 0.5)
                return equipmentSlotsUiRendered;

            IReadOnlyList<EquipmentSlotRuntimeEntry> visibleSlots = BuildEquipmentSlotsForUi();
            if (visibleSlots.Count == 0)
                return false;

            try
            {
                object? sourceSlot = ReadMember(accessoriesBar, "passiveItem2") ?? ReadMember(accessoriesBar, "passiveItem1") ?? ReadMember(accessoriesBar, "positiveItem");
                object? sourceGameObject = sourceSlot == null ? null : ReadMember(sourceSlot, "gameObject");
                object? sourceTransform = sourceGameObject == null ? null : ReadMember(sourceGameObject, "transform");
                object? parent = sourceTransform == null ? null : ReadMember(sourceTransform, "parent");
                if (sourceSlot == null || sourceGameObject == null || sourceTransform == null || parent == null)
                    return false;

                ClearEquipmentSlotsUi(parent);

                int rendered = 0;
                int occupied = 0;
                int interactive = 0;
                int hoverable = 0;
                var renderedNames = new List<string>();
                foreach (EquipmentSlotRuntimeEntry entry in visibleSlots.Take(6))
                {
                    object? cloneSlot = CloneUnityObject(sourceSlot);
                    object? clone = cloneSlot == null ? null : ReadMember(cloneSlot, "gameObject");
                    if (cloneSlot == null || clone == null)
                    {
                        if (!equipmentSlotsUiCloneDiagnosticLogged)
                        {
                            equipmentSlotsUiCloneDiagnosticLogged = true;
                            runtime.RuntimeMonitor.Log("EquipmentSlots UI clone diagnostic failed sourceSlotType=" + sourceSlot.GetType().FullName +
                                " sourceGameObjectType=" + sourceGameObject.GetType().FullName +
                                " cloneSlotType=" + (cloneSlot == null ? "null" : cloneSlot.GetType().FullName) +
                                " cloneType=" + (clone == null ? "null" : clone.GetType().FullName) + ".");
                        }
                        continue;
                    }

                    SetMemberValue(clone, "name", "DTMAPI.ExtraEquipmentSlot." + rendered);
                    SetActive(clone, false);
                    object? cloneTransform = ReadMember(clone, "transform");
                    if (cloneTransform == null)
                    {
                        DestroyUnityObject(clone);
                        continue;
                    }

                    SetParent(cloneTransform, parent, worldPositionStays: false);
                    PositionEquipmentSlotUiClone(sourceTransform, cloneTransform, rendered);
                    object? icon = ResolveEquipmentSlotIcon(entry);
                    if (icon != null)
                        occupied++;
                    MethodInfo? render = cloneSlot == null ? null : FindMethodInHierarchy(cloneSlot.GetType(), "Render", 1);
                    render?.Invoke(cloneSlot, new object?[] { icon });

                    if (cloneSlot != null && ConfigureEquipmentSlotUiClone(cloneSlot, entry, out bool hoverHooked))
                    {
                        interactive++;
                        if (hoverHooked)
                            hoverable++;
                    }

                    activeEquipmentSlotUiObjects.Add(clone);
                    SetActive(clone, true);
                    renderedNames.Add(entry.SlotId + "=" + (string.IsNullOrWhiteSpace(entry.ItemId) ? "empty" : entry.ItemId));
                    rendered++;
                }

                if (rendered <= 0)
                    return false;

                equipmentSlotsUiRendered = true;
                lastEquipmentSlotsUiRefreshAt = DateTimeOffset.Now;
                equipmentSlotsUiLastSummary = "reason=" + (reason ?? string.Empty) +
                    ", hooks=" + equipmentSlotsUiHooksInstalled +
                    ", rendered=" + rendered +
                    ", occupied=" + occupied +
                    ", interactive=" + interactive +
                    ", hoverable=" + hoverable +
                    ", readOnly=false, attributeOnly=true, preserveVanillaVisualSlots=true" +
                    ", slots=" + string.Join(";", renderedNames.ToArray());
                RefreshEquipmentSlotsUiStateFlags();
                runtime.SetHookStatus("Player.EquipmentSlotsApi", "configured-experimental-player-ui-storage-stats-hook", "AccessoriesBar DTMAPI cloned extra-slot strip", equipmentSlotsUiLastSummary);
                if (!equipmentSlotsUiEvidenceRecorded)
                    runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsUi", "pending", "AccessoriesBar cloned DTMAPI extra slots", equipmentSlotsUiLastSummary);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots UI render failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsUi", "failed", "AccessoriesBar cloned DTMAPI extra slots", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private bool ConfigureEquipmentSlotUiClone(object cloneSlot, EquipmentSlotRuntimeEntry entry, out bool hoverHooked)
        {
            hoverHooked = false;
            try
            {
                TryInvokeNoArg(cloneSlot, "Init");
                bool indexSet = TrySetMemberValue(cloneSlot, "index", entry.Index);
                bool interactableSet = TrySetMemberValue(cloneSlot, "interactable", true);
                bool raycastSet = TrySetMemberValue(cloneSlot, "blocksRaycasts", true);
                object? button = ReadMember(cloneSlot, "button");
                bool buttonInteractableSet = false;
                if (button != null)
                    buttonInteractableSet = TrySetMemberValue(button, "interactable", true);
                object? gameObject = ReadMember(cloneSlot, "gameObject");
                Type? canvasGroupType = ResolveType("UnityEngine.CanvasGroup, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.CanvasGroup, UnityEngine");
                object? canvasGroup = gameObject == null || canvasGroupType == null ? null : GetComponent(gameObject, canvasGroupType);
                bool canvasInteractableSet = canvasGroup != null && TrySetMemberValue(canvasGroup, "interactable", true);
                bool canvasRaycastSet = canvasGroup != null && TrySetMemberValue(canvasGroup, "blocksRaycasts", true);
                bool canvasAlphaSet = canvasGroup != null && TrySetMemberValue(canvasGroup, "alpha", 1f);

                ClearUnityEvent(ReadMember(cloneSlot, "onClick"));
                ClearUnityEvent(ReadMember(cloneSlot, "onSelect"));
                ClearUnityEvent(ReadMember(cloneSlot, "onDeselect"));
                ClearUnityEvent(ReadMember(cloneSlot, "onPointerEnter"));
                ClearUnityEvent(ReadMember(cloneSlot, "onPointerExit"));
                FindMethodInHierarchy(cloneSlot.GetType(), "ClearAllClickCallbacks", 0)?.Invoke(cloneSlot, null);

                MethodInfo? setClickCallbacks = cloneSlot.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "SetClickCallbacks" && m.GetParameters().Length >= 1);
                bool clickHooked = false;
                ParameterInfo[] clickParameters = Array.Empty<ParameterInfo>();
                Delegate? leftClick = null;
                Delegate? rightClick = null;
                if (setClickCallbacks != null)
                {
                    clickParameters = setClickCallbacks.GetParameters();
                    leftClick = CreateUnityCallback(clickParameters[0].ParameterType, _ => HandleEquipmentSlotUiClick(entry, rightClick: false));
                    rightClick = clickParameters.Length > 1
                        ? CreateUnityCallback(clickParameters[1].ParameterType, _ => HandleEquipmentSlotUiClick(entry, rightClick: true))
                        : null;
                    object?[] args = new object?[clickParameters.Length];
                    args[0] = leftClick;
                    if (args.Length > 1)
                        args[1] = rightClick;
                    if (args.Length > 4)
                        args[4] = leftClick;
                    if (leftClick != null)
                    {
                        setClickCallbacks.Invoke(cloneSlot, args);
                        clickHooked = true;
                    }
                }

                object? onSelect = ReadMember(cloneSlot, "onSelect");
                object? onDeselect = ReadMember(cloneSlot, "onDeselect");
                object? onPointerEnter = ReadMember(cloneSlot, "onPointerEnter");
                object? onPointerExit = ReadMember(cloneSlot, "onPointerExit");
                bool selectHooked = AddIntUnityEventListener(onSelect, _ => HandleEquipmentSlotUiHover(cloneSlot, entry), clearFirst: true);
                AddIntUnityEventListener(onDeselect, _ => HideNativeHoverBox(), clearFirst: true);
                hoverHooked = AddIntUnityEventListener(onPointerEnter, _ => HandleEquipmentSlotUiHover(cloneSlot, entry), clearFirst: true);
                AddIntUnityEventListener(onPointerExit, _ => HideNativeHoverBox(), clearFirst: true);
                if (!equipmentSlotsUiBindDiagnosticLogged)
                {
                    equipmentSlotsUiBindDiagnosticLogged = true;
                    runtime.RuntimeMonitor.Log("EquipmentSlots UI bind diagnostic slotType=" + cloneSlot.GetType().FullName +
                        " init=True" +
                        " indexSet=" + indexSet +
                        " interactableSet=" + interactableSet +
                        " raycastSet=" + raycastSet +
                        " button=" + (button == null ? "null" : button.GetType().FullName) +
                        " buttonInteractableSet=" + buttonInteractableSet +
                        " canvasGroup=" + (canvasGroup == null ? "null" : canvasGroup.GetType().FullName) +
                        " canvasInteractableSet=" + canvasInteractableSet +
                        " canvasRaycastSet=" + canvasRaycastSet +
                        " canvasAlphaSet=" + canvasAlphaSet +
                        " setClickCallbacks=" + (setClickCallbacks == null ? "missing" : setClickCallbacks.DeclaringType?.FullName + "(" + string.Join("|", clickParameters.Select(p => p.ParameterType.FullName).ToArray()) + ")") +
                        " leftDelegate=" + (leftClick == null ? "null" : leftClick.GetType().FullName) +
                        " rightDelegate=" + (rightClick == null ? "null" : rightClick.GetType().FullName) +
                        " clickHooked=" + clickHooked +
                        " onSelect=" + (onSelect == null ? "null" : onSelect.GetType().FullName) +
                        " onPointerEnter=" + (onPointerEnter == null ? "null" : onPointerEnter.GetType().FullName) +
                        " selectHooked=" + selectHooked +
                        " hoverHooked=" + hoverHooked + ".");
                }
                return clickHooked || hoverHooked;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots UI interaction bind failed.", ex.ToString());
                if (!equipmentSlotsUiInteractionFailureLogged)
                {
                    equipmentSlotsUiInteractionFailureLogged = true;
                    runtime.RuntimeMonitor.Log("EquipmentSlots UI interaction bind failed slotType=" + cloneSlot.GetType().FullName +
                        " slot=" + (entry == null ? "null" : entry.SlotId) +
                        " error=" + ex.GetType().Name + ": " + ex.Message + ".");
                }
                return false;
            }
        }

        private void HandleEquipmentSlotUiClick(EquipmentSlotRuntimeEntry entry, bool rightClick)
        {
            try
            {
                if (entry == null)
                    return;

                string ownerId = FirstText(entry.OwnerId, FindEquipmentSlotOwnerId(entry));
                if (string.IsNullOrWhiteSpace(ownerId))
                {
                    ShowNativeSmallMessage("DTMAPI extra slot owner is missing.", error: true);
                    return;
                }

                var owner = new EquipmentSlotUiManifest(ownerId);
                if (!string.IsNullOrWhiteSpace(entry.ItemId))
                {
                    EquipmentSlotEquipResult unequip = UnequipExtraSlot(owner, entry.SlotId, rightClick ? "player UI right-click unequip" : "player UI click unequip");
                    ShowNativeSmallMessage(unequip.Message, error: !unequip.Success);
                    runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsClick", unequip.Success ? "verified" : "failed", "AccessorySlot.SetClickCallbacks -> IEquipmentSlotsApi.UnequipExtraSlot", unequip.Message);
                    return;
                }

                if (TryGetNativeInventoryBuffer(out object? buffer, out object? bufferItem) && bufferItem != null)
                {
                    EquipmentSlotEquipResult bufferedEquip = EquipExtraSlotFromNativeBuffer(owner, entry, bufferItem, buffer);
                    ShowNativeSmallMessage(bufferedEquip.Message, error: !bufferedEquip.Success);
                    runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsClick", bufferedEquip.Success ? "verified" : "failed", "AccessorySlot.SetClickCallbacks -> native inventory buffer -> IEquipmentSlotsApi", bufferedEquip.Message);
                    return;
                }

                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi != null && TryFindBackpackEquipmentSlotCandidate(dolocApi, out string itemId, out string displayName))
                {
                    EquipmentSlotEquipResult equip = EquipExtraSlot(owner, entry.SlotId, itemId);
                    ShowNativeSmallMessage(equip.Success ? "已装备 " + FirstText(equip.DisplayName, displayName, itemId) : equip.Message, error: !equip.Success);
                    runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsClick", equip.Success ? "verified" : "failed", "AccessorySlot.SetClickCallbacks -> backpack candidate -> IEquipmentSlotsApi.EquipExtraSlot", equip.Message);
                    return;
                }

                string message = "拿起一个饰品或帽子后点击 DTMAPI 额外槽，或先把可用饰品/帽子放入背包。";
                ShowNativeSmallMessage(message, error: true);
                runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsClick", "pending", "AccessorySlot.SetClickCallbacks", "No native inventory buffer item or backpack passive/hat candidate was available.");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots UI click failed.", ex.ToString());
                ShowNativeSmallMessage("DTMAPI extra slot click failed: " + ex.Message, error: true);
                runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsClick", "failed", "AccessorySlot.SetClickCallbacks", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private EquipmentSlotEquipResult EquipExtraSlotFromNativeBuffer(IManifest owner, EquipmentSlotRuntimeEntry entry, object bufferItem, object? buffer)
        {
            string ownerId = owner.UniqueID;
            string itemId = ReadStringMember(bufferItem, "name");
            var result = new EquipmentSlotEquipResult
            {
                OwnerId = ownerId,
                SlotId = entry?.SlotId ?? string.Empty,
                ItemId = itemId
            };
            if (entry == null)
                return EquipmentSlotEquipFailed(result, "missing-slot", "No DTMAPI extra slot is available.");
            if (string.IsNullOrWhiteSpace(itemId))
                return EquipmentSlotEquipFailed(result, "missing-item", "The native inventory buffer item has no item id.");

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            result.BeforeBackpackCount = CountNativeBackpackItem(dolocApi, itemId);
            int bufferCount = Math.Max(1, ReadIntMember(bufferItem, "count", 1));
            if (bufferCount != 1)
                return EquipmentSlotEquipFailed(result, "split-one-item", "DTMAPI extra slots accept one equipment item at a time; split " + itemId + " to one item before placing it.");

            if (!TryValidateExtraEquipmentSlotItem(bufferItem, out string displayName, out string skillId, out string validationReason, out string validationMessage))
                return EquipmentSlotEquipFailed(result, validationReason, validationMessage);

            EquipmentSlotsOptions options = GetEquipmentSlotsOptions(ownerId);
            if (!options.Enabled || options.ExtraAttributeSlots <= 0)
                return EquipmentSlotEquipFailed(result, "not-enabled", "Equipment slots are not enabled for " + ownerId + ".");

            if (!RecoverEquipmentSlotEntry(ownerId, entry, "replace before player UI buffer equip", saveAfterRecovery: false, out string recoveryMessage, out int recovered))
                return EquipmentSlotEquipFailed(result, "recover-existing-failed", recoveryMessage);
            result.RecoveredCount = recovered;

            object? taken = buffer == null ? null : FindMethodInHierarchy(buffer.GetType(), "Take", 0)?.Invoke(buffer, null);
            if (taken == null)
                return EquipmentSlotEquipFailed(result, "buffer-take-failed", "Native inventory buffer did not return the held item for " + itemId + ".");

            entry.OwnerId = ownerId;
            entry.ItemId = itemId;
            entry.DisplayName = displayName;
            entry.SkillId = skillId;
            entry.LastMessage = "Equipped " + displayName + " from native inventory buffer as attribute-only extra slot item.";
            entry.Applied = false;
            entry.NativeItem = null;
            entry.NativeFunction = null;
            result.DisplayName = displayName;

            string applyMessage = TryApplyStoredEquipmentSlotFunctions(ownerId, "player UI buffer equip " + entry.SlotId)
                ? "Applied native AgentEquipmentFunction for " + itemId + "."
                : "Stored item; native AgentEquipmentFunction will apply after equipment manager is available.";
            entry.LastMessage = entry.LastMessage + " " + applyMessage;
            SaveEquipmentSlotStorage(ownerId);
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("player UI buffer equip " + entry.SlotId, force: true);

            EquipmentSlotsState state = BuildEquipmentSlotsState(ownerId, options);
            state.LastEquippedSlotId = entry.SlotId;
            state.LastEquippedItemId = itemId;
            state.LastRecoveryMessage = entry.LastMessage;
            equipmentSlotStates[ownerId] = state;

            result.AfterBackpackCount = CountNativeBackpackItem(dolocApi, itemId);
            result.Success = true;
            result.Message = entry.LastMessage + " backpack=" + result.BeforeBackpackCount + "->" + result.AfterBackpackCount + ".";
            runtime.RuntimeMonitor.Log("EquipmentSlots UI buffer equip OK owner=" + ownerId + " slot=" + entry.SlotId + " item=" + itemId + " display=" + displayName + " applied=" + entry.Applied + ".");
            runtime.SetHookStatus("Player.EquipmentSlotsApi", state.Status, "AccessorySlot.SetClickCallbacks -> native inventory buffer -> AgentEquipmentFunction", result.Message);
            return result;
        }

        private void HandleEquipmentSlotUiHover(object cloneSlot, EquipmentSlotRuntimeEntry entry)
        {
            try
            {
                object? item = null;
                string text = string.IsNullOrWhiteSpace(entry.ItemId)
                    ? "DTMAPI额外属性槽：拿起饰品或帽子后点击放入"
                    : "DTMAPI额外属性槽";
                if (!string.IsNullOrWhiteSpace(entry.ItemId))
                    TryGenerateNativeItem(entry.ItemId, 1, out item, out _, out _);

                MethodInfo? showViewer = cloneSlot.GetType().GetMethod("ShowEquipmentItemViewer", BindingFlags.Public | BindingFlags.Instance, null, new[] { ResolveType("DolocTown.Item, Assembly-CSharp") ?? typeof(object), typeof(string) }, null)
                    ?? cloneSlot.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == "ShowEquipmentItemViewer" && m.GetParameters().Length == 2);
                showViewer?.Invoke(cloneSlot, new object?[] { item, text });
                runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsHover", "verified", "AccessorySlot.ShowEquipmentItemViewer", "Hovered DTMAPI extra slot " + entry.SlotId + " item=" + FirstText(entry.ItemId, "empty") + ".");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots UI hover failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsHover", "failed", "AccessorySlot.ShowEquipmentItemViewer", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private bool TryFindBackpackEquipmentSlotCandidate(Type dolocApi, out string itemId, out string displayName)
        {
            itemId = string.Empty;
            displayName = string.Empty;
            foreach (InventoryDebugItem candidate in EnumerateInventoryDebugItems())
            {
                if (string.IsNullOrWhiteSpace(candidate.Id) || CountNativeBackpackItem(dolocApi, candidate.Id) <= 0)
                    continue;
                if (!TryGenerateNativeItem(candidate.Id, 1, out object? item, out _, out _) || item == null)
                    continue;
                if (!TryValidateExtraEquipmentSlotItem(item, out displayName, out _, out _, out _))
                    continue;
                itemId = candidate.Id;
                return true;
            }
            return false;
        }

        private string FindEquipmentSlotOwnerId(EquipmentSlotRuntimeEntry entry)
        {
            foreach (KeyValuePair<string, List<EquipmentSlotRuntimeEntry>> pair in equipmentSlotEntries)
            {
                if (pair.Value.Contains(entry))
                    return pair.Key;
            }
            return string.Empty;
        }

        private bool TryGetNativeInventoryBuffer(out object? buffer, out object? currentItem)
        {
            buffer = null;
            currentItem = null;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? inventorySystem = archive == null ? null : ReadMember(archive, "InventorySystem");
            buffer = inventorySystem == null ? null : ReadMember(inventorySystem, "buffer");
            currentItem = buffer == null ? null : ReadMember(buffer, "CurrentItem");
            return buffer != null;
        }

        private Delegate? CreateUnityCallback(Type? delegateType, Action<int> action)
        {
            try
            {
                if (delegateType == null || action == null)
                    return null;

                ParameterInfo[] invokeParameters = delegateType.GetMethod("Invoke")?.GetParameters() ?? Array.Empty<ParameterInfo>();
                if (invokeParameters.Length == 1)
                {
                    var binder = new IntActionBinder(action);
                    equipmentSlotUiEventBinders.Add(binder);
                    return Delegate.CreateDelegate(delegateType, binder, nameof(IntActionBinder.Invoke));
                }

                if (invokeParameters.Length == 0)
                {
                    var binder = new VoidActionBinder(() => action(0));
                    equipmentSlotUiEventBinders.Add(binder);
                    return Delegate.CreateDelegate(delegateType, binder, nameof(VoidActionBinder.Invoke));
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool AddIntUnityEventListener(object? unityEvent, Action<int> action, bool clearFirst)
        {
            if (unityEvent == null)
                return false;

            try
            {
                if (clearFirst)
                    ClearUnityEvent(unityEvent);

                foreach (MethodInfo add in unityEvent.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    ParameterInfo[] p = add.GetParameters();
                    if (add.Name != "AddListener" || p.Length != 1)
                        continue;

                    Delegate? del = CreateUnityCallback(p[0].ParameterType, action);
                    if (del == null || !p[0].ParameterType.IsInstanceOfType(del))
                        continue;

                    add.Invoke(unityEvent, new object[] { del });
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static void ClearUnityEvent(object? unityEvent)
        {
            try
            {
                unityEvent?.GetType().GetMethod("RemoveAllListeners", BindingFlags.Public | BindingFlags.Instance)?.Invoke(unityEvent, null);
            }
            catch
            {
            }
        }

        private static void HideNativeHoverBox()
        {
            try
            {
                ResolveType("DolocAPI, Assembly-CSharp")?.GetMethod("HideHoverBox", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)?.Invoke(null, null);
            }
            catch
            {
            }
        }

        private bool HasEnabledEquipmentSlots()
        {
            return equipmentSlotOptions.Any(entry => entry.Value.Enabled && entry.Value.ExtraAttributeSlots > 0);
        }

        private IReadOnlyList<EquipmentSlotRuntimeEntry> BuildEquipmentSlotsForUi()
        {
            var result = new List<EquipmentSlotRuntimeEntry>();
            foreach (KeyValuePair<string, EquipmentSlotsOptions> optionEntry in equipmentSlotOptions.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
            {
                EquipmentSlotsOptions options = optionEntry.Value;
                if (!options.Enabled || options.ExtraAttributeSlots <= 0)
                    continue;
                EnsureEquipmentSlotStorageLoaded(optionEntry.Key);
                foreach (EquipmentSlotRuntimeEntry entry in EnsureEquipmentSlotEntries(optionEntry.Key, options)
                    .Where(entry => entry.Index >= 0 && entry.Index < options.ExtraAttributeSlots)
                    .OrderBy(entry => entry.Index))
                {
                    entry.OwnerId = optionEntry.Key;
                    result.Add(entry);
                }
            }

            return result;
        }

        private object? ResolveEquipmentSlotIcon(EquipmentSlotRuntimeEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                return null;
            if (!TryGenerateNativeItem(entry.ItemId, 1, out object? item, out _, out _))
                return null;
            return ReadMember(item!, "uiSprite");
        }

        private void ClearEquipmentSlotsUi(object parentTransform)
        {
            foreach (object instance in activeEquipmentSlotUiObjects.ToArray())
            {
                SetActive(instance, false);
                DestroyUnityObject(instance);
            }
            activeEquipmentSlotUiObjects.Clear();
            equipmentSlotUiEventBinders.Clear();

            MethodInfo? find = parentTransform.GetType().GetMethod("Find", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            for (int i = 0; i < 12; i++)
            {
                object? child = find?.Invoke(parentTransform, new object[] { "DTMAPI.ExtraEquipmentSlot." + i });
                object? gameObject = child == null ? null : ReadMember(child, "gameObject");
                if (gameObject != null)
                    DestroyUnityObject(gameObject);
            }
        }

        private void RefreshEquipmentSlotsUiStateFlags()
        {
            foreach (KeyValuePair<string, EquipmentSlotsOptions> optionEntry in equipmentSlotOptions.ToArray())
            {
                EquipmentSlotsState state = BuildEquipmentSlotsState(optionEntry.Key, optionEntry.Value);
                state.RuntimeUiHookInstalled = equipmentSlotsUiHooksInstalled || equipmentSlotsUiRendered;
                state.RuntimeStatsHookInstalled = equipmentSlotsRuntimeHooksInstalled;
                if (state.IsConfigured && state.RuntimeUiHookInstalled && state.RuntimeStatsHookInstalled)
                    state.Status = "configured-experimental-player-ui-storage-stats-hook";
                state.LastRecoveryMessage = FirstText(equipmentSlotsUiLastSummary, state.LastRecoveryMessage);
                equipmentSlotStates[optionEntry.Key] = state;
            }
        }

        private static void PositionEquipmentSlotUiClone(object sourceTransform, object cloneTransform, int index)
        {
            object? localPosition = ReadMember(sourceTransform, "localPosition");
            double baseX = ReadVectorComponent(localPosition, "x");
            double baseY = ReadVectorComponent(localPosition, "y");
            double z = ReadVectorComponent(localPosition, "z");
            int column = index % 3;
            int row = index / 3;
            object? position = CreateUnityVector3(baseX + 44d * (column + 1), baseY - 44d * row, z);
            if (position != null)
                SetMemberValue(cloneTransform, "localPosition", position);

            object? localScale = ReadMember(sourceTransform, "localScale");
            if (localScale != null)
                SetMemberValue(cloneTransform, "localScale", localScale);
        }

        private static object? FindUnityObjectOfType(string assemblyQualifiedName)
        {
            Type? targetType = ResolveType(assemblyQualifiedName);
            if (targetType == null)
                return null;

            Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
            MethodInfo? findObject = objectType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "FindObjectOfType" && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(Type));
            object? found = findObject?.Invoke(null, new object[] { targetType });
            if (found != null)
                return found;

            Type? resourcesType = ResolveType("UnityEngine.Resources, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Resources, UnityEngine");
            MethodInfo? findAll = resourcesType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "FindObjectsOfTypeAll" && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(Type));
            object? all = findAll?.Invoke(null, new object[] { targetType });
            if (all is IEnumerable enumerable)
            {
                foreach (object candidate in enumerable)
                {
                    if (candidate != null)
                        return candidate;
                }
            }

            return null;
        }

        private static EquipmentSlotsOptions NormalizeEquipmentSlotsOptions(EquipmentSlotsOptions? options)
        {
            options ??= new EquipmentSlotsOptions();
            return new EquipmentSlotsOptions
            {
                Enabled = options.Enabled,
                ExtraAttributeSlots = ClampInt(options.ExtraAttributeSlots, 0, 24),
                SlotIdPrefix = FirstText(options.SlotIdPrefix, "dtmapi.extra"),
                PreserveVanillaVisualSlots = options.PreserveVanillaVisualSlots,
                ExtraSlotsAffectVisuals = false,
                SafeUnequipOnDisable = options.SafeUnequipOnDisable,
                AutoRecoverOnMissingMod = options.AutoRecoverOnMissingMod,
                VerboseLogging = options.VerboseLogging
            };
        }

        private sealed class EquipmentSlotRuntimeEntry
        {
            public string OwnerId { get; set; } = string.Empty;
            public int Index { get; set; }
            public string SlotId { get; set; } = string.Empty;
            public string ItemId { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string SkillId { get; set; } = string.Empty;
            public bool Applied { get; set; }
            public string LastMessage { get; set; } = string.Empty;
            public object? NativeItem { get; set; }
            public object? NativeFunction { get; set; }
        }

        private sealed class EquipmentSlotUiManifest : IManifest
        {
            public EquipmentSlotUiManifest(string uniqueId)
            {
                UniqueID = uniqueId ?? string.Empty;
                Name = UniqueID;
            }

            public string Name { get; }
            public string Author => "DTMAPI";
            public string Version => DTMAPI.Core.Runtime.DtmApiRuntime.ApiVersion;
            public string Description => "Runtime owner manifest for DTMAPI extra equipment slot UI callbacks.";
            public string UniqueID { get; }
            public string EntryDll => string.Empty;
            public string EntryType => string.Empty;
            public string MinimumDTMApiVersion => DTMAPI.Core.Runtime.DtmApiRuntime.ApiVersion;
            public string MinimumGameVersion => string.Empty;
            public string Type => "CodeMod";
            public IReadOnlyList<IManifestDependency> Dependencies => Array.Empty<IManifestDependency>();
            public IReadOnlyList<string> UpdateKeys => Array.Empty<string>();
        }

        private sealed class EquipmentSlotSaveScope
        {
            public static readonly EquipmentSlotSaveScope Missing = new EquipmentSlotSaveScope(-1, "slot-unknown", string.Empty, string.Empty, string.Empty, -1);

            public EquipmentSlotSaveScope(int archiveIndex, string scopeKey, string playerName, string customPlayerName, string currentScene, long totalGameSeconds)
            {
                ArchiveIndex = archiveIndex;
                ScopeKey = scopeKey ?? string.Empty;
                PlayerName = playerName ?? string.Empty;
                CustomPlayerName = customPlayerName ?? string.Empty;
                CurrentScene = currentScene ?? string.Empty;
                TotalGameSeconds = totalGameSeconds;
            }

            public bool HasArchive => ArchiveIndex >= 0;
            public int ArchiveIndex { get; }
            public string ScopeKey { get; }
            public string PlayerName { get; }
            public string CustomPlayerName { get; }
            public string CurrentScene { get; }
            public long TotalGameSeconds { get; }
        }

        private sealed class IntActionBinder
        {
            private readonly Action<int> action;

            public IntActionBinder(Action<int> action)
            {
                this.action = action;
            }

            public void Invoke(int value)
            {
                action(value);
            }
        }

        private sealed class VoidActionBinder
        {
            private readonly Action action;

            public VoidActionBinder(Action action)
            {
                this.action = action;
            }

            public void Invoke()
            {
                action();
            }
        }

        [DataContract]
        private sealed class EquipmentSlotStorageDocument
        {
            public EquipmentSlotStorageDocument()
            {
                SchemaVersion = 1;
                ArchiveIndex = -1;
                SavedTotalGameSeconds = -1;
            }

            [DataMember(Name = "schemaVersion")]
            public int SchemaVersion { get; set; }

            [DataMember(Name = "ownerId")]
            public string OwnerId { get; set; } = string.Empty;

            [DataMember(Name = "storageScope")]
            public string StorageScope { get; set; } = string.Empty;

            [DataMember(Name = "archiveIndex")]
            public int ArchiveIndex { get; set; }

            [DataMember(Name = "playerName")]
            public string PlayerName { get; set; } = string.Empty;

            [DataMember(Name = "customPlayerName")]
            public string CustomPlayerName { get; set; } = string.Empty;

            [DataMember(Name = "currentScene")]
            public string CurrentScene { get; set; } = string.Empty;

            [DataMember(Name = "savedTotalGameSeconds")]
            public long SavedTotalGameSeconds { get; set; }

            [DataMember(Name = "savedAt")]
            public string SavedAt { get; set; } = string.Empty;

            [DataMember(Name = "slots")]
            public List<EquipmentSlotStorageEntry> Slots { get; set; } = new List<EquipmentSlotStorageEntry>();
        }

        [DataContract]
        private sealed class EquipmentSlotStorageEntry
        {
            public EquipmentSlotStorageEntry()
            {
                TailIndexFromEnd = -1;
            }

            [DataMember(Name = "index")]
            public int Index { get; set; }

            [DataMember(Name = "tailIndexFromEnd")]
            public int TailIndexFromEnd { get; set; }

            [DataMember(Name = "slotId")]
            public string SlotId { get; set; } = string.Empty;

            [DataMember(Name = "itemId")]
            public string ItemId { get; set; } = string.Empty;

            [DataMember(Name = "displayName")]
            public string DisplayName { get; set; } = string.Empty;

            [DataMember(Name = "skillId")]
            public string SkillId { get; set; } = string.Empty;

            [DataMember(Name = "lastMessage")]
            public string LastMessage { get; set; } = string.Empty;
        }
    }
}
