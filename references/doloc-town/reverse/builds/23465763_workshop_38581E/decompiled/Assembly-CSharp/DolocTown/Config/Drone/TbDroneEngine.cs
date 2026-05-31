using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Drone;

public sealed class TbDroneEngine
{
	private readonly Dictionary<string, DroneEngineInfo> _dataMap;

	private readonly List<DroneEngineInfo> _dataList;

	public Dictionary<string, DroneEngineInfo> DataMap => _dataMap;

	public List<DroneEngineInfo> DataList => _dataList;

	public DroneEngineInfo this[string key] => _dataMap[key];

	public TbDroneEngine(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DroneEngineInfo>();
		_dataList = new List<DroneEngineInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DroneEngineInfo droneEngineInfo = DroneEngineInfo.DeserializeDroneEngineInfo(child);
			if (_dataMap.TryAdd(droneEngineInfo.Id, droneEngineInfo))
			{
				_dataList.Add(droneEngineInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + droneEngineInfo.Id + " in table: TbDroneEngine");
			}
		}
	}

	public DroneEngineInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DroneEngineInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DroneEngineInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DroneEngineInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
