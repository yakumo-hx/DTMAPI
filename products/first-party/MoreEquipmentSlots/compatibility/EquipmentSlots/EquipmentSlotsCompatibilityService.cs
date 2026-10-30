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
using DTMAPI.Core.Runtime;
using DTMAPI.MoreEquipmentSlots;
using static DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Reads supported legacy equipment storage and commits recovery after native saving.</summary>
    internal sealed partial class EquipmentSlotsCompatibilityService
    {
        private readonly DtmApiRuntime runtime;
        private readonly Func<string, bool>? equipmentSlotStorageValidationGate;
        private readonly Dictionary<string, List<EquipmentSlotRuntimeEntry>> equipmentSlotEntries = new Dictionary<string, List<EquipmentSlotRuntimeEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loadedEquipmentSlotStorageOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> blockedEquipmentSlotStorageOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> dirtyEquipmentSlotStorageOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> migratedLegacyEquipmentSlotStorageOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EquipmentSlotTransactionJournal> equipmentSlotJournals = new Dictionary<string, EquipmentSlotTransactionJournal>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<EquipmentSlotRuntimeEntry>> committedEquipmentSlotEntries = new Dictionary<string, List<EquipmentSlotRuntimeEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EquipmentSlotCompatibilityGameplayCandidate> equipmentSlotGameplayCandidates = new Dictionary<string, EquipmentSlotCompatibilityGameplayCandidate>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EquipmentSlotWorkingPlacementGuard> equipmentSlotWorkingPlacementGuards = new Dictionary<string, EquipmentSlotWorkingPlacementGuard>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, long> equipmentSlotStorageGenerations = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private bool equipmentSlotsOrphanRecoveryChecked;
        private long equipmentFileStatusCallCount;
        private long equipmentDirectoryEnumerationCount;

        internal EquipmentSlotsCompatibilityService(DtmApiRuntime runtime)
            : this(runtime, null)
        {
        }

        internal EquipmentSlotsCompatibilityService(
            DtmApiRuntime runtime,
            Func<string, bool>? equipmentSlotStorageValidationGate)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.equipmentSlotStorageValidationGate =
                equipmentSlotStorageValidationGate;
        }

        internal void NotifyEquipmentSlotsSaveLoaded(bool isNewGame)
        {
            ResetEquipmentSlotSessionState("SaveLoaded isNewGame=" + isNewGame, discardDirty: true);
        }

        internal void NotifyEquipmentSlotsSaveSaved(int? slot)
        {
            CommitProductColdRecoveryAfterNativeSave(slot);
            CommitEquipmentSlotJournalsAfterNativeSave(slot);
            CommitEquipmentSlotGameplayCandidatesAfterNativeSave(slot);
            if (dirtyEquipmentSlotStorageOwners.Count == 0)
                return;

            string[] owners = dirtyEquipmentSlotStorageOwners.ToArray();
            foreach (string ownerId in owners)
            {
                if (!PersistEquipmentSlotStorage(
                    ownerId,
                    "SaveSaved slot=" +
                    (slot?.ToString(
                        CultureInfo.InvariantCulture) ??
                     "unknown")))
                {
                    throw new InvalidOperationException(
                        "EquipmentSlots could not persist committed storage for " +
                        ownerId +
                        " after native SaveSaved.");
                }
                if (equipmentSlotEntries.TryGetValue(
                    ownerId,
                    out List<EquipmentSlotRuntimeEntry>? entries))
                {
                    foreach (EquipmentSlotRuntimeEntry entry in
                        entries)
                    {
                        entry.CommittedItemReleasedToNative =
                            false;
                    }
                    committedEquipmentSlotEntries[ownerId] =
                        CloneEquipmentSlotRuntimeEntries(entries);
                }
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
            equipmentSlotsOrphanRecoveryChecked = false;
            ClearProductColdRecoverySessions();
            if (equipmentSlotEntries.Count == 0 &&
                loadedEquipmentSlotStorageOwners.Count == 0 &&
                blockedEquipmentSlotStorageOwners.Count == 0 &&
                dirtyEquipmentSlotStorageOwners.Count == 0 &&
                migratedLegacyEquipmentSlotStorageOwners.Count == 0 &&
                equipmentSlotJournals.Count == 0 &&
                committedEquipmentSlotEntries.Count == 0 &&
                equipmentSlotGameplayCandidates.Count == 0 &&
                equipmentSlotWorkingPlacementGuards.Count == 0 &&
                equipmentSlotStorageGenerations.Count == 0)
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
            blockedEquipmentSlotStorageOwners.Clear();
            if (discardDirty)
                dirtyEquipmentSlotStorageOwners.Clear();
            migratedLegacyEquipmentSlotStorageOwners.Clear();
            equipmentSlotJournals.Clear();
            committedEquipmentSlotEntries.Clear();
            equipmentSlotGameplayCandidates.Clear();
            equipmentSlotWorkingPlacementGuards.Clear();
            equipmentSlotStorageGenerations.Clear();
            runtime.RuntimeMonitor.Log("EquipmentSlots session reset reason=" + (reason ?? string.Empty) + " discardedDirtyOwners=" + discardedDirty + ".");
            runtime.SetHookStatus("Player.EquipmentSlotsSaveTransaction", "session-reset", "SaveLoaded/ReturnedToTitle boundary", "Cleared in-memory equipment-slot state for " + (reason ?? string.Empty) + "; unsaved DTMAPI storage mutations are not written outside native SaveGame.");
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
            equipmentFileStatusCallCount++;
            if (File.Exists(scopedPath))
            {
                if (TryLoadEquipmentSlotStorageDocument(
                    ownerId,
                    scopedPath,
                    scope,
                    isLegacyGlobalStorage: false,
                    pathKnownToExist: true))
                {
                    blockedEquipmentSlotStorageOwners.Remove(ownerId);
                    return;
                }
                BlockEquipmentSlotStorage(
                    ownerId,
                    scopedPath,
                    "Protected per-save storage is present but invalid.");
                return;
            }

            string legacyPath = GetLegacyEquipmentSlotStoragePath(ownerId);
            equipmentFileStatusCallCount++;
            if (!File.Exists(legacyPath))
            {
                blockedEquipmentSlotStorageOwners.Remove(ownerId);
                return;
            }
            if (TryLoadEquipmentSlotStorageDocument(
                ownerId,
                legacyPath,
                scope,
                isLegacyGlobalStorage: true,
                pathKnownToExist: true))
            {
                blockedEquipmentSlotStorageOwners.Remove(ownerId);
                migratedLegacyEquipmentSlotStorageOwners.Add(ownerId);
                SaveEquipmentSlotStorage(ownerId);
                runtime.RuntimeMonitor.Log("EquipmentSlots adopted legacy global storage owner=" + ownerId + " legacyPath=" + legacyPath + " saveScope=" + scope.ScopeKey + "; it will be rewritten as per-save protected storage after the next native SaveGame.");
                return;
            }
            BlockEquipmentSlotStorage(
                ownerId,
                legacyPath,
                "Legacy storage is present but invalid.");
        }

        private void BlockEquipmentSlotStorage(
            string ownerId,
            string path,
            string reason)
        {
            blockedEquipmentSlotStorageOwners.Add(ownerId);
            runtime.RuntimeMonitor.Log(
                "EquipmentSlots storage blocked owner=" +
                ownerId +
                " path=" +
                path +
                " reason=" +
                reason +
                " No fallback or empty-state overwrite will run.",
                LogLevel.Warn);
            runtime.SetHookStatus(
                "Player.EquipmentSlotsSaveTransaction",
                "storage-fail-closed",
                "DTMAPI protected equipment-slot storage",
                "owner=" +
                ownerId +
                "; " +
                reason +
                " The existing file is retained unchanged.");
        }

        private void SaveEquipmentSlotStorage(string ownerId)
        {
            ownerId ??= string.Empty;
            if (string.IsNullOrWhiteSpace(ownerId))
                return;
            if (blockedEquipmentSlotStorageOwners.Contains(ownerId))
            {
                runtime.RuntimeMonitor.Log(
                    "EquipmentSlots refused to mark blocked storage dirty owner=" +
                    ownerId +
                    ".",
                    LogLevel.Warn);
                return;
            }

            dirtyEquipmentSlotStorageOwners.Add(ownerId);
            runtime.RuntimeMonitor.Log("EquipmentSlots storage marked dirty owner=" + ownerId + "; waiting for native SaveGame before writing DTMAPI sidecar storage.");
            runtime.SetHookStatus("Player.EquipmentSlotsSaveTransaction", "dirty", "runtime mutation -> SaveGame postfix", "Equipment-slot sidecar storage for " + ownerId + " is pending native SaveGame; unsaved exit will discard the sidecar mutation.");
        }

        private bool PersistEquipmentSlotStorage(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            if (string.IsNullOrWhiteSpace(ownerId))
                return false;
            if (blockedEquipmentSlotStorageOwners.Contains(ownerId))
            {
                runtime.RuntimeMonitor.Log(
                    "EquipmentSlots refused to overwrite present-but-invalid protected storage owner=" +
                    ownerId +
                    " reason=" +
                    (reason ?? string.Empty) +
                    ".",
                    LogLevel.Warn);
                return false;
            }

            EquipmentSlotSaveScope scope = GetEquipmentSlotSaveScope();
            if (!scope.HasArchive)
            {
                runtime.RuntimeMonitor.Log("EquipmentSlots storage save skipped owner=" + ownerId + " reason=" + (reason ?? string.Empty) + " because no loaded archive is available.", LogLevel.Warn);
                runtime.SetHookStatus("Player.EquipmentSlotsSaveTransaction", "blocked", "DTMAPI protected equipment-slot storage", "Could not persist equipment-slot sidecar storage for " + ownerId + " without a loaded archive.");
                return false;
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
                Generation = GetNextEquipmentSlotStorageGeneration(ownerId),
                Journal = equipmentSlotJournals.TryGetValue(ownerId, out EquipmentSlotTransactionJournal? journal)
                    ? journal
                    : null,
                GameplayCandidate = equipmentSlotGameplayCandidates.TryGetValue(ownerId, out EquipmentSlotCompatibilityGameplayCandidate? candidate)
                    ? candidate
                    : null
            };
            IReadOnlyList<EquipmentSlotRuntimeEntry> projectedEntries =
                candidate != null &&
                candidate.State ==
                    EquipmentSlotGameplayCandidateState.Prepared &&
                committedEquipmentSlotEntries.TryGetValue(ownerId, out List<EquipmentSlotRuntimeEntry>? committedEntries)
                    ? committedEntries
                    : entries;
            totalSlots = Math.Max(
                projectedEntries.Count,
                projectedEntries.Count == 0
                    ? 0
                    : projectedEntries.Max(entry => entry.Index) + 1);
            document.Slots = ToEquipmentSlotStorageEntries(
                projectedEntries,
                totalSlots);

            string path = GetEquipmentSlotStoragePath(ownerId, scope);
            try
            {
                WriteJson(path, document);
                ArchiveMigratedLegacyEquipmentSlotStorage(ownerId);
                runtime.RuntimeMonitor.Log("EquipmentSlots protected storage persisted owner=" + ownerId + " reason=" + (reason ?? string.Empty) + " scope=" + scope.ScopeKey + " path=" + path + " storedItems=" + entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId)) + ".");
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots storage save failed for " + ownerId + ".", ex.ToString());
                runtime.RuntimeMonitor.Log("EquipmentSlots storage save failed owner=" + ownerId + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                return false;
            }
        }

        private bool TryLoadEquipmentSlotStorageDocument(
            string ownerId,
            string path,
            EquipmentSlotSaveScope scope,
            bool isLegacyGlobalStorage,
            bool pathKnownToExist = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            if (!pathKnownToExist)
            {
                equipmentFileStatusCallCount++;
                if (!File.Exists(path))
                    return false;
            }

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

                if (!ReconcileEquipmentSlotGameplayCandidateDocument(
                    ownerId,
                    path,
                    scope,
                    document,
                    out string candidateMessage))
                {
                    runtime.SetHookStatus(
                        "Player.EquipmentSlotsSaveTransaction",
                        "candidate-fail-closed",
                        "DTMAPI protected equipment-slot gameplay candidate",
                        "owner=" + ownerId + "; " + candidateMessage);
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(candidateMessage))
                {
                    runtime.RuntimeMonitor.Log(
                        "EquipmentSlots gameplay candidate reconciliation owner=" +
                        ownerId +
                        " message=" +
                        candidateMessage +
                        ".");
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
                        DefenseBonus = Math.Max(0, slot.DefenseBonus),
                        IsShieldHat = slot.IsShieldHat,
                        ShieldMaxValue = Math.Max(0, slot.ShieldMaxValue),
                        ShieldValue = Math.Max(0, slot.ShieldValue),
                        ShieldDefend = Math.Max(0, slot.ShieldDefend),
                        LastMessage = FirstText(slot.LastMessage, isLegacyGlobalStorage ? "Loaded legacy DTMAPI extra-slot item." : "Loaded stored DTMAPI extra-slot item.")
                    });
                }

                equipmentSlotEntries[ownerId] = entries.OrderBy(entry => entry.Index).ToList();
                equipmentSlotStorageGenerations[ownerId] =
                    Math.Max(0, document.Generation);
                committedEquipmentSlotEntries[ownerId] =
                    CloneEquipmentSlotRuntimeEntries(
                        equipmentSlotEntries[ownerId]);
                if (document.GameplayCandidate != null)
                    equipmentSlotGameplayCandidates[ownerId] = document.GameplayCandidate;
                else
                    equipmentSlotGameplayCandidates.Remove(ownerId);
                if (document.Journal != null)
                {
                    document.Journal.Normalize();
                    equipmentSlotJournals[ownerId] = document.Journal;
                }
                else
                {
                    equipmentSlotJournals.Remove(ownerId);
                }
                if (!ReconcileEquipmentSlotJournal(ownerId, scope, out string journalMessage))
                {
                    runtime.SetHookStatus(
                        "Player.EquipmentSlotsSaveTransaction",
                        "journal-fail-closed",
                        "DTMAPI protected equipment-slot journal",
                        "owner=" + ownerId + "; " + journalMessage);
                    blockedEquipmentSlotStorageOwners.Add(
                        ownerId);
                    return false;
                }
                else if (!string.IsNullOrWhiteSpace(journalMessage))
                {
                    runtime.RuntimeMonitor.Log(
                        "EquipmentSlots journal reconciliation owner=" + ownerId + " message=" + journalMessage + ".");
                }
                runtime.RuntimeMonitor.Log("EquipmentSlots " + (isLegacyGlobalStorage ? "legacy global" : "protected per-save") + " storage loaded owner=" + ownerId + " scope=" + scope.ScopeKey + " path=" + path + " slots=" + entries.Count + " storedItems=" + entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId)) + ".");
                blockedEquipmentSlotStorageOwners.Remove(ownerId);
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
            equipmentFileStatusCallCount++;
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

        private void WriteJson<T>(string path, T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            string temporaryPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
            string backupPath = path + ".previous";
            try
            {
                using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
                    {
                        UseSimpleDictionaryFormat = true
                    });
                    serializer.WriteObject(stream, value);
                    stream.Flush(flushToDisk: true);
                }

                if (value is EquipmentSlotStorageDocument)
                {
                    EquipmentSlotStorageDocument? roundTripped =
                        ReadJson<EquipmentSlotStorageDocument>(
                            temporaryPath);
                    if (roundTripped == null ||
                        !EquipmentSlotStorageRoundTripMatches(
                            temporaryPath,
                            roundTripped))
                    {
                        throw new InvalidDataException(
                            "EquipmentSlots temporary sidecar failed transaction-identity round-trip validation.");
                    }
                }
                if (equipmentSlotStorageValidationGate != null &&
                    !equipmentSlotStorageValidationGate(
                        temporaryPath))
                {
                    throw new InvalidDataException(
                        "EquipmentSlots temporary sidecar was rejected by the storage validation gate.");
                }

                if (File.Exists(path))
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Replace(temporaryPath, path, backupPath, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static bool EquipmentSlotStorageRoundTripMatches(
            string temporaryPath,
            EquipmentSlotStorageDocument roundTripped)
        {
            byte[] persisted =
                File.ReadAllBytes(temporaryPath);
            using (var memory = new MemoryStream())
            {
                var serializer =
                    new DataContractJsonSerializer(
                        typeof(EquipmentSlotStorageDocument),
                        new DataContractJsonSerializerSettings
                        {
                            UseSimpleDictionaryFormat = true
                        });
                serializer.WriteObject(
                    memory,
                    roundTripped);
                byte[] reserialized =
                    memory.ToArray();
                if (persisted.Length != reserialized.Length)
                    return false;
                for (int index = 0;
                    index < persisted.Length;
                    index++)
                {
                    if (persisted[index] != reserialized[index])
                        return false;
                }
                return true;
            }
        }

        private static void ClearEquipmentSlotStoredItem(EquipmentSlotRuntimeEntry entry)
        {
            entry.ItemId = string.Empty;
            entry.DisplayName = string.Empty;
            entry.SkillId = string.Empty;
            entry.DefenseBonus = 0;
            entry.IsShieldHat = false;
            entry.ShieldMaxValue = 0;
            entry.ShieldValue = 0;
            entry.ShieldDefend = 0;
            entry.Applied = false;
            entry.NativeItem = null;
            entry.NativeFunction = null;
        }

        private bool RecoverOrphanEquipmentSlotsIfNeeded()
        {
            if (equipmentSlotsOrphanRecoveryChecked)
                return true;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null || ReadStaticMember(dolocApi, "archiveHandle") == null)
                return false;

            EquipmentSlotSaveScope scope = GetEquipmentSlotSaveScope();
            if (!scope.HasArchive)
                return false;

            equipmentSlotsOrphanRecoveryChecked = true;
            equipmentFileStatusCallCount++;
            if (!Directory.Exists(runtime.Paths.ConfigPath))
                return true;

            var scannedPaths = new List<string>();
            string scopedDirectory = GetEquipmentSlotStorageDirectory(scope);
            equipmentFileStatusCallCount++;
            if (Directory.Exists(scopedDirectory))
            {
                equipmentDirectoryEnumerationCount++;
                scannedPaths.AddRange(Directory.GetFiles(scopedDirectory, "equipment-slots-*.json", SearchOption.TopDirectoryOnly));
                scannedPaths.AddRange(
                    Directory.GetFiles(
                            scopedDirectory,
                            "equipment-slots-*.json.previous",
                            SearchOption.TopDirectoryOnly)
                        .Select(path =>
                            path.Substring(
                                0,
                                path.Length -
                                    ".previous".Length)));
            }
            string exactProductPath = Path.Combine(
                runtime.Paths.ConfigPath,
                "protected-items",
                "equipment-slots",
                "slot-" +
                    scope.ArchiveIndex.ToString(
                        CultureInfo.InvariantCulture),
                "equipment-slots-" +
                    MoreEquipmentSlotsProductOwner +
                    ".json");
            equipmentFileStatusCallCount++;
            if (File.Exists(exactProductPath) ||
                File.Exists(exactProductPath + ".previous"))
                scannedPaths.Add(exactProductPath);
            equipmentDirectoryEnumerationCount++;
            scannedPaths.AddRange(Directory.GetFiles(runtime.Paths.ConfigPath, "equipment-slots-*.json", SearchOption.TopDirectoryOnly));
            scannedPaths.AddRange(
                Directory.GetFiles(
                    runtime.Paths.ConfigPath,
                    "equipment-slots-" +
                        MoreEquipmentSlotsProductOwner +
                        ".json.migration-capture-*",
                    SearchOption.TopDirectoryOnly));
            scannedPaths.AddRange(
                Directory.GetFiles(
                    runtime.Paths.ConfigPath,
                    "equipment-slots-" +
                        MoreEquipmentSlotsProductOwner +
                        ".json.migrated-product-v3-*",
                    SearchOption.TopDirectoryOnly));
            string protectedEquipmentRoot =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots");
            if (Directory.Exists(protectedEquipmentRoot))
            {
                scannedPaths.AddRange(
                    Directory.GetFiles(
                        protectedEquipmentRoot,
                        "*.flat.json",
                        SearchOption.AllDirectories)
                    .Where(
                        IsCanonicalProductMigrationBackup));
            }
            string migrationClaimRoot =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    ".equipment-slot-migration-claims",
                    MoreEquipmentSlotsProductOwner);
            if (Directory.Exists(migrationClaimRoot))
            {
                scannedPaths.AddRange(
                    Directory.GetFiles(
                        migrationClaimRoot,
                        "*.json",
                        SearchOption.TopDirectoryOnly));
                scannedPaths.AddRange(
                    Directory.GetFiles(
                        migrationClaimRoot,
                        "*.transition-*",
                        SearchOption.TopDirectoryOnly));
            }

            string[] distinctPaths =
                scannedPaths
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            var claimedScopedOwners =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
            foreach (string path in distinctPaths)
            {
                if (IsLegacyGlobalEquipmentSlotStoragePath(path))
                    continue;
                string claimedOwner =
                    GetEquipmentSlotOwnerClaimFromPath(path);
                if (!string.IsNullOrWhiteSpace(claimedOwner))
                    claimedScopedOwners.Add(claimedOwner);
            }

            var recoveredOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in distinctPaths)
            {
                try
                {
                    if (IsMoreEquipmentSlotsMigrationResiduePath(
                        path))
                    {
                        if (runtime.ModRegistry.IsLoaded(
                                MoreEquipmentSlotsProductOwner) ||
                            recoveredOwners.Contains(
                                MoreEquipmentSlotsProductOwner))
                        {
                            continue;
                        }
                        runtime.RuntimeMonitor.Log(
                            "EquipmentSlots retained unresolved MoreEquipmentSlots migration residue without creating or recovering empty authority: " +
                            path +
                            ".",
                            LogLevel.Warn);
                        runtime.SetHookStatus(
                            "Player.EquipmentSlotsApi",
                            "orphan-recovery-blocked",
                            "MoreEquipmentSlots migration residue",
                            "Interrupted capture, winner or claim-transition evidence requires the exact Product authority before recovery: " +
                            path);
                        continue;
                    }
                    bool isLegacyGlobalStorage =
                        IsLegacyGlobalEquipmentSlotStoragePath(
                            path);
                    string pathOwnerClaim =
                        GetEquipmentSlotOwnerClaimFromPath(path);
                    if (EmbeddedMoreEquipmentSlotsBlocksRecovery(pathOwnerClaim))
                        continue;
                    if (IsMoreEquipmentSlotsProductLoaded() &&
                        string.Equals(
                            pathOwnerClaim,
                            MoreEquipmentSlotsProductOwner,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (isLegacyGlobalStorage &&
                        claimedScopedOwners.Contains(
                            pathOwnerClaim))
                    {
                        runtime.RuntimeMonitor.Log(
                            "EquipmentSlots global orphan fallback blocked owner=" +
                            pathOwnerClaim +
                            " because a canonical scoped authority exists.",
                            LogLevel.Warn);
                        runtime.SetHookStatus(
                            "Player.EquipmentSlotsApi",
                            "orphan-recovery-blocked",
                            "canonical scoped equipment-slot authority",
                            "Retained global fallback for " +
                            pathOwnerClaim +
                            " without native recovery because its scoped authority exists.");
                        continue;
                    }
                    if (TryRecoverMoreEquipmentSlotsProductStorage(
                        path,
                        scope,
                        out bool productHandled,
                        out int productRecovered,
                        out string productMessage))
                    {
                        recoveredOwners.Add(
                            MoreEquipmentSlotsProductOwner);
                        runtime.RuntimeMonitor.Log(
                            "EquipmentSlots orphan recovery OK owner=" +
                            MoreEquipmentSlotsProductOwner +
                            " recovered=" +
                            productRecovered +
                            " message=" +
                            productMessage +
                            ".");
                        runtime.SetHookStatus(
                            "Player.EquipmentSlotsApi",
                            "orphan-recovery-ok",
                            "exact MoreEquipmentSlots ProductNative cold storage",
                            "Recovered disabled/uninstalled ProductNative storage scope=" +
                            scope.ScopeKey +
                            ": " +
                            productMessage);
                        continue;
                    }
                    if (productHandled)
                    {
                        runtime.RuntimeMonitor.Log(
                            "EquipmentSlots orphan recovery failed owner=" +
                            MoreEquipmentSlotsProductOwner +
                            " message=" +
                            productMessage +
                            ".",
                            LogLevel.Warn);
                        runtime.SetHookStatus(
                            "Player.EquipmentSlotsApi",
                            "orphan-recovery-failed",
                            "exact MoreEquipmentSlots ProductNative cold storage",
                            productMessage);
                        continue;
                    }

                    EquipmentSlotStorageDocument? document = ReadJson<EquipmentSlotStorageDocument>(path);
                    string ownerId = document?.OwnerId ?? string.Empty;
                    if (EmbeddedMoreEquipmentSlotsBlocksRecovery(ownerId))
                        continue;
                    if (isLegacyGlobalStorage &&
                        claimedScopedOwners.Contains(ownerId))
                    {
                        runtime.RuntimeMonitor.Log(
                            "EquipmentSlots global orphan fallback blocked owner=" +
                            ownerId +
                            " because a canonical scoped authority exists.",
                            LogLevel.Warn);
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(ownerId) || runtime.ModRegistry.IsLoaded(ownerId) || recoveredOwners.Contains(ownerId))
                        continue;

                    loadedEquipmentSlotStorageOwners.Remove(ownerId);
                    loadedEquipmentSlotStorageOwners.Add(ownerId);
                    if (!TryLoadEquipmentSlotStorageDocument(ownerId, path, scope, isLegacyGlobalStorage))
                    {
                        loadedEquipmentSlotStorageOwners.Remove(ownerId);
                        continue;
                    }

                    if (isLegacyGlobalStorage)
                        migratedLegacyEquipmentSlotStorageOwners.Add(ownerId);

                    if (RecoverEquipmentSlotEntries(
                        ownerId,
                        "orphan storage without registered mod",
                        EquipmentSlotTransactionOrigin.OrphanRecovery,
                        saveAfterRecovery: true,
                        out EquipmentSlotsRecoveryResult result))
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
            return true;
        }

        private bool IsMoreEquipmentSlotsMigrationResiduePath(
            string path)
        {
            try
            {
                string fullPath =
                    Path.GetFullPath(path);
                string capturePrefix =
                    Path.GetFullPath(
                        Path.Combine(
                            runtime.Paths.ConfigPath,
                            "equipment-slots-" +
                                MoreEquipmentSlotsProductOwner +
                                ".json.migration-capture-"));
                if (fullPath.StartsWith(
                    capturePrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                string archivePrefix =
                    Path.GetFullPath(
                        Path.Combine(
                            runtime.Paths.ConfigPath,
                            "equipment-slots-" +
                                MoreEquipmentSlotsProductOwner +
                                ".json.migrated-product-v3-"));
                if (fullPath.StartsWith(
                    archivePrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (IsCanonicalProductMigrationBackup(
                    fullPath))
                {
                    return true;
                }

                string claimRoot =
                    Path.GetFullPath(
                        Path.Combine(
                            runtime.Paths.ConfigPath,
                            ".equipment-slot-migration-claims",
                            MoreEquipmentSlotsProductOwner)) +
                    Path.DirectorySeparatorChar;
                return fullPath.StartsWith(
                        claimRoot,
                        StringComparison.OrdinalIgnoreCase) &&
                    (fullPath.EndsWith(
                         ".json",
                         StringComparison.OrdinalIgnoreCase) ||
                     fullPath.IndexOf(
                         ".transition-",
                         StringComparison.OrdinalIgnoreCase) >=
                     claimRoot.Length);
            }
            catch
            {
                return false;
            }
        }

        private static bool
            IsCanonicalProductMigrationBackup(string path)
        {
            string? directory =
                Path.GetDirectoryName(path);
            if (!string.Equals(
                    Path.GetFileName(directory),
                    ".legacy-migrations",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            string name =
                Path.GetFileName(path);
            const string suffix = ".flat.json";
            if (!name.EndsWith(
                    suffix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            string hash =
                name.Substring(
                    0,
                    name.Length - suffix.Length);
            return hash.Length == 64 &&
                hash.All(
                    value =>
                        (value >= '0' && value <= '9') ||
                        (value >= 'A' && value <= 'F') ||
                        (value >= 'a' && value <= 'f'));
        }

        private bool IsLegacyGlobalEquipmentSlotStoragePath(
            string path) =>
            Path.GetDirectoryName(path)?.Equals(
                runtime.Paths.ConfigPath,
                StringComparison.OrdinalIgnoreCase) == true;

        private static string GetEquipmentSlotOwnerClaimFromPath(
            string path)
        {
            const string prefix = "equipment-slots-";
            const string suffix = ".json";
            string name = Path.GetFileName(path) ?? string.Empty;
            if (!name.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase) ||
                !name.EndsWith(
                    suffix,
                    StringComparison.OrdinalIgnoreCase) ||
                name.Length <= prefix.Length + suffix.Length)
            {
                return string.Empty;
            }
            return name.Substring(
                prefix.Length,
                name.Length - prefix.Length - suffix.Length);
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

        private bool RecoverEquipmentSlotEntries(
            string ownerId,
            string reason,
            EquipmentSlotTransactionOrigin origin,
            bool saveAfterRecovery,
            out EquipmentSlotsRecoveryResult result)
        {
            result = new EquipmentSlotsRecoveryResult
            {
                OwnerId = ownerId ?? string.Empty
            };
            EnsureEquipmentSlotStorageLoaded(ownerId ?? string.Empty);
            if (blockedEquipmentSlotStorageOwners.Contains(
                ownerId ?? string.Empty))
            {
                result.Success = false;
                result.FailureReason = "storage-blocked";
                result.Message =
                    "Protected EquipmentSlots storage is present but invalid; recovery is refused fail-closed.";
                return false;
            }
            if (origin ==
                    EquipmentSlotTransactionOrigin.OwnerRecovery &&
                !PrepareEquipmentSlotOwnerRecoveryProjection(
                    ownerId ?? string.Empty,
                    out string projectionMessage))
            {
                result.Success = false;
                result.FailureReason =
                    "working-committed-projection-failed";
                result.Message = projectionMessage;
                return false;
            }
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
                if (RecoverEquipmentSlotEntry(
                    ownerId ?? string.Empty,
                    entry,
                    reason,
                    origin,
                    saveAfterRecovery: false,
                    out string message,
                    out int entryRecovered))
                {
                    recovered += entryRecovered;
                    messages.Add(message);
                    if (equipmentSlotJournals.ContainsKey(
                        ownerId ?? string.Empty))
                    {
                        int remaining =
                            entries.Count(candidate =>
                                !string.IsNullOrWhiteSpace(
                                    candidate.ItemId));
                        if (remaining > 0)
                        {
                            messages.Add(
                                "Deferred " +
                                remaining.ToString(
                                    CultureInfo.InvariantCulture) +
                                " additional held item(s) until the current typed recovery journal commits.");
                        }
                        break;
                    }
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

        private bool RecoverEquipmentSlotEntry(
            string ownerId,
            EquipmentSlotRuntimeEntry entry,
            string reason,
            EquipmentSlotTransactionOrigin origin,
            bool saveAfterRecovery,
            out string message,
            out int recoveredCount) =>
            RecoverEquipmentSlotEntryTransactional(
                ownerId,
                entry,
                reason,
                origin,
                saveAfterRecovery,
                out message,
                out recoveredCount);

        private static int CountNativeBackpackItem(Type? dolocApi, string itemId)
        {
            if (dolocApi == null || string.IsNullOrWhiteSpace(itemId))
                return 0;
            return InvokeInt(dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null), null, new object?[] { itemId, false }, 0);
        }

        internal string GetEquipmentSlotsLifecycleSummary() => "equipmentStorageOwners=" + loadedEquipmentSlotStorageOwners.Count + ", equipmentDirtyOwners=" + dirtyEquipmentSlotStorageOwners.Count + ", equipmentJournals=" + equipmentSlotJournals.Count + ", equipmentProductColdRecoverySessions=" + productColdRecoverySessions.Count + ", equipmentOutcomeUnknown=" + equipmentSlotWorkingPlacementGuards.Count + ", equipmentEntries=" + equipmentSlotEntries.Values.Sum(entries => entries.Count);

        private sealed class EquipmentSlotRuntimeEntry
        {
            public string OwnerId { get; set; } = string.Empty;
            public int Index { get; set; }
            public string SlotId { get; set; } = string.Empty;
            public string ItemId { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string SkillId { get; set; } = string.Empty;
            public int DefenseBonus { get; set; }
            public bool IsShieldHat { get; set; }
            public int ShieldMaxValue { get; set; }
            public int ShieldValue { get; set; }
            public int ShieldDefend { get; set; }
            public bool Applied { get; set; }
            public string LastMessage { get; set; } = string.Empty;
            public object? NativeItem { get; set; }
            public object? NativeFunction { get; set; }
            public bool CommittedItemReleasedToNative { get; set; }
        }

        private static List<EquipmentSlotRuntimeEntry> CloneEquipmentSlotRuntimeEntries(
            IEnumerable<EquipmentSlotRuntimeEntry> entries) =>
            (entries ?? Array.Empty<EquipmentSlotRuntimeEntry>())
                .Select(entry => new EquipmentSlotRuntimeEntry
                {
                    OwnerId = entry.OwnerId,
                    Index = entry.Index,
                    SlotId = entry.SlotId,
                    ItemId = entry.ItemId,
                    DisplayName = entry.DisplayName,
                    SkillId = entry.SkillId,
                    DefenseBonus = entry.DefenseBonus,
                    IsShieldHat = entry.IsShieldHat,
                    ShieldMaxValue = entry.ShieldMaxValue,
                    ShieldValue = entry.ShieldValue,
                    ShieldDefend = entry.ShieldDefend,
                    LastMessage = entry.LastMessage,
                    CommittedItemReleasedToNative =
                        entry.CommittedItemReleasedToNative
                })
                .ToList();

        private static List<EquipmentSlotStorageEntry> ToEquipmentSlotStorageEntries(
            IEnumerable<EquipmentSlotRuntimeEntry> entries,
            int totalSlots) =>
            (entries ?? Array.Empty<EquipmentSlotRuntimeEntry>())
                .Select(entry => new EquipmentSlotStorageEntry
                {
                    Index = entry.Index,
                    TailIndexFromEnd =
                        EquipmentSlotProtectedStoragePolicy.GetTailIndexFromEnd(
                            entry.Index,
                            totalSlots),
                    SlotId = entry.SlotId,
                    ItemId = entry.ItemId,
                    DisplayName = entry.DisplayName,
                    SkillId = entry.SkillId,
                    DefenseBonus = entry.DefenseBonus,
                    IsShieldHat = entry.IsShieldHat,
                    ShieldMaxValue = entry.ShieldMaxValue,
                    ShieldValue = entry.ShieldValue,
                    ShieldDefend = entry.ShieldDefend,
                    LastMessage = entry.LastMessage
                })
                .ToList();

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

            [DataMember(Name = "generation", EmitDefaultValue = false)]
            public long Generation { get; set; }

            [DataMember(Name = "slots")]
            public List<EquipmentSlotStorageEntry> Slots { get; set; } = new List<EquipmentSlotStorageEntry>();

            [DataMember(Name = "journal", EmitDefaultValue = false)]
            public EquipmentSlotTransactionJournal? Journal { get; set; }

            [DataMember(Name = "gameplayCandidate", EmitDefaultValue = false)]
            public EquipmentSlotCompatibilityGameplayCandidate? GameplayCandidate { get; set; }
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

            [DataMember(Name = "defenseBonus")]
            public int DefenseBonus { get; set; }

            [DataMember(Name = "isShieldHat")]
            public bool IsShieldHat { get; set; }

            [DataMember(Name = "shieldMaxValue")]
            public int ShieldMaxValue { get; set; }

            [DataMember(Name = "shieldValue")]
            public int ShieldValue { get; set; }

            [DataMember(Name = "shieldDefend")]
            public int ShieldDefend { get; set; }

            [DataMember(Name = "lastMessage")]
            public string LastMessage { get; set; } = string.Empty;
        }
    }

}
