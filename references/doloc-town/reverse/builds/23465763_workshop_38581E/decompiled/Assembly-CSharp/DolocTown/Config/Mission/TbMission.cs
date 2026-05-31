using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbMission
{
	private readonly Dictionary<string, MissionInfo> _dataMap;

	private readonly List<MissionInfo> _dataList;

	public Dictionary<string, MissionInfo> DataMap => _dataMap;

	public List<MissionInfo> DataList => _dataList;

	public MissionInfo this[string key] => _dataMap[key];

	public TbMission(JSONNode _json)
	{
		_dataMap = new Dictionary<string, MissionInfo>();
		_dataList = new List<MissionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MissionInfo missionInfo = MissionInfo.DeserializeMissionInfo(child);
			if (_dataMap.TryAdd(missionInfo.Id, missionInfo))
			{
				_dataList.Add(missionInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + missionInfo.Id + " in table: TbMission");
			}
		}
	}

	public MissionInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MissionInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MissionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MissionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
