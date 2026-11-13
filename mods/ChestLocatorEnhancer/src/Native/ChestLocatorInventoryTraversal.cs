using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DTMAPI.ChestLocatorEnhancer
{
    internal static class ChestLocatorInventoryTraversal
    {
        private static readonly Dictionary<MemberCacheKey, MemberInfo?>
            MemberCache =
                new Dictionary<MemberCacheKey, MemberInfo?>();
        private static readonly Dictionary<MethodCacheKey, MethodInfo?>
            MethodCache =
                new Dictionary<MethodCacheKey, MethodInfo?>();

        private static TraversalScratch? cachedScratch;

        internal static ChestLocatorTraversalResult AppendSharedInventories(
            object archive,
            object? currentRootRoom,
            bool useBox,
            bool nativeAutoUseBox,
            Array nativeResult)
        {
            Type inventoryType = nativeResult.GetType().GetElementType()!;

            TraversalScratch scratch = AcquireScratch();
            try
            {
                for (int i = 0; i < nativeResult.Length; i++)
                {
                    object? nativeInventory =
                        nativeResult.GetValue(i);
                    scratch.Inventories.Add(nativeInventory!);
                    if (nativeInventory != null)
                    {
                        scratch.SeenInventories.Add(
                            nativeInventory);
                    }
                }

                object? currentRoom =
                    ReadMember(archive, "currentRoom") ??
                    ReadMember(archive, "CurrentRoom");
                AddRoom(currentRoom, scratch);
                AddRoom(
                    ReadMember(currentRoom, "RootRoom"),
                    scratch);
                AddRoom(currentRootRoom, scratch);
                AddRoom(
                    ReadMember(archive, "MainFarm"),
                    scratch);
                object? farmData =
                    ReadMember(archive, "farmData");
                AddRoom(
                    ReadMember(farmData, "currentRoom"),
                    scratch);
                AddRoom(
                    ReadMember(farmData, "MainFarm"),
                    scratch);

                while (scratch.PendingRooms.Count > 0)
                {
                    object room =
                        scratch.PendingRooms.Pop();
                    if (!scratch.VisitedRooms.Add(room))
                        continue;

                    scratch.ScannedRoots++;
                    // The native result already contains current-room inventories.
                    // Its buildings must still be traversed to reach remote interiors.
                    object? equipmentManager = ReferenceEquals(room, currentRoom)
                        ? null : ReadMember(room, "DM_equipment");
                    if (equipmentManager != null)
                    {
                        foreach (object equipment in NativeIndexes.Cases.Invoke(equipmentManager, null) as IEnumerable
                            ?? throw new InvalidOperationException("Native Case index returned no collection."))
                        {
                            scratch.ScannedEquipment++;
                            if (ReadBoolMember(equipment, "IsShared", false) &&
                                AddInventory(ReadMember(equipment, "inventory"), inventoryType, scratch))
                                scratch.SharedCases++;
                        }

                        if (useBox && nativeAutoUseBox)
                        {
                            foreach (object shelf in NativeIndexes.Shelves.Invoke(equipmentManager, null) as IEnumerable
                                ?? throw new InvalidOperationException("Native StorageShelf index returned no collection."))
                            {
                                scratch.ScannedEquipment++;
                                if (!ReadBoolMember(shelf, "IsShared", false))
                                    continue;
                                object? shelfInventory = ReadMember(shelf, "inventory");
                                MethodInfo? readAll = shelfInventory == null ? null :
                                    FindMethodInHierarchy(shelfInventory.GetType(), "ReadAll", 0);
                                if (!(readAll?.Invoke(shelfInventory, null) is IEnumerable items))
                                    continue;
                                foreach (object? item in items)
                                {
                                    if (item != null && NativeIndexes.ItemBox.IsInstanceOfType(item) &&
                                        AddInventory(ReadMember(item, "inventory"), inventoryType, scratch))
                                        scratch.SharedStorageBoxes++;
                                }
                            }
                        }
                    }
                    object? buildingManager =
                        ReadMember(room, "DM_building");
                    if (ReadMember(
                        buildingManager,
                        "Buildings") is IEnumerable buildings)
                    {
                        foreach (object? building in buildings)
                        {
                            AddRoom(
                                ReadMember(
                                    building,
                                    "room"),
                                scratch);
                        }
                    }
                }

                if (scratch.AppendedInventoryCount <= 0)
                {
                    return new ChestLocatorTraversalResult(
                        nativeResult,
                        nativeResult.Length,
                        0,
                        scratch.ScannedRoots,
                        scratch.ScannedEquipment,
                        scratch.SharedCases,
                        scratch.SharedStorageBoxes);
                }

                Array next =
                    Array.CreateInstance(
                        inventoryType,
                        scratch.Inventories.Count);
                for (int i = 0;
                    i < scratch.Inventories.Count;
                    i++)
                {
                    next.SetValue(
                        scratch.Inventories[i],
                        i);
                }

                return new ChestLocatorTraversalResult(
                    next,
                    nativeResult.Length,
                    scratch.AppendedInventoryCount,
                    scratch.ScannedRoots,
                    scratch.ScannedEquipment,
                    scratch.SharedCases,
                    scratch.SharedStorageBoxes);
            }
            finally
            {
                ReleaseScratch(scratch);
            }
        }

        // Resolve metadata once; Native V2 declares both closed generic uses.
        // Inventory members still come from a fresh query, including subclasses.
        private static class NativeIndexes
        {
            internal static readonly MethodInfo Cases = Resolve("DolocTown.Case");
            internal static readonly MethodInfo Shelves = Resolve("DolocTown.StorageShelf");
            internal static readonly Type ItemBox = typeof(DolocAPI).Assembly.GetType("DolocTown.ItemBox", true)!;

            private static MethodInfo Resolve(string elementName)
            {
                Assembly game = typeof(DolocAPI).Assembly;
                Type manager = game.GetType("DolocTown.EquipmentManager", true)!;
                Type element = game.GetType(elementName, true)!;
                foreach (MethodInfo method in manager.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    if (method.Name == "GetEquipments" && method.IsGenericMethodDefinition &&
                        method.GetGenericArguments().Length == 1 && method.GetParameters().Length == 0)
                        return method.MakeGenericMethod(element);
                throw new MissingMethodException(manager.FullName, "GetEquipments<T>()");
            }
        }
        private static TraversalScratch AcquireScratch()
        {
            TraversalScratch scratch =
                cachedScratch ??
                new TraversalScratch();
            cachedScratch = null;
            scratch.Reset();
            return scratch;
        }

        private static void ReleaseScratch(
            TraversalScratch scratch)
        {
            scratch.Reset();
            cachedScratch = scratch;
        }

        private static void AddRoom(
            object? room,
            TraversalScratch scratch)
        {
            if (room != null)
                scratch.PendingRooms.Push(room);
        }

        private static bool AddInventory(
            object? inventory,
            Type inventoryType,
            TraversalScratch scratch)
        {
            if (inventory == null ||
                !inventoryType.IsInstanceOfType(inventory) ||
                !scratch.SeenInventories.Add(inventory))
            {
                return false;
            }

            scratch.Inventories.Add(inventory);
            scratch.AppendedInventoryCount++;
            return true;
        }

        private static bool ReadBoolMember(
            object instance,
            string name,
            bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value is bool flag ? flag : fallback;
        }

        internal static object? ReadMember(
            object? instance,
            string name)
        {
            if (instance == null)
                return null;
            MemberInfo? member =
                FindMember(
                    instance.GetType(),
                    name,
                    isStatic: false);
            if (member is PropertyInfo property)
                return property.GetValue(instance);
            return (member as FieldInfo)?.GetValue(instance);
        }

        internal static object? ReadStaticMember(
            Type? type,
            string name)
        {
            if (type == null)
                return null;
            MemberInfo? member =
                FindMember(
                    type,
                    name,
                    isStatic: true);
            if (member is PropertyInfo property)
                return property.GetValue(null);
            return (member as FieldInfo)?.GetValue(null);
        }

        private static MemberInfo? FindMember(
            Type type,
            string name,
            bool isStatic)
        {
            var key =
                new MemberCacheKey(
                    type,
                    name,
                    isStatic);
            if (MemberCache.TryGetValue(key, out MemberInfo? cached))
                return cached;

            BindingFlags flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly |
                (isStatic
                    ? BindingFlags.Static
                    : BindingFlags.Instance);
            MemberInfo? resolved = null;
            for (Type? current = type;
                current != null && resolved == null;
                current = current.BaseType)
            {
                PropertyInfo? property =
                    current.GetProperty(name, flags);
                if (property != null &&
                    property.GetIndexParameters().Length == 0)
                {
                    resolved = property;
                    break;
                }

                resolved = current.GetField(name, flags);
            }

            MemberCache[key] = resolved;
            return resolved;
        }

        private static MethodInfo? FindMethodInHierarchy(
            Type type,
            string name,
            int parameterCount)
        {
            var key =
                new MethodCacheKey(
                    type,
                    name,
                    parameterCount);
            if (MethodCache.TryGetValue(key, out MethodInfo? cached))
                return cached;

            MethodInfo? resolved = null;
            for (Type? current = type;
                current != null && resolved == null;
                current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly))
                {
                    if (method.Name == name && !method.ContainsGenericParameters &&
                        method.GetParameters().Length ==
                            parameterCount)
                    {
                        resolved = method;
                        break;
                    }
                }
            }

            MethodCache[key] = resolved;
            return resolved;
        }

        private sealed class TraversalScratch
        {
            internal readonly List<object> Inventories =
                new List<object>();
            internal readonly HashSet<object> SeenInventories =
                new HashSet<object>(
                    ReferenceIdentityComparer<object>.Instance);
            internal readonly HashSet<object> VisitedRooms =
                new HashSet<object>(
                    ReferenceIdentityComparer<object>.Instance);
            internal readonly Stack<object> PendingRooms =
                new Stack<object>();

            internal int AppendedInventoryCount;
            internal int ScannedRoots;
            internal int ScannedEquipment;
            internal int SharedCases;
            internal int SharedStorageBoxes;

            internal void Reset()
            {
                Inventories.Clear();
                SeenInventories.Clear();
                VisitedRooms.Clear();
                PendingRooms.Clear();
                AppendedInventoryCount = 0;
                ScannedRoots = 0;
                ScannedEquipment = 0;
                SharedCases = 0;
                SharedStorageBoxes = 0;
            }
        }

        private readonly struct MemberCacheKey :
            IEquatable<MemberCacheKey>
        {
            private readonly Type type;
            private readonly string name;
            private readonly bool isStatic;

            internal MemberCacheKey(
                Type type,
                string name,
                bool isStatic)
            {
                this.type = type;
                this.name = name;
                this.isStatic = isStatic;
            }

            public bool Equals(MemberCacheKey other) =>
                type == other.type &&
                isStatic == other.isStatic &&
                name.Equals(
                    other.name,
                    StringComparison.Ordinal);

            public override bool Equals(object? value) =>
                value is MemberCacheKey other &&
                Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = type.GetHashCode();
                    hash = (hash * 397) ^
                        StringComparer.Ordinal.GetHashCode(name);
                    return (hash * 397) ^
                        isStatic.GetHashCode();
                }
            }
        }

        private readonly struct MethodCacheKey :
            IEquatable<MethodCacheKey>
        {
            private readonly Type type;
            private readonly string name;
            private readonly int parameterCount;

            internal MethodCacheKey(
                Type type,
                string name,
                int parameterCount)
            {
                this.type = type;
                this.name = name;
                this.parameterCount = parameterCount;
            }

            public bool Equals(MethodCacheKey other) =>
                type == other.type &&
                parameterCount == other.parameterCount &&
                name.Equals(
                    other.name,
                    StringComparison.Ordinal);

            public override bool Equals(object? value) =>
                value is MethodCacheKey other &&
                Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = type.GetHashCode();
                    hash = (hash * 397) ^
                        StringComparer.Ordinal.GetHashCode(name);
                    return (hash * 397) ^ parameterCount;
                }
            }
        }
    }

    internal readonly struct ChestLocatorTraversalResult
    {
        internal ChestLocatorTraversalResult(Array inventories, int baseCount, int appended, int roots, int equipment, int cases, int boxes)
        {
            Inventories = inventories;
            Counts = new ChestLocatorTraversalCounts(baseCount, appended, roots, equipment, cases, boxes);
        }
        internal Array Inventories { get; }
        internal ChestLocatorTraversalCounts Counts { get; }
    }

    // Diagnostics retain only scalar counts, never the returned inventory array.
    internal readonly struct ChestLocatorTraversalCounts
    {
        internal ChestLocatorTraversalCounts(int baseCount, int appended, int roots, int equipment, int cases, int boxes)
        {
            BaseInventoryCount = baseCount;
            AppendedInventoryCount = appended;
            ScannedRootCount = roots;
            ScannedEquipmentCount = equipment;
            SharedCaseCount = cases;
            SharedStorageBoxCount = boxes;
        }
        internal int BaseInventoryCount { get; }
        internal int AppendedInventoryCount { get; }
        internal int ScannedRootCount { get; }
        internal int ScannedEquipmentCount { get; }
        internal int SharedCaseCount { get; }
        internal int SharedStorageBoxCount { get; }
    }
    internal sealed class ReferenceIdentityComparer<T> :
        IEqualityComparer<T>
        where T : class
    {
        internal static readonly
            ReferenceIdentityComparer<T> Instance =
                new ReferenceIdentityComparer<T>();

        private ReferenceIdentityComparer()
        {
        }

        public bool Equals(
            T? left,
            T? right) =>
            ReferenceEquals(left, right);

        public int GetHashCode(T value) =>
            RuntimeHelpers.GetHashCode(value);
    }
}
