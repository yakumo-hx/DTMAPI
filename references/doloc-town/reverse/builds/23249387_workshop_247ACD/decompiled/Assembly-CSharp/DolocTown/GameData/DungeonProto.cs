using System.Collections.Generic;

namespace DolocTown.GameData;

public class DungeonProto
{
	public readonly string name;

	public readonly SceneInfo sceneInfo;

	public readonly RoomProto[] rooms;

	public readonly RoomProto entryRoomProto;

	public readonly string entryRoom;

	public readonly EnvBackgroundSO backgroundSO;

	public readonly bool customWeatherSystem;

	private readonly Dictionary<string, int> indexMap;

	public DungeonProto(string name, SceneInfo sceneInfo, RoomProto[] rooms, string entryRoom, EnvBackgroundSO backgroundSO)
	{
		this.name = name;
		this.sceneInfo = sceneInfo;
		this.rooms = rooms;
		this.entryRoom = entryRoom;
		indexMap = new Dictionary<string, int>();
		for (int i = 0; i < rooms.Length; i++)
		{
			indexMap.Add(rooms[i].name, i);
		}
		entryRoomProto = rooms[indexMap[entryRoom]];
		this.backgroundSO = backgroundSO;
	}

	public bool QueryRoomProto(string name, out RoomProto proto)
	{
		if (indexMap.TryGetValue(name, out var value))
		{
			proto = rooms[value];
			return true;
		}
		proto = null;
		return false;
	}
}
