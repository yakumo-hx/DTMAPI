using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbMapRoom
{
	private readonly Dictionary<string, MapRoomInfo> _dataMap;

	private readonly List<MapRoomInfo> _dataList;

	public Dictionary<string, MapRoomInfo> DataMap => _dataMap;

	public List<MapRoomInfo> DataList => _dataList;

	public MapRoomInfo this[string key] => _dataMap[key];

	public TbMapRoom(JSONNode _json)
	{
		_dataMap = new Dictionary<string, MapRoomInfo>();
		_dataList = new List<MapRoomInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MapRoomInfo mapRoomInfo = MapRoomInfo.DeserializeMapRoomInfo(child);
			if (_dataMap.TryAdd(mapRoomInfo.Id, mapRoomInfo))
			{
				_dataList.Add(mapRoomInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + mapRoomInfo.Id + " in table: TbMapRoom");
			}
		}
	}

	public MapRoomInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MapRoomInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MapRoomInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MapRoomInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
