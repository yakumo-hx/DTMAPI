using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Settings;

public sealed class TbTimer
{
	private readonly Dictionary<string, TimerInfo> _dataMap;

	private readonly List<TimerInfo> _dataList;

	public Dictionary<string, TimerInfo> DataMap => _dataMap;

	public List<TimerInfo> DataList => _dataList;

	public TimerInfo this[string key] => _dataMap[key];

	public TbTimer(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TimerInfo>();
		_dataList = new List<TimerInfo>();
		foreach (JSONNode child in _json.Children)
		{
			TimerInfo timerInfo = TimerInfo.DeserializeTimerInfo(child);
			if (_dataMap.TryAdd(timerInfo.Id, timerInfo))
			{
				_dataList.Add(timerInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + timerInfo.Id + " in table: TbTimer");
			}
		}
	}

	public TimerInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TimerInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TimerInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TimerInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
