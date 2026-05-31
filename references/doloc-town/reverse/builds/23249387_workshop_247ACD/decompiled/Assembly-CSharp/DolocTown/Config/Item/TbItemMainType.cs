using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbItemMainType
{
	private readonly Dictionary<string, ItemMainTypeInfo> _dataMap;

	private readonly List<ItemMainTypeInfo> _dataList;

	public Dictionary<string, ItemMainTypeInfo> DataMap => _dataMap;

	public List<ItemMainTypeInfo> DataList => _dataList;

	public ItemMainTypeInfo this[string key] => _dataMap[key];

	public TbItemMainType(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ItemMainTypeInfo>();
		_dataList = new List<ItemMainTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ItemMainTypeInfo itemMainTypeInfo = ItemMainTypeInfo.DeserializeItemMainTypeInfo(child);
			if (_dataMap.TryAdd(itemMainTypeInfo.Id, itemMainTypeInfo))
			{
				_dataList.Add(itemMainTypeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + itemMainTypeInfo.Id + " in table: TbItemMainType");
			}
		}
	}

	public ItemMainTypeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ItemMainTypeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ItemMainTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ItemMainTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
