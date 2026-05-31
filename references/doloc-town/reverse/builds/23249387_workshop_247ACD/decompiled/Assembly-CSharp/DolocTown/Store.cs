using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.General;
using DolocTown.Config.Item;
using DolocTown.Config.Store;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class Store : IStore
{
	[JsonProperty]
	private Dictionary<string, int> currentItems;

	[JsonProperty]
	private Dictionary<string, int> buybackItems;

	[JsonProperty]
	private Dictionary<string, int> soldItems;

	[JsonProperty]
	private HashSet<string> extraUnlockedItems;

	[JsonProperty]
	private long totalEarnings;

	[JsonProperty]
	private long totalSpending;

	public StoreInfo proto { get; private set; }

	public bool IsValid => proto != null;

	[JsonProperty]
	public string id => proto.Id;

	public string title => proto.Title;

	[JsonProperty]
	public int money { get; private set; }

	public int ItemCategory => currentItems.Count + buybackItems.Count;

	public int TotalEarnings => (int)totalEarnings;

	public int TotalSpending => (int)totalSpending;

	private int seasonIndex => DolocAPI.archiveHandle.timeData.SeasonIndex;

	private StorePriceScaleData seasonPrice => proto.PriceScale_Ref.SeasonPriceScale[seasonIndex];

	public Store(StoreInfo proto)
	{
		this.proto = proto;
		currentItems = new Dictionary<string, int>();
		buybackItems = new Dictionary<string, int>();
		soldItems = new Dictionary<string, int>();
		extraUnlockedItems = new HashSet<string>();
		money = proto.InitMoney;
		DolocAPI.OnAfterLoadArchiveData.AddListener(delegate
		{
			RefreshItems();
		});
	}

	[JsonConstructor]
	private Store(string id, int money, Dictionary<string, int> currentItems, Dictionary<string, int> buyBackItems, Dictionary<string, int> soldItems, HashSet<string> extraUnlockedItems, long totalEarnings, long totalSpending)
	{
		proto = DolocConfig.Tables.TbStore.GetOrDefault(id);
		if (proto != null)
		{
			this.money = money;
			this.currentItems = currentItems ?? new Dictionary<string, int>();
			buybackItems = buyBackItems ?? new Dictionary<string, int>();
			this.soldItems = soldItems ?? new Dictionary<string, int>();
			this.extraUnlockedItems = extraUnlockedItems ?? new HashSet<string>();
			this.totalEarnings = totalEarnings;
			this.totalSpending = totalSpending;
			Validate();
			DolocAPI.OnAfterLoadArchiveData.AddListener(delegate
			{
				ValidateFixedItems();
			});
		}
	}

	private void Validate()
	{
		ValidateItemDict(currentItems);
		ValidateItemDict(buybackItems);
		ValidateItemDict(soldItems);
		extraUnlockedItems = extraUnlockedItems.Where((string x) => DolocAPI.QueryItemProto(x, out var _)).ToHashSet();
	}

	private void ValidateItemDict(Dictionary<string, int> dict)
	{
		string[] array = dict.Keys.ToArray();
		foreach (string text in array)
		{
			if (!DolocAPI.QueryItemProto(text, out var _))
			{
				dict.Remove(text);
			}
		}
	}

	private void ValidateFixedItems()
	{
		List<StoreItemSeasonData> list = new List<StoreItemSeasonData>();
		foreach (StoreItemSeasonData item in proto.ItemRecords_Ref.ItemList)
		{
			if (CanSpawnItem(item) && item.IsFixedInSeason(seasonIndex) && !currentItems.ContainsKey(item.ItemName))
			{
				list.Add(item);
			}
		}
		foreach (StoreItemSeasonData item2 in proto.ItemRecords_Ref.ItemList)
		{
			if (list.Contains(item2))
			{
				currentItems[item2.ItemName] = GetItemSpawnCount(item2);
			}
		}
		string[] array = currentItems.Keys.ToArray();
		foreach (string key in array)
		{
			if (!proto.ItemRecords_Ref.ItemList_Index.ContainsKey(key))
			{
				currentItems.Remove(key);
			}
		}
	}

	public bool UnlockStoreItem(string itemName)
	{
		if (QueryItemRecord(itemName, out var _))
		{
			return extraUnlockedItems.Add(itemName);
		}
		return false;
	}

	public void RevertUnlockStoreItem(string itemName)
	{
		extraUnlockedItems.Remove(itemName);
	}

	public void Refresh()
	{
		RefreshMoney();
		RefreshItems();
	}

	private void RefreshMoney()
	{
		if (!proto.InitMoneyRange.IsNullOrEmpty())
		{
			int num = Mathf.Clamp(DolocAPI.archiveHandle.cityData.storeManager.GlobalStoreLevel, 0, proto.InitMoneyRange.Length - 1);
			DolocTown.Config.General.RangeInt rangeInt = proto.InitMoneyRange[num];
			money = Mathf.RoundToInt((float)rangeInt.RandomCount / 100f) * 100;
		}
	}

	private void RefreshItems()
	{
		currentItems = SpawnStoreItems();
		buybackItems.Clear();
	}

	private bool QueryItemRecord(string itemName, out StoreItemSeasonData record)
	{
		return proto.ItemRecords_Ref.ItemList_Index.TryGetValue(itemName, out record);
	}

	private Dictionary<string, int> SpawnStoreItems()
	{
		List<StoreItemSeasonData> list = new List<StoreItemSeasonData>();
		List<StoreItemSeasonData> list2 = new List<StoreItemSeasonData>();
		foreach (StoreItemSeasonData item2 in proto.ItemRecords_Ref.ItemList)
		{
			if (CanSpawnItem(item2))
			{
				if (item2.IsFixedInSeason(seasonIndex))
				{
					list.Add(item2);
				}
				else
				{
					list2.Add(item2);
				}
			}
		}
		int randomSlotCountBySeason = proto.GetRandomSlotCountBySeason(seasonIndex);
		while (randomSlotCountBySeason-- > 0 && list2.Count != 0)
		{
			float[] weights = list2.Select((StoreItemSeasonData x) => x.GetSeasonSpawnData(seasonIndex).SpawnWeight).ToArray();
			StoreItemSeasonData item = list2.ToArray().Choice(weights);
			list2.Remove(item);
			list.Add(item);
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (StoreItemSeasonData item3 in proto.ItemRecords_Ref.ItemList)
		{
			if (list.Contains(item3))
			{
				dictionary[item3.ItemName] = GetItemSpawnCount(item3);
			}
		}
		return dictionary;
	}

	private bool IsItemUnlock(string itemName)
	{
		if (QueryItemRecord(itemName, out var record))
		{
			return IsItemUnlock(record);
		}
		return false;
	}

	private bool IsItemUnlock(StoreItemSeasonData record)
	{
		if (!record.DefaultUnlock)
		{
			return extraUnlockedItems.Contains(record.ItemName);
		}
		return true;
	}

	private bool CanSpawnItem(StoreItemSeasonData record)
	{
		if (record.GetSeasonSpawnData(seasonIndex).CountRange.MaxCount > 0 && (record.DefaultUnlock || extraUnlockedItems.Contains(record.ItemName)))
		{
			if (!record.UnlimitedStorage)
			{
				return record.Storage - GetSoldCount(record.ItemName) > 0;
			}
			return true;
		}
		return false;
	}

	private int GetItemSpawnCount(StoreItemSeasonData record)
	{
		if (record.UnlimitedStorage)
		{
			return record.GetSeasonSpawnData(seasonIndex).CountRange.RandomCount;
		}
		return Mathf.Min(record.Storage - GetSoldCount(record.ItemName), record.GetSeasonSpawnData(seasonIndex).CountRange.RandomCount);
	}

	public int GetItemUnitSellingPrice(string itemName)
	{
		if (!DolocAPI.QueryItemProto(itemName, out var itemInfo))
		{
			return 0;
		}
		return Mathf.RoundToInt((float)DolocAPI.GetItemSellingPrice(itemName) * GetPriceScale(itemInfo.SubType_Ref));
	}

	public int GetItemUnitSellingPrice(Item item, out float priceScale)
	{
		priceScale = 0f;
		if (item == null)
		{
			return 0;
		}
		priceScale = GetPriceScale(item.subType);
		return Mathf.RoundToInt((float)item.sellingPrice * priceScale);
	}

	public int GetItemTotalSellingPrice(Item item)
	{
		if (item == null)
		{
			return 0;
		}
		return Mathf.RoundToInt((float)item.sellingPrice * GetPriceScale(item.subType)) * item.count;
	}

	private float GetPriceScale(ItemSubTypeInfo type)
	{
		if (seasonPrice.ScaleByType_Index.TryGetValue(type.Id, out var value))
		{
			return value.PriceScale;
		}
		return seasonPrice.DefaultScale;
	}

	public int GetItemUnitBuyingPrice(string itemName, bool isBuyback)
	{
		if (isBuyback)
		{
			return GetItemUnitSellingPrice(itemName);
		}
		if (!DolocAPI.QueryItemProto(itemName, out var itemInfo))
		{
			return 0;
		}
		return itemInfo.BuyingPrice;
	}

	public int GetSoldCount(string itemName)
	{
		soldItems.TryGetValue(itemName, out var value);
		return value;
	}

	public bool SellSingleItem(Item item, out int price)
	{
		return SellItemInternal(item, 1, limitCountByItem: true, out price) == 1;
	}

	public int SellItems(Item[] items, out int price)
	{
		price = 0;
		int num = 0;
		foreach (Item item in items)
		{
			num += SellItem(item, item.count, limitCountByItem: true, out var price2);
			price += price2;
		}
		return num;
	}

	public int SellItem(Item item, int count, bool limitCountByItem, out int price)
	{
		return SellItemInternal(item, count, limitCountByItem, out price);
	}

	private int SellItemInternal(Item item, int count, bool limitCountByItem, out int price)
	{
		price = 0;
		if (item == null)
		{
			return 0;
		}
		float priceScale;
		int itemUnitSellingPrice = GetItemUnitSellingPrice(item, out priceScale);
		int num = ((itemUnitSellingPrice > 0 && !proto.UnlimitedMoney) ? (money / itemUnitSellingPrice) : count);
		int num2 = (limitCountByItem ? Mathf.Min(count, item.count, num) : Mathf.Min(count, num));
		price = itemUnitSellingPrice * num2;
		if (item.canBuyback)
		{
			buybackItems.TryAdd(item.name, 0);
			buybackItems[item.name] += num2;
		}
		CostMoney(price);
		for (int i = 0; i < num2; i++)
		{
			DolocAPI.BroadcastString(GameEventType.SELL_ITEM_IN_STORE, item.name);
		}
		return num2;
	}

	public bool BuySingleItem(StoreItemRef storeItemRef, out int price)
	{
		return BuyItemInternal(storeItemRef.itemName, 1, storeItemRef.isBuyback, out price) == 1;
	}

	public bool BuyItem(StoreItemRef storeItemRef, int count, out int price)
	{
		return BuyItemInternal(storeItemRef.itemName, count, storeItemRef.isBuyback, out price) == count;
	}

	private int BuyItemInternal(string itemName, int count, bool isBuyback, out int price)
	{
		price = 0;
		Dictionary<string, int> dictionary = (isBuyback ? buybackItems : currentItems);
		if (!dictionary.TryGetValue(itemName, out var value) || !DolocAPI.QueryItemProto(itemName, out var _))
		{
			return 0;
		}
		int num = Mathf.Min(count, value);
		price = GetItemUnitBuyingPrice(itemName, isBuyback) * num;
		dictionary[itemName] -= num;
		if (!isBuyback)
		{
			soldItems.TryAdd(itemName, 0);
			soldItems[itemName] += num;
		}
		else if (buybackItems[itemName] <= 0)
		{
			buybackItems.Remove(itemName);
		}
		AddMoney(price, isBuyback);
		return num;
	}

	private void AddMoney(int price, bool isBuyback)
	{
		if (!proto.UnlimitedMoney)
		{
			money += price;
		}
		if (!isBuyback)
		{
			totalEarnings += price;
		}
		else
		{
			totalSpending -= price;
		}
	}

	private void CostMoney(int price)
	{
		if (!proto.UnlimitedMoney)
		{
			money -= price;
		}
		totalSpending += price;
	}

	public int GetCurrentCount(StoreItemRef storeItemRef)
	{
		(storeItemRef.isBuyback ? buybackItems : currentItems).TryGetValue(storeItemRef.itemName, out var value);
		return value;
	}

	public StoreItemRef[] GetSortedItems()
	{
		StoreItemRef[] array = new StoreItemRef[ItemCategory];
		int num = 0;
		foreach (KeyValuePair<string, int> currentItem in currentItems)
		{
			array[num].itemName = currentItem.Key;
			array[num].isBuyback = false;
			num++;
		}
		foreach (KeyValuePair<string, int> buybackItem in buybackItems)
		{
			array[num].itemName = buybackItem.Key;
			array[num].isBuyback = true;
			num++;
		}
		return array;
	}
}
