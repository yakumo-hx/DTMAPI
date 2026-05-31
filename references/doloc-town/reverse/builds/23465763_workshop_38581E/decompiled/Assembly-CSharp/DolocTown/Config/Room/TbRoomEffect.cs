using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbRoomEffect
{
	private readonly Dictionary<string, RoomEffectInfo> _dataMap;

	private readonly List<RoomEffectInfo> _dataList;

	public Dictionary<string, RoomEffectInfo> DataMap => _dataMap;

	public List<RoomEffectInfo> DataList => _dataList;

	public RoomEffectInfo this[string key] => _dataMap[key];

	public TbRoomEffect(JSONNode _json)
	{
		_dataMap = new Dictionary<string, RoomEffectInfo>();
		_dataList = new List<RoomEffectInfo>();
		foreach (JSONNode child in _json.Children)
		{
			RoomEffectInfo roomEffectInfo = RoomEffectInfo.DeserializeRoomEffectInfo(child);
			if (_dataMap.TryAdd(roomEffectInfo.Id, roomEffectInfo))
			{
				_dataList.Add(roomEffectInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + roomEffectInfo.Id + " in table: TbRoomEffect");
			}
		}
	}

	public RoomEffectInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public RoomEffectInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (RoomEffectInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (RoomEffectInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
