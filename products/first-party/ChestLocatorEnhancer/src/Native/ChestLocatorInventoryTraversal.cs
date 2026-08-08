using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DTMAPI.ChestLocatorEnhancer
{
    internal static class ChestLocatorInventoryTraversal
    {
        private const string CaseTypeName = "DolocTown.Case";
        private const string StorageShelfTypeName =
            "DolocTown.StorageShelf";
        private const string ItemBoxTypeName =
            "DolocTown.ItemBox";
        private static readonly object MetadataGate =
            new object();
        private static readonly Dictionary<MemberCacheKey, MemberInfo?>
            MemberCache =
                new Dictionary<MemberCacheKey, MemberInfo?>();
        private static readonly Dictionary<MethodCacheKey, MethodInfo?>
            MethodCache =
                new Dictionary<MethodCacheKey, MethodInfo?>();
        private static readonly Dictionary<Type, EquipmentKind>
            EquipmentKindCache =
                new Dictionary<Type, EquipmentKind>();

        [ThreadStatic]
        private static TraversalScratch? cachedScratch;

        internal static ChestLocatorTraversalResult AppendSharedInventories(
            object archive,
            object? currentRootRoom,
            bool useBox,
            bool includeSharedCases,
            bool includeSharedStorageShelfBoxes,
            bool nativeAutoUseBox,
            Array nativeResult)
        {
            if (archive == null)
                throw new ArgumentNullException(nameof(archive));
            if (nativeResult == null)
                throw new ArgumentNullException(nameof(nativeResult));

            Type? inventoryType =
                nativeResult.GetType().GetElementType();
            if (inventoryType == null)
                throw new InvalidOperationException(
                    "The native inventory result has no array element type.");

            TraversalScratch scratch = AcquireScratch();
            try
            {
                for (int i = 0; i < nativeResult.Length; i++)
                {
                    object? nativeInventory =
                        nativeResult.GetValue(i);
                    scratch.Inventories.Add(nativeInventory!);
                    if (nativeInventory != null &&
                        inventoryType.IsInstanceOfType(
                            nativeInventory))
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
                    object? equipmentManager =
                        ReadMember(room, "DM_equipment");
                    if (ReadMember(
                        equipmentManager,
                        "AllEquipments") is IEnumerable equipmentList)
                    {
                        foreach (object? candidate in equipmentList)
                        {
                            if (candidate == null)
                                continue;

                            object equipment = candidate;
                            scratch.ScannedEquipment++;
                            ProcessEquipment(
                                equipment,
                                useBox,
                                includeSharedCases,
                                includeSharedStorageShelfBoxes,
                                nativeAutoUseBox,
                                inventoryType,
                                scratch);
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

        private static void ProcessEquipment(
            object equipment,
            bool useBox,
            bool includeSharedCases,
            bool includeSharedStorageShelfBoxes,
            bool nativeAutoUseBox,
            Type inventoryType,
            TraversalScratch scratch)
        {
            if (!ReadBoolMember(
                equipment,
                "IsShared",
                fallback: false))
            {
                return;
            }

            switch (GetEquipmentKind(equipment.GetType()))
            {
                case EquipmentKind.Case:
                    if (!includeSharedCases)
                        return;
                    if (AddInventory(
                        ReadMember(equipment, "inventory"),
                        inventoryType,
                        scratch))
                    {
                        scratch.SharedCases++;
                    }
                    return;

                case EquipmentKind.StorageShelf:
                    if (!includeSharedStorageShelfBoxes ||
                        !useBox ||
                        !nativeAutoUseBox)
                    {
                        return;
                    }

                    object? shelfInventory =
                        ReadMember(equipment, "inventory");
                    MethodInfo? readAll =
                        shelfInventory == null
                            ? null
                            : FindMethodInHierarchy(
                                shelfInventory.GetType(),
                                "ReadAll",
                                parameterCount: 0);
                    if (!(readAll?.Invoke(
                        shelfInventory,
                        null) is IEnumerable items))
                    {
                        return;
                    }

                    foreach (object? item in items)
                    {
                        if (item == null ||
                            !IsTypeOrBase(
                                item.GetType(),
                                ItemBoxTypeName))
                        {
                            continue;
                        }

                        if (AddInventory(
                            ReadMember(item, "inventory"),
                            inventoryType,
                            scratch))
                        {
                            scratch.SharedStorageBoxes++;
                        }
                    }
                    return;

                default:
                    return;
            }
        }

        private static EquipmentKind GetEquipmentKind(
            Type type)
        {
            lock (MetadataGate)
            {
                if (EquipmentKindCache.TryGetValue(
                    type,
                    out EquipmentKind cached))
                {
                    return cached;
                }
            }

            EquipmentKind resolved =
                IsTypeOrBase(type, CaseTypeName)
                    ? EquipmentKind.Case
                    : IsTypeOrBase(
                        type,
                        StorageShelfTypeName)
                        ? EquipmentKind.StorageShelf
                        : EquipmentKind.Other;
            lock (MetadataGate)
                EquipmentKindCache[type] = resolved;
            return resolved;
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
            lock (MetadataGate)
            {
                if (MemberCache.TryGetValue(
                    key,
                    out MemberInfo? cached))
                {
                    return cached;
                }
            }

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

            lock (MetadataGate)
                MemberCache[key] = resolved;
            return resolved;
        }

        private static bool IsTypeOrBase(
            Type? type,
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

        private static MethodInfo? FindMethodInHierarchy(
            Type? type,
            string name,
            int parameterCount)
        {
            if (type == null)
                return null;
            var key =
                new MethodCacheKey(
                    type,
                    name,
                    parameterCount);
            lock (MetadataGate)
            {
                if (MethodCache.TryGetValue(
                    key,
                    out MethodInfo? cached))
                {
                    return cached;
                }
            }

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
                    if (method.Name == name &&
                        method.GetParameters().Length ==
                            parameterCount)
                    {
                        resolved = method;
                        break;
                    }
                }
            }

            lock (MetadataGate)
                MethodCache[key] = resolved;
            return resolved;
        }

        private enum EquipmentKind
        {
            Other,
            Case,
            StorageShelf
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
        internal ChestLocatorTraversalResult(
            Array inventories,
            int baseInventoryCount,
            int appendedInventoryCount,
            int scannedRootCount,
            int scannedEquipmentCount,
            int sharedCaseCount,
            int sharedStorageBoxCount)
        {
            Inventories =
                inventories ??
                throw new ArgumentNullException(
                    nameof(inventories));
            BaseInventoryCount = baseInventoryCount;
            AppendedInventoryCount =
                appendedInventoryCount;
            ScannedRootCount = scannedRootCount;
            ScannedEquipmentCount =
                scannedEquipmentCount;
            SharedCaseCount = sharedCaseCount;
            SharedStorageBoxCount =
                sharedStorageBoxCount;
        }

        internal Array Inventories { get; }

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
