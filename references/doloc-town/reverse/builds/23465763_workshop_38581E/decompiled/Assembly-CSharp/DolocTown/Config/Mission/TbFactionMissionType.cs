using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbFactionMissionType
{
	private readonly Dictionary<FactionMissionType, FactionMissionTypeInfo> _dataMap;

	private readonly List<FactionMissionTypeInfo> _dataList;

	public Dictionary<FactionMissionType, FactionMissionTypeInfo> DataMap => _dataMap;

	public List<FactionMissionTypeInfo> DataList => _dataList;

	public FactionMissionTypeInfo this[FactionMissionType key] => _dataMap[key];

	public TbFactionMissionType(JSONNode _json)
	{
		_dataMap = new Dictionary<FactionMissionType, FactionMissionTypeInfo>();
		_dataList = new List<FactionMissionTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FactionMissionTypeInfo factionMissionTypeInfo = FactionMissionTypeInfo.DeserializeFactionMissionTypeInfo(child);
			if (_dataMap.TryAdd(factionMissionTypeInfo.Id, factionMissionTypeInfo))
			{
				_dataList.Add(factionMissionTypeInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {factionMissionTypeInfo.Id} in table: TbFactionMissionType");
			}
		}
	}

	public FactionMissionTypeInfo GetOrDefault(FactionMissionType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FactionMissionTypeInfo Get(FactionMissionType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FactionMissionTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FactionMissionTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
