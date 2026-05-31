using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbItemSource
{
	private readonly Dictionary<string, ItemSourceInfo> _dataMap;

	private readonly List<ItemSourceInfo> _dataList;

	public Dictionary<string, ItemSourceInfo> DataMap => _dataMap;

	public List<ItemSourceInfo> DataList => _dataList;

	public ItemSourceInfo this[string key] => _dataMap[key];

	public TbItemSource(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ItemSourceInfo>();
		_dataList = new List<ItemSourceInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ItemSourceInfo itemSourceInfo = ItemSourceInfo.DeserializeItemSourceInfo(child);
			if (_dataMap.TryAdd(itemSourceInfo.Id, itemSourceInfo))
			{
				_dataList.Add(itemSourceInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + itemSourceInfo.Id + " in table: TbItemSource");
			}
		}
	}

	public ItemSourceInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ItemSourceInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ItemSourceInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ItemSourceInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
