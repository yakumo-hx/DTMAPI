using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class TbLamp
{
	private readonly Dictionary<string, LampInfo> _dataMap;

	private readonly List<LampInfo> _dataList;

	public Dictionary<string, LampInfo> DataMap => _dataMap;

	public List<LampInfo> DataList => _dataList;

	public LampInfo this[string key] => _dataMap[key];

	public TbLamp(JSONNode _json)
	{
		_dataMap = new Dictionary<string, LampInfo>();
		_dataList = new List<LampInfo>();
		foreach (JSONNode child in _json.Children)
		{
			LampInfo lampInfo = LampInfo.DeserializeLampInfo(child);
			if (_dataMap.TryAdd(lampInfo.Id, lampInfo))
			{
				_dataList.Add(lampInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + lampInfo.Id + " in table: TbLamp");
			}
		}
	}

	public LampInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public LampInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (LampInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (LampInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
