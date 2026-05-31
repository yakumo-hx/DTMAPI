using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public class MotorDataManager
{
	private Vector2 initPosition;

	[JsonProperty]
	public bool isUnlocked { get; private set; }

	[JsonProperty]
	public string roomId { get; private set; }

	[JsonProperty]
	public string dungeonName { get; private set; }

	[JsonProperty]
	public Vector2 position => DolocAPI.Motor.position2d;

	public Room CurrentRoom { get; private set; }

	public MotorDataManager()
	{
		isUnlocked = false;
		roomId = null;
	}

	[JsonConstructor]
	public MotorDataManager(bool isUnlocked, string roomId, string dungeonName, Vector2 position)
	{
		this.isUnlocked = isUnlocked;
		this.roomId = roomId;
		this.dungeonName = dungeonName;
		initPosition = position;
	}

	public void UnlockMotor()
	{
		isUnlocked = true;
	}

	private static string GetDungeonId(Room room)
	{
		if (room.Type != RoomType.Dungeon)
		{
			return null;
		}
		if (!DolocAPI.assets.dungeons.QueryDungeonProto(room.SceneShortName, out var dungeon))
		{
			return null;
		}
		return dungeon.name;
	}

	public void UpdateMotorRoom(Room room)
	{
		CurrentRoom = room;
		roomId = room?.RoomId;
		dungeonName = GetDungeonId(room);
	}

	public void OnExitRoom(Room room)
	{
		if (isUnlocked && room == CurrentRoom && !DolocAPI.gameStateManager.agentController.IsRidingNow)
		{
			DolocAPI.Motor.SetVisible(value: false);
		}
	}

	public void OnEnterRoom(Room room)
	{
		if (!isUnlocked)
		{
			return;
		}
		if (DolocAPI.gameStateManager.agentController.IsRidingNow)
		{
			UpdateMotorRoom(room);
			return;
		}
		MotorController motor = DolocAPI.Motor;
		if (room != CurrentRoom)
		{
			if (DolocAPI.IsInSameDungeon(room, CurrentRoom))
			{
				motor.SetVisible(value: true);
				motor.motorInteractable.SetVisible(value: false);
			}
			else
			{
				motor.SetVisible(value: false);
			}
		}
		else
		{
			motor.SetVisible(value: true);
			motor.motorInteractable.SetVisible(value: true);
			motor.position2d = position;
		}
	}

	public void AfterLoadData()
	{
		if (!isUnlocked)
		{
			roomId = null;
			CurrentRoom = null;
			return;
		}
		DolocAPI.QueryRoom(roomId, out var room);
		CurrentRoom = room;
		DolocAPI.Motor.position = initPosition;
		OnEnterRoom(CurrentRoom);
	}
}
