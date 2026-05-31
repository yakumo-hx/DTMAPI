using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbMissionDecorator
{
	private readonly Dictionary<string, MissionDecoratorInfo> _dataMap;

	private readonly List<MissionDecoratorInfo> _dataList;

	public Dictionary<string, MissionDecoratorInfo> DataMap => _dataMap;

	public List<MissionDecoratorInfo> DataList => _dataList;

	public MissionDecoratorInfo this[string key] => _dataMap[key];

	public TbMissionDecorator(JSONNode _json)
	{
		_dataMap = new Dictionary<string, MissionDecoratorInfo>();
		_dataList = new List<MissionDecoratorInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MissionDecoratorInfo missionDecoratorInfo = MissionDecoratorInfo.DeserializeMissionDecoratorInfo(child);
			if (_dataMap.TryAdd(missionDecoratorInfo.Id, missionDecoratorInfo))
			{
				_dataList.Add(missionDecoratorInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + missionDecoratorInfo.Id + " in table: TbMissionDecorator");
			}
		}
	}

	public MissionDecoratorInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MissionDecoratorInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MissionDecoratorInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MissionDecoratorInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
