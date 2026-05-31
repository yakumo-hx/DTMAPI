using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Drone;

public sealed class TbDroneAssist
{
	private readonly Dictionary<string, DroneAssistInfo> _dataMap;

	private readonly List<DroneAssistInfo> _dataList;

	public Dictionary<string, DroneAssistInfo> DataMap => _dataMap;

	public List<DroneAssistInfo> DataList => _dataList;

	public DroneAssistInfo this[string key] => _dataMap[key];

	public TbDroneAssist(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DroneAssistInfo>();
		_dataList = new List<DroneAssistInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DroneAssistInfo droneAssistInfo = DroneAssistInfo.DeserializeDroneAssistInfo(child);
			if (_dataMap.TryAdd(droneAssistInfo.Id, droneAssistInfo))
			{
				_dataList.Add(droneAssistInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + droneAssistInfo.Id + " in table: TbDroneAssist");
			}
		}
	}

	public DroneAssistInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DroneAssistInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DroneAssistInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DroneAssistInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
