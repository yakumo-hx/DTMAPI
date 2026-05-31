using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class LinearInventory
{
	[JsonProperty("items")]
	private Item[] inventory;

	[JsonProperty]
	private bool[] slotLockStates;

	private List<InventoryReceiver> receivers;

	public int capacity => inventory.Length;

	private int[] filledList => (from i in Enumerable.Range(0, capacity)
		where inventory[i] != null
		select i).ToArray();

	private int[] emptyList => (from i in Enumerable.Range(0, capacity)
		where inventory[i] == null
		select i).ToArray();

	public int filledCount => inventory.Count((Item x) => x != null);

	public int emptyCount => inventory.Count((Item x) => x == null);

	public int realItemCount => inventory.Where((Item x) => x != null).Sum((Item x) => x.count);

	public bool isEmpty => filledCount == 0;

	public bool isFull => emptyCount == 0;

	public bool isRealFull
	{
		get
		{
			if (filledCount == capacity)
			{
				return inventory.All((Item x) => x.count >= x.proto.Overlay);
			}
			return false;
		}
	}

	public int FirstEmptyIndex
	{
		get
		{
			for (int i = 0; i < capacity; i++)
			{
				if (inventory[i] == null)
				{
					return i;
				}
			}
			return -1;
		}
	}

	public Item FirstItem => inventory.Where((Item item) => item != null).FirstOrDefault();

	public string FirstItemName => (from item in inventory
		where item != null
		select item.name).FirstOrDefault();

	public LinearInventory(int size)
	{
		inventory = new Item[size];
		receivers = new List<InventoryReceiver>();
		slotLockStates = new bool[size];
	}

	public LinearInventory(Item[] items)
	{
		inventory = items;
		slotLockStates = new bool[items.Length];
		receivers = new List<InventoryReceiver>();
	}

	[JsonConstructor]
	protected LinearInventory(Item[] items, bool[] slotLockStates)
	{
		inventory = items;
		this.slotLockStates = slotLockStates ?? new bool[capacity];
		receivers = new List<InventoryReceiver>();
		Validate();
	}

	private void __fill(int index, Item item)
	{
		if (item != null)
		{
			inventory[index] = item;
			emit(index, item);
		}
	}

	private Item __take(int index)
	{
		if (index < 0 || index >= inventory.Length)
		{
			return null;
		}
		Item item = inventory[index];
		if (item == null)
		{
			return null;
		}
		inventory[index] = null;
		slotLockStates[index] = false;
		emit(index, null);
		return item;
	}

	private void emit(int index, Item item)
	{
		if (receivers.Count <= 0)
		{
			return;
		}
		foreach (InventoryReceiver receiver in receivers)
		{
			receiver(index, item, slotLockStates[index]);
		}
	}

	public void InvokeAll()
	{
		for (int i = 0; i < capacity; i++)
		{
			emit(i, inventory[i]);
		}
	}

	public void InvokeAll(InventoryReceiver receiver)
	{
		for (int i = 0; i < inventory.Length; i++)
		{
			receiver(i, inventory[i], slotLockStates[i]);
		}
	}

	public void Invoke(int index)
	{
		emit(index, inventory[index]);
	}

	public void Invoke(Item item)
	{
		int num = IndexOf(item);
		if (num >= 0)
		{
			Invoke(num);
		}
	}

	public void Overwrite(Item[] items, bool shouldEmit)
	{
		for (int i = 0; i < capacity; i++)
		{
			if (i < items.Length)
			{
				inventory[i] = items[i];
			}
			else
			{
				inventory[i] = null;
			}
		}
		if (shouldEmit)
		{
			ReEmit();
		}
	}

	public void Overwrite(LinearInventory other, bool shouldEmit)
	{
		inventory = other.inventory.ToArray();
		slotLockStates = other.slotLockStates.ToArray();
		if (shouldEmit)
		{
			ReEmit();
		}
	}

	private bool EqualByName(object obj, Item item)
	{
		if (obj is string text)
		{
			return text == item?.name;
		}
		if (obj is Item item2)
		{
			return item2.name == item?.name;
		}
		return obj == item;
	}

	private bool EqualByItem(object obj, Item itemB)
	{
		if (!(obj is Item item))
		{
			return false;
		}
		if (item == itemB)
		{
			return true;
		}
		if (itemB != null)
		{
			return item.IsSame(itemB);
		}
		return false;
	}

	public int Count(string name, HashSet<int> itemIndexes = null)
	{
		return CountInternal(name, EqualByName, itemIndexes);
	}

	public int Count(Item item, bool shouldEqualAsItem, HashSet<int> itemIndexes = null)
	{
		if (shouldEqualAsItem)
		{
			return CountInternal(item, EqualByItem, itemIndexes);
		}
		return CountInternal(item?.name, EqualByName, itemIndexes);
	}

	private int CountInternal(object obj, Func<object, Item, bool> equal, HashSet<int> itemIndexes)
	{
		if (obj == null)
		{
			return 0;
		}
		int num = 0;
		int[] array = filledList;
		foreach (int num2 in array)
		{
			Item item = inventory[num2];
			if (equal(obj, item))
			{
				num += item.count;
				itemIndexes?.Add(num2);
			}
		}
		return num;
	}

	public void AddReceiver(InventoryReceiver receiver)
	{
		if (receiver != null && !receivers.Contains(receiver))
		{
			receivers.Add(receiver);
			for (int i = 0; i < inventory.Length; i++)
			{
				receiver(i, inventory[i], slotLockStates[i]);
			}
		}
	}

	public void RemoveAllReceivers()
	{
		receivers.Clear();
	}

	public void RemoveReceiver(InventoryReceiver receiver)
	{
		if (receivers.Contains(receiver))
		{
			receivers.Remove(receiver);
		}
	}

	private int ForceCostAtIndexInternal(int index, object obj, int costCount, Func<object, Item, bool> equal)
	{
		if (costCount == 0)
		{
			return 0;
		}
		if (index >= inventory.Length)
		{
			return costCount;
		}
		Item item = inventory[index];
		if (item == null || !equal(obj, item))
		{
			return costCount;
		}
		if (costCount >= item.count)
		{
			__take(index);
			return costCount - item.count;
		}
		item.count -= costCount;
		emit(index, item);
		return 0;
	}

	private int ForceCostInternal(int startIndex, object obj, int costCount, Func<object, Item, bool> equal)
	{
		int costCount2 = costCount;
		costCount2 = ForceCostAtIndexInternal(startIndex, obj, costCount2, equal);
		int[] array = filledList.ToArray();
		foreach (int num in array)
		{
			if (num != startIndex)
			{
				costCount2 = ForceCostAtIndexInternal(num, obj, costCount2, equal);
				if (costCount == 0)
				{
					return 0;
				}
			}
		}
		return costCount2;
	}

	public bool TryCost(string name, int costCount, int startIndex = 0)
	{
		int leftover;
		return TryCost(name, costCount, out leftover, startIndex);
	}

	public bool TryCost(string name, int costCount, out int leftover, int startIndex = 0)
	{
		int num = Count(name);
		leftover = Mathf.Max(0, costCount - num);
		if (num < costCount)
		{
			return false;
		}
		ForceCostInternal(startIndex, name, costCount, EqualByName);
		return true;
	}

	public bool TryCostAtIndex(int index, int costCount, bool shouldEqualAsItem = false)
	{
		int leftover;
		return TryCostAtIndex(index, costCount, shouldEqualAsItem, null, out leftover);
	}

	public bool TryCostAtIndex(int index, int costCount, bool shouldEqualAsItem, HashSet<int> itemIndexes, out int leftover)
	{
		Item item = inventory[index];
		object obj = (shouldEqualAsItem ? ((object)item) : ((object)item.name));
		Func<object, Item, bool> equal = (shouldEqualAsItem ? new Func<object, Item, bool>(EqualByItem) : new Func<object, Item, bool>(EqualByName));
		int num = CountInternal(obj, equal, itemIndexes);
		leftover = Mathf.Max(0, costCount - num);
		if (num < costCount)
		{
			return false;
		}
		ForceCostInternal(index, obj, costCount, equal);
		return true;
	}

	public int MaxCost(string name, int maxCostCount, int startIndex = 0)
	{
		return ForceCostInternal(startIndex, name, maxCostCount, EqualByName);
	}

	public int MaxCost(Item item, int count, bool shouldEqualAsItem, int startIndex = 0)
	{
		return ForceCostInternal(startIndex, item, count, shouldEqualAsItem ? new Func<object, Item, bool>(EqualByItem) : new Func<object, Item, bool>(EqualByName));
	}

	public bool MaxCost(CountItem[] countItems, int startIndex = 0)
	{
		if (countItems == null)
		{
			return true;
		}
		bool flag = true;
		for (int i = 0; i < countItems.Length; i++)
		{
			CountItem countItem = countItems[i];
			flag &= ForceCostInternal(startIndex, countItem.itemName, countItem.itemCount, EqualByName) == 0;
		}
		return flag;
	}

	public Item Take(int index)
	{
		return __take(index);
	}

	public bool Take(Item item)
	{
		if (item == null)
		{
			return false;
		}
		int num = IndexOf(item);
		if (num < 0)
		{
			return false;
		}
		__take(num);
		return true;
	}

	public Item PlaceItem(Item item)
	{
		if (item == null)
		{
			return null;
		}
		int[] array = filledList;
		foreach (int num in array)
		{
			if (inventory[num].IsSame(item))
			{
				item.count = inventory[num].TryCombine(item.count);
				emit(num, inventory[num]);
				if (item.count == 0)
				{
					return null;
				}
			}
		}
		if (!isFull)
		{
			__fill(emptyList[0], item);
			return null;
		}
		return item;
	}

	public Item PlaceItemAt(int index, Item item)
	{
		if (item == null || item.count == 0)
		{
			return null;
		}
		if (inventory[index] != null && !inventory[index].IsSame(item))
		{
			return item;
		}
		if (inventory[index] == null)
		{
			int count = item.count;
			inventory[index] = item.Clone(count);
			item.count -= count;
		}
		else
		{
			item.count = inventory[index].TryCombine(item.count);
		}
		emit(index, inventory[index]);
		if (item.count != 0)
		{
			return item;
		}
		return null;
	}

	public bool CanPlaceIn(Item target)
	{
		if (target == null || target.count == 0)
		{
			return true;
		}
		if (emptyList.Length != 0)
		{
			return true;
		}
		int num = target.count;
		int[] array = filledList;
		foreach (int num2 in array)
		{
			Item item = inventory[num2];
			if (item.IsSame(target))
			{
				num = item.TestCombine(num);
				if (num == 0)
				{
					return true;
				}
			}
		}
		return num == 0;
	}

	public int MaxItemPlaceCount(string itemName)
	{
		DolocAPI.QueryItemProto(itemName, out var proto);
		int num = emptyList.Length * proto.Overlay;
		int[] array = filledList;
		foreach (int num2 in array)
		{
			Item item = inventory[num2];
			if (!(item.name != itemName))
			{
				num += proto.Overlay - item.count;
			}
		}
		return num;
	}

	public bool CanPlaceIn(Item target, out int firstAvailableIndex)
	{
		firstAvailableIndex = -1;
		if (target == null || target.count == 0)
		{
			return true;
		}
		int num = target.count;
		int[] array = filledList;
		foreach (int num2 in array)
		{
			Item item = inventory[num2];
			if (item.IsSame(target))
			{
				if (firstAvailableIndex < 0)
				{
					firstAvailableIndex = num2;
				}
				num = item.TestCombine(num);
				if (num == 0)
				{
					return true;
				}
			}
		}
		if (num != 0)
		{
			if (emptyList.Length != 0)
			{
				firstAvailableIndex = emptyList[0];
			}
			else
			{
				firstAvailableIndex = -1;
			}
			return emptyList.Length != 0;
		}
		return true;
	}

	public Item SwapItem(int index, Item item)
	{
		if (item == null)
		{
			return __take(index);
		}
		if (inventory[index] == null)
		{
			__fill(index, item);
			return null;
		}
		if (inventory[index].IsSame(item))
		{
			item.count = inventory[index].TryCombine(item.count);
			if (item.count > 0)
			{
				Item item2 = PlaceItem(item);
				if (item2 != null)
				{
					item.count = item2.count;
					return item;
				}
			}
			emit(index, inventory[index]);
			return null;
		}
		Item result = inventory[index];
		inventory[index] = item;
		slotLockStates[index] = false;
		emit(index, item);
		return result;
	}

	public Item Read(int index)
	{
		if (index < 0 || index >= inventory.Length)
		{
			return null;
		}
		return inventory[index];
	}

	public Item[] ReadAll()
	{
		return inventory.Where((Item x) => x != null).ToArray();
	}

	public IEnumerable<T> ReadAll<T>() where T : Item
	{
		Item[] array = inventory;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is T val)
			{
				yield return val;
			}
		}
	}

	public Item[] ReadAllWithNull()
	{
		return inventory.ToArray();
	}

	public void ForEach(Action<Item> handle)
	{
		Item[] array = inventory;
		foreach (Item obj in array)
		{
			handle(obj);
		}
	}

	public void ForEach(Action<Item, int> handle)
	{
		for (int i = 0; i < inventory.Length; i++)
		{
			handle(inventory[i], i);
		}
	}

	public void SwitchSlotLockStatus(int index)
	{
		if (index >= 0 && index < slotLockStates.Length)
		{
			if (inventory[index] == null)
			{
				slotLockStates[index] = false;
				return;
			}
			slotLockStates[index] = !slotLockStates[index];
			emit(index, inventory[index]);
		}
	}

	public void ClearSlotLockStatus(int index)
	{
		slotLockStates[index] = false;
	}

	public void Sort()
	{
		List<(int, Item)> list = new List<(int, Item)>();
		List<Item> list2 = new List<Item>();
		for (int i = 0; i < slotLockStates.Length; i++)
		{
			if (slotLockStates[i])
			{
				list.Add((i, inventory[i]));
				continue;
			}
			list2.Add(inventory[i]);
			inventory[i] = null;
		}
		list2 = ((CheckSortStatus(list2) <= 0) ? list2.OrderBy((Item x) => x?.sortingOrder ?? 2.1474836E+09f).ToList() : list2.OrderByDescending((Item x) => x?.sortingOrder ?? (-1f)).ToList());
		Clear();
		foreach (var (num, item) in list)
		{
			inventory[num] = item;
		}
		foreach (Item item2 in list2)
		{
			PlaceItem(item2);
		}
		ReEmit();
	}

	public int IsSorted()
	{
		if (capacity <= 1)
		{
			return 1;
		}
		if (inventory[0] == null)
		{
			if (filledCount != 0)
			{
				return 0;
			}
			return 1;
		}
		if (inventory[1] == null)
		{
			if (filledCount != 1)
			{
				return 0;
			}
			return 1;
		}
		bool flag = inventory[0].sortingOrder > inventory[1].sortingOrder;
		for (int i = 0; i < inventory.Length - 1; i++)
		{
			ref Item reference = ref inventory[i];
			ref Item reference2 = ref inventory[i + 1];
			if (reference2 != null && (reference == null || (!flag && reference.sortingOrder > reference2.sortingOrder) || (flag && reference.sortingOrder < reference2.sortingOrder)))
			{
				return 0;
			}
		}
		if (!flag)
		{
			return 1;
		}
		return -1;
	}

	public int CheckSortStatus(List<Item> items)
	{
		if (items.Count <= 1)
		{
			return 1;
		}
		bool flag = true;
		for (int i = 0; i < items.Count - 1; i++)
		{
			if (items[i + 1] != null && (items[i] == null || items[i].sortingOrder > items[i + 1].sortingOrder))
			{
				flag = false;
				break;
			}
		}
		bool flag2 = true;
		for (int j = 0; j < items.Count - 1; j++)
		{
			if (items[j + 1] != null && (items[j] == null || items[j].sortingOrder < items[j + 1].sortingOrder))
			{
				flag2 = false;
				break;
			}
		}
		if (!flag && !flag2)
		{
			return 0;
		}
		if (!flag)
		{
			return -1;
		}
		return 1;
	}

	public int IndexOf(Item item)
	{
		if (item == null)
		{
			return -1;
		}
		int[] array = filledList;
		foreach (int num in array)
		{
			if (inventory[num] == item)
			{
				return num;
			}
		}
		return -1;
	}

	public int[] IndexOf(string itemName)
	{
		if (itemName.IsNullOrEmpty())
		{
			return Array.Empty<int>();
		}
		List<int> list = new List<int>();
		int[] array = filledList;
		foreach (int num in array)
		{
			if (inventory[num]?.name == itemName)
			{
				list.Add(num);
			}
		}
		return list.ToArray();
	}

	public void RefreshItemSlot(int index, Item item)
	{
		emit(index, item);
	}

	public void ReEmit()
	{
		for (int i = 0; i < inventory.Length; i++)
		{
			emit(i, inventory[i]);
		}
	}

	public void ReEmit(int index)
	{
		if (index >= 0 && index < inventory.Length)
		{
			emit(index, inventory[index]);
		}
	}

	private void Validate()
	{
		Queue<int> queue = new Queue<int>();
		int[] array = filledList;
		foreach (int num in array)
		{
			Item item = inventory[num];
			inventory[num] = item?.CheckValid();
			if (inventory[num] == null || inventory[num].invalid)
			{
				queue.Enqueue(num);
			}
		}
		while (queue.Count > 0)
		{
			int index = queue.Dequeue();
			__take(index);
		}
	}

	public void ValidateCapacity(int capacity, bool allowDispose = false)
	{
		if (this.capacity == capacity)
		{
			return;
		}
		Item[] array = ReadAll();
		if (array.Length <= capacity || allowDispose)
		{
			inventory = new Item[capacity];
			Item[] array2 = array;
			foreach (Item item in array2)
			{
				PlaceItem(item);
			}
			slotLockStates = new bool[capacity];
		}
	}

	public LinearInventory Copy()
	{
		return new LinearInventory(ReadAllWithNull(), slotLockStates.ToArray());
	}

	public void Clear()
	{
		for (int i = 0; i < capacity; i++)
		{
			inventory[i] = null;
		}
		ReEmit();
	}

	public void Shuffle()
	{
		inventory.Shuffle();
	}
}
