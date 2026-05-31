using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Store;

public sealed class TbStoreItemUnlock
{
	private readonly Dictionary<string, StoreItemUnlockInfo> _dataMap;

	private readonly List<StoreItemUnlockInfo> _dataList;

	public Dictionary<string, StoreItemUnlockInfo> DataMap => _dataMap;

	public List<StoreItemUnlockInfo> DataList => _dataList;

	public StoreItemUnlockInfo this[string key] => _dataMap[key];

	public TbStoreItemUnlock(JSONNode _json)
	{
		_dataMap = new Dictionary<string, StoreItemUnlockInfo>();
		_dataList = new List<StoreItemUnlockInfo>();
		foreach (JSONNode child in _json.Children)
		{
			StoreItemUnlockInfo storeItemUnlockInfo = StoreItemUnlockInfo.DeserializeStoreItemUnlockInfo(child);
			if (_dataMap.TryAdd(storeItemUnlockInfo.Id, storeItemUnlockInfo))
			{
				_dataList.Add(storeItemUnlockInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + storeItemUnlockInfo.Id + " in table: TbStoreItemUnlock");
			}
		}
	}

	public StoreItemUnlockInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public StoreItemUnlockInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (StoreItemUnlockInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (StoreItemUnlockInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
