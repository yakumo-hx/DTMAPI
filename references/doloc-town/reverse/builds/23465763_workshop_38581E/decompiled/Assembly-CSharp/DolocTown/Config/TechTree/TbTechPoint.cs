using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.TechTree;

public sealed class TbTechPoint
{
	private readonly Dictionary<TechPointType, TechPointInfo> _dataMap;

	private readonly List<TechPointInfo> _dataList;

	public Dictionary<TechPointType, TechPointInfo> DataMap => _dataMap;

	public List<TechPointInfo> DataList => _dataList;

	public TechPointInfo this[TechPointType key] => _dataMap[key];

	public TbTechPoint(JSONNode _json)
	{
		_dataMap = new Dictionary<TechPointType, TechPointInfo>();
		_dataList = new List<TechPointInfo>();
		foreach (JSONNode child in _json.Children)
		{
			TechPointInfo techPointInfo = TechPointInfo.DeserializeTechPointInfo(child);
			if (_dataMap.TryAdd(techPointInfo.Id, techPointInfo))
			{
				_dataList.Add(techPointInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {techPointInfo.Id} in table: TbTechPoint");
			}
		}
	}

	public TechPointInfo GetOrDefault(TechPointType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TechPointInfo Get(TechPointType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TechPointInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TechPointInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
