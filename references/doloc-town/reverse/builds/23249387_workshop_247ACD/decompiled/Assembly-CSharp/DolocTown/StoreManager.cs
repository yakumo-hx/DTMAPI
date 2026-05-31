using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Store;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class StoreManager
{
	[JsonProperty]
	private Dictionary<string, Store> stores;

	[JsonProperty]
	private Dictionary<string, ExchangeStore> exchangeStores;

	[JsonProperty]
	private int globalStoreLevel;

	private Dictionary<Type, Dictionary<string, IStore>> allTypedStores = new Dictionary<Type, Dictionary<string, IStore>>();

	private List<IStore> allStores = new List<IStore>();

	private Dictionary<string, StoreItemUnlockInfo> itemsNeedUnlock = new Dictionary<string, StoreItemUnlockInfo>();

	public int GlobalStoreLevel => globalStoreLevel;

	[JsonConstructor]
	public StoreManager(Dictionary<string, Store> stores = null, Dictionary<string, ExchangeStore> exchangeStores = null, int globalStoreLevel = 0)
	{
		this.stores = stores ?? new Dictionary<string, Store>();
		this.exchangeStores = exchangeStores ?? new Dictionary<string, ExchangeStore>();
		this.globalStoreLevel = globalStoreLevel;
		Validate();
		InitCache();
	}

	private void Validate()
	{
		string[] array = stores.Keys.ToArray();
		foreach (string text in array)
		{
			if (!stores[text].IsValid)
			{
				Debug.LogError("普通商店失效: " + text);
				stores.Remove(text);
			}
		}
		array = exchangeStores.Keys.ToArray();
		foreach (string text2 in array)
		{
			if (!exchangeStores[text2].IsValid)
			{
				Debug.LogError("升级商店失效: " + text2);
				exchangeStores.Remove(text2);
			}
		}
	}

	private void InitCache()
	{
		allTypedStores.Add(typeof(Store), new Dictionary<string, IStore>());
		allTypedStores.Add(typeof(ExchangeStore), new Dictionary<string, IStore>());
		foreach (StoreInfo data in DolocConfig.Tables.TbStore.DataList)
		{
			stores.TryAdd(data.Id, new Store(data));
			allTypedStores[typeof(Store)].TryAdd(data.Id, stores[data.Id]);
			allStores.Add(stores[data.Id]);
		}
		foreach (ExchangeStoreInfo data2 in DolocConfig.Tables.TbExchangeStore.DataList)
		{
			exchangeStores.TryAdd(data2.Id, new ExchangeStore(data2));
			allTypedStores[typeof(ExchangeStore)].TryAdd(data2.Id, exchangeStores[data2.Id]);
			allStores.Add(exchangeStores[data2.Id]);
		}
	}

	public void AfterLoadData()
	{
		foreach (StoreItemUnlockInfo data in DolocConfig.Tables.TbStoreItemUnlock.DataList)
		{
			if ((data.ConditionType == StoreItemUnlockType.ObtainItem && DolocAPI.GetEventTriggerCount(GameEventType.OBTAIN_ITEM, data.Id) > 0) || (data.ConditionType == StoreItemUnlockType.CompleteFactionMission && DolocAPI.IsFactionMissionComplete(data.Id)) || (data.ConditionType == StoreItemUnlockType.ReadEmail && DolocAPI.GetEventTriggerCount(GameEventType.READ_EMAIL, data.Id) > 0))
			{
				DolocAPI.UnlockStoreItem(data.StoreId, data.UnlockItemName);
			}
			else
			{
				itemsNeedUnlock[data.Id] = data;
			}
		}
	}

	public void OnMessage(GameMessage message)
	{
		if (itemsNeedUnlock.Count != 0 && (message.Type == GameEventType.OBTAIN_ITEM || message.Type == GameEventType.FACTION_MISSION_FINISH) && message.Args is GameEventArgsString { value: var value } && itemsNeedUnlock.TryGetValue(value, out var value2) && ((message.Type == GameEventType.OBTAIN_ITEM && value2.ConditionType == StoreItemUnlockType.ObtainItem) || (message.Type == GameEventType.FACTION_MISSION_FINISH && value2.ConditionType == StoreItemUnlockType.CompleteFactionMission) || (message.Type == GameEventType.READ_EMAIL && value2.ConditionType == StoreItemUnlockType.ReadEmail)))
		{
			DolocAPI.UnlockStoreItem(value2.StoreId, value2.UnlockItemName);
		}
	}

	public bool QueryStore<T>(string name, out T store) where T : class, IStore
	{
		store = null;
		if (!allTypedStores.TryGetValue(typeof(T), out var value))
		{
			return false;
		}
		value.TryGetValue(name, out var value2);
		store = value2 as T;
		return store != null;
	}

	public bool QueryStore(string storeName, out IStore store)
	{
		store = null;
		if (QueryStore(storeName, out Store store2))
		{
			store = store2;
			return true;
		}
		if (QueryStore(storeName, out ExchangeStore store3))
		{
			store = store3;
			return true;
		}
		return false;
	}

	public void RefreshAllStores()
	{
		foreach (IStore allStore in allStores)
		{
			allStore.Refresh();
		}
	}

	public void SetGlobalStoreLevel(int level)
	{
		level = Mathf.Max(0, level);
		globalStoreLevel = level;
	}
}
