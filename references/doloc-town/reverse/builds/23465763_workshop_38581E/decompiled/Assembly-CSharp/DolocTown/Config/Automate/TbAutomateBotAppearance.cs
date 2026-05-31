using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Automate;

public sealed class TbAutomateBotAppearance
{
	private readonly Dictionary<string, AutomateBotAppearanceInfo> _dataMap;

	private readonly List<AutomateBotAppearanceInfo> _dataList;

	public Dictionary<string, AutomateBotAppearanceInfo> DataMap => _dataMap;

	public List<AutomateBotAppearanceInfo> DataList => _dataList;

	public AutomateBotAppearanceInfo this[string key] => _dataMap[key];

	public TbAutomateBotAppearance(JSONNode _json)
	{
		_dataMap = new Dictionary<string, AutomateBotAppearanceInfo>();
		_dataList = new List<AutomateBotAppearanceInfo>();
		foreach (JSONNode child in _json.Children)
		{
			AutomateBotAppearanceInfo automateBotAppearanceInfo = AutomateBotAppearanceInfo.DeserializeAutomateBotAppearanceInfo(child);
			if (_dataMap.TryAdd(automateBotAppearanceInfo.Id, automateBotAppearanceInfo))
			{
				_dataList.Add(automateBotAppearanceInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + automateBotAppearanceInfo.Id + " in table: TbAutomateBotAppearance");
			}
		}
	}

	public AutomateBotAppearanceInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public AutomateBotAppearanceInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (AutomateBotAppearanceInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (AutomateBotAppearanceInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
