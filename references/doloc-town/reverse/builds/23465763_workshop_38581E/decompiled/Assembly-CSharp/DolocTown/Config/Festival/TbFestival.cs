using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Festival;

public sealed class TbFestival
{
	private readonly Dictionary<string, FestivalInfo> _dataMap;

	private readonly List<FestivalInfo> _dataList;

	public Dictionary<string, FestivalInfo> DataMap => _dataMap;

	public List<FestivalInfo> DataList => _dataList;

	public FestivalInfo this[string key] => _dataMap[key];

	public TbFestival(JSONNode _json)
	{
		_dataMap = new Dictionary<string, FestivalInfo>();
		_dataList = new List<FestivalInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FestivalInfo festivalInfo = FestivalInfo.DeserializeFestivalInfo(child);
			if (_dataMap.TryAdd(festivalInfo.Id, festivalInfo))
			{
				_dataList.Add(festivalInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + festivalInfo.Id + " in table: TbFestival");
			}
		}
	}

	public FestivalInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FestivalInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FestivalInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FestivalInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
