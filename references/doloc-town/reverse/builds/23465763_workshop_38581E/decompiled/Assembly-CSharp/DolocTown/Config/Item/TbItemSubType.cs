using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbItemSubType
{
	private readonly Dictionary<string, ItemSubTypeInfo> _dataMap;

	private readonly List<ItemSubTypeInfo> _dataList;

	public Dictionary<string, ItemSubTypeInfo> DataMap => _dataMap;

	public List<ItemSubTypeInfo> DataList => _dataList;

	public ItemSubTypeInfo this[string key] => _dataMap[key];

	public TbItemSubType(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ItemSubTypeInfo>();
		_dataList = new List<ItemSubTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ItemSubTypeInfo itemSubTypeInfo = ItemSubTypeInfo.DeserializeItemSubTypeInfo(child);
			if (_dataMap.TryAdd(itemSubTypeInfo.Id, itemSubTypeInfo))
			{
				_dataList.Add(itemSubTypeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + itemSubTypeInfo.Id + " in table: TbItemSubType");
			}
		}
	}

	public ItemSubTypeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ItemSubTypeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ItemSubTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ItemSubTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
