using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DungeonRoom : Room
{
	[JsonProperty]
	public readonly string dungeonProtoName;

	public readonly RoomProto proto;

	public override string RoomId => base.SceneRawName + "." + base.baseProto.name;

	[JsonProperty]
	public string ProtoName => proto.name;

	public Vector2 entryPosition => base.baseProto.geometry.DefaultEntryPosition;

	public override string Title => base.baseProto.name;

	public override RoomType Type => RoomType.Dungeon;

	public DungeonRoom(string dungeonProtoName, RoomProto proto)
		: base(proto)
	{
		this.dungeonProtoName = dungeonProtoName;
		this.proto = proto;
		InitRoom();
	}

	[JsonConstructor]
	public DungeonRoom(string dungeonProtoName, string ProtoName, DropItemManager DM_dropitem, MissionItemManager DM_missionItem, DungeonResourceManager DM_dungeonResource, VegetationManager DM_vegetation, bool blockCreate, PlatformManager DM_platform = null, BuildingManager DM_building = null, EquipmentManager DM_equipment = null, AnimalManager DM_animal = null, int stopTime = 0)
		: base(DM_dropitem, null, DM_missionItem, DM_dungeonResource, DM_vegetation, DM_platform, DM_building, DM_equipment, DM_animal, stopTime, blockCreate)
	{
		if (!DolocAPI.assets.dungeons.QueryDungeonProto(dungeonProtoName, out var dungeon) || !dungeon.QueryRoomProto(ProtoName, out var roomProto))
		{
			Debug.LogError("地牢房间\"" + dungeonProtoName + "." + ProtoName + "\"数据丢失");
		}
		else
		{
			this.dungeonProtoName = dungeonProtoName;
			proto = roomProto;
		}
	}

	protected override RoomProto GetBaseRoomProto()
	{
		return proto;
	}

	public void SetInteractableObjectManager(RoomInteractableObjectManager DM_interactableObject)
	{
		base.DM_interactableObject = DM_interactableObject;
	}

	public override bool ReGenRoomDatas()
	{
		bool num = isRenderNow;
		if (num)
		{
			ClearRender();
		}
		base.DM_monster.Clear();
		base.DM_dropitem.RemoveDisposableItems();
		base.DM_dungeonResource.Clear();
		base.DM_terrain.Clear(TerrainLayerName.Resource);
		((IDungeonResourceHost)this).GenerateDungeonResource_DungeonMode(refreshPresets: true);
		((IMonsterHost)this).GenerateMonstersData();
		((IVegetationHost)this).ReGenRoomVegetation(isRender: false);
		if (num)
		{
			Render();
		}
		return true;
	}

	public override void SetPresetObject(RoomPresetObjectProto preset)
	{
		if (preset.presetType == RoomPresetObjectType.Resource)
		{
			((IDungeonResourceHost)this).CreateResourceFromPreset(preset);
		}
	}
}
