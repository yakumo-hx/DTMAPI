using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Store;

public sealed class TbStoreItemList
{
	private readonly Dictionary<string, StoreItemListInfo> _dataMap;

	private readonly List<StoreItemListInfo> _dataList;

	public Dictionary<string, StoreItemListInfo> DataMap => _dataMap;

	public List<StoreItemListInfo> DataList => _dataList;

	public StoreItemListInfo this[string key] => _dataMap[key];

	public TbStoreItemList(JSONNode _json)
	{
		_dataMap = new Dictionary<string, StoreItemListInfo>();
		_dataList = new List<StoreItemListInfo>();
		foreach (JSONNode child in _json.Children)
		{
			StoreItemListInfo storeItemListInfo = StoreItemListInfo.DeserializeStoreItemListInfo(child);
			if (_dataMap.TryAdd(storeItemListInfo.Id, storeItemListInfo))
			{
				_dataList.Add(storeItemListInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + storeItemListInfo.Id + " in table: TbStoreItemList");
			}
		}
	}

	public StoreItemListInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public StoreItemListInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (StoreItemListInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (StoreItemListInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
