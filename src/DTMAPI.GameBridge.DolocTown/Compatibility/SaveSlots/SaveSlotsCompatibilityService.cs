#pragma warning disable CS0618 // Frozen ISaveSlotsApi compatibility implementation.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Frozen fixed-six/twelve executor for already-built ISaveSlotsApi consumers.</summary>
    internal sealed class SaveSlotsCompatibilityService : ISaveSlotsApi
    {
        private const int VanillaSlotCount = 6;
        private const int ExpandedSlotCount = 12;
        private const string ManagedProductUniqueId = "DTMAPI.MoreSavesMod";
        private readonly DtmApiRuntime runtime;
        private readonly Func<object?> managerProvider;
        private readonly Func<object, int> reader;
        private readonly Func<object, int, bool> writer;
        private readonly Dictionary<string, SaveSlotsOptions> options = new Dictionary<string, SaveSlotsOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SaveSlotsState> states = new Dictionary<string, SaveSlotsState>(StringComparer.OrdinalIgnoreCase);
        private bool managerPending;
        private bool restorePending;

        internal SaveSlotsCompatibilityService(
            DtmApiRuntime runtime,
            Func<object?>? managerProvider = null,
            Func<object, int>? reader = null,
            Func<object, int, bool>? writer = null)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.managerProvider = managerProvider ?? ResolveManager;
            this.reader = reader ?? (manager => ReadIntMember(manager, "archiveFileCount", VanillaSlotCount));
            this.writer = writer ?? WriteArchiveCount;
        }

        public SaveSlotsRegisterResult RegisterSlots(IManifest owner, SaveSlotsOptions requested)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            SaveSlotsOptions normalized = Normalize(requested);
            if (!MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.CompatibilityOwner))
            {
                if (!MoreSavesNativeOwnerCoordinator.TryAcquire(MoreSavesNativeOwnerCoordinator.CompatibilityOwner, out string failure))
                    throw new InvalidOperationException("Frozen ISaveSlotsApi compatibility failed closed against " + ManagedProductUniqueId + ": " + failure);
            }

            options[owner.UniqueID] = normalized;
            SetOwnerDemand(owner.UniqueID, normalized.Enabled, "frozen save-slot policy registered");
            SaveSlotsRegisterResult result;
            if (HasEnabledOwners())
            {
                restorePending = false;
                result = ApplyTarget(owner.UniqueID, ExpandedSlotCount, "register");
            }
            else if (MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.CompatibilityOwner))
            {
                result = ApplyTarget(owner.UniqueID, VanillaSlotCount, "disabled");
                ReleaseAfterRestore(result.Success, "disabled registration");
            }
            else
                throw new InvalidOperationException("Frozen ISaveSlotsApi compatibility lost its exact owner before disabled-state restoration.");
            ReconcilePendingDemand("register");
            return result;
        }

        public SaveSlotsState GetState(string uniqueId)
        {
            string ownerId = uniqueId ?? string.Empty;
            if (states.TryGetValue(ownerId, out SaveSlotsState? state))
            {
                state.NativeSlotCount = ReadNativeOrZero();
                return state;
            }
            int native = ReadNativeOrZero();
            return BuildState(
                ownerId,
                new SaveSlotsOptions { Enabled = false, SlotCount = VanillaSlotCount },
                native,
                native,
                "not-configured",
                "No frozen save-slot policy registered.",
                false);
        }

        public BridgeFeatureStatus GetStatus(string uniqueId)
        {
            SaveSlotsState state = GetState(uniqueId);
            return new BridgeFeatureStatus(state.Status, state.LastMessage);
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            SetOwnerDemand(ownerId, false, "frozen save-slot owner cleanup " + (reason ?? string.Empty));
            int removed = (options.Remove(ownerId) ? 1 : 0) + (states.Remove(ownerId) ? 1 : 0);
            if (removed == 0)
                return 0;

            if (HasEnabledOwners())
                ApplyTarget(string.Empty, ExpandedSlotCount, "owner cleanup");
            else if (MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.CompatibilityOwner))
            {
                SaveSlotsRegisterResult restored = ApplyTarget(string.Empty, VanillaSlotCount, "final owner cleanup");
                ReleaseAfterRestore(restored.Success, "final owner cleanup");
            }
            ReconcilePendingDemand("owner cleanup");
            return removed;
        }

        internal int CountOwnerResources(string ownerId) =>
            (options.ContainsKey(ownerId ?? string.Empty) ? 1 : 0) +
            (states.ContainsKey(ownerId ?? string.Empty) ? 1 : 0);

        internal void RefreshSaveSlotExpansionForRuntime(bool force, string reason)
        {
            if (HasEnabledOwners())
            {
                if (!MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.CompatibilityOwner) &&
                    !MoreSavesNativeOwnerCoordinator.TryAcquire(MoreSavesNativeOwnerCoordinator.CompatibilityOwner, out _))
                    return;
                ApplyTarget(string.Empty, ExpandedSlotCount, reason ?? "runtime retry");
            }
            else if ((restorePending || managerPending) &&
                MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.CompatibilityOwner))
            {
                SaveSlotsRegisterResult restored = ApplyTarget(string.Empty, VanillaSlotCount, reason ?? "restore retry");
                ReleaseAfterRestore(restored.Success, reason ?? "restore retry");
            }
            ReconcilePendingDemand(reason ?? "runtime retry");
        }

        private SaveSlotsRegisterResult ApplyTarget(string ownerId, int target, string reason)
        {
            object? manager;
            try
            {
                manager = managerProvider();
            }
            catch (Exception ex)
            {
                manager = null;
                return Fail(ownerId, target, "native-manager-read-failed", ex.GetType().Name + ": " + ex.Message);
            }
            if (manager == null)
                return Fail(ownerId, target, "missing-game-manager", "DolocAPI.gameManager is unavailable; the frozen executor will retry.");

            int previous;
            try
            {
                previous = reader(manager);
                bool wrote = writer(manager, target);
                int actual = reader(manager);
                bool success = wrote && actual == target;
                managerPending = !success;
                if (target == VanillaSlotCount)
                    restorePending = !success;
                string message = success
                    ? "Frozen save-slot compatibility set GameManager.archiveFileCount " + previous + "->" + actual + " without a UI Hook."
                    : "Frozen save-slot compatibility could not set GameManager.archiveFileCount to " + target + "; native=" + actual + ".";
                UpdateStates(actual, target, success ? "configured-official-archive-count" : "archive-count-set-failed", message);
                runtime.SetHookStatus("Save.MoreSlotsApi", success ? "configured-official-archive-count" : "failed", "DolocAPI.gameManager.archiveFileCount", message + " reason=" + reason + ".");
                return BuildResult(ownerId, previous, RequestedFor(ownerId, target), actual, success, success ? string.Empty : "archive-count-set-failed", message);
            }
            catch (Exception ex)
            {
                return Fail(ownerId, target, "native-archive-count-failed", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private SaveSlotsRegisterResult Fail(string ownerId, int target, string failure, string message)
        {
            managerPending = true;
            if (target == VanillaSlotCount)
                restorePending = true;
            UpdateStates(0, 0, "pending-native-game-manager", message);
            runtime.SetHookStatus("Save.MoreSlotsApi", "pending", "DolocAPI.gameManager.archiveFileCount", message);
            return BuildResult(ownerId, 0, RequestedFor(ownerId, target), 0, false, failure, message);
        }

        private void ReleaseAfterRestore(bool restored, string reason)
        {
            restorePending = !restored;
            if (MoreSavesNativeOwnerCoordinator.ReleaseAfterNativeRestore(MoreSavesNativeOwnerCoordinator.CompatibilityOwner, restored))
                runtime.RuntimeMonitor.Log("Frozen ISaveSlotsApi released archive-count ownership after native six-slot restoration; reason=" + reason + ".");
        }

        private void UpdateStates(int native, int applied, string status, string message)
        {
            foreach (KeyValuePair<string, SaveSlotsOptions> item in options.ToArray())
            {
                string ownerStatus = status == "configured-official-archive-count" && !item.Value.Enabled
                    ? "disabled-vanilla-slot-count"
                    : status;
                states[item.Key] = BuildState(item.Key, item.Value, native, applied, ownerStatus, message, true);
            }
        }

        private void SetOwnerDemand(string ownerId, bool active, string reason) =>
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.SaveSlots, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "frozen-enabled-policy", active, reason);

        private void ReconcilePendingDemand(string reason) =>
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.SaveSlotsPendingRestore, GameBridgeDemandRoutes.OperationOwner, RuntimeDemandSourceType.CapabilityOperation, RuntimeDemandLifetime.Operation, "frozen-native-manager-restore", managerPending || restorePending, reason);

        private bool HasEnabledOwners() => options.Values.Any(value => value.Enabled);

        private int RequestedFor(string ownerId, int effectiveTarget) =>
            options.TryGetValue(ownerId ?? string.Empty, out SaveSlotsOptions? value)
                ? (value.Enabled ? ExpandedSlotCount : VanillaSlotCount)
                : effectiveTarget;

        private int ReadNativeOrZero()
        {
            try
            {
                object? manager = managerProvider();
                return manager == null ? 0 : reader(manager);
            }
            catch
            {
                return 0;
            }
        }

        private static SaveSlotsOptions Normalize(SaveSlotsOptions? value) =>
            new SaveSlotsOptions
            {
                Enabled = value?.Enabled ?? true,
                SlotCount = value?.Enabled == false ? VanillaSlotCount : ExpandedSlotCount,
                VerboseLogging = value?.VerboseLogging ?? false
            };

        private static SaveSlotsRegisterResult BuildResult(string owner, int previous, int requested, int applied, bool success, string failure, string message) =>
            new SaveSlotsRegisterResult
            {
                OwnerId = owner ?? string.Empty,
                PreviousSlotCount = previous,
                RequestedSlotCount = requested,
                AppliedSlotCount = applied,
                Success = success,
                FailureReason = failure ?? string.Empty,
                Message = message ?? string.Empty
            };

        private static SaveSlotsState BuildState(
            string owner,
            SaveSlotsOptions value,
            int native,
            int applied,
            string status,
            string message,
            bool isConfigured) =>
            new SaveSlotsState
            {
                OwnerId = owner ?? string.Empty,
                IsConfigured = isConfigured,
                Enabled = value.Enabled,
                NativeSlotCount = native,
                RequestedSlotCount = value.Enabled ? ExpandedSlotCount : VanillaSlotCount,
                AppliedSlotCount = applied,
                Status = status ?? string.Empty,
                LastMessage = message ?? string.Empty
            };

        private static object? ResolveManager() =>
            ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "gameManager");

        private static bool WriteArchiveCount(object manager, int value)
        {
            for (Type? type = manager.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField("archiveFileCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(manager, value);
                    return true;
                }
                PropertyInfo? property = type.GetProperty("archiveFileCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property?.CanWrite == true)
                {
                    property.SetValue(manager, value);
                    return true;
                }
            }
            return false;
        }
    }
}
