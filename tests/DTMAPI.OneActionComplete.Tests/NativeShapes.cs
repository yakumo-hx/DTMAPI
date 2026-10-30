// Test doubles provide shape and controlled side effects only. They do not
// reproduce game implementations or stand in for the native acceptance matrix.
using System;
using DTMAPI.Abstractions;
namespace UnityEngine
{
    internal struct Vector2 { }
    internal sealed class Collider2D
    {
        internal DolocTown.DungeonResourceRenderer? Renderer;
        public T? GetComponent<T>() where T : class => Renderer as T;
    }
}
namespace DolocTown
{
    internal class ItemInfo { internal bool Suitable = true; }
    internal class Item
    {
        public int count;
        public ItemInfo proto = new ItemInfo();
        public bool CostSelf(out Item item, bool show)
        {
            item = null!;
            if (DolocAPI.archiveHandle.InventorySystem.inventory.IndexOf(this) < 0) return false;
            item = new Item { count = 1, proto = proto };
            if (--count == 0) { DolocAPI.SelectedItem = null; DolocAPI.archiveHandle.InventorySystem.inventory.Current = null; }
            return true;
        }
    }
    internal sealed class ItemTool : Item { internal int Damage; internal bool Valid = true, LevelMatch = true; }
    internal sealed class DungeonResourceRenderer { public DungeonResource? DungeonResource; }
    internal class DungeonResource
    {
        public int currentHealth;
        public bool IsRemoved;
        public UnityEngine.Vector2 PositionCenter => default;
        internal int Hits, Drops, Bonus;
        public void _Fell(ResourceFellData data)
        {
            if (!data.Valid || !data.levelMatch || IsRemoved) return;
            Hits++; currentHealth -= data.Damage + Bonus;
            if (currentHealth <= 0) { IsRemoved = true; Drops++; }
        }
    }
    internal class DungeonResourceTree : DungeonResource { }
    internal class DungeonResourceTreeTrunk : DungeonResource { }
    internal class DungeonResourceOre : DungeonResource { }
    internal class DungeonResourceWeeds : DungeonResource { }
    internal class DungeonResourceBuildingsGarbage : DungeonResource { }
    internal class DungeonResourceModelMechanicalGarbage : DungeonResource { }
    internal readonly struct ResourceFellData
    {
        public readonly bool Valid, levelMatch;
        public readonly int Damage;
        public ResourceFellData(DungeonResource r, ItemTool tool, UnityEngine.Vector2 p) { Valid = tool.Valid; levelMatch = tool.LevelMatch; Damage = tool.Damage; }
    }
    internal class Equipment
    {
        internal int Amount, Capacity = 5, ExceedCapacity;
        public object IElectronicComponent => new DolocTown.GameData.ElectronicComponentGeneratorFuel(this);
        internal bool ThrowBefore, ThrowAfter;
        internal void Add(ItemInfo proto)
        {
            if (ThrowBefore) throw new InvalidOperationException("before native mutation");
            Amount++;
            if (ThrowAfter) throw new InvalidOperationException("after native mutation");
        }
    }
    internal class PowerGeneratorFuel : Equipment
    {
        public float FuelPercent => Math.Min(1, (float)Amount / Capacity);
        public bool IsSuitableFuel(Item item) => item.proto.Suitable;
        public void AddFuel(ItemInfo proto, bool message) => Add(proto);
    }
    internal class Feeder : Equipment
    {
        public float progress => (float)Amount / Capacity;
        public bool IsFeederFull => Amount >= Capacity;
        public bool IsAnimalFeeds(ItemInfo proto, out int energy) { energy = 1; return proto.Suitable; }
        public void AddFeeds(ItemInfo proto) => Add(proto);
    }
    internal class Archive { public InventorySystem InventorySystem = new InventorySystem(); }
    internal class InventorySystem { public Inventory inventory = new Inventory(); }
    internal class Inventory { internal Item? Current; public int IndexOf(Item item) => ReferenceEquals(Current, item) ? 0 : -1; }
}
internal static class DolocAPI
{
    public static DolocTown.Equipment? SelectedEquipment;
    public static DolocTown.Item? SelectedItem;
    public static DolocTown.Archive archiveHandle = new DolocTown.Archive();
    internal static int Energy;
    public static bool HasEnoughEnergyForUsingTool() => Energy > 0;
    public static bool CostToolEnergy() { if (Energy <= 0) return false; Energy--; return true; }
}
namespace Yuuka.DTMAPI.OneActionComplete
{
    internal sealed class OneActionHookInstaller
    {
        internal static bool FailCleanup;
        internal OneActionHookInstaller(IMonitor monitor) { }
        internal int InstalledPatchCount { get; private set; }
        internal void InstallAtomically() => InstalledPatchCount = 4;
        internal void UnpatchOwnedHooks() { if (FailCleanup) throw new InvalidOperationException("unpatch"); InstalledPatchCount = 0; }
    }
}
namespace DolocTown.GameData
{
    internal sealed class ElectronicComponentGeneratorFuel
    {
        private readonly Equipment equipment;
        internal ElectronicComponentGeneratorFuel(Equipment equipment) => this.equipment = equipment;
        public float Fuel => equipment.Amount;
        public bool IsFull => equipment.Amount >= equipment.Capacity + equipment.ExceedCapacity;
    }
}
