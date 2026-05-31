using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Automate;

public sealed class TbAutomateBotPerformance
{
	private readonly Dictionary<string, AutomateBotPerformanceInfo> _dataMap;

	private readonly List<AutomateBotPerformanceInfo> _dataList;

	public Dictionary<string, AutomateBotPerformanceInfo> DataMap => _dataMap;

	public List<AutomateBotPerformanceInfo> DataList => _dataList;

	public AutomateBotPerformanceInfo this[string key] => _dataMap[key];

	public TbAutomateBotPerformance(JSONNode _json)
	{
		_dataMap = new Dictionary<string, AutomateBotPerformanceInfo>();
		_dataList = new List<AutomateBotPerformanceInfo>();
		foreach (JSONNode child in _json.Children)
		{
			AutomateBotPerformanceInfo automateBotPerformanceInfo = AutomateBotPerformanceInfo.DeserializeAutomateBotPerformanceInfo(child);
			if (_dataMap.TryAdd(automateBotPerformanceInfo.Id, automateBotPerformanceInfo))
			{
				_dataList.Add(automateBotPerformanceInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + automateBotPerformanceInfo.Id + " in table: TbAutomateBotPerformance");
			}
		}
	}

	public AutomateBotPerformanceInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public AutomateBotPerformanceInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (AutomateBotPerformanceInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (AutomateBotPerformanceInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
