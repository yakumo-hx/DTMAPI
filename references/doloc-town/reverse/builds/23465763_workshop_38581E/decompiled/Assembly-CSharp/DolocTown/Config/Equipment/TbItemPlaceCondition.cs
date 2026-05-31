using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class TbItemPlaceCondition
{
	private readonly Dictionary<string, ItemPlaceConditionInfo> _dataMap;

	private readonly List<ItemPlaceConditionInfo> _dataList;

	public Dictionary<string, ItemPlaceConditionInfo> DataMap => _dataMap;

	public List<ItemPlaceConditionInfo> DataList => _dataList;

	public ItemPlaceConditionInfo this[string key] => _dataMap[key];

	public TbItemPlaceCondition(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ItemPlaceConditionInfo>();
		_dataList = new List<ItemPlaceConditionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ItemPlaceConditionInfo itemPlaceConditionInfo = ItemPlaceConditionInfo.DeserializeItemPlaceConditionInfo(child);
			if (_dataMap.TryAdd(itemPlaceConditionInfo.Id, itemPlaceConditionInfo))
			{
				_dataList.Add(itemPlaceConditionInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + itemPlaceConditionInfo.Id + " in table: TbItemPlaceCondition");
			}
		}
	}

	public ItemPlaceConditionInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ItemPlaceConditionInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ItemPlaceConditionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ItemPlaceConditionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
