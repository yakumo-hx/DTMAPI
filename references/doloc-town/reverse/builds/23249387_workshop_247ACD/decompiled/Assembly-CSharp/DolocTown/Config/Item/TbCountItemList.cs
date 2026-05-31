using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbCountItemList
{
	private readonly Dictionary<string, CountItemListInfo> _dataMap;

	private readonly List<CountItemListInfo> _dataList;

	public Dictionary<string, CountItemListInfo> DataMap => _dataMap;

	public List<CountItemListInfo> DataList => _dataList;

	public CountItemListInfo this[string key] => _dataMap[key];

	public TbCountItemList(JSONNode _json)
	{
		_dataMap = new Dictionary<string, CountItemListInfo>();
		_dataList = new List<CountItemListInfo>();
		foreach (JSONNode child in _json.Children)
		{
			CountItemListInfo countItemListInfo = CountItemListInfo.DeserializeCountItemListInfo(child);
			if (_dataMap.TryAdd(countItemListInfo.Id, countItemListInfo))
			{
				_dataList.Add(countItemListInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + countItemListInfo.Id + " in table: TbCountItemList");
			}
		}
	}

	public CountItemListInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public CountItemListInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (CountItemListInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (CountItemListInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
