using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbRangedItemList
{
	private readonly Dictionary<string, RangedItemListInfo> _dataMap;

	private readonly List<RangedItemListInfo> _dataList;

	public Dictionary<string, RangedItemListInfo> DataMap => _dataMap;

	public List<RangedItemListInfo> DataList => _dataList;

	public RangedItemListInfo this[string key] => _dataMap[key];

	public TbRangedItemList(JSONNode _json)
	{
		_dataMap = new Dictionary<string, RangedItemListInfo>();
		_dataList = new List<RangedItemListInfo>();
		foreach (JSONNode child in _json.Children)
		{
			RangedItemListInfo rangedItemListInfo = RangedItemListInfo.DeserializeRangedItemListInfo(child);
			if (_dataMap.TryAdd(rangedItemListInfo.Id, rangedItemListInfo))
			{
				_dataList.Add(rangedItemListInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + rangedItemListInfo.Id + " in table: TbRangedItemList");
			}
		}
	}

	public RangedItemListInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public RangedItemListInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (RangedItemListInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (RangedItemListInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
