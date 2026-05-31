using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbItem
{
	private readonly Dictionary<string, ItemInfo> _dataMap;

	private readonly List<ItemInfo> _dataList;

	public Dictionary<string, ItemInfo> DataMap => _dataMap;

	public List<ItemInfo> DataList => _dataList;

	public ItemInfo this[string key] => _dataMap[key];

	public TbItem(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ItemInfo>();
		_dataList = new List<ItemInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ItemInfo itemInfo = ItemInfo.DeserializeItemInfo(child);
			if (_dataMap.TryAdd(itemInfo.Id, itemInfo))
			{
				_dataList.Add(itemInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + itemInfo.Id + " in table: TbItem");
			}
		}
	}

	public ItemInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ItemInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ItemInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ItemInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
