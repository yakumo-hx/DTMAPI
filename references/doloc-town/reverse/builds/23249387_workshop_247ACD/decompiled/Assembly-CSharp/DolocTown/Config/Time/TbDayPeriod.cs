using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Time;

public sealed class TbDayPeriod
{
	private readonly Dictionary<int, DayPeriodInfo> _dataMap;

	private readonly List<DayPeriodInfo> _dataList;

	public Dictionary<int, DayPeriodInfo> DataMap => _dataMap;

	public List<DayPeriodInfo> DataList => _dataList;

	public DayPeriodInfo this[int key] => _dataMap[key];

	public TbDayPeriod(JSONNode _json)
	{
		_dataMap = new Dictionary<int, DayPeriodInfo>();
		_dataList = new List<DayPeriodInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DayPeriodInfo dayPeriodInfo = DayPeriodInfo.DeserializeDayPeriodInfo(child);
			if (_dataMap.TryAdd(dayPeriodInfo.DayHour, dayPeriodInfo))
			{
				_dataList.Add(dayPeriodInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {dayPeriodInfo.DayHour} in table: TbDayPeriod");
			}
		}
	}

	public DayPeriodInfo GetOrDefault(int key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DayPeriodInfo Get(int key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DayPeriodInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DayPeriodInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
