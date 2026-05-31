using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbCustomEvent
{
	private readonly Dictionary<string, CustomEventInfo> _dataMap;

	private readonly List<CustomEventInfo> _dataList;

	public Dictionary<string, CustomEventInfo> DataMap => _dataMap;

	public List<CustomEventInfo> DataList => _dataList;

	public CustomEventInfo this[string key] => _dataMap[key];

	public TbCustomEvent(JSONNode _json)
	{
		_dataMap = new Dictionary<string, CustomEventInfo>();
		_dataList = new List<CustomEventInfo>();
		foreach (JSONNode child in _json.Children)
		{
			CustomEventInfo customEventInfo = CustomEventInfo.DeserializeCustomEventInfo(child);
			if (_dataMap.TryAdd(customEventInfo.Id, customEventInfo))
			{
				_dataList.Add(customEventInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + customEventInfo.Id + " in table: TbCustomEvent");
			}
		}
	}

	public CustomEventInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public CustomEventInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (CustomEventInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (CustomEventInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
