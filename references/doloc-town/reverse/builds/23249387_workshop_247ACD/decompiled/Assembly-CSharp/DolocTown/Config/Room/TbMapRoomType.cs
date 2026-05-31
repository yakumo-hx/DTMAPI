using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbMapRoomType
{
	private readonly Dictionary<string, MapRoomTypeInfo> _dataMap;

	private readonly List<MapRoomTypeInfo> _dataList;

	public Dictionary<string, MapRoomTypeInfo> DataMap => _dataMap;

	public List<MapRoomTypeInfo> DataList => _dataList;

	public MapRoomTypeInfo this[string key] => _dataMap[key];

	public TbMapRoomType(JSONNode _json)
	{
		_dataMap = new Dictionary<string, MapRoomTypeInfo>();
		_dataList = new List<MapRoomTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MapRoomTypeInfo mapRoomTypeInfo = MapRoomTypeInfo.DeserializeMapRoomTypeInfo(child);
			if (_dataMap.TryAdd(mapRoomTypeInfo.Id, mapRoomTypeInfo))
			{
				_dataList.Add(mapRoomTypeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + mapRoomTypeInfo.Id + " in table: TbMapRoomType");
			}
		}
	}

	public MapRoomTypeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MapRoomTypeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MapRoomTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MapRoomTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
