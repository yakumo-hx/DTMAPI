using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Time;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class Dungeon
{
	[JsonProperty]
	private DungeonRoom[] rooms;

	private readonly Dictionary<string, ushort> indexMap = new Dictionary<string, ushort>();

	private readonly HashSet<string> currentRenderedRooms = new HashSet<string>();

	[JsonProperty]
	private WeatherSystem weather;

	[JsonProperty]
	private WeatherMapPatch weatherPatch;

	[JsonProperty]
	private int weatherSeed;

	public DungeonProto proto { get; private set; }

	public DungeonRoom entryRoom => GetRoom(proto.entryRoom);

	[JsonProperty]
	public string ProtoName => proto.name;

	[JsonProperty]
	public RoomInteractableObjectManager DM_interactableObject { get; private set; }

	public IEnumerable<DungeonRoom> AllRooms => rooms;

	public IEnumerable<DungeonRoom> AllRenderedRooms => rooms.Where((DungeonRoom x) => currentRenderedRooms.Contains(x.ProtoName));

	public SeasonInfo SeasonInfo => null;

	public WeatherInfo CurrentWeatherInfo => null;

	public Dungeon(DungeonProto proto)
	{
		this.proto = proto;
		DM_interactableObject = new RoomInteractableObjectManager();
		rooms = new DungeonRoom[proto.rooms.Length];
		for (int i = 0; i < rooms.Length; i++)
		{
			rooms[i] = new DungeonRoom(proto.name, proto.rooms[i]);
			indexMap.Add(proto.rooms[i].name, (ushort)i);
			rooms[i].SetInteractableObjectManager(DM_interactableObject);
		}
	}

	[JsonConstructor]
	public Dungeon(string ProtoName, DungeonRoom[] rooms, RoomInteractableObjectManager DM_interactableObject, int weatherSeed = -1, WeatherSystem weather = null, WeatherMapPatch weatherPatch = null)
	{
		if (DolocAPI.assets.dungeons.QueryDungeonProto(ProtoName, out var dungeon))
		{
			proto = dungeon;
			this.DM_interactableObject = DM_interactableObject ?? new RoomInteractableObjectManager();
			RestoreRooms(dungeon.rooms, rooms);
			if (weatherSeed < 0 || weather == null)
			{
				this.weatherSeed = Random.Range(0, int.MaxValue);
				this.weather = new WeatherSystem(WeatherType.NONE);
			}
		}
	}

	private void RestoreRooms(RoomProto[] roomProtos, DungeonRoom[] rooms)
	{
		Dictionary<string, DungeonRoom> dictionary = new Dictionary<string, DungeonRoom>();
		foreach (DungeonRoom dungeonRoom in rooms)
		{
			if (dungeonRoom.proto != null)
			{
				dictionary.Add(dungeonRoom.ProtoName, dungeonRoom);
			}
		}
		indexMap.Clear();
		this.rooms = new DungeonRoom[roomProtos.Length];
		for (int j = 0; j < roomProtos.Length; j++)
		{
			string name = roomProtos[j].name;
			if (dictionary.TryGetValue(name, out var value))
			{
				this.rooms[j] = value;
			}
			else
			{
				this.rooms[j] = new DungeonRoom(proto.name, roomProtos[j]);
			}
			indexMap.Add(name, (ushort)j);
		}
	}

	public int __ReGenDungeonDatas()
	{
		return rooms.Count((DungeonRoom room) => room.ReGenRoomDatas());
	}

	public void AfterNewGame()
	{
		DungeonRoom[] array = rooms;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].__AfterNewGame();
		}
	}

	public void AfterLoadData()
	{
		DungeonRoom[] array = rooms;
		foreach (DungeonRoom obj in array)
		{
			obj.__AfterLoadData();
			obj.SetInteractableObjectManager(DM_interactableObject);
		}
	}

	public void Update()
	{
		DungeonRoom[] array = rooms;
		foreach (DungeonRoom dungeonRoom in array)
		{
			if (currentRenderedRooms.Contains(dungeonRoom.ProtoName))
			{
				dungeonRoom.Update();
			}
			else
			{
				dungeonRoom.UpdateNoRender();
			}
		}
	}

	public void UpdateNoRender()
	{
		DungeonRoom[] array = rooms;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].UpdateNoRender();
		}
	}

	public void UpdatePerHour(int hourNow)
	{
		DungeonRoom[] array = rooms;
		foreach (DungeonRoom dungeonRoom in array)
		{
			if (currentRenderedRooms.Contains(dungeonRoom.ProtoName))
			{
				dungeonRoom.UpdatePerHour(hourNow);
			}
			else
			{
				dungeonRoom.UpdatePerHourNoRender(hourNow);
			}
		}
	}

	public void UpdatePerHourNoRender(int hourNow)
	{
		DungeonRoom[] array = rooms;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].UpdatePerHourNoRender(hourNow);
		}
	}

	public void UnRenderAllRooms()
	{
		foreach (string currentRenderedRoom in currentRenderedRooms)
		{
			if (QueryRoom(currentRenderedRoom, out var room))
			{
				room.ClearRender();
			}
		}
		currentRenderedRooms.Clear();
	}

	public void _ExitCurrentRoom(Room nextRoom)
	{
		if (ContainsRoom(nextRoom))
		{
			((IMonsterHost)nextRoom).RunMonsters();
			return;
		}
		UnRenderAllRooms();
		ResetAllRoomStopTime();
	}

	private void ResetAllRoomStopTime()
	{
		Debug.Log("地牢系统: 重设所有房间的停止时间");
		int totalSeconds = DolocAPI.archiveHandle.timeData.totalSeconds;
		DungeonRoom[] array = rooms;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].StopTime = totalSeconds;
		}
	}

	private bool ContainsRoom(Room nextRoom)
	{
		return rooms.Any((DungeonRoom room) => room == nextRoom);
	}

	public void _RenderNearRooms(Room room)
	{
		Debug.Log("<color=yellow>渲染" + room.RoomId + "附近的房间</color>");
		if (room.Type != RoomType.Dungeon)
		{
			return;
		}
		DungeonRoom dungeonRoom = (DungeonRoom)room;
		HashSet<string> nearRooms = new HashSet<string>(dungeonRoom.proto.neighbourRooms) { room.baseProto.name };
		Queue<string> queue = new Queue<string>();
		foreach (string item in currentRenderedRooms.Where((string roomName) => !nearRooms.Contains(roomName)))
		{
			queue.Enqueue(item);
			if (QueryRoom(item, out var room2))
			{
				room2.ClearRender();
			}
		}
		while (queue.Count > 0)
		{
			currentRenderedRooms.Remove(queue.Dequeue());
		}
		foreach (string item2 in nearRooms)
		{
			if (!QueryRoom(item2, out var room3))
			{
				continue;
			}
			if (!currentRenderedRooms.Add(item2))
			{
				if (room3 != room)
				{
					((IMonsterHost)room3).HideAllMonsters();
				}
			}
			else
			{
				room3.Render(room3 == room);
			}
		}
	}

	public bool QueryBuildingRoom(string guid, out Room room)
	{
		DungeonRoom[] array = rooms;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].QueryBuildingRoom(guid, out room))
			{
				return true;
			}
		}
		room = null;
		return false;
	}

	public bool QueryRoom(string name, out DungeonRoom room)
	{
		if (indexMap.TryGetValue(name, out var value))
		{
			room = rooms[value];
			return true;
		}
		room = null;
		return false;
	}

	public DungeonRoom GetRoom(string name)
	{
		if (indexMap.TryGetValue(name, out var value))
		{
			return rooms[value];
		}
		return null;
	}

	public DungeonRoom GetRoomFromPosition(Vector2 positionWS)
	{
		return rooms.FirstOrDefault((DungeonRoom room) => room.Geometry.Contains(positionWS));
	}

	public DungeonRoom GetRoom(ushort id)
	{
		if (id >= rooms.Length)
		{
			return null;
		}
		return rooms[id];
	}

	public void OnMonthlyRefresh()
	{
		foreach (string currentRenderedRoom in currentRenderedRooms)
		{
			if (QueryRoom(currentRenderedRoom, out var room))
			{
				room.SceneHandle?.OnMonthlyRefresh();
			}
		}
	}
}
