using System;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class EquipmentRoom : Equipment
{
	[JsonProperty]
	private readonly TemplateRoomInHouse room;

	public EquipmentRoom(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		EquipmentFuncEquipmentRoom equipmentFuncEquipmentRoom = (EquipmentFuncEquipmentRoom)proto.Function;
		if (!DolocAPI.QueryTemplateRoom(equipmentFuncEquipmentRoom.RoomName, out var roomProto))
		{
			Debug.LogError("未知房间：\"" + equipmentFuncEquipmentRoom.RoomName + "\"");
			return;
		}
		if (!roomProto.isInHouse)
		{
			throw new Exception("房间\"" + equipmentFuncEquipmentRoom.RoomName + "\"不是室内房间！");
		}
		room = new TemplateRoomInHouse(Guid.NewGuid().ToString(), (TemplateRoomOutdoor)host, roomProto);
	}

	[JsonConstructor]
	protected EquipmentRoom(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, TemplateRoomInHouse room)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		this.room = room;
	}

	public override void AfterNewGame()
	{
		base.AfterNewGame();
		room.__AfterNewGame();
	}

	public override void AfterLoadEquipment()
	{
		base.AfterLoadEquipment();
		room.__AfterLoadData();
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		base.Renderer.Sprite = null;
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		if (base.Renderer != null)
		{
			base.Renderer.Sprite = proto.Sprite;
		}
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (room != null)
		{
			DolocAPI.EnterRoom(room, room.proto.geometry.DefaultEntryPosition);
		}
	}

	protected override void Update()
	{
		base.Update();
		room.UpdateNoRender();
	}

	protected override void UpdateNoRender()
	{
		base.UpdateNoRender();
		if (DolocAPI.CurrentRoom == room)
		{
			room.Update();
		}
		else
		{
			room.UpdateNoRender();
		}
	}
}
