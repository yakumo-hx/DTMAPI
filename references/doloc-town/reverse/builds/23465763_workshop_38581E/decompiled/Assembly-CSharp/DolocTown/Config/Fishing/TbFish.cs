using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Fishing;

public sealed class TbFish
{
	private readonly Dictionary<string, FishInfo> _dataMap;

	private readonly List<FishInfo> _dataList;

	public Dictionary<string, FishInfo> DataMap => _dataMap;

	public List<FishInfo> DataList => _dataList;

	public FishInfo this[string key] => _dataMap[key];

	public TbFish(JSONNode _json)
	{
		_dataMap = new Dictionary<string, FishInfo>();
		_dataList = new List<FishInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FishInfo fishInfo = FishInfo.DeserializeFishInfo(child);
			if (_dataMap.TryAdd(fishInfo.Id, fishInfo))
			{
				_dataList.Add(fishInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + fishInfo.Id + " in table: TbFish");
			}
		}
	}

	public FishInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FishInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FishInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FishInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
