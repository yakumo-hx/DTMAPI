using System;
using DolocTown.UI;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class InventorySystem
{
	public SingleInventory buffer { get; private set; }

	[JsonProperty("backpack")]
	public LinearInventory inventory { get; private set; }

	public Item this[int value] => inventory.Read(value);

	public int totalCapacity { get; private set; }

	[JsonProperty("backpackColum")]
	public int lineCapacity { get; private set; }

	public int rowCount { get; private set; }

	public LinearInventory currentCase { get; private set; }

	public InventorySystem(int totalCapacity, int lineCapacity)
	{
		this.totalCapacity = totalCapacity;
		this.lineCapacity = lineCapacity;
		rowCount = Mathf.CeilToInt((float)totalCapacity / (float)lineCapacity);
		inventory = new LinearInventory(this.totalCapacity);
		SetBackpackCapacity(totalCapacity, lineCapacity);
		buffer = new SingleInventory();
	}

	[JsonConstructor]
	public InventorySystem(LinearInventory backpack, int backpackColum)
	{
		inventory = backpack;
		totalCapacity = backpack.capacity;
		lineCapacity = backpackColum;
		rowCount = Mathf.CeilToInt((float)totalCapacity / (float)lineCapacity);
		SetBackpackCapacity(totalCapacity, lineCapacity);
		buffer = new SingleInventory();
	}

	public Item TakeItemInBuffer()
	{
		return buffer.Take();
	}

	private void SetBackpackCapacity(int totalCapacity, int lineCapacity)
	{
		QuickInventoryPanel inventoryQuick = DolocAPI.uiSystem.inventoryQuick;
		if (this.totalCapacity == totalCapacity)
		{
			this.lineCapacity = lineCapacity;
		}
		else
		{
			LinearInventory linearInventory = inventory;
			inventoryQuick.UnBindInventory();
			inventory = new LinearInventory(totalCapacity);
			int num = Mathf.Min(inventory.capacity, linearInventory.capacity);
			for (int i = 0; i < num; i++)
			{
				inventory.SwapItem(i, linearInventory.Take(i));
			}
			inventoryQuick.BindInventory(inventory);
			this.totalCapacity = totalCapacity;
		}
		inventoryQuick.SetCapacity(totalCapacity, lineCapacity);
	}

	public void SetBackpackCapacity(int totalCapacity)
	{
		int num = totalCapacity / lineCapacity * lineCapacity;
		SetBackpackCapacity(num, lineCapacity);
	}

	public int GetCount(string name, bool checkBox)
	{
		if (checkBox)
		{
			return DolocAPI.GetBackpackWithInsideBoxes().CountItem(name);
		}
		return inventory.Count(name);
	}

	public int GetCount(Item item, bool checkBox, bool shouldEqualAsItem)
	{
		if (checkBox)
		{
			return DolocAPI.GetBackpackWithInsideBoxes().CountItem(item, shouldEqualAsItem);
		}
		return inventory.Count(item, shouldEqualAsItem);
	}

	public Item ReleaseBufferBack()
	{
		return PlaceItem(buffer.Take());
	}

	public Item PlaceItem(Item item, bool checkBox = false, bool shouldEqualAsItem = false, bool useFade = false)
	{
		if (item == null)
		{
			return null;
		}
		if (checkBox)
		{
			Item[] array = inventory.ReadAll();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] is ItemBox itemBox && itemBox.ContentFilter(item) && itemBox.inventory.Count(item, shouldEqualAsItem) > 0 && itemBox.inventory.CanPlaceIn(item))
				{
					if (useFade)
					{
						int index = inventory.IndexOf(itemBox);
						DolocAPI.uiSystem.inventoryQuick.RaiseSpriteFadeDown(index, item.uiSprite);
					}
					item = itemBox.inventory.PlaceItem(item);
					if (item == null || item.count == 0)
					{
						return null;
					}
				}
			}
		}
		return inventory.PlaceItem(item);
	}

	public void Deposit(int index)
	{
		buffer.CurrentItem = inventory.SwapItem(index, buffer.CurrentItem);
	}

	public bool CanPlaceItem(Item item)
	{
		return inventory.CanPlaceIn(item);
	}

	public int MaxItemPlaceCount(string itemName)
	{
		return inventory.MaxItemPlaceCount(itemName);
	}

	public bool CanPlaceItem(Item item, out int firstAvailableIndex)
	{
		return inventory.CanPlaceIn(item, out firstAvailableIndex);
	}

	public void DepositFromOutside(LinearInventory outside, int index)
	{
		if (outside != null)
		{
			buffer.CurrentItem = outside.SwapItem(index, buffer.CurrentItem);
		}
	}

	public bool Cost(string name, int count, bool useBox)
	{
		if (!useBox)
		{
			return inventory.TryCost(name, count);
		}
		return DolocAPI.GetBackpackWithInsideBoxes().TryCostItem(name, count);
	}

	public bool CostItem(Item item, int count, bool useBox, bool shouldEqualAsItem)
	{
		if (!useBox)
		{
			if (inventory.Count(item, shouldEqualAsItem) >= count)
			{
				inventory.MaxCost(item, count, shouldEqualAsItem);
				return true;
			}
			return false;
		}
		LinearInventory[] backpackWithInsideBoxes = DolocAPI.GetBackpackWithInsideBoxes();
		if (backpackWithInsideBoxes.CountItem(item, shouldEqualAsItem) >= count)
		{
			backpackWithInsideBoxes.MaxCostItem(item, count, shouldEqualAsItem);
			return true;
		}
		return false;
	}

	public bool CostAt(int index, int count, bool useBox = false, bool shouldEqualAsItem = false)
	{
		if (!useBox)
		{
			return inventory.TryCostAtIndex(index, count, shouldEqualAsItem);
		}
		Item item = inventory.Read(index);
		LinearInventory[] backpackWithInsideBoxes = DolocAPI.GetBackpackWithInsideBoxes();
		if (backpackWithInsideBoxes.CountItem(item, shouldEqualAsItem) < count)
		{
			return false;
		}
		int num = inventory.Count(item, shouldEqualAsItem);
		inventory.TryCostAtIndex(index, num, shouldEqualAsItem);
		backpackWithInsideBoxes.MaxCostItem(item, count - num, shouldEqualAsItem);
		return true;
	}

	public Item Take(int index)
	{
		return inventory.Take(index);
	}

	public void SetCurrentCase(LinearInventory _inventory)
	{
		currentCase = _inventory;
	}

	public void ForEach(Action<Item> handle)
	{
		inventory.ForEach(handle);
	}

	public void ForEach(Action<Item, int> handle)
	{
		inventory.ForEach(handle);
	}
}
