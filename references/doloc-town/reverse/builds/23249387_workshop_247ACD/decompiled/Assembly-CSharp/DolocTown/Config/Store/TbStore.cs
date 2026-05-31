using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Store;

public sealed class TbStore
{
	private readonly Dictionary<string, StoreInfo> _dataMap;

	private readonly List<StoreInfo> _dataList;

	public Dictionary<string, StoreInfo> DataMap => _dataMap;

	public List<StoreInfo> DataList => _dataList;

	public StoreInfo this[string key] => _dataMap[key];

	public TbStore(JSONNode _json)
	{
		_dataMap = new Dictionary<string, StoreInfo>();
		_dataList = new List<StoreInfo>();
		foreach (JSONNode child in _json.Children)
		{
			StoreInfo storeInfo = StoreInfo.DeserializeStoreInfo(child);
			if (_dataMap.TryAdd(storeInfo.Id, storeInfo))
			{
				_dataList.Add(storeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + storeInfo.Id + " in table: TbStore");
			}
		}
	}

	public StoreInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public StoreInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (StoreInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (StoreInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
