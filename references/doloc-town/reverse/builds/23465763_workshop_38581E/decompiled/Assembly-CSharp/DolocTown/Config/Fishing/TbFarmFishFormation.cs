using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Fishing;

public sealed class TbFarmFishFormation
{
	private readonly Dictionary<string, FarmFishFormationInfo> _dataMap;

	private readonly List<FarmFishFormationInfo> _dataList;

	public Dictionary<string, FarmFishFormationInfo> DataMap => _dataMap;

	public List<FarmFishFormationInfo> DataList => _dataList;

	public FarmFishFormationInfo this[string key] => _dataMap[key];

	public TbFarmFishFormation(JSONNode _json)
	{
		_dataMap = new Dictionary<string, FarmFishFormationInfo>();
		_dataList = new List<FarmFishFormationInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FarmFishFormationInfo farmFishFormationInfo = FarmFishFormationInfo.DeserializeFarmFishFormationInfo(child);
			if (_dataMap.TryAdd(farmFishFormationInfo.Id, farmFishFormationInfo))
			{
				_dataList.Add(farmFishFormationInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + farmFishFormationInfo.Id + " in table: TbFarmFishFormation");
			}
		}
	}

	public FarmFishFormationInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FarmFishFormationInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FarmFishFormationInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FarmFishFormationInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
