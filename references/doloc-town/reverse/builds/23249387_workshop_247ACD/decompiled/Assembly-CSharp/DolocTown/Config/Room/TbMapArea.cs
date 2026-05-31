using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbMapArea
{
	private readonly List<MapAreaInfo> _dataList;

	private Dictionary<(string, string), MapAreaInfo> _dataMapUnion;

	private Dictionary<string, MapAreaInfo> roomId2RoomAreaDict = new Dictionary<string, MapAreaInfo>();

	private Dictionary<string, List<MapAreaInfo>> mapId2RoomAreaDict = new Dictionary<string, List<MapAreaInfo>>();

	private Dictionary<string, List<MapAreaInfo>> roomId2HiddenAreaDict = new Dictionary<string, List<MapAreaInfo>>();

	private Dictionary<string, List<MapAreaInfo>> mapId2HiddenAreaDict = new Dictionary<string, List<MapAreaInfo>>();

	public List<MapAreaInfo> DataList => _dataList;

	public TbMapArea(JSONNode _json)
	{
		_dataList = new List<MapAreaInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MapAreaInfo item = MapAreaInfo.DeserializeMapAreaInfo(child);
			_dataList.Add(item);
		}
		_dataMapUnion = new Dictionary<(string, string), MapAreaInfo>();
		List<MapAreaInfo> list = new List<MapAreaInfo>();
		foreach (MapAreaInfo data in _dataList)
		{
			if (!_dataMapUnion.TryAdd((data.MapId, data.AreaId), data))
			{
				list.Add(data);
				Debug.LogError($"[Config] Duplicate key: {(data.MapId, data.AreaId)} in table: TbMapArea");
			}
		}
		foreach (MapAreaInfo item2 in list)
		{
			_dataList.Remove(item2);
		}
	}

	public MapAreaInfo Get(string map_id, string area_id)
	{
		if (!_dataMapUnion.TryGetValue((map_id, area_id), out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MapAreaInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MapAreaInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	private void PostResolve()
	{
		foreach (MapAreaInfo data in _dataList)
		{
			if (data.MapAreaType == MapAreaType.Room)
			{
				mapId2RoomAreaDict.TryAdd(data.MapId, new List<MapAreaInfo>());
				mapId2RoomAreaDict[data.MapId].Add(data);
				roomId2RoomAreaDict[data.RoomName] = data;
			}
			else if (data.MapAreaType == MapAreaType.HiddenArea)
			{
				string[] coveredRooms = data.CoveredRooms;
				foreach (string key in coveredRooms)
				{
					mapId2HiddenAreaDict.TryAdd(data.MapId, new List<MapAreaInfo>());
					mapId2HiddenAreaDict[data.MapId].Add(data);
					roomId2HiddenAreaDict.TryAdd(key, new List<MapAreaInfo>());
					roomId2HiddenAreaDict[key].Add(data);
				}
			}
		}
	}

	public MapAreaInfo GetRoomAreaByRoomId(string roomId)
	{
		return roomId2RoomAreaDict.GetValueOrDefault(roomId);
	}

	public List<MapAreaInfo> GetRoomAreasByMapId(string mapId)
	{
		return mapId2RoomAreaDict.GetValueOrDefault(mapId) ?? new List<MapAreaInfo>();
	}

	public List<MapAreaInfo> GetHiddenAreaByRoomId(string roomId)
	{
		return roomId2HiddenAreaDict.GetValueOrDefault(roomId) ?? new List<MapAreaInfo>();
	}

	public List<MapAreaInfo> GetHiddenAreaByMapId(string roomId)
	{
		return mapId2HiddenAreaDict.GetValueOrDefault(roomId) ?? new List<MapAreaInfo>();
	}
}
