using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class DolocAPI
{
    public static object? archiveHandle;
    public static object? CurrentRoom, agent, SelectedItem;
}

namespace DolocTown.Config.Item
{
    public sealed class ItemInfo
    {
    }

    public sealed class ItemFunctionFarmingGun
    {
        public ItemFunctionFarmingGun(int capacity) => Capacity = capacity;
        public int Capacity { get; set; }
        public NativeArea Area { get; private set; } = new NativeArea(7, 7);
        public NativeArea Offset { get; private set; } = new NativeArea(-3, 0);
    }

    public struct NativeArea
    {
        public int x, y;
        public NativeArea(int x, int y) { this.x = x; this.y = y; }
    }
}

namespace DolocTown
{
    public delegate void InventoryReceiver(
        int index,
        Item? item,
        bool isSlotLocked,
        Item? replacedItem);

    public sealed class ArchiveHandle
    {
        public ArchiveHandle(LinearInventory inventory) =>
            InventorySystem = new InventorySystem(inventory);

        public InventorySystem InventorySystem { get; }
    }

    public sealed class InventorySystem
    {
        public InventorySystem(LinearInventory inventory) =>
            this.inventory = inventory;

        public LinearInventory inventory;
    }

    public class LinearInventory
    {
        private object?[] inventory;
        private bool[] slotLockStates;
        private readonly List<InventoryReceiver> receivers =
            new List<InventoryReceiver>();

        public LinearInventory(int capacity)
        {
            this.capacity = capacity;
            inventory = new object?[capacity];
            slotLockStates = new bool[capacity];
        }

        public int capacity;

        public bool ThrowOnTakeAfterMutation { get; set; }

        public bool ThrowOnPlaceAfterMutation { get; set; }

        public bool ThrowOnCostAfterMutation { get; set; }

        public bool LastCostExactItem { get; private set; }

        public int ThrowOnReadIndex { get; set; } = -1;

        public bool isEmpty
        {
            get
            {
                for (int index = 0;
                     index < inventory.Length;
                     index++)
                {
                    if (inventory[index] != null)
                        return false;
                }
                return true;
            }
        }

        public void ValidateCapacity(
            int value,
            bool preserveOverflow)
        {
            if (value <= capacity)
                return;
            Array.Resize(ref inventory, value);
            Array.Resize(ref slotLockStates, value);
            capacity = value;
        }

        public object? Read(int index)
        {
            if (index == ThrowOnReadIndex)
            {
                throw new InvalidOperationException(
                    "fixture Read failure");
            }
            return index >= 0 && index < inventory.Length
                ? inventory[index]
                : null;
        }

        public object? Take(int index)
        {
            object? item = Read(index);
            if (index >= 0 && index < inventory.Length)
            {
                inventory[index] = null;
                slotLockStates[index] = false;
                Emit(index);
            }
            if (ThrowOnTakeAfterMutation)
            {
                throw new InvalidOperationException(
                    "fixture Take failure after mutation");
            }
            return item;
        }

        public bool TryCostAtIndex(
            int index,
            int count,
            bool exactItem)
        {
            LastCostExactItem = exactItem;
            if (count != 1 ||
                !(Read(index) is Item item) ||
                item.count < count)
            {
                return false;
            }
            item.count -= count;
            if (item.count == 0)
            {
                inventory[index] = null;
                slotLockStates[index] = false;
            }
            Emit(index);
            if (ThrowOnCostAfterMutation)
            {
                throw new InvalidOperationException(
                    "fixture TryCostAtIndex failure after mutation");
            }
            return true;
        }

        public bool CanPlaceIn(object item) =>
            item != null &&
            Array.IndexOf(inventory, null) >= 0;

        public object? PlaceItem(object item)
        {
            int index = Array.IndexOf(inventory, null);
            if (index < 0)
                return item;
            inventory[index] = item;
            slotLockStates[index] = false;
            Emit(index);
            if (ThrowOnPlaceAfterMutation)
            {
                throw new InvalidOperationException(
                    "fixture PlaceItem failure after mutation");
            }
            return null;
        }

        public object? PlaceItemAt(int index, object item)
        {
            if (index < 0 ||
                index >= inventory.Length ||
                inventory[index] != null)
            {
                return item;
            }
            inventory[index] = item;
            slotLockStates[index] = false;
            Emit(index);
            return null;
        }

