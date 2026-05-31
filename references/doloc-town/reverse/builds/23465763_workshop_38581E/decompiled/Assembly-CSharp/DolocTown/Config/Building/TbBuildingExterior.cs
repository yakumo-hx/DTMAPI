using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Building;

public sealed class TbBuildingExterior
{
	private readonly Dictionary<string, BuildingExteriorInfo> _dataMap;

	private readonly List<BuildingExteriorInfo> _dataList;

	public Dictionary<string, BuildingExteriorInfo> DataMap => _dataMap;

	public List<BuildingExteriorInfo> DataList => _dataList;

	public BuildingExteriorInfo this[string key] => _dataMap[key];

	public TbBuildingExterior(JSONNode _json)
	{
		_dataMap = new Dictionary<string, BuildingExteriorInfo>();
		_dataList = new List<BuildingExteriorInfo>();
		foreach (JSONNode child in _json.Children)
		{
			BuildingExteriorInfo buildingExteriorInfo = BuildingExteriorInfo.DeserializeBuildingExteriorInfo(child);
			if (_dataMap.TryAdd(buildingExteriorInfo.Id, buildingExteriorInfo))
			{
				_dataList.Add(buildingExteriorInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + buildingExteriorInfo.Id + " in table: TbBuildingExterior");
			}
		}
	}

	public BuildingExteriorInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public BuildingExteriorInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BuildingExteriorInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BuildingExteriorInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
