using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Archives;

public sealed class TbSpecialChipEvent
{
	private readonly Dictionary<int, SpecialChipEventInfo> _dataMap;

	private readonly List<SpecialChipEventInfo> _dataList;

	public Dictionary<int, SpecialChipEventInfo> DataMap => _dataMap;

	public List<SpecialChipEventInfo> DataList => _dataList;

	public SpecialChipEventInfo this[int key] => _dataMap[key];

	public TbSpecialChipEvent(JSONNode _json)
	{
		_dataMap = new Dictionary<int, SpecialChipEventInfo>();
		_dataList = new List<SpecialChipEventInfo>();
		foreach (JSONNode child in _json.Children)
		{
			SpecialChipEventInfo specialChipEventInfo = SpecialChipEventInfo.DeserializeSpecialChipEventInfo(child);
			if (_dataMap.TryAdd(specialChipEventInfo.Id, specialChipEventInfo))
			{
				_dataList.Add(specialChipEventInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {specialChipEventInfo.Id} in table: TbSpecialChipEvent");
			}
		}
	}

	public SpecialChipEventInfo GetOrDefault(int key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public SpecialChipEventInfo Get(int key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (SpecialChipEventInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (SpecialChipEventInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
