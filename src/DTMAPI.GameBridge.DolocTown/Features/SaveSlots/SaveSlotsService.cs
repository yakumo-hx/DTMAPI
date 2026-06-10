using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class SaveSlotsService : ISaveSlotsApi
    {
        private const int VanillaArchiveSlotCount = 6;
        private static readonly TimeSpan PendingRefreshInterval = TimeSpan.FromMilliseconds(750);
        private static readonly TimeSpan ConfiguredRefreshInterval = TimeSpan.FromSeconds(3);
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, SaveSlotsOptions> saveSlotOptions = new Dictionary<string, SaveSlotsOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SaveSlotsState> saveSlotStates = new Dictionary<string, SaveSlotsState>(StringComparer.OrdinalIgnoreCase);
        private DateTimeOffset lastRuntimeRefreshAttemptAtUtc = DateTimeOffset.MinValue;
        private bool nativeArchiveSlotManagerPending;

        public SaveSlotsService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        public SaveSlotsRegisterResult RegisterSlots(IManifest owner, SaveSlotsOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            SaveSlotsOptions normalized = NormalizeSaveSlotsOptions(options);
            saveSlotOptions[owner.UniqueID] = normalized;
            SaveSlotsRegisterResult result = ApplySaveSlotExpansion(owner.UniqueID, "register");
            runtime.RuntimeMonitor.Log("SaveSlots API register success=" + result.Success + " owner=" + owner.UniqueID + " requested=" + result.RequestedSlotCount + " applied=" + result.AppliedSlotCount + " reason=register message=" + result.Message);
            return result;
        }

        SaveSlotsState ISaveSlotsApi.GetState(string uniqueId)
        {
            string ownerId = uniqueId ?? string.Empty;
            int nativeCount = GetNativeArchiveSlotCount();
            if (saveSlotStates.TryGetValue(ownerId, out SaveSlotsState state))
            {
                state.NativeSlotCount = nativeCount;
                return state;
            }

            return new SaveSlotsState
            {
                OwnerId = ownerId,
                IsConfigured = false,
                Enabled = false,
                NativeSlotCount = nativeCount,
                RequestedSlotCount = VanillaArchiveSlotCount,
                AppliedSlotCount = nativeCount,
                Status = "not-configured",
                LastMessage = "No save-slot expansion policy registered."
            };
        }

        BridgeFeatureStatus ISaveSlotsApi.GetStatus(string uniqueId)
        {
            SaveSlotsState state = ((ISaveSlotsApi)this).GetState(uniqueId ?? string.Empty);
            return new BridgeFeatureStatus(state.Status, state.LastMessage);
        }

        internal void RefreshSaveSlotExpansionForRuntime(bool force, string reason)
        {
            if (saveSlotOptions.Count == 0)
                return;

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!force && !ShouldRefreshForRuntime(now))
                return;

            lastRuntimeRefreshAttemptAtUtc = now;
            int target = ComputeRequestedSaveSlotCount();
            int native = GetNativeArchiveSlotCount();
            if (force || native != target || HasPendingNativeManagerState())
                ApplySaveSlotExpansion(string.Empty, (reason ?? "runtime refresh") + " native=" + native + " target=" + target);
        }

        private bool ShouldRefreshForRuntime(DateTimeOffset now)
        {
            TimeSpan interval = nativeArchiveSlotManagerPending || HasPendingNativeManagerState()
                ? PendingRefreshInterval
                : ConfiguredRefreshInterval;
            return now - lastRuntimeRefreshAttemptAtUtc >= interval;
        }

        private bool HasPendingNativeManagerState()
        {
            return saveSlotStates.Values.Any(state => state.Status == "pending-native-game-manager");
        }

        private SaveSlotsRegisterResult ApplySaveSlotExpansion(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            SaveSlotsOptions ownerOptions = GetSaveSlotsOptions(ownerId);
            int requested = ownerOptions.Enabled ? ownerOptions.SlotCount : VanillaArchiveSlotCount;
            var result = new SaveSlotsRegisterResult
            {
                OwnerId = ownerId,
                RequestedSlotCount = requested
            };

            if (!TryGetNativeArchiveSlotManager(out object? manager, out string managerMessage))
            {
                nativeArchiveSlotManagerPending = true;
                result.Success = false;
                result.FailureReason = "missing-game-manager";
                result.Message = managerMessage;
                saveSlotStates[ownerId] = BuildSaveSlotsState(ownerId, ownerOptions, 0, 0, "pending-native-game-manager", managerMessage);
                runtime.SetHookStatus("Save.MoreSlotsApi", "pending", "DolocAPI.gameManager.archiveFileCount", managerMessage);
                return result;
            }

            nativeArchiveSlotManagerPending = false;
            if (ownerId.Length == 0)
                saveSlotStates.Remove(string.Empty);

            int previous = ReadIntMember(manager!, "archiveFileCount", VanillaArchiveSlotCount);
            int applied = ComputeRequestedSaveSlotCount();
            bool set = SetMemberValue(manager!, "archiveFileCount", applied);
            int nativeAfter = ReadIntMember(manager!, "archiveFileCount", previous);
            result.PreviousSlotCount = previous;
            result.AppliedSlotCount = nativeAfter;
            result.Success = set && nativeAfter == applied;
            result.FailureReason = result.Success ? string.Empty : "archive-count-set-failed";
            result.Message = result.Success
                ? "Official save slot count set " + previous + "->" + nativeAfter + " through DolocAPI.gameManager.archiveFileCount; LocalSave and GameDataPanel keep owning archive files/UI."
                : "Failed to set DolocAPI.gameManager.archiveFileCount to " + applied + "; native count is " + nativeAfter + ".";

            UpdateSaveSlotsStates(nativeAfter, applied, result.Message);
            runtime.SetHookStatus("Save.MoreSlotsApi", result.Success ? "configured-official-archive-count" : "failed", "DolocAPI.gameManager.archiveFileCount -> LocalSave.GetAllArchiveInfo -> GameDataPanel.Render", result.Message + " reason=" + (reason ?? string.Empty) + ".");
            return result;
        }

        private void UpdateSaveSlotsStates(int nativeSlotCount, int appliedSlotCount, string message)
        {
            foreach (KeyValuePair<string, SaveSlotsOptions> entry in saveSlotOptions.ToArray())
            {
                SaveSlotsOptions options = entry.Value ?? new SaveSlotsOptions { Enabled = false, SlotCount = VanillaArchiveSlotCount };
                saveSlotStates[entry.Key] = BuildSaveSlotsState(entry.Key, options, nativeSlotCount, appliedSlotCount, options.Enabled ? "configured-official-archive-count" : "disabled-vanilla-slot-count", message);
            }
        }

        private static SaveSlotsState BuildSaveSlotsState(string ownerId, SaveSlotsOptions options, int nativeSlotCount, int appliedSlotCount, string status, string message)
        {
            return new SaveSlotsState
            {
                OwnerId = ownerId ?? string.Empty,
                IsConfigured = true,
                Enabled = options.Enabled,
                NativeSlotCount = nativeSlotCount,
                RequestedSlotCount = options.Enabled ? options.SlotCount : VanillaArchiveSlotCount,
                AppliedSlotCount = appliedSlotCount,
                Status = status ?? string.Empty,
                LastMessage = message ?? string.Empty
            };
        }

        private SaveSlotsOptions GetSaveSlotsOptions(string ownerId)
        {
            return saveSlotOptions.TryGetValue(ownerId ?? string.Empty, out SaveSlotsOptions? options)
                ? options
                : new SaveSlotsOptions { Enabled = false, SlotCount = VanillaArchiveSlotCount };
        }

        private int ComputeRequestedSaveSlotCount()
        {
            int target = VanillaArchiveSlotCount;
            foreach (SaveSlotsOptions options in saveSlotOptions.Values)
            {
                if (options?.Enabled == true)
                    target = Math.Max(target, ClampInt(options.SlotCount, VanillaArchiveSlotCount, 60));
            }
            return target;
        }

        private static SaveSlotsOptions NormalizeSaveSlotsOptions(SaveSlotsOptions? options)
        {
            options ??= new SaveSlotsOptions();
            return new SaveSlotsOptions
            {
                Enabled = options.Enabled,
                SlotCount = ClampInt(options.SlotCount, VanillaArchiveSlotCount, 60),
                VerboseLogging = options.VerboseLogging
            };
        }

        private static bool TryGetNativeArchiveSlotManager(out object? manager, out string message)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            manager = ReadStaticMember(dolocApi, "gameManager");
            if (manager == null)
            {
                message = "DolocAPI.gameManager is not available yet; save slot expansion will retry during runtime updates.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static int GetNativeArchiveSlotCount()
        {
            return TryGetNativeArchiveSlotManager(out object? manager, out _)
                ? ReadIntMember(manager!, "archiveFileCount", VanillaArchiveSlotCount)
                : 0;
        }

        private static int ClampInt(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static bool SetMemberValue(object instance, string name, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && (value == null || field.FieldType.IsInstanceOfType(value)))
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && (value == null || property.PropertyType.IsInstanceOfType(value)))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }
    }
}
