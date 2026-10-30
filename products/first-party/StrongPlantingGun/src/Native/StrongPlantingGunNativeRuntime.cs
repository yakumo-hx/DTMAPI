using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using global::DTMAPI.Abstractions;

namespace DTMAPI.StrongPlantingGun
{
    internal sealed partial class StrongPlantingGunNativeRuntime
    {
        private readonly IMonitor monitor;
        private readonly StrongPlantingGunHookInstaller hooks;
        private readonly StrongPlantingGunReflectionCache reflection =
            new StrongPlantingGunReflectionCache();
        private readonly StrongPlantingGunLogGate logGate =
            new StrongPlantingGunLogGate();
        private readonly Dictionary<object, int> capacitySnapshots =
            new Dictionary<object, int>(
                ReferenceIdentityComparer.Instance);
        private bool callbackAttached;

        internal StrongPlantingGunNativeRuntime(
            IMonitor monitor,
            Func<int, bool>? installGate = null,
            Action? unpatchOverride = null)
        {
            this.monitor = monitor ??
                throw new ArgumentNullException(nameof(monitor));
            hooks = new StrongPlantingGunHookInstaller(
                monitor,
                installGate,
                unpatchOverride);
            Status = "created";
            LastMessage =
                "ProductNative fixed-four policy has not been configured.";
        }

        internal bool Enabled => callbackAttached;

        internal int InstalledPatchCount =>
            hooks.InstalledPatchCount;

        internal int ExpandedGunCount { get; private set; }

        internal int LastConsumedItemCount { get; private set; }

        internal string Status { get; private set; }

        internal string LastMessage { get; private set; }

        internal int CachedMemberCount => reflection.Count;

        internal int CapacitySnapshotCount =>
            capacitySnapshots.Count;

        internal void SetCallbackAttached(bool value) =>
            callbackAttached = value;

        internal void Configure(
            string reason)
        {
            CancelToolBatch("activation boundary", refreshTip: true);

            try
            {
                if (!callbackAttached &&
                    (hooks.InstalledPatchCount > 0 ||
                     capacitySnapshots.Count > 0))
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun retained a failed prior owner root and requires restart before re-enable.");
                }
                hooks.InstallAtomically(this);
                SubscribeBatchContext();
                Status = "configured-product-native";
                LastMessage = "Four native material slots; reason=" + reason + ".";
                StrongPlantingGunCallbacks
                    .PublishLifecycleSummary(
                        BuildLifecycleSummary());
            }
            catch (Exception ex)
            {
                Status = hooks.InstalledPatchCount > 0
                    ? "enable-failed-owned"
                    : "enable-failed-closed";
                LastMessage =
                    "ProductNative enable failed: " +
                    ex.GetType().Name +
                    ": " +
                    ex.Message;
                throw;
            }
        }

