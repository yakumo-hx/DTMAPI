using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Drone;

public sealed class TbDroneStructure
{
	private readonly Dictionary<string, DroneStructureInfo> _dataMap;

	private readonly List<DroneStructureInfo> _dataList;

	public Dictionary<string, DroneStructureInfo> DataMap => _dataMap;

	public List<DroneStructureInfo> DataList => _dataList;

	public DroneStructureInfo this[string key] => _dataMap[key];

	public TbDroneStructure(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DroneStructureInfo>();
		_dataList = new List<DroneStructureInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DroneStructureInfo droneStructureInfo = DroneStructureInfo.DeserializeDroneStructureInfo(child);
			if (_dataMap.TryAdd(droneStructureInfo.Id, droneStructureInfo))
			{
				_dataList.Add(droneStructureInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + droneStructureInfo.Id + " in table: TbDroneStructure");
			}
		}
	}

	public DroneStructureInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DroneStructureInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DroneStructureInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DroneStructureInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
