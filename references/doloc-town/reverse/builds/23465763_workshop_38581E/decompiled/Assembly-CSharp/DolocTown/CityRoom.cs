using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class CityRoom : Room
{
	public readonly RoomProto proto;

	public override string Title => proto.sceneInfo.name;

	public override RoomType Type => RoomType.City;

	public override string RoomId => base.SceneRawName;

	[JsonProperty]
	public string ProtoName => proto.name;

	public CityRoom(RoomProto proto)
		: base(proto)
	{
		this.proto = proto;
		base.DM_interactableObject = new RoomInteractableObjectManager();
	}

	[JsonConstructor]
	public CityRoom(string ProtoName, int stopTime, MissionItemManager DM_missionItem, DropItemManager DM_dropitem, RoomInteractableObjectManager DM_interactableObject = null, DungeonResourceManager DM_dungeonResource = null, VegetationManager DM_vegetation = null, PlatformManager DM_platform = null, BuildingManager DM_building = null, EquipmentManager DM_equipment = null, AnimalManager DM_animal = null, bool blockCreate = false)
		: base(DM_dropitem, DM_interactableObject, DM_missionItem, DM_dungeonResource, DM_vegetation, DM_platform, DM_building, DM_equipment, DM_animal, stopTime, blockCreate)
	{
		if (!DolocAPI.assets.cityRooms.QueryData(ProtoName, out var roomProto))
		{
			Debug.LogError("城镇房间\"" + ProtoName + "\"数据丢失");
		}
		else
		{
			proto = roomProto;
		}
	}

	protected override RoomProto GetBaseRoomProto()
	{
		return proto;
	}
}
