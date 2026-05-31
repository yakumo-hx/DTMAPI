using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbPositionType
{
	private readonly Dictionary<MapMissionTipType, PositionTypeInfo> _dataMap;

	private readonly List<PositionTypeInfo> _dataList;

	public Dictionary<MapMissionTipType, PositionTypeInfo> DataMap => _dataMap;

	public List<PositionTypeInfo> DataList => _dataList;

	public PositionTypeInfo this[MapMissionTipType key] => _dataMap[key];

	public TbPositionType(JSONNode _json)
	{
		_dataMap = new Dictionary<MapMissionTipType, PositionTypeInfo>();
		_dataList = new List<PositionTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			PositionTypeInfo positionTypeInfo = PositionTypeInfo.DeserializePositionTypeInfo(child);
			if (_dataMap.TryAdd(positionTypeInfo.Id, positionTypeInfo))
			{
				_dataList.Add(positionTypeInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {positionTypeInfo.Id} in table: TbPositionType");
			}
		}
	}

	public PositionTypeInfo GetOrDefault(MapMissionTipType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public PositionTypeInfo Get(MapMissionTipType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (PositionTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (PositionTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
