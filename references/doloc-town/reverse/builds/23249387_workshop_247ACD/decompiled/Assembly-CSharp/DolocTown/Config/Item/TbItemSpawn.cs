using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbItemSpawn
{
	private readonly Dictionary<string, ItemSpawnInfo> _dataMap;

	private readonly List<ItemSpawnInfo> _dataList;

	public Dictionary<string, ItemSpawnInfo> DataMap => _dataMap;

	public List<ItemSpawnInfo> DataList => _dataList;

	public ItemSpawnInfo this[string key] => _dataMap[key];

	public TbItemSpawn(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ItemSpawnInfo>();
		_dataList = new List<ItemSpawnInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ItemSpawnInfo itemSpawnInfo = ItemSpawnInfo.DeserializeItemSpawnInfo(child);
			if (_dataMap.TryAdd(itemSpawnInfo.Id, itemSpawnInfo))
			{
				_dataList.Add(itemSpawnInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + itemSpawnInfo.Id + " in table: TbItemSpawn");
			}
		}
	}

	public ItemSpawnInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ItemSpawnInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ItemSpawnInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ItemSpawnInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