        internal int ConfigureLoadedSaveBoundary(
            string reason)
        {
            Configure(reason);

            try
            {
                return PrepareLoadedBackpackGuns(reason);
            }
            catch (Exception preparationFailure)
            {
                var failures = new List<Exception>
                {
                    preparationFailure
                };
                try
                {
                    CleanupProductNative(
                        "save-load-failed-closed",
                        reason,
                        clearObservations: true);
                }
                catch (Exception cleanupFailure)
                {
                    failures.Add(cleanupFailure);
                }
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "StrongPlantingGun SaveLoaded preparation failed and exact-owner rollback also failed.",
                    failures);
            }
        }

        internal int PrepareLoadedBackpackGuns(
            string reason)
        {
            object archive =
                reflection.ReadStaticMember(
                    typeof(DolocAPI),
                    "archiveHandle") ??
                throw new InvalidOperationException(
                    "StrongPlantingGun could not resolve the loaded native archive at SaveLoaded.");
            object inventorySystem =
                reflection.ReadMember(
                    archive,
                    "InventorySystem") ??
                throw new InvalidOperationException(
                    "StrongPlantingGun could not resolve the loaded native InventorySystem at SaveLoaded.");
            object backpack =
                reflection.ReadMember(
                    inventorySystem,
                    "inventory") ??
                throw new InvalidOperationException(
                    "StrongPlantingGun could not resolve the loaded native backpack at SaveLoaded.");
            return PrepareLoadedInventory(
                backpack,
                reason);
        }

        internal int PrepareLoadedInventory(
            object inventory,
            string reason)
        {
            if (!callbackAttached || inventory == null)
                return 0;

            int capacity = Math.Max(
                0,
                reflection.ReadInt(
                    inventory,
                    "capacity",
                    0));
            int prepared = 0;
            for (int index = 0;
                 index < capacity;
                 index++)
            {
                object? candidate =
                    ReadInventoryItemAt(
                        inventory,
                        index);
                if (candidate == null ||
                    !IsTypeOrBase(
                        candidate.GetType(),
                        "DolocTown.ItemFarmingGun"))
                {
                    continue;
                }

                PrepareGun(
                    candidate,
                    (reason ?? string.Empty) +
                    " backpack[" +
                    index.ToString(
                        CultureInfo.InvariantCulture) +
                    "]");
                prepared++;
            }
            return prepared;
        }

        internal void SuspendForTitle(string reason) =>
            CleanupProductNative(
                "title-suspended",
                reason,
                clearObservations: true);

        internal void DeactivateOwner(string reason) =>
            CleanupProductNative(
                "deactivated",
                reason,
                clearObservations: true);

        internal void PrepareGun(
            object gun,
            string reason)
        {
            if (!callbackAttached ||
                gun == null ||
                !IsTypeOrBase(
                    gun.GetType(),
                    "DolocTown.ItemFarmingGun"))
            {
                return;
            }

            object? inventory =
                reflection.ReadMember(gun, "inventory");
            object? function =
                reflection.ReadMember(gun, "func");
            if (inventory == null || function == null)
                return;

            SnapshotFunctionCapacity(function);
            int previous =
                reflection.ReadInt(inventory, "capacity", 0);
            int inventoryTarget =
                StrongPlantingGunProductContract
                    .GetTargetInventoryCapacity(previous);
            int functionTarget =
                StrongPlantingGunProductContract.FixedSlotCount;
            bool expanded = false;
            if (previous < inventoryTarget)
            {
                MethodInfo? validate =
                    reflection.GetMethod(
                        inventory.GetType(),
                        "ValidateCapacity",
                        2) ??
                    reflection.GetMethod(
                        inventory.GetType(),
                        "ValidateCapacity",
                        1);
                if (validate == null)
                {
                    throw new MissingMethodException(
                        inventory.GetType().FullName,
                        "ValidateCapacity(int[,bool])");
                }
                ParameterInfo[] parameters =
                    validate.GetParameters();
                validate.Invoke(
                    inventory,
                    parameters.Length == 2
                        ? new object[]
                        {
                            inventoryTarget,
                            false
                        }
                        : new object[]
                        {
                            inventoryTarget
                        });
                expanded = true;
                ExpandedGunCount++;
            }

            if (!reflection.TrySetCapacity(
                function,
                functionTarget))
            {
                throw new MissingMemberException(
                    function.GetType().FullName,
                    "Capacity");
            }

            Status = "configured-product-native";
            LastMessage =
                "Prepared native farming gun capacity=" +
                inventoryTarget +
                " functionCapacity=" +
                functionTarget +
                " previous=" +
                previous +
                " fixedSlots=4 reason=" +
                (reason ?? string.Empty) +
                ".";
            StrongPlantingGunCallbacks.PublishLifecycleSummary(
                BuildLifecycleSummary());
            if (logGate.ShouldLog(
                false,
                "prepare",
                expanded))
            {
                monitor.Log(
                    "StrongPlantingGun " +
                    LastMessage);
            }
        }

        internal bool HandleToolUse(object gun) => StartToolBatch(gun);

        internal string BuildLifecycleSummary(
            int? callbacksOverride = null,
            int? hooksOverride = null) =>
            StrongPlantingGunLifecycleSummary.Format(
                callbacksOverride ??
                    (callbackAttached ? 1 : 0),
                hooksOverride ?? hooks.InstalledPatchCount,
                reflection.Count,
                capacitySnapshots.Count,
                batchContextSubscription == null ? 0 : 1) + ";pendingBatches=" + PendingBatchCount + ";pendingCallbacks=" + PendingCallbackCount;

        private void CleanupProductNative(
            string finalStatus,
            string reason,
            bool clearObservations)
        {
            CancelToolBatch(reason, refreshTip: false);
            batchContextSubscription?.Dispose();
            batchContextSubscription = null;
            List<Exception> failures =
                StrongPlantingGunCleanupTransaction.Run(
                    RestoreFunctionCapacities,
                    () => { hooks.UnpatchOwnedHooks(this); RestoreRangePreview(); });
            reflection.Clear();
            logGate.Reset();
            if (clearObservations)
            {
                ExpandedGunCount = 0;
                LastConsumedItemCount = 0;
            }
            StrongPlantingGunCallbacks.Detach(
                this,
                BuildLifecycleSummary(
                    callbacksOverride: 0));

            if (failures.Count > 0)
            {
                Status = hooks.InstalledPatchCount > 0
                    ? "cleanup-failed-owned"
                    : "cleanup-failed-unpatched";
                LastMessage =
                    "ProductNative cleanup had " +
                    failures.Count +
                    " failure(s) reason=" +
                    (reason ?? string.Empty) +
                    "; " +
                    BuildLifecycleSummary(
                        callbacksOverride: 0) +
                    ".";
                if (failures.Count == 1)
                    throw failures[0];
                throw new AggregateException(
                    "StrongPlantingGun native capacity restoration and exact-owner cleanup failed.",
                    failures);
            }

            Status = finalStatus;
            LastMessage =
                "Native function capacity restored; per-item inventories retained; owner removed reason=" +
                (reason ?? string.Empty) +
                ".";
        }

        private int SnapshotFunctionCapacity(object function)
        {
            if (capacitySnapshots.TryGetValue(
                function,
                out int original))
            {
                return original;
            }
            original = reflection.ReadCapacity(function);
            if (original <= 0)
            {
                throw new InvalidOperationException(
                    "StrongPlantingGun could not prove a positive original ItemFunctionFarmingGun.Capacity.");
            }
            capacitySnapshots.Add(function, original);
            return original;
        }

        private void RestoreFunctionCapacities()
        {
            if (capacitySnapshots.Count == 0)
                return;

            var failures = new List<Exception>();
            var restored = new List<object>();
            foreach (KeyValuePair<object, int> entry
                     in capacitySnapshots)
            {
                try
                {
                    if (!reflection.TrySetCapacity(
                        entry.Key,
                        entry.Value))
                    {
                        throw new MissingMemberException(
                            entry.Key.GetType().FullName,
                            "Capacity");
                    }
                    restored.Add(entry.Key);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }
            for (int index = 0;
                 index < restored.Count;
                 index++)
            {
                capacitySnapshots.Remove(restored[index]);
            }
            if (failures.Count == 1)
                throw failures[0];
            if (failures.Count > 1)
            {
                throw new AggregateException(
                    "StrongPlantingGun failed to restore one or more native function capacities.",
                    failures);
            }
        }

        private List<object> ReadUniqueEquipments(object gun)
        {
            MethodInfo? method = reflection.GetMethod(
                gun.GetType(),
                "GetEquipmentsFromArea",
                0);
            object? value = method?.Invoke(gun, null);
            var result = new List<object>();
            var seen = new HashSet<object>(
                ReferenceIdentityComparer.Instance);
            if (value is IEnumerable enumerable)
            {
                foreach (object? candidate in enumerable)
                {
                    if (candidate == null)
                        continue;
                    if (seen.Add(candidate))
                        result.Add(candidate);
                }
            }
            else if (value != null)
            {
                result.Add(value);
            }
            return result;
        }

        private ItemKind GetAllowedItemKind(object? item)
        {
            if (item == null)
                return ItemKind.None;
            Type type = item.GetType();
            if (IsTypeOrBase(type, "DolocTown.ItemSeed"))
            {
                return ItemKind.Seed;
            }
            if (IsTypeOrBase(type, "DolocTown.ItemFilm"))
            {
                return ItemKind.Film;
            }
            if (IsTypeOrBase(
                    type,
                    "DolocTown.ItemFertilizer"))
            {
                return ItemKind.Fertilizer;
            }
            object? globals = reflection.ReadStaticMember(typeof(DolocAPI), "GlobalParameter");
            if (globals != null &&
                reflection.ReadString(item, "name") == reflection.ReadString(globals, "ItemRefBottleOfWater"))
                return ItemKind.Water;
            return ItemKind.None;
        }

        private object? ReadInventoryItemAt(
            object inventory,
            int index) =>
            InvokeInventoryIndex(inventory, "Read", index);

        private object? InvokeInventoryIndex(
            object inventory,
            string methodName,
            int index)
        {
            MethodInfo? method = reflection.GetMethod(
                inventory.GetType(),
                methodName,
                1);
            return method?.Invoke(
                inventory,
                new object[]
                {
                    index
                });
        }

        private object? CloneItem(object item, int count)
        {
            MethodInfo? clone = reflection.GetMethod(
                item.GetType(),
                "Clone",
                1);
            return clone?.Invoke(
                item,
                new object[]
                {
                    count
                });
        }

        private bool InvokeInteraction(
            object gun,
            string methodName,
            object equipment,
            object item)
        {
            MethodInfo? method =
                reflection.GetInteractionMethod(
                    gun.GetType(),
                    methodName);
            if (method == null)
                return false;
            ParameterInfo[] parameters = method.GetParameters();
            if (!parameters[0].ParameterType
                .IsInstanceOfType(equipment))
            {
                return false;
            }
            return method.Invoke(
                gun,
                new[]
                {
                    equipment,
                    item
                }) is bool result &&
                result;
        }

        private bool TryCostAtIndex(
            object inventory,
            int index,
            int count,
            bool exactItem = false)
        {
            MethodInfo method = reflection.GetMethod(
                inventory.GetType(),
                "TryCostAtIndex",
                3) ?? throw new MissingMethodException(
                    inventory.GetType().FullName,
                    "TryCostAtIndex(int,int,bool)");
            return method.Invoke(
                inventory,
                new object[]
                {
                    index,
                    count,
                    exactItem
                }) is bool result &&
                result;
        }

        private void InvokeNoArg(
            object instance,
            string methodName) =>
            reflection.GetMethod(
                instance.GetType(),
                methodName,
                0)?.Invoke(instance, null);

        private static bool IsTypeOrBase(
            Type type,
            string fullName)
        {
            for (Type? current = type;
                 current != null;
                 current = current.BaseType)
            {
                if (current.FullName == fullName)
                    return true;
            }
            return false;
        }

        private enum ItemKind : byte
        {
            None,
            Seed,
            Fertilizer,
            Film,
            Water
        }

        private sealed class ReferenceIdentityComparer :
            IEqualityComparer<object>
        {
            internal static readonly ReferenceIdentityComparer Instance =
                new ReferenceIdentityComparer();

            public new bool Equals(object? left, object? right) =>
                ReferenceEquals(left, right);

            public int GetHashCode(object value) =>
                RuntimeHelpers.GetHashCode(value);
        }
    }
}
