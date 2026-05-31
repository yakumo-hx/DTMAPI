using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbItemGeneCondition
{
	private readonly Dictionary<string, ItemGeneConditionInfo> _dataMap;

	private readonly List<ItemGeneConditionInfo> _dataList;

	public Dictionary<string, ItemGeneConditionInfo> DataMap => _dataMap;

	public List<ItemGeneConditionInfo> DataList => _dataList;

	public ItemGeneConditionInfo this[string key] => _dataMap[key];

	public TbItemGeneCondition(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ItemGeneConditionInfo>();
		_dataList = new List<ItemGeneConditionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ItemGeneConditionInfo itemGeneConditionInfo = ItemGeneConditionInfo.DeserializeItemGeneConditionInfo(child);
			if (_dataMap.TryAdd(itemGeneConditionInfo.Id, itemGeneConditionInfo))
			{
				_dataList.Add(itemGeneConditionInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + itemGeneConditionInfo.Id + " in table: TbItemGeneCondition");
			}
		}
	}

	public ItemGeneConditionInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ItemGeneConditionInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ItemGeneConditionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ItemGeneConditionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
