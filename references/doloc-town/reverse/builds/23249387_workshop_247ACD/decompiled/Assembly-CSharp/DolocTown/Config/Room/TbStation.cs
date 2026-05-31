using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbStation
{
	private readonly Dictionary<string, StationInfo> _dataMap;

	private readonly List<StationInfo> _dataList;

	public Dictionary<string, StationInfo> DataMap => _dataMap;

	public List<StationInfo> DataList => _dataList;

	public StationInfo this[string key] => _dataMap[key];

	public TbStation(JSONNode _json)
	{
		_dataMap = new Dictionary<string, StationInfo>();
		_dataList = new List<StationInfo>();
		foreach (JSONNode child in _json.Children)
		{
			StationInfo stationInfo = StationInfo.DeserializeStationInfo(child);
			if (_dataMap.TryAdd(stationInfo.Id, stationInfo))
			{
				_dataList.Add(stationInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + stationInfo.Id + " in table: TbStation");
			}
		}
	}

	public StationInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public StationInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (StationInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (StationInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