        public void SetItem(int index, Item? item)
        {
            if (index < 0 || index >= inventory.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            inventory[index] = item;
            if (item == null)
                slotLockStates[index] = false;
            Emit(index);
        }

        public void SetSlotLocked(
            int index,
            bool value)
        {
            if (index < 0 || index >= slotLockStates.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            slotLockStates[index] =
                inventory[index] != null && value;
            Emit(index);
        }

        public bool GetSlotLocked(int index)
        {
            if (index < 0 || index >= slotLockStates.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return slotLockStates[index];
        }

        public void AddReceiver(InventoryReceiver receiver)
        {
            if (receiver == null || receivers.Contains(receiver))
                return;
            receivers.Add(receiver);
            for (int index = 0;
                 index < inventory.Length;
                 index++)
            {
                receiver(
                    index,
                    inventory[index] as Item,
                    slotLockStates[index],
                    null);
            }
        }

        public void RemoveReceiver(InventoryReceiver receiver)
        {
            if (receiver != null)
                receivers.Remove(receiver);
        }

        public void Invoke(int index) =>
            Emit(index);

        private void Emit(int index)
        {
            foreach (InventoryReceiver receiver in receivers)
            {
                receiver(
                    index,
                    inventory[index] as Item,
                    slotLockStates[index],
                    null);
            }
        }

        public int TotalCount()
        {
            int total = 0;
            for (int index = 0;
                 index < inventory.Length;
                 index++)
            {
                if (inventory[index] is Item item)
                    total += item.count;
            }
            return total;
        }
    }

    public class Item
    {
        public string name = "fixture-item";

        public int count = 1;

        public object Clone(int requestedCount)
        {
            var clone =
                (Item)(Activator.CreateInstance(GetType())
                    ?? throw new InvalidOperationException(
                        "Could not clone fixture item."));
            clone.name = name;
            clone.count = requestedCount;
            return clone;
        }
    }

    public class ItemFarmingGun : Item
    {
        private static readonly Config.Item.ItemFunctionFarmingGun
            SharedProtoFunction =
                new Config.Item.ItemFunctionFarmingGun(1);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ItemFarmingGun(
            Config.Item.ItemInfo info,
            int count)
        {
            inventory = new LinearInventory(1);
            func = SharedProtoFunction;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ItemFarmingGun(
            string itemId,
            int count,
            LinearInventory inventory)
        {
            this.inventory = inventory;
            func = SharedProtoFunction;
        }

        public LinearInventory inventory;

        public Config.Item.ItemFunctionFarmingGun func;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OpenContainerUI() { }

        public CellTip cellTip = new CellTip();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RefreshCellTip() { cellTip.CellTipValid = false; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InitCellTip() { cellTip.CellSize = func.Area; }

        public bool CheckCanInteractResult { get; set; } = true;

        public bool DoInteractResult { get; set; } = true;

        public bool ThrowOnDoInteract { get; set; }

        public int OriginalToolUseCount { get; private set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OnUseAsTool()
        {
            OriginalToolUseCount++;
        }

        public int EquipmentQueryCount { get; private set; }
        public object[] GetEquipmentsFromArea()
        {
            EquipmentQueryCount++;
            return new object[] { this };
        }

        public bool CheckCanInteract(
            object equipment,
            object item) =>
            CheckCanInteractResult && item is ItemFilm;

        public bool DoInteract(
            object equipment,
            object item)
        {
            if (ThrowOnDoInteract)
            {
                throw new InvalidOperationException(
                    "fixture DoInteract failure after cost");
            }
            return DoInteractResult;
        }

        public void HideCellTip()
        {
        }

        public void ShineArea()
        {
        }

    }

    public class ContainerBaseUiState
    {
        public string LastNativeOperation = "";
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void HandlePlaceToOtherSide(int index) { LastNativeOperation = "HandlePlaceToOtherSide"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void HandleSwapOneItem(int index) { LastNativeOperation = "HandleSwapOneItem"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void OnBackpackItemClick(int index) { LastNativeOperation = "OnBackpackItemClick"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void OnContainerItemClick(int index) { LastNativeOperation = "OnContainerItemClick"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void HandleSwapHalfItems(int index) { LastNativeOperation = "HandleSwapHalfItems"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void PutAll() { LastNativeOperation = "PutAll"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void PutMax() { LastNativeOperation = "PutMax"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual string[] GetTipInBackpack() { LastNativeOperation = "GetTipInBackpack"; return new[] { "native-tip" }; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual string[] GetTipInContainer() { LastNativeOperation = "GetTipInContainer"; return new[] { "native-tip" }; }
    }
    public sealed class FarmingGunUiState : ContainerBaseUiState
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void HandlePlaceToOtherSide(int index) { LastNativeOperation = "legacy-HandlePlaceToOtherSide"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void HandleSwapOneItem(int index) { LastNativeOperation = "legacy-HandleSwapOneItem"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void OnBackpackItemClick(int index) { LastNativeOperation = "legacy-OnBackpackItemClick"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void OnContainerItemClick(int index) { LastNativeOperation = "legacy-OnContainerItemClick"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void HandleSwapHalfItems(int index) { LastNativeOperation = "legacy-HandleSwapHalfItems"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void PutAll() { LastNativeOperation = "legacy-PutAll"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void PutMax() { LastNativeOperation = "legacy-PutMax"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string[] GetTipInBackpack() { LastNativeOperation = "legacy-GetTipInBackpack"; return new[] { "legacy-tip" }; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string[] GetTipInContainer() { LastNativeOperation = "legacy-GetTipInContainer"; return new[] { "legacy-tip" }; }
    }

    public class CellTip { public bool CellTipValid { get; set; } public Config.Item.NativeArea CellSize; }

    public sealed class EmptyBuffer
    {
        public bool IsEmpty => true;
    }

    public class ItemSeed : Item
    {
    }

    public class ItemFilm : Item
    {
    }

    public class ItemFertilizer : Item
    {
    }
}
