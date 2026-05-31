using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Drone;

public sealed class TbDroneSlot
{
	private readonly Dictionary<ComponentType, DroneSlotInfo> _dataMap;

	private readonly List<DroneSlotInfo> _dataList;

	public Dictionary<ComponentType, DroneSlotInfo> DataMap => _dataMap;

	public List<DroneSlotInfo> DataList => _dataList;

	public DroneSlotInfo this[ComponentType key] => _dataMap[key];

	public TbDroneSlot(JSONNode _json)
	{
		_dataMap = new Dictionary<ComponentType, DroneSlotInfo>();
		_dataList = new List<DroneSlotInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DroneSlotInfo droneSlotInfo = DroneSlotInfo.DeserializeDroneSlotInfo(child);
			if (_dataMap.TryAdd(droneSlotInfo.Id, droneSlotInfo))
			{
				_dataList.Add(droneSlotInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {droneSlotInfo.Id} in table: TbDroneSlot");
			}
		}
	}

	public DroneSlotInfo GetOrDefault(ComponentType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DroneSlotInfo Get(ComponentType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DroneSlotInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DroneSlotInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
