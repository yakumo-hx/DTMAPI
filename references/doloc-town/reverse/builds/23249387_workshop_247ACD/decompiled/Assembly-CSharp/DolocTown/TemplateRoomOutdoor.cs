using DolocTown.Config.Tile;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class TemplateRoomOutdoor : TemplateRoom
{
	public TemplateRoomOutdoor(string guid, RoomProto proto)
		: base(guid, proto)
	{
	}

	[JsonConstructor]
	protected TemplateRoomOutdoor(string guid, string ProtoName, int stopTime, BuildingManager DM_building, DungeonResourceManager DM_dungeonResource, PlatformManager DM_platform, EquipmentManager DM_equipment, DropItemManager DM_dropitem, MissionItemManager DM_missionItem = null, AnimalManager DM_animal = null)
		: base(guid, ProtoName, DM_platform, DM_equipment, DM_dropitem, DM_animal, DM_dungeonResource, DM_missionItem, null, null, DM_building, stopTime)
	{
	}

	public override void __AfterNewGame()
	{
		base.__AfterNewGame();
		InitRoom();
		((IDungeonResourceHost)this).SetAllResourceToMaxLevel(useTween: false);
	}

	public override void BeforeTimePass()
	{
		if (DolocAPI.archiveHandle.currentRoom is TemplateRoom templateRoom)
		{
			templateRoom.SceneHandle.BeforeTimePass();
			templateRoom.ClearRender();
			templateRoom.animalSystem.SetCurrentRoom(null);
			templateRoom.DM_automate.SetCurrentRoom(null);
		}
	}

	public override void AfterTimePass()
	{
		if (DolocAPI.archiveHandle.currentRoom is TemplateRoom templateRoom)
		{
			templateRoom.SceneHandle?.AfterTimePass();
			templateRoom.Render();
			templateRoom.DM_automate.SetCurrentRoom(templateRoom);
			templateRoom.animalSystem.SetCurrentRoom(templateRoom);
		}
	}

	public override void OnEnterRoom()
	{
		base.OnEnterRoom();
		base.SceneHandle.AdjustAirWall(base.ScenePosition, base.SceneSize);
		DolocAPI.archiveHandle.InvokeFirstComeoutAfterSleep();
	}

	public override bool GetTileMaterial(Vector2 position, out TileMaterial material)
	{
		position.y -= 0.75f;
		Vector2Int key = base.Geometry.CalcFaceCellPosition(position);
		key += base.Geometry.gridPos;
		if (materialMapBase.ContainsKey(key))
		{
			material = materialMapBase[key];
			return true;
		}
		return materialMapPt.TryGetValue(key, out material);
	}

	public override void SetPresetObject(RoomPresetObjectProto preset)
	{
		base.SetPresetObject(preset);
		if (preset.presetType == RoomPresetObjectType.Building)
		{
			((IBuildingHost)this).CreateBuildingFromPreset(preset);
		}
		else if (preset.presetType == RoomPresetObjectType.Resource)
		{
			((IDungeonResourceHost)this).CreateResourceFromPreset(preset);
		}
	}

	protected override void OnTerrainExtent(Vector2Int offset)
	{
		base.OnTerrainExtent(offset);
		base.proto = base.baseProto;
	}
}
