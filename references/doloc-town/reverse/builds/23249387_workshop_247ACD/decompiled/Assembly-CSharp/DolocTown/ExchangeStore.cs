using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Store;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class ExchangeStore : IStore, IRecipeGroup
{
	[JsonProperty]
	private readonly HashSet<string> currentItems;

	[JsonProperty]
	private readonly Dictionary<string, int> soldItems;

	[JsonProperty]
	private readonly HashSet<string> forceUnlockedItems;

	public readonly ExchangeStoreInfo proto;

	[JsonProperty]
	private string id => proto.Id;

	public HashSet<string> CurrentItems => currentItems;

	public Dictionary<string, int> SoldItems => soldItems;

	public bool IsValid => proto != null;

	public string title => proto.Title;

	public int ItemCategory => currentItems.Count;

	public bool IsFixedDuration => false;

	public string GroupId => id;

	public List<string> RecipeIds => GetRecipeIdByOrder();

	public string Title => title;

	public float BaseTimeRatio => 0f;

	public float TimeAddition { get; set; }

	public int MaxCraftCount => 0;

	public bool isValid => proto != null;

	public string SwitchMainInfo => "";

	public string SwitchSubInfo => "";

	public ExchangeStore(ExchangeStoreInfo proto)
	{
		this.proto = proto;
		currentItems = new HashSet<string>();
		soldItems = new Dictionary<string, int>();
		forceUnlockedItems = new HashSet<string>();
		InitItems();
	}

	[JsonConstructor]
	private ExchangeStore(string id, HashSet<string> currentItems, Dictionary<string, int> soldItems, HashSet<string> forceUnlockedItems)
	{
		proto = DolocConfig.Tables.TbExchangeStore.GetOrDefault(id);
		if (proto != null)
		{
			this.currentItems = currentItems ?? new HashSet<string>();
			this.soldItems = soldItems ?? new Dictionary<string, int>();
			this.forceUnlockedItems = forceUnlockedItems ?? new HashSet<string>();
			ValidateFixedItems();
		}
	}

	private void InitItems()
	{
		foreach (ExchangeStoreItemData item in proto.ItemList)
		{
			if (item.UnlockOnInit)
			{
				currentItems.Add(item.ItemId);
			}
		}
	}

	private void ValidateFixedItems()
	{
		foreach (ExchangeStoreItemData item in proto.ItemList)
		{
			if (CheckItemUnlock(item))
			{
				currentItems.Add(item.ItemId);
			}
		}
	}

	public void Refresh()
	{
		currentItems.Clear();
		foreach (ExchangeStoreItemData item in proto.ItemList)
		{
			if (CheckItemUnlock(item))
			{
				currentItems.Add(item.ItemId);
			}
		}
	}

	public bool UnlockStoreItem(string itemName)
	{
		bool result = forceUnlockedItems.Add(itemName) && proto.StoreItemMap.ContainsKey(itemName);
		if (proto.RefreshImmediatly)
		{
			Refresh();
		}
		return result;
	}

	public void RevertUnlockStoreItem(string itemName)
	{
		forceUnlockedItems.Remove(itemName);
	}

	private bool CheckItemUnlock(ExchangeStoreItemData storeItem)
	{
		bool flag;
		if (!storeItem.HasPreCondition)
		{
			flag = true;
		}
		else
		{
			soldItems.TryGetValue(storeItem.PreConditionItemName ?? "", out var value);
			flag = value >= storeItem.PreConditionItemCount;
		}
		if (flag)
		{
			if (!storeItem.DefaultUnlock)
			{
				return forceUnlockedItems.Contains(storeItem.ItemId);
			}
			return true;
		}
		return false;
	}

	public int GetItemCurrentStorage(string itemName)
	{
		ExchangeStoreItemData storeItem = GetStoreItem(itemName);
		if (storeItem == null)
		{
			return 0;
		}
		if (storeItem.Unlimited)
		{
			return int.MaxValue;
		}
		soldItems.TryGetValue(storeItem.ItemId, out var value);
		return storeItem.Storage - value;
	}

	public int GetItemPrice(string itemName)
	{
		if (!proto.StoreItemMap.TryGetValue(itemName, out var value))
		{
			return 0;
		}
		return value.GoldCost;
	}

	public CountItem[] GetItemCost(string itemName)
	{
		if (!proto.StoreItemMap.TryGetValue(itemName, out var value))
		{
			return Array.Empty<CountItem>();
		}
		return value.ItemCosts;
	}

	public bool BuyItem(string itemName)
	{
		if (!currentItems.Contains(itemName))
		{
			return false;
		}
		soldItems.TryAdd(itemName, 0);
		soldItems[itemName]++;
		if (proto.RefreshImmediatly)
		{
			Refresh();
		}
		return true;
	}

	public ExchangeStoreItemData GetStoreItem(string itemName)
	{
		proto.StoreItemMap.TryGetValue(itemName, out var value);
		return value;
	}

	public string[] GetSortedItems()
	{
		string[] array = new string[currentItems.Count];
		int num = 0;
		foreach (ExchangeStoreItemData item in proto.ItemList)
		{
			if (currentItems.Contains(item.ItemId))
			{
				array[num++] = item.ItemId;
			}
		}
		return array;
	}

	public bool CheckRecipeUnlock(string recipeId)
	{
		return currentItems.Contains(recipeId);
	}

	public bool TryGetRecipe(CountItem[] items, out IRecipe recipe)
	{
		recipe = null;
		return false;
	}

	public IRecipe GetRecipe(string recipeId)
	{
		if (!proto.StoreItemMap.TryGetValue(recipeId, out var value))
		{
			return null;
		}
		return new ExchangeStoreRecipe(value, GetItemCurrentStorage(value.ItemId));
	}

	private List<string> GetRecipeIdByOrder()
	{
		List<string> list = new List<string>();
		List<string> list2 = proto.StoreItemMap.Keys.ToList();
		foreach (string item in list2)
		{
			if (GetItemCurrentStorage(item) != 0)
			{
				list.Add(item);
			}
		}
		foreach (string item2 in list2)
		{
			if (GetItemCurrentStorage(item2) == 0)
			{
				list.Add(item2);
			}
		}
		return list;
	}

	public int GetSoldCount(string itemName)
	{
		return 0;
	}
}
