using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Drone;

public sealed class TbDroneSkill
{
	private readonly Dictionary<string, DroneSkillInfo> _dataMap;

	private readonly List<DroneSkillInfo> _dataList;

	public Dictionary<string, DroneSkillInfo> DataMap => _dataMap;

	public List<DroneSkillInfo> DataList => _dataList;

	public DroneSkillInfo this[string key] => _dataMap[key];

	public TbDroneSkill(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DroneSkillInfo>();
		_dataList = new List<DroneSkillInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DroneSkillInfo droneSkillInfo = DroneSkillInfo.DeserializeDroneSkillInfo(child);
			if (_dataMap.TryAdd(droneSkillInfo.Id, droneSkillInfo))
			{
				_dataList.Add(droneSkillInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + droneSkillInfo.Id + " in table: TbDroneSkill");
			}
		}
	}

	public DroneSkillInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DroneSkillInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DroneSkillInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DroneSkillInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
