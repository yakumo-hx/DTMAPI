using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Automate;

public sealed class TbAutomateBot
{
	private readonly Dictionary<string, AutomateBotInfo> _dataMap;

	private readonly List<AutomateBotInfo> _dataList;

	public Dictionary<string, AutomateBotInfo> DataMap => _dataMap;

	public List<AutomateBotInfo> DataList => _dataList;

	public AutomateBotInfo this[string key] => _dataMap[key];

	public TbAutomateBot(JSONNode _json)
	{
		_dataMap = new Dictionary<string, AutomateBotInfo>();
		_dataList = new List<AutomateBotInfo>();
		foreach (JSONNode child in _json.Children)
		{
			AutomateBotInfo automateBotInfo = AutomateBotInfo.DeserializeAutomateBotInfo(child);
			if (_dataMap.TryAdd(automateBotInfo.Id, automateBotInfo))
			{
				_dataList.Add(automateBotInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + automateBotInfo.Id + " in table: TbAutomateBot");
			}
		}
	}

	public AutomateBotInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public AutomateBotInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (AutomateBotInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (AutomateBotInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
