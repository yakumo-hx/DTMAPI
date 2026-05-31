using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbMissionItem
{
	private readonly Dictionary<string, MissionItemInfo> _dataMap;

	private readonly List<MissionItemInfo> _dataList;

	public Dictionary<string, MissionItemInfo> DataMap => _dataMap;

	public List<MissionItemInfo> DataList => _dataList;

	public MissionItemInfo this[string key] => _dataMap[key];

	public TbMissionItem(JSONNode _json)
	{
		_dataMap = new Dictionary<string, MissionItemInfo>();
		_dataList = new List<MissionItemInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MissionItemInfo missionItemInfo = MissionItemInfo.DeserializeMissionItemInfo(child);
			if (_dataMap.TryAdd(missionItemInfo.Id, missionItemInfo))
			{
				_dataList.Add(missionItemInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + missionItemInfo.Id + " in table: TbMissionItem");
			}
		}
	}

	public MissionItemInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MissionItemInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MissionItemInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MissionItemInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
