using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public abstract class TemplateRoom : Room
{
	[JsonProperty]
	private readonly string guid;

	public override string Title => guid;

	public override string RoomId
	{
		get
		{
			if (!guid.IsNullOrEmpty())
			{
				return base.SceneRawName + "." + guid;
			}
			return base.SceneRawName;
		}
	}

	public override RoomType Type => RoomType.Farm;

	public RoomProto proto { get; protected set; }

	[JsonProperty]
	protected string ProtoName => base.baseProto.name;

	public override float GrowthAddition => RoomEffectInfo.GrowthAddition;

	public override float GrowthAdditionFungus => RoomEffectInfo.GrowthAdditionFungus;

	protected TemplateRoom(string guid, RoomProto proto)
		: base(proto)
	{
		this.guid = guid;
		this.proto = proto;
		base.DM_interactableObject = new RoomInteractableObjectManager();
	}

	[JsonConstructor]
	protected TemplateRoom(string guid, string ProtoName, PlatformManager DM_platform, EquipmentManager DM_equipment, DropItemManager DM_dropitem, AnimalManager DM_animal = null, DungeonResourceManager DM_dungeonResource = null, MissionItemManager DM_missionItem = null, RoomInteractableObjectManager DM_interactableObject = null, VegetationManager DM_vegetation = null, BuildingManager DM_building = null, int stopTime = 0, bool blockCreate = false)
		: base(DM_dropitem, DM_interactableObject, DM_missionItem, DM_dungeonResource, DM_vegetation, DM_platform, DM_building, DM_equipment, DM_animal, stopTime, blockCreate)
	{
		this.guid = guid;
		if (!DolocAPI.assets.rooms.QueryData(ProtoName, out var data))
		{
			Debug.LogError("查询模板房间原型失败: " + ProtoName);
		}
		proto = data;
	}

	protected override RoomProto GetBaseRoomProto()
	{
		return proto;
	}

	public override void OnEnterRoom()
	{
		base.OnEnterRoom();
		base.DM_automate.SetCurrentRoom(this);
		base.animalSystem.SetCurrentRoom(this);
	}

	public override void OnExitRoom(Room nextRoom)
	{
		if (!(nextRoom is TemplateRoom))
		{
			base.DM_automate.SetCurrentRoom(null);
			base.animalSystem.SetCurrentRoom(null);
		}
		base.OnExitRoom(nextRoom);
	}

	public override void SetPresetObject(RoomPresetObjectProto preset)
	{
		if (preset.presetType == RoomPresetObjectType.Equipment)
		{
			if (((IEquipmentHost)this).CreateEquipmentFromPreset(preset, turn: false) is SimpleWell simpleWell)
			{
				simpleWell.DrawMax();
			}
		}
		else if (preset.presetType == RoomPresetObjectType.Platform)
		{
			((IPlatformHost)this).CreatePlatformFromPreset(preset);
		}
	}
}
