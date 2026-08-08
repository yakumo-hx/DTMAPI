#pragma warning disable CS0618 // This file implements the frozen IChestLocatorEnhancerApi compatibility island.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Frozen 0.5.5 executor for already-built IChestLocatorEnhancerApi consumers only.</summary>
    internal sealed class ChestLocatorEnhancerCompatibilityService : IChestLocatorEnhancerApi
    {
        private const string ManagedProductUniqueId = "DTMAPI.ChestLocatorEnhancerMod";
        private const string ManagedProductHarmonyOwner = "dtmapi.mod.dtmapi.chestlocatorenhancermod";
        private readonly DtmApiRuntime runtime;
        private readonly Func<bool> managedProductOwnerPresent;
        private readonly Dictionary<string, ChestLocatorEnhancerOptions> chestLocatorOptions = new Dictionary<string, ChestLocatorEnhancerOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ChestLocatorEnhancerState> chestLocatorStates = new Dictionary<string, ChestLocatorEnhancerState>(StringComparer.OrdinalIgnoreCase);
        private bool chestLocatorInventoryHookInstalled;

        public ChestLocatorEnhancerCompatibilityService(DtmApiRuntime runtime)
            : this(runtime, IsManagedProductOwnerPresent)
        {
        }

        internal ChestLocatorEnhancerCompatibilityService(DtmApiRuntime runtime, Func<bool> managedProductOwnerPresent)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.managedProductOwnerPresent = managedProductOwnerPresent ?? throw new ArgumentNullException(nameof(managedProductOwnerPresent));
        }

        internal string LastChestLocatorEnhancerSummary { get; private set; } = string.Empty;

        internal int ChestLocatorEnhancerExtensionApplications { get; private set; }

        internal void SetInventoryHookInstalled(bool installed)
        {
            chestLocatorInventoryHookInstalled = installed;
            foreach (KeyValuePair<string, ChestLocatorEnhancerState> entry in chestLocatorStates.ToArray())
            {
                ChestLocatorEnhancerState state = entry.Value;
                state.HookInstalled = installed;
                if (state.IsConfigured)
                    state.Status = state.Enabled ? (installed ? "configured-experimental-inventory-hook" : "configured-pending-hook") : "disabled";
                chestLocatorStates[entry.Key] = state;
            }
        }

        public ChestLocatorEnhancerRegisterResult Register(IManifest owner, ChestLocatorEnhancerOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            ChestLocatorEnhancerOptions normalized = NormalizeChestLocatorEnhancerOptions(options);
            if (normalized.Enabled)
                ThrowIfManagedProductOwnsInventoryRoute();
            chestLocatorOptions[owner.UniqueID] = normalized;
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ChestLocatorEnhancer, owner.UniqueID, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "policy", normalized.Enabled, "chest locator policy registered");
            UpdateChestLocatorEnhancerRegistrationStates();
            ChestLocatorEnhancerState state = GetChestLocatorEnhancerState(owner.UniqueID);
            state.IsConfigured = true;
            state.Enabled = normalized.Enabled;
            state.HookInstalled = chestLocatorInventoryHookInstalled;
            state.Status = normalized.Enabled ? (chestLocatorInventoryHookInstalled ? "configured-experimental-inventory-hook" : "configured-pending-hook") : "disabled";
            chestLocatorStates[owner.UniqueID] = state;

            var result = new ChestLocatorEnhancerRegisterResult
            {
                Success = true,
                OwnerId = owner.UniqueID,
                Enabled = normalized.Enabled,
                HookInstalled = chestLocatorInventoryHookInstalled,
                Message = state.LastMessage
            };
            runtime.RuntimeMonitor.Log("ChestLocatorEnhancer API register success=True owner=" + owner.UniqueID + " enabled=" + normalized.Enabled + " hook=" + chestLocatorInventoryHookInstalled + " message=" + result.Message);
            runtime.SetHookStatus("Inventory.ChestLocatorEnhancer", state.Status, "IChestLocatorEnhancerApi -> ArchiveDataHandle.GetAvailableInventories Postfix", state.LastMessage);
            return result;
        }

        internal int ReconcileManagedProductOwnerBeforeHookInstall()
        {
            if (chestLocatorOptions.Count == 0 || !managedProductOwnerPresent())
                return 0;

            var owners = new List<string>(chestLocatorOptions.Keys);
            int removed = 0;
            foreach (string ownerId in owners)
                removed += RemoveOwner(ownerId, "managed ChestLocatorEnhancer product acquired native ownership before compatibility Hook installation");

            chestLocatorInventoryHookInstalled = false;
            runtime.RuntimeMonitor.Log(
                "Frozen IChestLocatorEnhancerApi compatibility removed " + removed +
                " pending owner resource(s) because managed product " + ManagedProductUniqueId +
                " owns ArchiveDataHandle.GetAvailableInventories.");
            return removed;
        }

        private void ThrowIfManagedProductOwnsInventoryRoute()
        {
            if (managedProductOwnerPresent())
            {
                throw new InvalidOperationException(
                    "Frozen IChestLocatorEnhancerApi compatibility refused inventory ownership because managed product " +
                    ManagedProductUniqueId + " is active.");
            }
        }

        private static bool IsManagedProductOwnerPresent()
        {
            Type? targetType = ResolveType("DolocTown.GameData.ArchiveDataHandle, Assembly-CSharp");
            MethodInfo? target = FindExactInventoryTarget(targetType);
            Type? harmonyType = ResolveType("HarmonyLib.Harmony, 0Harmony");
            MethodInfo? getPatchInfo = harmonyType?.GetMethod(
                "GetPatchInfo",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(MethodBase) },
                null);
            object? info = target == null || getPatchInfo == null
                ? null
                : getPatchInfo.Invoke(null, new object[] { target });
            object? ownerList = info?.GetType()
                .GetProperty("Owners", BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(info);
            if (!(ownerList is IEnumerable owners))
                return false;

            foreach (object? ownerValue in owners)
            {
                if ((ownerValue as string ?? string.Empty).Equals(ManagedProductHarmonyOwner, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static MethodInfo? FindExactInventoryTarget(Type? type)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                MethodInfo? target = current
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly)
                    .Where(method =>
                    {
                        if (!method.Name.Equals("GetAvailableInventories", StringComparison.Ordinal) ||
                            !method.ReturnType.IsArray)
                        {
                            return false;
                        }

                        ParameterInfo[] parameters = method.GetParameters();
                        return parameters.Length == 3 &&
                            string.Equals(
                                parameters[0].ParameterType.FullName,
                                "UnityEngine.Vector2Int",
                                StringComparison.Ordinal) &&
                            string.Equals(
                                parameters[1].ParameterType.FullName,
                                "UnityEngine.Vector2Int",
                                StringComparison.Ordinal) &&
                            parameters[2].ParameterType == typeof(bool);
                    })
                    .SingleOrDefault();
                if (target != null)
                    return target;
            }

            return null;
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            int removed = 0;
            try
            {
                GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ChestLocatorEnhancer, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "policy", false, "chest locator owner cleanup " + (reason ?? string.Empty));
            }
            finally
            {
                if (chestLocatorOptions.Remove(ownerId))
                    removed++;
                if (chestLocatorStates.Remove(ownerId))
                    removed++;
                UpdateChestLocatorEnhancerRegistrationStates();
            }
            return removed;
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId ??= string.Empty;
            return (chestLocatorOptions.ContainsKey(ownerId) ? 1 : 0) + (chestLocatorStates.ContainsKey(ownerId) ? 1 : 0);
        }

        ChestLocatorEnhancerState IChestLocatorEnhancerApi.GetState(string uniqueId)
        {
            return CloneChestLocatorEnhancerState(GetChestLocatorEnhancerState(uniqueId ?? string.Empty));
        }

        BridgeFeatureStatus IChestLocatorEnhancerApi.GetStatus(string uniqueId)
        {
            ChestLocatorEnhancerState state = GetChestLocatorEnhancerState(uniqueId ?? string.Empty);
            return new BridgeFeatureStatus(state.Status, state.LastMessage);
        }

        internal Array ExtendAvailableInventoriesForChestLocator(object archive, object anchor, object area, bool useBox, Array nativeResult)
        {
            if (nativeResult == null || !TryGetChestLocatorPolicy(out EffectiveChestLocatorPolicy policy))
                return nativeResult!;

            ChestLocatorEnhancerOptions options = policy.Options;
            Type? inventoryType = nativeResult.GetType().GetElementType();
            if (inventoryType == null)
                return nativeResult;

            int baseCount = nativeResult.Length;
            var inventories = new List<object>(Math.Max(baseCount + 4, 4));
            var seen = new HashSet<int>();
            for (int i = 0; i < nativeResult.Length; i++)
                AddInventory(nativeResult.GetValue(i), inventoryType, inventories, seen);

            int scannedRoots = 0;
            int scannedEquipment = 0;
            int sharedCases = 0;
            int sharedStorageBoxes = 0;
            bool nativeAutoUseBox = !options.RespectNativeAutoUseBoxSetting || IsNativeAutoUseBoxEnabled();
            Type? caseType = ResolveType("DolocTown.Case, Assembly-CSharp");
            Type? storageShelfType = ResolveType("DolocTown.StorageShelf, Assembly-CSharp");

            foreach (object room in EnumerateMachineCandidateRooms(ResolveType("DolocAPI, Assembly-CSharp") ?? archive.GetType(), archive, ReadMember(archive, "currentRoom") ?? ReadMember(archive, "CurrentRoom") ?? ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "CurrentRoom") ?? archive))
            {
                scannedRoots++;
                foreach (object equipment in EnumerateEquipments(room))
                {
                    scannedEquipment++;
                    if (!ReadBoolMember(equipment, "IsShared", false))
                        continue;

                    Type equipmentType = equipment.GetType();
                    if (options.IncludeSharedCases && caseType != null && caseType.IsAssignableFrom(equipmentType))
                    {
                        object? inventory = ReadMember(equipment, "inventory");
                        int before = inventories.Count;
                        AddInventory(inventory, inventoryType, inventories, seen);
                        if (inventories.Count > before)
                            sharedCases++;
                        continue;
                    }

                    if (options.IncludeSharedStorageShelfBoxes && useBox && nativeAutoUseBox && storageShelfType != null && storageShelfType.IsAssignableFrom(equipmentType))
                    {
                        object? shelfInventory = ReadMember(equipment, "inventory");
                        MethodInfo? readAll = shelfInventory == null ? null : FindMethodInHierarchy(shelfInventory.GetType(), "ReadAll", 0);
                        object? readResult = readAll == null ? null : readAll.Invoke(shelfInventory, null);
                        if (!(readResult is IEnumerable shelfItems))
                            continue;

                        foreach (object? item in shelfItems)
                        {
                            if (item == null || !IsTypeOrBase(item.GetType(), "DolocTown.ItemBox"))
                                continue;
                            object? boxInventory = ReadMember(item, "inventory");
                            int before = inventories.Count;
                            AddInventory(boxInventory, inventoryType, inventories, seen);
                            if (inventories.Count > before)
                                sharedStorageBoxes++;
                        }
                    }
                }
            }

            int appended = inventories.Count - baseCount;
            ChestLocatorEnhancerExtensionApplications++;
            LastChestLocatorEnhancerSummary = "owner=" + policy.OwnerSummary +
                ", effectiveOwners=" + policy.OwnerSummary +
                ", includeSharedCases=" + options.IncludeSharedCases +
                ", includeSharedStorageShelfBoxes=" + options.IncludeSharedStorageShelfBoxes +
                ", respectNativeAutoUseBox=" + options.RespectNativeAutoUseBoxSetting +
                ", verboseLogging=" + options.VerboseLogging +
                ", useBox=" + useBox +
                ", nativeAutoUseBox=" + nativeAutoUseBox +
                ", base=" + baseCount +
                ", appended=" + appended +
                ", roots=" + scannedRoots +
                ", equipments=" + scannedEquipment +
                ", sharedCases=" + sharedCases +
                ", sharedStorageBoxes=" + sharedStorageBoxes +
                ", applications=" + ChestLocatorEnhancerExtensionApplications;
            UpdateChestLocatorEnhancerStates(baseCount, appended, scannedRoots, scannedEquipment, sharedCases, sharedStorageBoxes, LastChestLocatorEnhancerSummary);
            if (options.VerboseLogging || appended > 0 || ChestLocatorEnhancerExtensionApplications <= 3)
                runtime.RuntimeMonitor.Log("ChestLocatorEnhancer inventories " + LastChestLocatorEnhancerSummary);
            runtime.SetHookStatus("Inventory.ChestLocatorEnhancer", appended > 0 ? "verified" : "experimental", "Harmony Postfix: ArchiveDataHandle.GetAvailableInventories", LastChestLocatorEnhancerSummary);

            if (appended <= 0)
                return nativeResult;

            Array next = Array.CreateInstance(inventoryType, inventories.Count);
            for (int i = 0; i < inventories.Count; i++)
                next.SetValue(inventories[i], i);
            return next;
        }

        private bool TryGetChestLocatorPolicy(out EffectiveChestLocatorPolicy policy)
        {
            KeyValuePair<string, ChestLocatorEnhancerOptions>[] enabledOwners = chestLocatorOptions
                .Select(entry => new KeyValuePair<string, ChestLocatorEnhancerOptions>(entry.Key, entry.Value ?? new ChestLocatorEnhancerOptions { Enabled = false }))
                .Where(entry => entry.Value.Enabled)
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (enabledOwners.Length > 0)
            {
                var options = new ChestLocatorEnhancerOptions
                {
                    Enabled = true,
                    IncludeSharedCases = enabledOwners.Any(entry => entry.Value.IncludeSharedCases),
                    IncludeSharedStorageShelfBoxes = enabledOwners.Any(entry => entry.Value.IncludeSharedStorageShelfBoxes),
                    RespectNativeAutoUseBoxSetting = enabledOwners.All(entry => entry.Value.RespectNativeAutoUseBoxSetting),
                    VerboseLogging = enabledOwners.Any(entry => entry.Value.VerboseLogging)
                };
                policy = new EffectiveChestLocatorPolicy(string.Join("|", enabledOwners.Select(entry => entry.Key)), options);
                return true;
            }

            policy = new EffectiveChestLocatorPolicy("none", new ChestLocatorEnhancerOptions { Enabled = false });
            return false;
        }

        private void UpdateChestLocatorEnhancerRegistrationStates()
        {
            bool hasEffectivePolicy = TryGetChestLocatorPolicy(out EffectiveChestLocatorPolicy policy);
            string effectiveSummary = hasEffectivePolicy ? FormatEffectivePolicy(policy) : "effectiveOwners=none.";
            foreach (KeyValuePair<string, ChestLocatorEnhancerOptions> entry in chestLocatorOptions.ToArray())
            {
                ChestLocatorEnhancerOptions entryOptions = entry.Value ?? new ChestLocatorEnhancerOptions();
                ChestLocatorEnhancerState state = GetChestLocatorEnhancerState(entry.Key);
                state.IsConfigured = true;
                state.Enabled = entryOptions.Enabled;
                state.HookInstalled = chestLocatorInventoryHookInstalled;
                state.Status = entryOptions.Enabled ? (chestLocatorInventoryHookInstalled ? "configured-experimental-inventory-hook" : "configured-pending-hook") : "disabled";
                state.LastMessage = entryOptions.Enabled
                    ? "Chest locator enhancer policy registered; shared Case inventories and shared StorageShelf ItemBox inventories are appended to the native inventory array when the hook is installed. " + effectiveSummary
                    : "Chest locator enhancer policy is disabled. " + effectiveSummary;
                chestLocatorStates[entry.Key] = state;
            }
        }

        private void UpdateChestLocatorEnhancerStates(int baseCount, int appended, int roots, int equipment, int cases, int storageBoxes, string message)
        {
            foreach (KeyValuePair<string, ChestLocatorEnhancerOptions> entry in chestLocatorOptions.ToArray())
            {
                ChestLocatorEnhancerOptions entryOptions = entry.Value ?? new ChestLocatorEnhancerOptions();
                ChestLocatorEnhancerState state = GetChestLocatorEnhancerState(entry.Key);
                state.IsConfigured = true;
                state.Enabled = entryOptions.Enabled;
                state.HookInstalled = chestLocatorInventoryHookInstalled;
                state.ExtensionApplications = ChestLocatorEnhancerExtensionApplications;
                state.LastBaseInventoryCount = baseCount;
                state.LastAppendedInventoryCount = appended;
                state.LastScannedRootCount = roots;
                state.LastScannedEquipmentCount = equipment;
                state.LastSharedCaseCount = cases;
                state.LastSharedStorageBoxCount = storageBoxes;
                state.Status = entryOptions.Enabled ? (appended > 0 ? "verified" : "configured-experimental-inventory-hook") : "disabled";
                state.LastMessage = message ?? string.Empty;
                chestLocatorStates[entry.Key] = state;
            }
        }

        private ChestLocatorEnhancerState GetChestLocatorEnhancerState(string ownerId)
        {
            ownerId ??= string.Empty;
            if (chestLocatorStates.TryGetValue(ownerId, out ChestLocatorEnhancerState state))
                return state;

            ChestLocatorEnhancerOptions options = chestLocatorOptions.TryGetValue(ownerId, out ChestLocatorEnhancerOptions? configured)
                ? configured
                : new ChestLocatorEnhancerOptions { Enabled = false };
            return new ChestLocatorEnhancerState
            {
                OwnerId = ownerId,
                IsConfigured = chestLocatorOptions.ContainsKey(ownerId),
                Enabled = options.Enabled && chestLocatorOptions.ContainsKey(ownerId),
                HookInstalled = chestLocatorInventoryHookInstalled,
                Status = chestLocatorOptions.ContainsKey(ownerId) ? (options.Enabled ? "registered" : "disabled") : "not-configured",
                LastMessage = chestLocatorOptions.ContainsKey(ownerId) ? "Chest locator enhancer policy registered." : "No chest locator enhancer policy registered."
            };
        }

        private static ChestLocatorEnhancerState CloneChestLocatorEnhancerState(ChestLocatorEnhancerState state)
        {
            return new ChestLocatorEnhancerState
            {
                OwnerId = state.OwnerId,
                IsConfigured = state.IsConfigured,
                Enabled = state.Enabled,
                HookInstalled = state.HookInstalled,
                ExtensionApplications = state.ExtensionApplications,
                LastBaseInventoryCount = state.LastBaseInventoryCount,
                LastAppendedInventoryCount = state.LastAppendedInventoryCount,
                LastScannedRootCount = state.LastScannedRootCount,
                LastScannedEquipmentCount = state.LastScannedEquipmentCount,
                LastSharedCaseCount = state.LastSharedCaseCount,
                LastSharedStorageBoxCount = state.LastSharedStorageBoxCount,
                Status = state.Status,
                LastMessage = state.LastMessage
            };
        }

        private static ChestLocatorEnhancerOptions NormalizeChestLocatorEnhancerOptions(ChestLocatorEnhancerOptions? options)
        {
            options ??= new ChestLocatorEnhancerOptions();
            return new ChestLocatorEnhancerOptions
            {
                Enabled = options.Enabled,
                IncludeSharedCases = options.IncludeSharedCases,
                IncludeSharedStorageShelfBoxes = options.IncludeSharedStorageShelfBoxes,
                RespectNativeAutoUseBoxSetting = options.RespectNativeAutoUseBoxSetting,
                VerboseLogging = options.VerboseLogging
            };
        }

        private static string FormatEffectivePolicy(EffectiveChestLocatorPolicy policy)
        {
            ChestLocatorEnhancerOptions options = policy.Options;
            return "effectiveOwners=" + policy.OwnerSummary +
                ", includeSharedCases=" + options.IncludeSharedCases +
                ", includeSharedStorageShelfBoxes=" + options.IncludeSharedStorageShelfBoxes +
                ", respectNativeAutoUseBox=" + options.RespectNativeAutoUseBoxSetting +
                ", verboseLogging=" + options.VerboseLogging + ".";
        }

        private static bool AddInventory(object? inventory, Type inventoryType, List<object> inventories, HashSet<int> seen)
        {
            if (inventory == null || !inventoryType.IsInstanceOfType(inventory))
                return false;

            int key = RuntimeHelpers.GetHashCode(inventory);
            if (!seen.Add(key))
                return false;

            inventories.Add(inventory);
            return true;
        }

        private static bool IsNativeAutoUseBoxEnabled()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? userSettings = ReadStaticMember(dolocApi, "userSettings");
            return userSettings == null || ReadBoolMember(userSettings, "autoUseBox", true);
        }

        private sealed class EffectiveChestLocatorPolicy
        {
            internal EffectiveChestLocatorPolicy(string ownerSummary, ChestLocatorEnhancerOptions options)
            {
                OwnerSummary = ownerSummary;
                Options = options;
            }

            internal string OwnerSummary { get; }

            internal ChestLocatorEnhancerOptions Options { get; }
        }

        private static IEnumerable<object> EnumerateMachineCandidateRooms(Type dolocApi, object archive, object currentRoom)
        {
            var visitedRooms = new HashSet<int>();
            void AddRoom(object? room, List<object> rooms)
            {
                if (room == null)
                    return;
                int key = RuntimeHelpers.GetHashCode(room);
                if (visitedRooms.Add(key))
                    rooms.Add(room);
            }

            var result = new List<object>();
            AddRoom(currentRoom, result);
            AddRoom(ReadMember(currentRoom, "RootRoom"), result);
            AddRoom(ReadStaticMember(dolocApi, "CurrentRootRoom"), result);
            AddRoom(ReadMember(archive, "currentRoom"), result);
            AddRoom(ReadMember(archive, "MainFarm"), result);
            object? farmData = ReadMember(archive, "farmData");
            AddRoom(farmData == null ? null : ReadMember(farmData, "currentRoom"), result);
            AddRoom(farmData == null ? null : ReadMember(farmData, "MainFarm"), result);
            return result;
        }

        private static IEnumerable<object> EnumerateEquipments(object room)
        {
            var visitedRooms = new HashSet<object>();
            foreach (object equipment in EnumerateEquipments(room, visitedRooms))
                yield return equipment;
        }

        private static IEnumerable<object> EnumerateEquipments(object room, HashSet<object> visitedRooms)
        {
            if (room == null || !visitedRooms.Add(room))
                yield break;

            object? equipmentManager = ReadMember(room, "DM_equipment");
            object? allEquipments = equipmentManager == null ? null : ReadMember(equipmentManager, "AllEquipments");
            if (allEquipments is IEnumerable enumerable)
            {
                foreach (object? equipment in enumerable)
                {
                    if (equipment != null)
                        yield return equipment;
                }
            }

            object? buildingManager = ReadMember(room, "DM_building");
            object? buildings = buildingManager == null ? null : ReadMember(buildingManager, "Buildings");
            if (!(buildings is IEnumerable buildingEnumerable))
                yield break;

            foreach (object? building in buildingEnumerable)
            {
                if (building == null)
                    continue;
                object? childRoom = ReadMember(building, "room");
                if (childRoom == null)
                    continue;
                foreach (object childEquipment in EnumerateEquipments(childRoom, visitedRooms))
                    yield return childEquipment;
            }
        }

    }
}
