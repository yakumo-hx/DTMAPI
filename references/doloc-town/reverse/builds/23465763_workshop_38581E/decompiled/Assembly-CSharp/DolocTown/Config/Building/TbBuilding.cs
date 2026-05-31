using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Building;

public sealed class TbBuilding
{
	private readonly Dictionary<string, BuildingInfo> _dataMap;

	private readonly List<BuildingInfo> _dataList;

	public Dictionary<string, BuildingInfo> DataMap => _dataMap;

	public List<BuildingInfo> DataList => _dataList;

	public BuildingInfo this[string key] => _dataMap[key];

	public TbBuilding(JSONNode _json)
	{
		_dataMap = new Dictionary<string, BuildingInfo>();
		_dataList = new List<BuildingInfo>();
		foreach (JSONNode child in _json.Children)
		{
			BuildingInfo buildingInfo = BuildingInfo.DeserializeBuildingInfo(child);
			if (_dataMap.TryAdd(buildingInfo.Id, buildingInfo))
			{
				_dataList.Add(buildingInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + buildingInfo.Id + " in table: TbBuilding");
			}
		}
	}

	public BuildingInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public BuildingInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BuildingInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BuildingInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
