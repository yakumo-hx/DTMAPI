using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbMissionType
{
	private readonly Dictionary<string, MissionTypeInfo> _dataMap;

	private readonly List<MissionTypeInfo> _dataList;

	public Dictionary<string, MissionTypeInfo> DataMap => _dataMap;

	public List<MissionTypeInfo> DataList => _dataList;

	public MissionTypeInfo this[string key] => _dataMap[key];

	public TbMissionType(JSONNode _json)
	{
		_dataMap = new Dictionary<string, MissionTypeInfo>();
		_dataList = new List<MissionTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MissionTypeInfo missionTypeInfo = MissionTypeInfo.DeserializeMissionTypeInfo(child);
			if (_dataMap.TryAdd(missionTypeInfo.Id, missionTypeInfo))
			{
				_dataList.Add(missionTypeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + missionTypeInfo.Id + " in table: TbMissionType");
			}
		}
	}

	public MissionTypeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MissionTypeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MissionTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MissionTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
