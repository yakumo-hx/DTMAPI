using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class TbChair
{
	private readonly Dictionary<string, ChairInfo> _dataMap;

	private readonly List<ChairInfo> _dataList;

	public Dictionary<string, ChairInfo> DataMap => _dataMap;

	public List<ChairInfo> DataList => _dataList;

	public ChairInfo this[string key] => _dataMap[key];

	public TbChair(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ChairInfo>();
		_dataList = new List<ChairInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ChairInfo chairInfo = ChairInfo.DeserializeChairInfo(child);
			if (_dataMap.TryAdd(chairInfo.Id, chairInfo))
			{
				_dataList.Add(chairInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + chairInfo.Id + " in table: TbChair");
			}
		}
	}

	public ChairInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ChairInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ChairInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ChairInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
