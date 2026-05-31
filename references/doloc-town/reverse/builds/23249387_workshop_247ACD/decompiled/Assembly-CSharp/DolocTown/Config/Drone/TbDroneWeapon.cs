using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Drone;

public sealed class TbDroneWeapon
{
	private readonly Dictionary<string, DroneWeaponInfo> _dataMap;

	private readonly List<DroneWeaponInfo> _dataList;

	public Dictionary<string, DroneWeaponInfo> DataMap => _dataMap;

	public List<DroneWeaponInfo> DataList => _dataList;

	public DroneWeaponInfo this[string key] => _dataMap[key];

	public TbDroneWeapon(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DroneWeaponInfo>();
		_dataList = new List<DroneWeaponInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DroneWeaponInfo droneWeaponInfo = DroneWeaponInfo.DeserializeDroneWeaponInfo(child);
			if (_dataMap.TryAdd(droneWeaponInfo.Id, droneWeaponInfo))
			{
				_dataList.Add(droneWeaponInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + droneWeaponInfo.Id + " in table: TbDroneWeapon");
			}
		}
	}

	public DroneWeaponInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DroneWeaponInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DroneWeaponInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DroneWeaponInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
