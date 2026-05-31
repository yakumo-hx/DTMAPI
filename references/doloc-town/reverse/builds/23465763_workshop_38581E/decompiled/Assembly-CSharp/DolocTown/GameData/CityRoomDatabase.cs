using System;
using System.Collections.Generic;
using DolocTown.Config.Room;
using UnityEngine;

namespace DolocTown.GameData;

public class CityRoomDatabase
{
	private readonly Dictionary<string, RoomProto> rooms = new Dictionary<string, RoomProto>();

	private readonly Dictionary<string, RoomProto> roomBySceneName = new Dictionary<string, RoomProto>();

	private CityPath _path;

	public int totalCount => roomBySceneName.Count;

	public IEnumerable<RoomProto> AllRooms => roomBySceneName.Values;

	public void AddData(RoomProto proto)
	{
		SceneInfo sceneInfo = proto.sceneInfo;
		if (!rooms.ContainsKey(proto.sceneInfo.shortName) && !string.IsNullOrEmpty(sceneInfo.shortName) && !string.IsNullOrEmpty(sceneInfo.name) && !rooms.ContainsKey(sceneInfo.shortName) && !roomBySceneName.ContainsKey(sceneInfo.name))
		{
			rooms.Add(sceneInfo.shortName, proto);
			roomBySceneName.Add(sceneInfo.name, proto);
		}
	}

	public bool QueryData(string roomName, out RoomProto proto)
	{
		return rooms.TryGetValue(roomName, out proto);
	}

	public bool QueryDataBySceneName(string sceneName, out RoomProto proto)
	{
		return roomBySceneName.TryGetValue(sceneName, out proto);
	}

	public void BuildPath(Dictionary<string, PortalInfo> portalProtos, Dictionary<string, InwalkableAreaInfo> inwalkableAreaProtos)
	{
		Dictionary<string, List<CityPath.Translator>> dictionary = new Dictionary<string, List<CityPath.Translator>>();
		foreach (PortalInfo value3 in portalProtos.Values)
		{
			if (value3.AvailableToNpc)
			{
				RoomProto value;
				CityPath.Translator item = new CityPath.Translator(value3.Id, value3.SceneRawName, value3.TargetId_Ref.SceneRawName, value3.Position, value3.TargetId_Ref.Position, roomBySceneName.TryGetValue(value3.TargetId_Ref.SceneRawName, out value) && value.isInHouse);
				dictionary.TryAdd(value3.SceneRawName, new List<CityPath.Translator>());
				dictionary[value3.SceneRawName].Add(item);
			}
		}
		Dictionary<string, List<CityPath.InwalkableArea>> dictionary2 = new Dictionary<string, List<CityPath.InwalkableArea>>();
		foreach (InwalkableAreaInfo value4 in inwalkableAreaProtos.Values)
		{
			dictionary2.TryAdd(value4.SceneName, new List<CityPath.InwalkableArea>());
			dictionary2[value4.SceneName].Add(new CityPath.InwalkableArea(value4.Left, value4.Right));
		}
		Dictionary<string, CityPath.CityScene> dictionary3 = new Dictionary<string, CityPath.CityScene>();
		foreach (KeyValuePair<string, List<CityPath.Translator>> item2 in dictionary)
		{
			CityPath.Translator[] translators = item2.Value.ToArray();
			List<CityPath.InwalkableArea> value2;
			CityPath.InwalkableArea[] inwalkableAreas = (dictionary2.TryGetValue(item2.Key, out value2) ? value2.ToArray() : Array.Empty<CityPath.InwalkableArea>());
			dictionary3.Add(item2.Key, new CityPath.CityScene(translators, inwalkableAreas));
		}
		_path = new CityPath(dictionary3);
	}

	public PathAction[] GeneratePathActionsEx(string src, string dest, Vector2 currentPos, Vector2 targetPos)
	{
		return _path.GeneratePathActions(src, dest, currentPos, targetPos);
	}
}
