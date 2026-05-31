using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbRoom
{
	private readonly Dictionary<string, RoomInfo> _dataMap;

	private readonly List<RoomInfo> _dataList;

	public Dictionary<string, RoomInfo> DataMap => _dataMap;

	public List<RoomInfo> DataList => _dataList;

	public RoomInfo this[string key] => _dataMap[key];

	public TbRoom(JSONNode _json)
	{
		_dataMap = new Dictionary<string, RoomInfo>();
		_dataList = new List<RoomInfo>();
		foreach (JSONNode child in _json.Children)
		{
			RoomInfo roomInfo = RoomInfo.DeserializeRoomInfo(child);
			if (_dataMap.TryAdd(roomInfo.Id, roomInfo))
			{
				_dataList.Add(roomInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + roomInfo.Id + " in table: TbRoom");
			}
		}
	}

	public RoomInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public RoomInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (RoomInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (RoomInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
