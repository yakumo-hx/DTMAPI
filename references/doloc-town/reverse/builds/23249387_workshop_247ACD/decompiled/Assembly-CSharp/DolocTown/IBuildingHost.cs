using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Building;
using DolocTown.GameData;
using DolocTown.Patch;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public interface IBuildingHost : IBaseHost
{
	[JsonProperty]
	BuildingManager DM_building { get; }

	TilemapGroupManager RM_supportTilemaps => DolocAPI.farmRenderer.RM_supportTilemaps;

	Building CreateBuildingNoRender(BuildingInfo proto, Vector2Int anchor, Vector3 wp)
	{
		if (DM_building.CreateBuilding(CurrentRoom, anchor, wp, proto, out var output))
		{
			DM_terrain.FillContent(output);
			RoomPresetObjectProto[] presetObjects = output.room.baseProto.presetObjects;
			foreach (RoomPresetObjectProto roomPresetObjectProto in presetObjects)
			{
				if (roomPresetObjectProto.presetType == RoomPresetObjectType.Equipment && roomPresetObjectProto.autoAttach)
				{
					output.room.SetPresetObject(roomPresetObjectProto);
				}
			}
			return output;
		}
		return null;
	}

	Building CreateBuilding(BuildingInfo proto, Vector2Int anchor, Vector3 wp)
	{
		Building building = CreateBuildingNoRender(proto, anchor, wp);
		if (building != null)
		{
			Render(building);
			UpdateSupportAroundBuilding(building);
		}
		return building;
	}

	Building CreateBuildingFromDirty(Building dirty, Vector2Int anchor, Vector3 wp)
	{
		Building building = DM_building.CreateBuildingFromDirty(CurrentRoom, anchor, wp, dirty);
		if (building != null)
		{
			DM_terrain.FillContent(building);
			Render(building);
			UpdateSupportAroundBuilding(building);
		}
		return building;
	}

	BuildingSupport GenerateBuildingSupport(Building building)
	{
		return GenerateBuildingSupport(building.proto, building.Anchor);
	}

	BuildingSupport GenerateBuildingSupport(BuildingInfo proto, Vector2Int anchor)
	{
		Vector2Int[] positionsOfType = proto.GetPositionsOfType(anchor, BuildingTileType.Floor, BuildingTileType.Side, Vector2Int.down);
		List<Vector2Int> list = new List<Vector2Int>();
		Vector2Int[] array = positionsOfType;
		foreach (Vector2Int vector2Int in array)
		{
			if (DM_terrain.IsFilled(vector2Int, TerrainLayerName.Ground | TerrainLayerName.CeilingFront))
			{
				list.Add(vector2Int);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		FulcrumData[] fulcrumDatas = proto.FulcrumDatas;
		List<int> list2 = new List<int>();
		FulcrumData[] array2 = fulcrumDatas;
		for (int i = 0; i < array2.Length; i++)
		{
			FulcrumData fulcrumData = array2[i];
			for (int j = 0; j < fulcrumData.Width; j++)
			{
				Vector2Int vector2Int2 = fulcrumData.Offsets[j];
				TerrainLayerName layerMask = fulcrumData.RaycastLayerMasks[j];
				Vector2Int vector2Int3 = anchor + vector2Int2 + Vector2Int.down;
				if (list.Contains(vector2Int3) || DM_terrain.IsFilled(vector2Int3, TerrainLayerName.Ground | TerrainLayerName.CeilingFront))
				{
					list2.Add(0);
					continue;
				}
				int item = 0;
				for (int k = 1; k < (int)DolocAPI.GlobalParameter.BuilderRaycastDistance; k++)
				{
					Vector2Int pos = new Vector2Int(vector2Int3.x, vector2Int3.y - k);
					_ = pos.x;
					_ = 8;
					if (DM_terrain.IsFilled(pos, layerMask))
					{
						item = (CurrentRoom.Geometry.IsValidPositionY(pos.y) ? k : (DolocAPI.GlobalParameter.MaxSupportHeight + 3));
						break;
					}
				}
				list2.Add(item);
			}
		}
		return new BuildingSupport(proto, anchor, list2.ToArray(), list.ToArray(), GetBuildingTileMaxDepth);
	}

	void UpdateSupportAroundBuilding(Building building)
	{
		foreach (Building item in GetBuildingsMaySupportedByBuilding(building))
		{
			item.buildingSupport = GenerateBuildingSupport(item);
		}
		RenderAllSupports();
	}

	IEnumerable<Building> GetBuildingsMaySupportedByBuilding(Building building)
	{
		int x = building.proto.CoverSize.x;
		int y = Mathf.CeilToInt(DolocAPI.GlobalParameter.MaxSupportHeight + building.proto.CoverSize.y + 1);
		return DM_terrain.GetContentsFromArea<Building>(building.Anchor, new Vector2Int(x, y));
	}

	void RenderAllSupports()
	{
		RM_supportTilemaps.transform.position = CurrentRoom.RoomPosition;
		RM_supportTilemaps.ClearAllTiles();
		foreach (Building building in DM_building.Buildings)
		{
			building.buildingSupport?.Render(RM_supportTilemaps);
		}
	}

	int GetBuildingTileMaxDepth(Vector2Int pos)
	{
		if (DM_terrain.IsFilled(pos, TerrainLayerName.BuildingFront))
		{
			return 0;
		}
		return GetBuilding(pos)?.proto.MaxDepth ?? 0;
	}

	Building CreateBuildingFromPreset(RoomPresetObjectProto preset)
	{
		if (!DolocAPI.QueryBuilding(preset.name, out var proto))
		{
			Debug.LogWarning("无法找到名为" + preset.name + "的建筑预设");
			return null;
		}
		Vector2 vector = (preset.pos + new Vector2((float)proto.CoverSize.x * 0.5f, 0f)) * 1.5f + CurrentRoom.RoomPosition;
		Building building = CreateBuildingNoRender(proto, preset.pos, vector);
		building.room.InitRoom();
		return building;
	}

	bool RemoveBuilding(Building building, bool retrieveItem = true, bool putInBackpack = false)
	{
		if (DM_building.RemoveBuilding(building))
		{
			DM_terrain.RemoveContent(building);
			building.OnRemove();
			if (retrieveItem)
			{
				building.RetrieveItemOnRemoval(putInBackpack);
			}
			DolocAPI.EntitySystem.Recycle(building.Renderer);
			UpdateSupportAroundBuilding(building);
			RendererAllColliders();
			return true;
		}
		return false;
	}

	bool CanRemoveBuilding(Building building)
	{
		DM_terrain.RemoveContent(building);
		IEnumerable<Building> buildingsMaySupportedByBuilding = GetBuildingsMaySupportedByBuilding(building);
		bool flag = true;
		foreach (Building item in buildingsMaySupportedByBuilding)
		{
			if (item != building)
			{
				BuildingSupport buildingSupport = GenerateBuildingSupport(item);
				flag = flag && buildingSupport != null;
				if (!flag)
				{
					break;
				}
				flag &= buildingSupport.ColumnHeights.Max() <= DolocAPI.GlobalParameter.MaxSupportHeight;
				if (!flag)
				{
					break;
				}
				int num = item.proto.GetPositionsOfType(Vector2Int.zero, BuildingTileType.Floor, BuildingTileType.Side).Length;
				float num2 = (float)buildingSupport.ContactPositions.Length / (float)num;
				flag &= num2 >= DolocAPI.GlobalParameter.MinFloorCoveredRatio;
				if (!flag)
				{
					break;
				}
			}
		}
		DM_terrain.FillContent(building);
		return flag;
	}

	Building GetBuilding(Vector2Int position)
	{
		if (DM_terrain.QueryContent<Building>(position, TerrainLayerName.BuildingNoOverlay, out var cnt))
		{
			return cnt;
		}
		if (DM_terrain.QueryContent<Building>(position, TerrainLayerName.Building, out cnt))
		{
			return cnt;
		}
		return null;
	}

	Building GetBuildingByDoor(Vector2Int position)
	{
		if (DM_terrain.QueryContent<Building>(position, TerrainLayerName.Door, out var cnt))
		{
			return cnt;
		}
		return null;
	}

	Building GetBuilding(string roomGUID)
	{
		if (DM_building.QueryBuildingByRoomTitle(roomGUID, out var building))
		{
			return building;
		}
		return null;
	}

	void RenderAllBuildings()
	{
		DolocAPI.EntitySystem.SetupAll<BuildingRenderer, Building>(DM_building.Buildings, RenderBuilding);
		RenderAllSupports();
		RendererAllColliders();
	}

	void HideAllBuildings()
	{
		foreach (Building building in DM_building.Buildings)
		{
			DolocAPI.EntitySystem.Recycle(building.Renderer);
		}
		RM_supportTilemaps.ClearAllTiles();
		DolocAPI.EntitySystem.Clear<OnewayColliderRenderer>();
	}

	bool MoveBuilding(Building building, Vector2Int anchor, Vector3 wp)
	{
		if (building == null)
		{
			return false;
		}
		DM_terrain.RemoveContent(building);
		building.MoveTerrainContent(anchor, wp);
		DM_terrain.FillContent(building);
		building.Renderer.SetVisible(value: true);
		building.Renderer.position = building.Position;
		building.Renderer.spriteRenderer.sortingOrder = building.Anchor.x;
		building.OnMove();
		UpdateSupportAroundBuilding(building);
		RendererAllColliders();
		return true;
	}

	void Render(Building building)
	{
		RenderBuilding(DolocAPI.EntitySystem.Next<BuildingRenderer>(), building);
		if (building.proto.FrontCellingWidth > 0)
		{
			RenderCellingCollider(DolocAPI.EntitySystem.Next<OnewayColliderRenderer>(), building);
		}
		if (building.proto.FrontFloorWidth > 0)
		{
			RenderFloorCollider(DolocAPI.EntitySystem.Next<OnewayColliderRenderer>(), building);
		}
	}

	void RenderBuilding(BuildingRenderer renderer, Building building)
	{
		renderer.SetVisible(value: true);
		renderer.Building = building;
		building.Renderer = renderer;
		renderer.spriteRenderer.sprite = building.SceneSprite;
		renderer.transform.position = building.Position;
		renderer.spriteRenderer.sortingOrder = building.Anchor.x;
		renderer.InitMaterialInfos();
		building.OnRender();
	}

	void RendererAllColliders()
	{
		DolocAPI.EntitySystem.Clear<OnewayColliderRenderer>("buildings");
		DolocAPI.EntitySystem.SetupAll<OnewayColliderRenderer, Building>(DM_building.Buildings.Where((Building b) => b.proto.FrontCellingWidth > 0), RenderCellingCollider, "buildings");
		DolocAPI.EntitySystem.SetupAll<OnewayColliderRenderer, Building>(DM_building.Buildings.Where((Building b) => b.proto.FrontFloorWidth > 0), RenderFloorCollider, "buildings");
	}

	void RenderCellingCollider(OnewayColliderRenderer renderer, Building building)
	{
		Vector2 vector = (Vector2)building.proto.GetPositionsOfType(building.Anchor, BuildingTileType.Ceiling).First() + (Vector2)new Vector2Int(0, 1);
		Vector2 vector2 = CurrentRoom.RoomPosition + vector * 1.5f;
		float x = (float)building.proto.FrontCellingWidth * 1.5f;
		renderer.transform.position = new Vector3(vector2.x, vector2.y, 0f);
		renderer.ptCollider.points = new Vector2[2]
		{
			Vector2.zero,
			new Vector2(x, 0f)
		};
	}

	void RenderFloorCollider(OnewayColliderRenderer renderer, Building building)
	{
		Vector2 vector;
		int num4;
		if (building.buildingSupport == null || building.buildingSupport.ColumnHeights.Max() == 0)
		{
			int y = building.Anchor.y - 1;
			int num = building.Anchor.x + building.proto.FrontFloorWidth;
			int num2 = building.Anchor.x + building.proto.FrontFloorWidth;
			bool flag = false;
			for (int i = 0; i <= building.proto.FrontFloorWidth; i++)
			{
				int num3 = building.Anchor.x + i;
				if (!flag && !DM_terrain.IsFilled(new Vector2Int(num3, y), TerrainLayerName.Ground))
				{
					num = num3;
					flag = true;
				}
				else if (flag && DM_terrain.IsFilled(new Vector2Int(num3, y), TerrainLayerName.Ground))
				{
					num2 = num3;
					break;
				}
			}
			vector = new Vector2(num, building.Anchor.y);
			num4 = num2 - num;
		}
		else
		{
			vector = building.proto.GetPositionsOfType(building.Anchor, BuildingTileType.Floor).First();
			num4 = building.proto.FrontFloorWidth;
		}
		if (num4 == 0)
		{
			renderer.SetVisible(value: false);
			return;
		}
		Vector2 vector2 = CurrentRoom.RoomPosition + vector * 1.5f;
		float x = (float)num4 * 1.5f;
		renderer.transform.position = new Vector3(vector2.x, vector2.y, 0f);
		renderer.ptCollider.points = new Vector2[2]
		{
			Vector2.zero,
			new Vector2(x, 0f)
		};
	}

	void RenderAllBuildingsOccupied()
	{
		HashSet<Vector2Int> hashSet = DM_terrain.GetOccupiedPositions(TerrainLayerName.CeilingFront).ToHashSet();
		HashSet<Vector2Int> hashSet2 = DM_terrain.GetOccupiedPositions(TerrainLayerName.Building).ToHashSet();
		HashSet<Vector2Int> hashSet3 = DM_terrain.GetOccupiedPositions(TerrainLayerName.BuildingNoOverlay).ToHashSet();
		TerrainTilemapRenderer terrainTilemapRenderer = DolocAPI.EntitySystem.NextTilemapRenderer(CurrentRoom.RoomPosition, DolocAPI.eftConfig.cellingColor, "buildings", 0f);
		TerrainTilemapRenderer terrainTilemapRenderer2 = DolocAPI.EntitySystem.NextTilemapRenderer(CurrentRoom.RoomPosition, DolocAPI.eftConfig.noOverlayColor, "buildings", 0f);
		TerrainTilemapRenderer terrainTilemapRenderer3 = DolocAPI.EntitySystem.NextTilemapRenderer(CurrentRoom.RoomPosition, DolocAPI.eftConfig.canOverlayColor, "buildings", 0f);
		foreach (Vector2Int item in hashSet2)
		{
			if (hashSet.Contains(item))
			{
				terrainTilemapRenderer.tilemap.SetTile((Vector3Int)item, DolocAPI.eftConfig.terrainSolidTile);
			}
			if (hashSet3.Contains(item))
			{
				terrainTilemapRenderer2.tilemap.SetTile((Vector3Int)item, DolocAPI.eftConfig.terrainSolidTile);
			}
			else
			{
				terrainTilemapRenderer3.tilemap.SetTile((Vector3Int)item, DolocAPI.eftConfig.terrainHollowTile);
			}
		}
	}

	void RenderAllDoorOccupied()
	{
		DolocAPI.EntitySystem.SetupAll(DM_building.Buildings, delegate(GridRenderer gridRenderer, Building building)
		{
			building.proto.GetDoorInfo(building.GlobalAnchor, out var lb, out var size);
			gridRenderer.SetGridRendererState(lb, size, DolocAPI.eftConfig.terrainInvalidColor);
		});
	}

	void RecycleOccupiedBuildingsIndicator()
	{
		DolocAPI.EntitySystem.Clear<TerrainTilemapRenderer>();
	}

	int CountBuilding(string id)
	{
		if (id.IsNullOrEmpty())
		{
			return 0;
		}
		if (DM_building.Buildings.IsNullOrEmpty())
		{
			return 0;
		}
		return DM_building.Buildings.Count((Building x) => x.proto.Id == id);
	}

	void AfterNewGame()
	{
		foreach (Building building in DM_building.Buildings)
		{
			building.AfterNewGame();
		}
	}

	void AfterLoadBuildings()
	{
		foreach (Building building in DM_building.Buildings)
		{
			building.AfterLoadData();
			building.room.SetParent(CurrentRoom);
			DM_terrain.FillContent(building);
		}
		GenerateAllBuildingSupport();
		DistinctUniqueRoom();
	}

	void GenerateAllBuildingSupport()
	{
		foreach (Building building in DM_building.Buildings)
		{
			building.buildingSupport = GenerateBuildingSupport(building);
		}
	}

	void DistinctUniqueRoom()
	{
		Dictionary<string, List<Building>> dictionary = new Dictionary<string, List<Building>>();
		foreach (Building building in DM_building.Buildings)
		{
			if (building.proto.IsUnique)
			{
				string buildingName = building.BuildingName;
				dictionary.TryAdd(buildingName, new List<Building>());
				dictionary[buildingName].Add(building);
			}
		}
		foreach (List<Building> value in dictionary.Values)
		{
			TemplateRoomInHouse room = _GetLowestLevelBuilding(value).room;
			foreach (Building item in value)
			{
				item.SetRoom(room);
			}
		}
	}

	Building _GetLowestLevelBuilding(List<Building> buildings)
	{
		Building building = buildings[0];
		if (!building.canUpgrade)
		{
			return building;
		}
		int num = -1;
		foreach (Building building2 in buildings)
		{
			int level = building2.level;
			if (level < num)
			{
				building = building2;
				num = level;
			}
		}
		return building;
	}

	TemplateRoomInHouse FindFirstBuildingRoomById(string protoName)
	{
		return DM_building.Buildings.FirstOrDefault((Building b) => b.BuildingName == protoName)?.room;
	}

	Building FindFirstBuildingById(string protoName)
	{
		return DM_building.Buildings.FirstOrDefault((Building b) => b.BuildingName == protoName);
	}
}
