using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbItemAutomationType
{
	private readonly Dictionary<string, ItemAutomationTypeInfo> _dataMap;

	private readonly List<ItemAutomationTypeInfo> _dataList;

	public Dictionary<string, ItemAutomationTypeInfo> DataMap => _dataMap;

	public List<ItemAutomationTypeInfo> DataList => _dataList;

	public ItemAutomationTypeInfo this[string key] => _dataMap[key];

	public TbItemAutomationType(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ItemAutomationTypeInfo>();
		_dataList = new List<ItemAutomationTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ItemAutomationTypeInfo itemAutomationTypeInfo = ItemAutomationTypeInfo.DeserializeItemAutomationTypeInfo(child);
			if (_dataMap.TryAdd(itemAutomationTypeInfo.Id, itemAutomationTypeInfo))
			{
				_dataList.Add(itemAutomationTypeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + itemAutomationTypeInfo.Id + " in table: TbItemAutomationType");
			}
		}
	}

	public ItemAutomationTypeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ItemAutomationTypeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ItemAutomationTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ItemAutomationTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
