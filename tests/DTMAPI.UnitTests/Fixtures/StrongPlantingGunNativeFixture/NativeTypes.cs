using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class DolocAPI
{
    public static object? archiveHandle;
}

namespace DolocTown.Config.Item
{
    public sealed class ItemInfo
    {
    }
}

namespace DolocTown
{
    public delegate void InventoryReceiver(
        int index,
        Item? item,
        bool isSlotLocked);

    public sealed class ItemFunctionFarmingGun
    {
        public ItemFunctionFarmingGun(int capacity) =>
            Capacity = capacity;

        public int Capacity { get; set; }
    }

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
            bool allowPartial)
        {
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
                    slotLockStates[index]);
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
                    slotLockStates[index]);
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
        private static readonly ItemFunctionFarmingGun
            SharedProtoFunction =
                new ItemFunctionFarmingGun(1);

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

        public ItemFunctionFarmingGun func;

        public bool CheckCanInteractResult { get; set; } = true;

        public bool DoInteractResult { get; set; } = true;

        public bool ThrowOnDoInteract { get; set; }

        public int OriginalToolUseCount { get; private set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OnUseAsTool()
        {
            OriginalToolUseCount++;
        }

        public object[] GetEquipmentsFromArea() =>
            new[]
            {
                (object)this
            };

        public bool CheckCanInteract(
            object equipment,
            object item) =>
            CheckCanInteractResult;

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

        public void InitCellTip()
        {
        }
    }

    public sealed class FarmingGunUiState
    {
        public FarmingGunUiState(
            ItemFarmingGun container,
            LinearInventory backpackInventory,
            Item selectedItem,
            int currentIndex)
        {
            this.container = container;
            containerInventory = container.inventory;
            this.backpackInventory = backpackInventory;
            this.selectedItem = selectedItem;
            this.currentIndex = currentIndex;
        }

        public bool inBackpack = true;

        public EmptyBuffer buffer { get; } =
            new EmptyBuffer();

        public ItemFarmingGun container;

        public LinearInventory containerInventory;

        public LinearInventory backpackInventory;

        public Item selectedItem;

        public int currentIndex;

        public int OriginalPlaceCount { get; private set; }

        public int OriginalSwapCount { get; private set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void HandlePlaceToOtherSide(int index)
        {
            OriginalPlaceCount++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void HandleSwapOneItem(int index)
        {
            OriginalSwapCount++;
        }
    }

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
