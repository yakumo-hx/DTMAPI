using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using global::DTMAPI.Abstractions;

namespace DTMAPI.StrongPlantingGun
{
    internal sealed class StrongPlantingGunNativeRuntime
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
        private StrongPlantingGunConfig config =
            new StrongPlantingGunConfig();
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
                "ProductNative fixed-three policy has not been configured.";
        }

        internal bool Enabled => config.Enabled;

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
            StrongPlantingGunConfig value,
            string reason)
        {
            config = (value ??
                new StrongPlantingGunConfig()).Copy();
            if (!config.Enabled)
            {
                CleanupProductNative(
                    "disabled",
                    reason,
                    clearObservations: true);
                return;
            }

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
                Status = "configured-product-native";
                LastMessage =
                    "Fixed slots=3 seed=" +
                    config.IncludeSeeds +
                    " film=" +
                    config.IncludeFilms +
                    " fertilizer=" +
                    config.IncludeFertilizers +
                    " water=False reason=" +
                    (reason ?? string.Empty) +
                    ".";
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
            StrongPlantingGunConfig value,
            string reason)
        {
            Configure(value, reason);
            if (!config.Enabled)
                return 0;

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
            if (!config.Enabled || inventory == null)
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
            if (!config.Enabled ||
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
                " fixedSlots=3 reason=" +
                (reason ?? string.Empty) +
                ".";
            StrongPlantingGunCallbacks.PublishLifecycleSummary(
                BuildLifecycleSummary());
            if (logGate.ShouldLog(
                config.VerboseLogging,
                "prepare",
                expanded))
            {
                monitor.Log(
                    "StrongPlantingGun " +
                    LastMessage);
            }
        }

        internal bool HandleToolUse(object gun)
        {
            if (!config.Enabled)
                return true;
            bool ownsMutation = false;
            try
            {
                PrepareGun(
                    gun,
                    "ItemFarmingGun.OnUseAsTool");
                object? inventory =
                    reflection.ReadMember(gun, "inventory");
                if (inventory == null ||
                    reflection.ReadBool(
                        inventory,
                        "isEmpty",
                        false))
                {
                    return true;
                }

                List<SlotItem> slots =
                    ReadSupportedSlots(inventory);
                if (slots.Count == 0)
                    return true;
                List<object> equipments =
                    ReadUniqueEquipments(gun);
                if (equipments.Count == 0)
                    return true;

                int seedActions = 0;
                int filmActions = 0;
                int fertilizerActions = 0;
                int consumed = 0;
                for (int equipmentIndex = 0;
                     equipmentIndex < equipments.Count;
                     equipmentIndex++)
                {
                    object equipment =
                        equipments[equipmentIndex];
                    for (int slotIndex = 0;
                         slotIndex < slots.Count;
                         slotIndex++)
                    {
                        int index = slots[slotIndex].Index;
                        object? current =
                            ReadInventoryItemAt(
                                inventory,
                                index);
                        ItemKind kind =
                            GetAllowedItemKind(current);
                        if (kind == ItemKind.None)
                            continue;
                        object checkItem =
                            CloneItem(current!, 1) ??
                            current!;
                        if (!InvokeInteraction(
                            gun,
                            "CheckCanInteract",
                            equipment,
                            checkItem))
                        {
                            continue;
                        }
                        InventorySnapshot attempt =
                            InventorySnapshot.Capture(
                                reflection,
                                inventory);
                        ownsMutation = true;
                        try
                        {
                            if (!TryCostAtIndex(
                                inventory,
                                index,
                                1))
                            {
                                attempt.Restore(reflection);
                                return false;
                            }

                            object interactItem =
                                CloneItem(current!, 1) ??
                                checkItem;
                            if (!InvokeInteraction(
                                gun,
                                "DoInteract",
                                equipment,
                                interactItem))
                            {
                                attempt.Restore(reflection);
                                return false;
                            }
                        }
                        catch
                        {
                            attempt.Restore(reflection);
                            throw;
                        }

                        consumed++;
                        if (kind == ItemKind.Seed)
                            seedActions++;
                        else if (kind == ItemKind.Film)
                            filmActions++;
                        else if (kind == ItemKind.Fertilizer)
                            fertilizerActions++;
                    }
                }

                if (consumed == 0)
                    return true;

                InvokeNoArg(gun, "HideCellTip");
                InvokeNoArg(gun, "ShineArea");
                InvokeNoArg(gun, "InitCellTip");
                LastConsumedItemCount = consumed;
                Status = "verified-product-native";
                LastMessage =
                    "slots=" +
                    slots.Count +
                    ", equipments=" +
                    equipments.Count +
                    ", seedActions=" +
                    seedActions +
                    ", filmActions=" +
                    filmActions +
                    ", fertilizerActions=" +
                    fertilizerActions +
                    ", consumed=" +
                    consumed +
                    ".";
                if (logGate.ShouldLog(
                    config.VerboseLogging,
                    "tool",
                    materialChange: true))
                {
                    monitor.Log(
                        "StrongPlantingGun use " +
                        LastMessage);
                }
                return false;
            }
            catch (Exception ex)
            {
                Status = "failed-closed";
                LastMessage =
                    "Tool use failed closed: " +
                    ex.GetType().Name +
                    ": " +
                    ex.Message;
                monitor.Log(
                    "StrongPlantingGun " +
                    LastMessage,
                    LogLevel.Error);
                return !ownsMutation;
            }
        }

        internal bool HandleUiPlaceToOtherSide(
            object uiState,
            int index)
        {
            InventoryTransferSnapshot? transaction = null;
            bool ownsMutation = false;
            try
            {
                if (!TryPrepareUiTransfer(
                    uiState,
                    out object container,
                    out object containerInventory,
                    out object backpackInventory,
                    out object selected))
                {
                    return true;
                }

                PrepareGun(
                    container,
                    "FarmingGunUiState.HandlePlaceToOtherSide");
                transaction =
                    InventoryTransferSnapshot.Capture(
                        reflection,
                        backpackInventory,
                        containerInventory);
                ownsMutation = true;
                object? taken = InvokeInventoryIndex(
                    backpackInventory,
                    "Take",
                    index);
                if (taken == null)
                    return false;
                object? leftover =
                    PlaceItem(containerInventory, taken);
                if (leftover != null)
                {
                    RestoreUiLeftover(
                        backpackInventory,
                        index,
                        leftover);
                }
                transaction.AssertConserved(reflection);
                ObserveUi(
                    "put-stack",
                    selected,
                    index,
                    leftover);
                return false;
            }
            catch (Exception ex)
            {
                return RollBackUiMutation(
                    "put-stack",
                    ex,
                    transaction,
                    ownsMutation);
            }
        }

        internal bool HandleUiSwapOneItem(
            object uiState,
            int index)
        {
            InventoryTransferSnapshot? transaction = null;
            bool ownsMutation = false;
            try
            {
                if (!TryPrepareUiTransfer(
                    uiState,
                    out object container,
                    out object containerInventory,
                    out object backpackInventory,
                    out object selected))
                {
                    return true;
                }

                PrepareGun(
                    container,
                    "FarmingGunUiState.HandleSwapOneItem");
                object? one = CloneItem(selected, 1);
                if (one == null ||
                    !CanPlace(containerInventory, one))
                {
                    return true;
                }
                int currentIndex = reflection.ReadInt(
                    uiState,
                    "currentIndex",
                    index);
                transaction =
                    InventoryTransferSnapshot.Capture(
                        reflection,
                        backpackInventory,
                        containerInventory);
                ownsMutation = true;
                if (!TryCostAtIndex(
                    backpackInventory,
                    currentIndex,
                    1))
                {
                    transaction.Restore(reflection);
                    return false;
                }
                object? leftover =
                    PlaceItem(containerInventory, one);
                if (leftover != null)
                {
                    RestoreUiLeftover(
                        backpackInventory,
                        currentIndex,
                        leftover);
                }
                transaction.AssertConserved(reflection);
                ObserveUi(
                    "put-one",
                    selected,
                    currentIndex,
                    leftover);
                return false;
            }
            catch (Exception ex)
            {
                return RollBackUiMutation(
                    "put-one",
                    ex,
                    transaction,
                    ownsMutation);
            }
        }

        internal string BuildLifecycleSummary(
            int? callbacksOverride = null,
            int? hooksOverride = null) =>
            StrongPlantingGunLifecycleSummary.Format(
                callbacksOverride ??
                    (callbackAttached ? 1 : 0),
                hooksOverride ?? hooks.InstalledPatchCount,
                reflection.Count,
                capacitySnapshots.Count);

        private void CleanupProductNative(
            string finalStatus,
            string reason,
            bool clearObservations)
        {
            List<Exception> failures =
                StrongPlantingGunCleanupTransaction.Run(
                    RestoreFunctionCapacities,
                    () => hooks.UnpatchOwnedHooks(this));
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

        private List<SlotItem> ReadSupportedSlots(object inventory)
        {
            int capacity = Math.Min(
                StrongPlantingGunProductContract.FixedSlotCount,
                Math.Max(
                    0,
                    reflection.ReadInt(
                        inventory,
                        "capacity",
                        0)));
            var result = new List<SlotItem>(capacity);
            for (int index = 0; index < capacity; index++)
            {
                object? item =
                    ReadInventoryItemAt(inventory, index);
                if (GetAllowedItemKind(item) != ItemKind.None)
                    result.Add(new SlotItem(index));
            }
            return result;
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
            if (config.IncludeSeeds &&
                IsTypeOrBase(type, "DolocTown.ItemSeed"))
            {
                return ItemKind.Seed;
            }
            if (config.IncludeFilms &&
                IsTypeOrBase(type, "DolocTown.ItemFilm"))
            {
                return ItemKind.Film;
            }
            if (config.IncludeFertilizers &&
                IsTypeOrBase(
                    type,
                    "DolocTown.ItemFertilizer"))
            {
                return ItemKind.Fertilizer;
            }
            // Water is intentionally absent from ProductNative even if the
            // frozen compatibility DTO still exposes IncludeWater.
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
            int count)
        {
            MethodInfo? method = reflection.GetMethod(
                inventory.GetType(),
                "TryCostAtIndex",
                3);
            if (method != null)
            {
                return method.Invoke(
                    inventory,
                    new object[]
                    {
                        index,
                        count,
                        false
                    }) is bool result &&
                    result;
            }
            method = reflection.GetMethod(
                inventory.GetType(),
                "TryCostAtIndex",
                2);
            return method?.Invoke(
                inventory,
                new object[]
                {
                    index,
                    count
                }) is bool fallback &&
                fallback;
        }

        private bool TryPrepareUiTransfer(
            object uiState,
            out object container,
            out object containerInventory,
            out object backpackInventory,
            out object selected)
        {
            container = null!;
            containerInventory = null!;
            backpackInventory = null!;
            selected = null!;
            if (!config.Enabled ||
                uiState == null ||
                !reflection.ReadBool(
                    uiState,
                    "inBackpack",
                    false))
            {
                return false;
            }
            object? buffer =
                reflection.ReadMember(uiState, "buffer");
            if (buffer != null &&
                !reflection.ReadBool(
                    buffer,
                    "IsEmpty",
                    reflection.ReadBool(
                        buffer,
                        "isEmpty",
                        false)))
            {
                return false;
            }

            object? possibleContainer =
                reflection.ReadMember(uiState, "container");
            object? possibleSelected =
                reflection.ReadMember(uiState, "selectedItem");
            if (possibleContainer == null ||
                possibleSelected == null ||
                !IsTypeOrBase(
                    possibleContainer.GetType(),
                    "DolocTown.ItemFarmingGun") ||
                GetAllowedItemKind(possibleSelected) ==
                    ItemKind.None)
            {
                return false;
            }

            object? possibleContainerInventory =
                reflection.ReadMember(
                    uiState,
                    "containerInventory");
            object? possibleBackpackInventory =
                reflection.ReadMember(
                    uiState,
                    "backpackInventory");
            if (possibleContainerInventory == null ||
                possibleBackpackInventory == null)
            {
                return false;
            }

            container = possibleContainer;
            selected = possibleSelected;
            containerInventory = possibleContainerInventory;
            backpackInventory = possibleBackpackInventory;
            return true;
        }

        private bool CanPlace(object inventory, object item) =>
            reflection.GetMethod(
                inventory.GetType(),
                "CanPlaceIn",
                1)?.Invoke(
                inventory,
                new[]
                {
                    item
                }) is bool result &&
            result;

        private object? PlaceItem(
            object inventory,
            object item)
        {
            MethodInfo? method = reflection.GetMethod(
                inventory.GetType(),
                "PlaceItem",
                1);
            return method == null
                ? item
                : method.Invoke(
                    inventory,
                    new[]
                    {
                        item
                    });
        }

        private object? PlaceItemAt(
            object inventory,
            int index,
            object item)
        {
            MethodInfo? method = reflection.GetMethod(
                inventory.GetType(),
                "PlaceItemAt",
                2);
            return method == null
                ? item
                : method.Invoke(
                    inventory,
                    new object[]
                    {
                        index,
                        item
                    });
        }

        private void RestoreUiLeftover(
            object backpackInventory,
            int originalIndex,
            object leftover)
        {
            object? remaining = PlaceItemAt(
                backpackInventory,
                originalIndex,
                leftover);
            if (remaining != null)
                remaining = PlaceItem(
                    backpackInventory,
                    remaining);
            if (remaining != null)
            {
                throw new InvalidOperationException(
                    "StrongPlantingGun could not return a UI transfer leftover to the native backpack.");
            }
        }

        private void InvokeNoArg(
            object instance,
            string methodName) =>
            reflection.GetMethod(
                instance.GetType(),
                methodName,
                0)?.Invoke(instance, null);

        private void ObserveUi(
            string action,
            object selected,
            int index,
            object? leftover)
        {
            Status = "verified-product-native";
            LastMessage =
                "action=" +
                action +
                ", index=" +
                index +
                ", item=" +
                reflection.ReadString(selected, "name") +
                ", leftover=" +
                (leftover == null
                    ? "none"
                    : reflection.ReadInt(
                        leftover,
                        "count",
                        0).ToString(
                            CultureInfo.InvariantCulture)) +
                ".";
            if (logGate.ShouldLog(
                config.VerboseLogging,
                action,
                materialChange: false))
            {
                monitor.Log(
                    "StrongPlantingGun UI transfer " +
                    LastMessage);
            }
        }

        private bool RollBackUiMutation(
            string action,
            Exception ex,
            InventoryTransferSnapshot? transaction,
            bool ownsMutation)
        {
            Exception failure = ex;
            if (ownsMutation && transaction != null)
            {
                try
                {
                    transaction.Restore(reflection);
                }
                catch (Exception restoreError)
                {
                    failure = new AggregateException(
                        "StrongPlantingGun UI mutation and exact inventory rollback both failed.",
                        ex,
                        restoreError);
                }
            }
            Status = "failed-closed";
            LastMessage =
                "UI " +
                action +
                " failed closed: " +
                failure.GetType().Name +
                ": " +
                failure.Message;
            monitor.Log(
                "StrongPlantingGun " +
                LastMessage,
                LogLevel.Error);
            return !ownsMutation;
        }

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

        private readonly struct SlotItem
        {
            internal SlotItem(int index) => Index = index;

            internal int Index { get; }
        }

        private sealed class InventoryTransferSnapshot
        {
            private readonly InventorySnapshot source;
            private readonly InventorySnapshot destination;
            private readonly int totalCount;

            private InventoryTransferSnapshot(
                InventorySnapshot source,
                InventorySnapshot destination)
            {
                this.source = source;
                this.destination = destination;
                totalCount =
                    source.TotalCount +
                    destination.TotalCount;
            }

            internal static InventoryTransferSnapshot Capture(
                StrongPlantingGunReflectionCache reflection,
                object source,
                object destination) =>
                new InventoryTransferSnapshot(
                    InventorySnapshot.Capture(
                        reflection,
                        source),
                    InventorySnapshot.Capture(
                        reflection,
                        destination));

            internal void AssertConserved(
                StrongPlantingGunReflectionCache reflection)
            {
                int current =
                    InventorySnapshot.ReadTotalCount(
                        reflection,
                        source.Inventory) +
                    InventorySnapshot.ReadTotalCount(
                        reflection,
                        destination.Inventory);
                if (current != totalCount)
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun UI transfer did not conserve the combined native inventory count.");
                }
            }

            internal void Restore(
                StrongPlantingGunReflectionCache reflection)
            {
                var failures = new List<Exception>();
                CaptureFailure(
                    failures,
                    () => source.RestoreData(
                        reflection));
                CaptureFailure(
                    failures,
                    () => destination.RestoreData(
                        reflection));
                CaptureFailure(
                    failures,
                    () => source.SynchronizeReceivers(
                        reflection));
                CaptureFailure(
                    failures,
                    () => destination.SynchronizeReceivers(
                        reflection));
                ThrowFailures(
                    "StrongPlantingGun failed to restore both native UI inventories and synchronize their receivers.",
                    failures);
            }

            private static void CaptureFailure(
                List<Exception> failures,
                Action action)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }

            private static void ThrowFailures(
                string message,
                List<Exception> failures)
            {
                if (failures.Count == 1)
                    throw failures[0];
                if (failures.Count > 1)
                {
                    throw new AggregateException(
                        message,
                        failures);
                }
            }
        }

        private sealed class InventorySnapshot
        {
            private readonly object?[] items;
            private readonly int[] counts;
            private readonly bool[] slotLockStates;

            private InventorySnapshot(
                object inventory,
                object?[] items,
                int[] counts,
                bool[] slotLockStates)
            {
                Inventory = inventory;
                this.items = items;
                this.counts = counts;
                this.slotLockStates = slotLockStates;
                TotalCount = SumCounts(counts);
            }

            internal object Inventory { get; }

            internal int TotalCount { get; }

            internal static InventorySnapshot Capture(
                StrongPlantingGunReflectionCache reflection,
                object inventory)
            {
                Array nativeItems =
                    reflection.ReadMember(
                        inventory,
                        "inventory") as Array ??
                    throw new MissingMemberException(
                        inventory.GetType().FullName,
                        "inventory");
                var items =
                    new object?[nativeItems.Length];
                var counts =
                    new int[nativeItems.Length];
                Array nativeSlotLocks =
                    reflection.ReadMember(
                        inventory,
                        "slotLockStates") as Array ??
                    throw new MissingMemberException(
                        inventory.GetType().FullName,
                        "slotLockStates");
                if (nativeSlotLocks.Length !=
                    nativeItems.Length)
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun found mismatched native inventory and slot-lock capacities.");
                }
                var slotLocks =
                    new bool[nativeSlotLocks.Length];
                for (int index = 0;
                     index < nativeItems.Length;
                     index++)
                {
                    object? item =
                        nativeItems.GetValue(index);
                    items[index] = item;
                    counts[index] = item == null
                        ? 0
                        : reflection.ReadInt(
                            item,
                            "count",
                            0);
                    slotLocks[index] =
                        nativeSlotLocks.GetValue(index) is bool
                            locked &&
                        locked;
                }
                return new InventorySnapshot(
                    inventory,
                    items,
                    counts,
                    slotLocks);
            }

            internal static int ReadTotalCount(
                StrongPlantingGunReflectionCache reflection,
                object inventory)
            {
                Array nativeItems =
                    reflection.ReadMember(
                        inventory,
                        "inventory") as Array ??
                    throw new MissingMemberException(
                        inventory.GetType().FullName,
                        "inventory");
                int total = 0;
                for (int index = 0;
                     index < nativeItems.Length;
                     index++)
                {
                    object? item =
                        nativeItems.GetValue(index);
                    if (item != null)
                    {
                        total += reflection.ReadInt(
                            item,
                            "count",
                            0);
                    }
                }
                return total;
            }

            internal void Restore(
                StrongPlantingGunReflectionCache reflection)
            {
                RestoreData(reflection);
                SynchronizeReceivers(reflection);
            }

            internal void RestoreData(
                StrongPlantingGunReflectionCache reflection)
            {
                Array nativeItems =
                    reflection.ReadMember(
                        Inventory,
                        "inventory") as Array ??
                    throw new MissingMemberException(
                        Inventory.GetType().FullName,
                        "inventory");
                if (nativeItems.Length != items.Length)
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun cannot restore an inventory whose capacity changed during a transaction.");
                }
                Array nativeSlotLocks =
                    reflection.ReadMember(
                        Inventory,
                        "slotLockStates") as Array ??
                    throw new MissingMemberException(
                        Inventory.GetType().FullName,
                        "slotLockStates");
                if (nativeSlotLocks.Length !=
                    slotLockStates.Length)
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun cannot restore slot locks whose capacity changed during a transaction.");
                }
                for (int index = 0;
                     index < items.Length;
                     index++)
                {
                    object? item = items[index];
                    nativeItems.SetValue(item, index);
                    if (item != null &&
                        !reflection.TryWriteInt(
                            item,
                            "count",
                            counts[index]))
                    {
                        throw new MissingMemberException(
                            item.GetType().FullName,
                            "count");
                    }
                    nativeSlotLocks.SetValue(
                        slotLockStates[index],
                        index);
                }
            }

            internal void SynchronizeReceivers(
                StrongPlantingGunReflectionCache reflection)
            {
                object? value = reflection.ReadMember(
                    Inventory,
                    "receivers");
                if (value == null)
                    return;
                if (!(value is IEnumerable enumerable))
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun could not enumerate native inventory receivers after rollback.");
                }

                var receivers = new List<Delegate>();
                foreach (object? candidate in enumerable)
                {
                    if (candidate is Delegate receiver)
                    {
                        receivers.Add(receiver);
                    }
                    else if (candidate != null)
                    {
                        throw new InvalidOperationException(
                            "StrongPlantingGun found a native inventory receiver that was not a delegate.");
                    }
                }

                var failures = new List<Exception>();
                for (int receiverIndex = 0;
                     receiverIndex < receivers.Count;
                     receiverIndex++)
                {
                    Delegate receiver =
                        receivers[receiverIndex];
                    for (int itemIndex = 0;
                         itemIndex < items.Length;
                         itemIndex++)
                    {
                        try
                        {
                            receiver.DynamicInvoke(
                                itemIndex,
                                items[itemIndex],
                                slotLockStates[itemIndex]);
                        }
                        catch (Exception ex)
                        {
                            failures.Add(ex);
                        }
                    }
                }
                if (failures.Count == 1)
                    throw failures[0];
                if (failures.Count > 1)
                {
                    throw new AggregateException(
                        "StrongPlantingGun restored native inventory data but one or more receivers rejected rollback synchronization.",
                        failures);
                }
            }

            private static int SumCounts(int[] values)
            {
                int total = 0;
                for (int index = 0;
                     index < values.Length;
                     index++)
                {
                    total += values[index];
                }
                return total;
            }
        }

        private enum ItemKind : byte
        {
            None,
            Seed,
            Film,
            Fertilizer
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
