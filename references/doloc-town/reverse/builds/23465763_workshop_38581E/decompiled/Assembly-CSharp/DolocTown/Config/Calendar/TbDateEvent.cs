using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Calendar;

public sealed class TbDateEvent
{
	private readonly Dictionary<string, DateEventInfo> _dataMap;

	private readonly List<DateEventInfo> _dataList;

	public Dictionary<string, DateEventInfo> DataMap => _dataMap;

	public List<DateEventInfo> DataList => _dataList;

	public DateEventInfo this[string key] => _dataMap[key];

	public TbDateEvent(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DateEventInfo>();
		_dataList = new List<DateEventInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DateEventInfo dateEventInfo = DateEventInfo.DeserializeDateEventInfo(child);
			if (_dataMap.TryAdd(dateEventInfo.Id, dateEventInfo))
			{
				_dataList.Add(dateEventInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + dateEventInfo.Id + " in table: TbDateEvent");
			}
		}
	}

	public DateEventInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DateEventInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DateEventInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DateEventInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
