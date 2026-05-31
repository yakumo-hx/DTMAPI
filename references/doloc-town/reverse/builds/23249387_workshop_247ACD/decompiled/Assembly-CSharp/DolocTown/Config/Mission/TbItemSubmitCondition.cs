using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbItemSubmitCondition
{
	private readonly Dictionary<string, ItemSubmitConditionInfo> _dataMap;

	private readonly List<ItemSubmitConditionInfo> _dataList;

	public Dictionary<string, ItemSubmitConditionInfo> DataMap => _dataMap;

	public List<ItemSubmitConditionInfo> DataList => _dataList;

	public ItemSubmitConditionInfo this[string key] => _dataMap[key];

	public TbItemSubmitCondition(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ItemSubmitConditionInfo>();
		_dataList = new List<ItemSubmitConditionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ItemSubmitConditionInfo itemSubmitConditionInfo = ItemSubmitConditionInfo.DeserializeItemSubmitConditionInfo(child);
			if (_dataMap.TryAdd(itemSubmitConditionInfo.Id, itemSubmitConditionInfo))
			{
				_dataList.Add(itemSubmitConditionInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + itemSubmitConditionInfo.Id + " in table: TbItemSubmitCondition");
			}
		}
	}

	public ItemSubmitConditionInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ItemSubmitConditionInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ItemSubmitConditionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ItemSubmitConditionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
