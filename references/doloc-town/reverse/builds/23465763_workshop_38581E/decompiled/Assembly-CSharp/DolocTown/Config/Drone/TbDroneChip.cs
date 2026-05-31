using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Drone;

public sealed class TbDroneChip
{
	private readonly Dictionary<string, DroneChipInfo> _dataMap;

	private readonly List<DroneChipInfo> _dataList;

	public Dictionary<string, DroneChipInfo> DataMap => _dataMap;

	public List<DroneChipInfo> DataList => _dataList;

	public DroneChipInfo this[string key] => _dataMap[key];

	public TbDroneChip(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DroneChipInfo>();
		_dataList = new List<DroneChipInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DroneChipInfo droneChipInfo = DroneChipInfo.DeserializeDroneChipInfo(child);
			if (_dataMap.TryAdd(droneChipInfo.Id, droneChipInfo))
			{
				_dataList.Add(droneChipInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + droneChipInfo.Id + " in table: TbDroneChip");
			}
		}
	}

	public DroneChipInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DroneChipInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DroneChipInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DroneChipInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
