using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class StrongPlantingGunService : IStrongPlantingGunApi
    {
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, StrongPlantingGunOptions> strongPlantingGunOptions = new Dictionary<string, StrongPlantingGunOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, StrongPlantingGunState> strongPlantingGunStates = new Dictionary<string, StrongPlantingGunState>(StringComparer.OrdinalIgnoreCase);
        private bool strongPlantingGunToolHookInstalled;
        private bool strongPlantingGunUiHookInstalled;

        public StrongPlantingGunService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal void SetHooksInstalled(bool toolInstalled, bool uiInstalled, bool ctorInstalled)
        {
            strongPlantingGunToolHookInstalled = toolInstalled;
            strongPlantingGunUiHookInstalled = uiInstalled;
            foreach (KeyValuePair<string, StrongPlantingGunState> entry in strongPlantingGunStates.ToArray())
            {
                StrongPlantingGunState state = entry.Value;
                state.ToolHookInstalled = toolInstalled;
                state.UiHookInstalled = uiInstalled;
                if (state.IsConfigured)
                    state.Status = state.Enabled ? ((toolInstalled && uiInstalled) ? "configured-experimental-tool-ui-hooks" : "configured-pending-hook") : "disabled";
                strongPlantingGunStates[entry.Key] = state;
            }
        }

        public StrongPlantingGunRegisterResult Register(IManifest owner, StrongPlantingGunOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            StrongPlantingGunOptions normalized = NormalizeStrongPlantingGunOptions(options);
            strongPlantingGunOptions[owner.UniqueID] = normalized;
            StrongPlantingGunState state = GetStrongPlantingGunState(owner.UniqueID);
            state.IsConfigured = true;
            state.Enabled = normalized.Enabled;
            state.SlotCount = normalized.SlotCount;
            state.ToolHookInstalled = strongPlantingGunToolHookInstalled;
            state.UiHookInstalled = strongPlantingGunUiHookInstalled;
            state.Status = normalized.Enabled
                ? ((strongPlantingGunToolHookInstalled && strongPlantingGunUiHookInstalled) ? "configured-experimental-tool-ui-hooks" : "configured-pending-hook")
                : "disabled";
            state.LastMessage = normalized.Enabled
                ? "Strong planting gun policy registered; DTMAPI expands official farming gun storage and uses official basin interaction checks across seed, film, and fertilizer slots."
                : "Strong planting gun policy is disabled.";
            strongPlantingGunStates[owner.UniqueID] = state;

            var result = new StrongPlantingGunRegisterResult
            {
                Success = true,
                OwnerId = owner.UniqueID,
                Enabled = normalized.Enabled,
                SlotCount = normalized.SlotCount,
                ToolHookInstalled = strongPlantingGunToolHookInstalled,
                UiHookInstalled = strongPlantingGunUiHookInstalled,
                Message = state.LastMessage
            };
            runtime.RuntimeMonitor.Log("StrongPlantingGun API register success=True owner=" + owner.UniqueID + " enabled=" + normalized.Enabled + " slots=" + normalized.SlotCount + " toolHook=" + strongPlantingGunToolHookInstalled + " uiHook=" + strongPlantingGunUiHookInstalled + " message=" + result.Message);
            runtime.SetHookStatus("Farming.StrongPlantingGun", state.Status, "IStrongPlantingGunApi -> ItemFarmingGun/FarmingGunUiState Harmony hooks", state.LastMessage);
            return result;
        }

        StrongPlantingGunState IStrongPlantingGunApi.GetState(string uniqueId)
        {
            return CloneStrongPlantingGunState(GetStrongPlantingGunState(uniqueId ?? string.Empty));
        }

        BridgeFeatureStatus IStrongPlantingGunApi.GetStatus(string uniqueId)
        {
            StrongPlantingGunState state = GetStrongPlantingGunState(uniqueId ?? string.Empty);
            return new BridgeFeatureStatus(state.Status, state.LastMessage);
        }

        internal void ExpandFarmingGunInventoryIfNeeded(object gun, string reason)
        {
            if (gun == null || !IsFarmingGun(gun) || !TryGetStrongPlantingGunPolicy(out string ownerId, out StrongPlantingGunOptions options))
                return;

            object? inventory = ReadMember(gun, "inventory");
            if (inventory == null)
                return;

            int currentCapacity = ReadIntMember(inventory, "capacity", 0);
            int targetCapacity = Math.Max(currentCapacity, options.SlotCount);
            if (targetCapacity <= 0)
                return;

            bool expanded = false;
            if (currentCapacity < targetCapacity)
            {
                MethodInfo? validateCapacity = FindMethodInHierarchy(inventory.GetType(), "ValidateCapacity", 2) ?? FindMethodInHierarchy(inventory.GetType(), "ValidateCapacity", 1);
                if (validateCapacity == null)
                {
                    UpdateStrongPlantingGunStates(ownerId, options, 0, 0, 0, 0, 0, 0, "LinearInventory.ValidateCapacity was not available for farming gun expansion.", "failed");
                    return;
                }

                object?[] args = validateCapacity.GetParameters().Length == 2
                    ? new object?[] { targetCapacity, false }
                    : new object?[] { targetCapacity };
                validateCapacity.Invoke(inventory, args);
                expanded = true;
            }

            object? func = ReadMember(gun, "func");
            if (func != null)
                TrySetStrongPlantingGunFunctionCapacity(func, targetCapacity);

            StrongPlantingGunState state = GetStrongPlantingGunState(ownerId);
            if (expanded)
                state.ExpandedGunCount++;
            state.IsConfigured = true;
            state.Enabled = options.Enabled;
            state.SlotCount = options.SlotCount;
            state.ToolHookInstalled = strongPlantingGunToolHookInstalled;
            state.UiHookInstalled = strongPlantingGunUiHookInstalled;
            state.Status = (strongPlantingGunToolHookInstalled && strongPlantingGunUiHookInstalled) ? "configured-experimental-tool-ui-hooks" : "configured-pending-hook";
            state.LastMessage = "Strong planting gun storage prepared slots=" + targetCapacity + " previousSlots=" + currentCapacity + " reason=" + reason + ".";
            strongPlantingGunStates[ownerId] = state;

            if (expanded || options.VerboseLogging)
                runtime.RuntimeMonitor.Log("StrongPlantingGun expanded official farming gun storage owner=" + ownerId + " slots=" + targetCapacity + " previousSlots=" + currentCapacity + " reason=" + reason + ".");
        }

        internal bool HandleStrongPlantingGunToolUse(object gun)
        {
            try
            {
                if (gun == null || !IsFarmingGun(gun) || !TryGetStrongPlantingGunPolicy(out string ownerId, out StrongPlantingGunOptions options))
                    return true;

                ExpandFarmingGunInventoryIfNeeded(gun, "ItemFarmingGun.OnUseAsTool");
                object? inventory = ReadMember(gun, "inventory");
                if (inventory == null || ReadBoolMember(inventory, "isEmpty", false))
                    return true;

                List<StrongPlantingGunSlotItem> slots = ReadStrongPlantingGunSlotItems(inventory, options);
                if (slots.Count == 0)
                    return true;

                List<object> equipments = ReadStrongPlantingGunEquipments(gun).ToList();
                if (equipments.Count == 0)
                    return true;

                int seedActions = 0;
                int filmActions = 0;
                int fertilizerActions = 0;
                int waterActions = 0;
                int consumed = 0;

                foreach (object equipment in equipments)
                {
                    foreach (StrongPlantingGunSlotItem slot in slots.ToArray())
                    {
                        object? currentItem = ReadInventoryItemAt(inventory, slot.Index);
                        if (currentItem == null || !IsStrongPlantingGunItemAllowed(options, currentItem))
                            continue;

                        string kind = GetStrongPlantingGunItemKind(currentItem);
                        object itemForCheck = CloneItem(currentItem, 1) ?? currentItem;
                        if (!InvokeStrongPlantingGunCheckCanInteract(gun, equipment, itemForCheck))
                            continue;

                        if (!TryCostInventoryAtIndex(inventory, slot.Index, 1))
                            continue;

                        object itemForInteract = CloneItem(currentItem, 1) ?? itemForCheck;
                        if (!InvokeStrongPlantingGunDoInteract(gun, equipment, itemForInteract))
                            continue;

                        consumed++;
                        if (kind == "seed")
                            seedActions++;
                        else if (kind == "film")
                            filmActions++;
                        else if (kind == "fertilizer")
                            fertilizerActions++;
                        else if (kind == "water")
                            waterActions++;
                    }
                }

                if (consumed <= 0)
                    return true;

                InvokeNoArgIfAvailable(gun, "HideCellTip");
                InvokeNoArgIfAvailable(gun, "ShineArea");
                InvokeNoArgIfAvailable(gun, "InitCellTip");

                string message = "owner=" + ownerId +
                    ", slots=" + slots.Count +
                    ", equipments=" + equipments.Count +
                    ", seedActions=" + seedActions +
                    ", filmActions=" + filmActions +
                    ", fertilizerActions=" + fertilizerActions +
                    ", waterActions=" + waterActions +
                    ", consumed=" + consumed;
                UpdateStrongPlantingGunStates(ownerId, options, equipments.Count, seedActions, filmActions, fertilizerActions, waterActions, consumed, message, "verified");
                runtime.RuntimeMonitor.Log("StrongPlantingGun use " + message);
                runtime.SetHookStatus("Farming.StrongPlantingGun", "verified", "Harmony Prefix: ItemFarmingGun.OnUseAsTool", message);
                return false;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Strong planting gun tool use failed.", ex.ToString());
                runtime.SetHookStatus("Farming.StrongPlantingGun", "failed", "Harmony Prefix: ItemFarmingGun.OnUseAsTool", ex.GetType().Name + ": " + ex.Message);
                return true;
            }
        }

        internal bool HandleStrongPlantingGunUiPlaceToOtherSide(object uiState, int index)
        {
            try
            {
                if (!TryPrepareStrongPlantingGunUiTransfer(uiState, "FarmingGunUiState.HandlePlaceToOtherSide", out string ownerId, out StrongPlantingGunOptions options, out object container, out object containerInventory, out object backpackInventory, out object selectedItem))
                    return true;

                ExpandFarmingGunInventoryIfNeeded(container, "FarmingGunUiState.HandlePlaceToOtherSide");
                object? taken = InvokeInventoryMethod(backpackInventory, "Take", index);
                if (taken == null)
                    return false;

                object? leftover = PlaceInventoryItem(containerInventory, taken);
                if (leftover != null)
                    PlaceInventoryItemAt(backpackInventory, index, leftover);

                string message = "owner=" + ownerId + ", action=put-stack, index=" + index + ", item=" + ReadStringMember(selectedItem, "name") + ", leftover=" + (leftover == null ? "none" : ReadIntMember(leftover, "count", 0).ToString(CultureInfo.InvariantCulture));
                runtime.SetHookStatus("Farming.StrongPlantingGunUi", "verified", "Harmony Prefix: FarmingGunUiState.HandlePlaceToOtherSide", message);
                if (options.VerboseLogging)
                    runtime.RuntimeMonitor.Log("StrongPlantingGun UI transfer " + message);
                return false;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Strong planting gun UI stack transfer failed.", ex.ToString());
                runtime.SetHookStatus("Farming.StrongPlantingGunUi", "failed", "Harmony Prefix: FarmingGunUiState.HandlePlaceToOtherSide", ex.GetType().Name + ": " + ex.Message);
                return true;
            }
        }

        internal bool HandleStrongPlantingGunUiSwapOneItem(object uiState, int index)
        {
            try
            {
                if (!TryPrepareStrongPlantingGunUiTransfer(uiState, "FarmingGunUiState.HandleSwapOneItem", out string ownerId, out StrongPlantingGunOptions options, out object container, out object containerInventory, out object backpackInventory, out object selectedItem))
                    return true;

                ExpandFarmingGunInventoryIfNeeded(container, "FarmingGunUiState.HandleSwapOneItem");
                object? oneItem = CloneItem(selectedItem, 1);
                if (oneItem == null || !CanPlaceInventoryItem(containerInventory, oneItem))
                    return true;

                int currentIndex = ReadIntMember(uiState, "currentIndex", index);
                if (!TryCostInventoryAtIndex(backpackInventory, currentIndex, 1))
                    return true;

                object? leftover = PlaceInventoryItem(containerInventory, oneItem);
                if (leftover != null)
                    PlaceInventoryItemAt(backpackInventory, currentIndex, leftover);

                string message = "owner=" + ownerId + ", action=put-one, index=" + currentIndex + ", item=" + ReadStringMember(selectedItem, "name") + ", leftover=" + (leftover == null ? "none" : ReadIntMember(leftover, "count", 0).ToString(CultureInfo.InvariantCulture));
                runtime.SetHookStatus("Farming.StrongPlantingGunUi", "verified", "Harmony Prefix: FarmingGunUiState.HandleSwapOneItem", message);
                if (options.VerboseLogging)
                    runtime.RuntimeMonitor.Log("StrongPlantingGun UI transfer " + message);
                return false;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Strong planting gun UI one-item transfer failed.", ex.ToString());
                runtime.SetHookStatus("Farming.StrongPlantingGunUi", "failed", "Harmony Prefix: FarmingGunUiState.HandleSwapOneItem", ex.GetType().Name + ": " + ex.Message);
                return true;
            }
        }

        private bool TryGetStrongPlantingGunPolicy(out string ownerId, out StrongPlantingGunOptions options)
        {
            foreach (KeyValuePair<string, StrongPlantingGunOptions> entry in strongPlantingGunOptions)
            {
                StrongPlantingGunOptions candidate = entry.Value ?? new StrongPlantingGunOptions { Enabled = false };
                if (!candidate.Enabled)
                    continue;
                ownerId = entry.Key;
                options = candidate;
                return true;
            }

            ownerId = string.Empty;
            options = new StrongPlantingGunOptions { Enabled = false };
            return false;
        }

        private void UpdateStrongPlantingGunStates(string ownerId, StrongPlantingGunOptions options, int visitedEquipment, int seedActions, int filmActions, int fertilizerActions, int waterActions, int consumed, string message, string status)
        {
            foreach (KeyValuePair<string, StrongPlantingGunOptions> entry in strongPlantingGunOptions.ToArray())
            {
                StrongPlantingGunOptions entryOptions = entry.Value ?? new StrongPlantingGunOptions();
                StrongPlantingGunState state = GetStrongPlantingGunState(entry.Key);
                state.IsConfigured = true;
                state.Enabled = entryOptions.Enabled;
                state.SlotCount = entryOptions.SlotCount;
                state.ToolHookInstalled = strongPlantingGunToolHookInstalled;
                state.UiHookInstalled = strongPlantingGunUiHookInstalled;
                state.LastVisitedEquipmentCount = visitedEquipment;
                state.LastSeedActions = seedActions;
                state.LastFilmActions = filmActions;
                state.LastFertilizerActions = fertilizerActions;
                state.LastWaterActions = waterActions;
                state.LastConsumedItemCount = consumed;
                state.Status = entryOptions.Enabled
                    ? (entry.Key.Equals(ownerId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(status) ? status : "configured-experimental-tool-ui-hooks")
                    : "disabled";
                state.LastMessage = message ?? string.Empty;
                strongPlantingGunStates[entry.Key] = state;
            }
        }

        private StrongPlantingGunState GetStrongPlantingGunState(string ownerId)
        {
            ownerId ??= string.Empty;
            if (strongPlantingGunStates.TryGetValue(ownerId, out StrongPlantingGunState state))
                return state;

            StrongPlantingGunOptions options = strongPlantingGunOptions.TryGetValue(ownerId, out StrongPlantingGunOptions? configured)
                ? configured
                : new StrongPlantingGunOptions { Enabled = false };
            return new StrongPlantingGunState
            {
                OwnerId = ownerId,
                IsConfigured = strongPlantingGunOptions.ContainsKey(ownerId),
                Enabled = options.Enabled && strongPlantingGunOptions.ContainsKey(ownerId),
                SlotCount = Math.Max(1, options.SlotCount),
                ToolHookInstalled = strongPlantingGunToolHookInstalled,
                UiHookInstalled = strongPlantingGunUiHookInstalled,
                Status = strongPlantingGunOptions.ContainsKey(ownerId) ? (options.Enabled ? "registered" : "disabled") : "not-configured",
                LastMessage = strongPlantingGunOptions.ContainsKey(ownerId) ? "Strong planting gun policy registered." : "No strong planting gun policy registered."
            };
        }

        private static StrongPlantingGunState CloneStrongPlantingGunState(StrongPlantingGunState state)
        {
            return new StrongPlantingGunState
            {
                OwnerId = state.OwnerId,
                IsConfigured = state.IsConfigured,
                Enabled = state.Enabled,
                SlotCount = state.SlotCount,
                ToolHookInstalled = state.ToolHookInstalled,
                UiHookInstalled = state.UiHookInstalled,
                ExpandedGunCount = state.ExpandedGunCount,
                LastVisitedEquipmentCount = state.LastVisitedEquipmentCount,
                LastSeedActions = state.LastSeedActions,
                LastFilmActions = state.LastFilmActions,
                LastFertilizerActions = state.LastFertilizerActions,
                LastWaterActions = state.LastWaterActions,
                LastConsumedItemCount = state.LastConsumedItemCount,
                Status = state.Status,
                LastMessage = state.LastMessage
            };
        }

        private static StrongPlantingGunOptions NormalizeStrongPlantingGunOptions(StrongPlantingGunOptions? options)
        {
            options ??= new StrongPlantingGunOptions();
            return new StrongPlantingGunOptions
            {
                Enabled = options.Enabled,
                SlotCount = Math.Max(1, Math.Min(12, options.SlotCount)),
                IncludeSeeds = options.IncludeSeeds,
                IncludeFilms = options.IncludeFilms,
                IncludeFertilizers = options.IncludeFertilizers,
                IncludeWater = options.IncludeWater,
                VerboseLogging = options.VerboseLogging
            };
        }

        private static bool IsFarmingGun(object instance)
        {
            return instance != null && IsTypeOrBase(instance.GetType(), "DolocTown.ItemFarmingGun");
        }

        private static void TrySetStrongPlantingGunFunctionCapacity(object func, int capacity)
        {
            try
            {
                for (Type? type = func.GetType(); type != null; type = type.BaseType)
                {
                    FieldInfo? backingField = type.GetField("<Capacity>k__BackingField", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (backingField != null && backingField.FieldType == typeof(int))
                    {
                        backingField.SetValue(func, capacity);
                        return;
                    }

                    PropertyInfo? property = type.GetProperty("Capacity", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (property != null && property.CanWrite && property.PropertyType == typeof(int))
                    {
                        property.SetValue(func, capacity);
                        return;
                    }
                }
            }
            catch
            {
            }
        }

        private static List<StrongPlantingGunSlotItem> ReadStrongPlantingGunSlotItems(object inventory, StrongPlantingGunOptions options)
        {
            int capacity = Math.Max(0, ReadIntMember(inventory, "capacity", 0));
            var items = new List<StrongPlantingGunSlotItem>();
            for (int i = 0; i < capacity; i++)
            {
                object? item = ReadInventoryItemAt(inventory, i);
                if (item != null && IsStrongPlantingGunItemAllowed(options, item))
                    items.Add(new StrongPlantingGunSlotItem(i, item));
            }
            return items;
        }

        private static object? ReadInventoryItemAt(object inventory, int index)
        {
            MethodInfo? read = FindMethodInHierarchy(inventory.GetType(), "Read", 1);
            try
            {
                return read?.Invoke(inventory, new object[] { index });
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<object> ReadStrongPlantingGunEquipments(object gun)
        {
            MethodInfo? getEquipments = FindMethodInHierarchy(gun.GetType(), "GetEquipmentsFromArea", 0);
            object? result = null;
            try
            {
                result = getEquipments?.Invoke(gun, null);
            }
            catch
            {
            }

            var seen = new HashSet<int>();
            foreach (object equipment in EnumerateObjects(result))
            {
                int key = RuntimeHelpers.GetHashCode(equipment);
                if (seen.Add(key))
                    yield return equipment;
            }

        }

        private static bool IsStrongPlantingGunItemAllowed(StrongPlantingGunOptions options, object item)
        {
            string kind = GetStrongPlantingGunItemKind(item);
            return (options.IncludeSeeds && kind == "seed") ||
                (options.IncludeFilms && kind == "film") ||
                (options.IncludeFertilizers && kind == "fertilizer") ||
                (options.IncludeWater && kind == "water");
        }

        private static string GetStrongPlantingGunItemKind(object item)
        {
            if (item == null)
                return string.Empty;
            Type type = item.GetType();
            if (IsTypeOrBase(type, "DolocTown.ItemSeed"))
                return "seed";
            if (IsTypeOrBase(type, "DolocTown.ItemFilm"))
                return "film";
            if (IsTypeOrBase(type, "DolocTown.ItemFertilizer"))
                return "fertilizer";

            string itemName = ReadStringMember(item, "name");
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            string bottleOfWater = globalParameter == null ? string.Empty : ReadStringMember(globalParameter, "ItemRefBottleOfWater");
            return !string.IsNullOrWhiteSpace(itemName) && itemName.Equals(bottleOfWater, StringComparison.OrdinalIgnoreCase) ? "water" : string.Empty;
        }

        private static object? CloneItem(object item, int count)
        {
            try
            {
                MethodInfo? clone = FindMethodInHierarchy(item.GetType(), "Clone", 1);
                return clone?.Invoke(item, new object[] { count });
            }
            catch
            {
                return null;
            }
        }

        private static bool InvokeStrongPlantingGunCheckCanInteract(object gun, object equipment, object item)
        {
            MethodInfo? method = FindStrongPlantingGunMethod(
                gun.GetType(),
                "CheckCanInteract",
                2,
                parameters => !parameters[0].ParameterType.IsArray && parameters[0].ParameterType.IsInstanceOfType(equipment));
            try
            {
                return method?.Invoke(gun, new[] { equipment, item }) is bool result && result;
            }
            catch
            {
                return false;
            }
        }

        private static bool InvokeStrongPlantingGunDoInteract(object gun, object equipment, object item)
        {
            MethodInfo? method = FindStrongPlantingGunMethod(
                gun.GetType(),
                "DoInteract",
                2,
                parameters => !parameters[0].ParameterType.IsArray && parameters[0].ParameterType.IsInstanceOfType(equipment));
            try
            {
                return method?.Invoke(gun, new[] { equipment, item }) is bool result && result;
            }
            catch
            {
                return false;
            }
        }

        private static MethodInfo? FindStrongPlantingGunMethod(Type? type, string name, int parameterCount, Func<ParameterInfo[], bool> predicate)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    if (method.Name == name && parameters.Length == parameterCount && predicate(parameters))
                        return method;
                }
            }
            return null;
        }

        private static bool TryCostInventoryAtIndex(object inventory, int index, int count)
        {
            try
            {
                MethodInfo? method = FindMethodInHierarchy(inventory.GetType(), "TryCostAtIndex", 3);
                if (method != null)
                    return method.Invoke(inventory, new object[] { index, count, false }) is bool result && result;

                method = FindMethodInHierarchy(inventory.GetType(), "TryCostAtIndex", 2);
                return method?.Invoke(inventory, new object[] { index, count }) is bool fallbackResult && fallbackResult;
            }
            catch
            {
                return false;
            }
        }

        private static object? InvokeInventoryMethod(object inventory, string methodName, int index)
        {
            try
            {
                MethodInfo? method = FindMethodInHierarchy(inventory.GetType(), methodName, 1);
                return method?.Invoke(inventory, new object[] { index });
            }
            catch
            {
                return null;
            }
        }

        private static bool CanPlaceInventoryItem(object inventory, object item)
        {
            try
            {
                MethodInfo? method = FindMethodInHierarchy(inventory.GetType(), "CanPlaceIn", 1);
                return method?.Invoke(inventory, new[] { item }) is bool result && result;
            }
            catch
            {
                return false;
            }
        }

        private static object? PlaceInventoryItem(object inventory, object item)
        {
            try
            {
                MethodInfo? method = FindMethodInHierarchy(inventory.GetType(), "PlaceItem", 1);
                return method?.Invoke(inventory, new[] { item });
            }
            catch
            {
                return item;
            }
        }

        private static object? PlaceInventoryItemAt(object inventory, int index, object item)
        {
            try
            {
                MethodInfo? method = FindMethodInHierarchy(inventory.GetType(), "PlaceItemAt", 2);
                return method?.Invoke(inventory, new object[] { index, item });
            }
            catch
            {
                return item;
            }
        }

        private static void InvokeNoArgIfAvailable(object instance, string methodName)
        {
            try
            {
                FindMethodInHierarchy(instance.GetType(), methodName, 0)?.Invoke(instance, null);
            }
            catch
            {
            }
        }

        private bool TryPrepareStrongPlantingGunUiTransfer(object uiState, string source, out string ownerId, out StrongPlantingGunOptions options, out object container, out object containerInventory, out object backpackInventory, out object selectedItem)
        {
            ownerId = string.Empty;
            options = null!;
            container = null!;
            containerInventory = null!;
            backpackInventory = null!;
            selectedItem = null!;

            if (uiState == null)
                return false;

            if (!ReadBoolMember(uiState, "inBackpack", false))
                return false;

            object? buffer = ReadMember(uiState, "buffer");
            if (buffer != null && !ReadBoolMember(buffer, "IsEmpty", ReadBoolMember(buffer, "isEmpty", false)))
                return false;

            object? possibleContainer = ReadMember(uiState, "container");
            if (possibleContainer == null || !IsFarmingGun(possibleContainer))
                return false;

            object? possibleSelectedItem = ReadMember(uiState, "selectedItem");
            if (possibleSelectedItem == null)
                return false;

            if (!TryGetStrongPlantingGunPolicy(out ownerId, out options) || !IsStrongPlantingGunItemAllowed(options, possibleSelectedItem))
                return false;

            object? possibleContainerInventory = ReadMember(uiState, "containerInventory");
            object? possibleBackpackInventory = ReadMember(uiState, "backpackInventory");
            if (possibleContainerInventory == null || possibleBackpackInventory == null)
                return false;

            container = possibleContainer;
            containerInventory = possibleContainerInventory;
            backpackInventory = possibleBackpackInventory;
            selectedItem = possibleSelectedItem;
            return true;
        }

        private static Type? ResolveType(string assemblyQualifiedName)
        {
            Type? type = Type.GetType(assemblyQualifiedName);
            if (type != null)
                return type;
            int comma = assemblyQualifiedName.IndexOf(',');
            string typeName = comma >= 0 ? assemblyQualifiedName.Substring(0, comma).Trim() : assemblyQualifiedName;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }

        private static object? ReadStaticMember(Type? type, string name)
        {
            if (type == null)
                return null;

            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null)
            {
                try
                {
                    object? value = field.GetValue(null);
                    if (value != null)
                        return value;
                }
                catch
                {
                }
            }

            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (property != null)
            {
                try
                {
                    return property.GetValue(null);
                }
                catch
                {
                }
            }

            return null;
        }

        private static object? ReadMember(object instance, string name)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    try
                    {
                        object? value = field.GetValue(instance);
                        if (value != null)
                            return value;
                    }
                    catch
                    {
                    }
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null)
                {
                    try
                    {
                        object? value = property.GetValue(instance);
                        if (value != null)
                            return value;
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }

        private static int ReadIntMember(object instance, string name, int fallback)
        {
            object? value = ReadMember(instance, name);
            if (value is int result)
                return result;
            if (value is long longValue)
                return (int)longValue;
            return value is short shortValue ? shortValue : fallback;
        }

        private static bool ReadBoolMember(object instance, string name, bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value is bool result ? result : fallback;
        }

        private static string ReadStringMember(object instance, string name)
        {
            object? value = ReadMember(instance, name);
            return value as string ?? string.Empty;
        }

        private static MethodInfo? FindMethodInHierarchy(Type? type, string name, int parameterCount)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                MethodInfo? method = current.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .FirstOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == parameterCount);
                if (method != null)
                    return method;
            }

            return null;
        }

        private static bool IsTypeOrBase(Type type, string fullName)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.FullName, fullName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static IEnumerable<object> EnumerateObjects(object? value)
        {
            if (value == null)
                yield break;

            if (value is IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                {
                    if (item != null)
                        yield return item;
                }
                yield break;
            }

            yield return value;
        }

        private sealed class StrongPlantingGunSlotItem
        {
            public StrongPlantingGunSlotItem(int index, object item)
            {
                Index = index;
                Item = item;
            }

            public int Index { get; }
            public object Item { get; }
        }
    }
}
