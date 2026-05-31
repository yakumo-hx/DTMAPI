using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbFactionMission
{
	private readonly Dictionary<string, FactionMissionInfo> _dataMap;

	private readonly List<FactionMissionInfo> _dataList;

	public Dictionary<string, FactionMissionInfo> DataMap => _dataMap;

	public List<FactionMissionInfo> DataList => _dataList;

	public FactionMissionInfo this[string key] => _dataMap[key];

	public TbFactionMission(JSONNode _json)
	{
		_dataMap = new Dictionary<string, FactionMissionInfo>();
		_dataList = new List<FactionMissionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FactionMissionInfo factionMissionInfo = FactionMissionInfo.DeserializeFactionMissionInfo(child);
			if (_dataMap.TryAdd(factionMissionInfo.Id, factionMissionInfo))
			{
				_dataList.Add(factionMissionInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + factionMissionInfo.Id + " in table: TbFactionMission");
			}
		}
	}

	public FactionMissionInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FactionMissionInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FactionMissionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FactionMissionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
