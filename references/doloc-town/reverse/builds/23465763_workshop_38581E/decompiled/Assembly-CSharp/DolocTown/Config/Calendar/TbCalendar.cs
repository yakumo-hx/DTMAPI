using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Calendar;

public sealed class TbCalendar
{
	private readonly Dictionary<int, CalendarInfo> _dataMap;

	private readonly List<CalendarInfo> _dataList;

	public Dictionary<int, CalendarInfo> DataMap => _dataMap;

	public List<CalendarInfo> DataList => _dataList;

	public CalendarInfo this[int key] => _dataMap[key];

	public TbCalendar(JSONNode _json)
	{
		_dataMap = new Dictionary<int, CalendarInfo>();
		_dataList = new List<CalendarInfo>();
		foreach (JSONNode child in _json.Children)
		{
			CalendarInfo calendarInfo = CalendarInfo.DeserializeCalendarInfo(child);
			if (_dataMap.TryAdd(calendarInfo.Month, calendarInfo))
			{
				_dataList.Add(calendarInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {calendarInfo.Month} in table: TbCalendar");
			}
		}
	}

	public CalendarInfo GetOrDefault(int key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public CalendarInfo Get(int key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (CalendarInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (CalendarInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
