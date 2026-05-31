using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class RoomGizmos : MonoBehaviour
{
	[SerializeField]
	protected bool DrawAll;

	[SerializeField]
	protected bool drawCameraRect = true;

	[SerializeField]
	protected Vector2 cameraRectPadding = new Vector2(0.1f, 0.1f);

	[SerializeField]
	protected Color cameraRectColor = Color.white;

	[SerializeField]
	protected Color roomRectColor = Color.white;

	[SerializeField]
	protected Vector2 padding = new Vector2(0.1f, 0.1f);

	[SerializeField]
	protected bool drawTerrainInfo = true;

	[SerializeField]
	protected bool drawRoomRect = true;

	[SerializeField]
	protected Color terrainInfoColor = Color.white;

	[SerializeField]
	protected bool drawGroundInfo = true;

	[SerializeField]
	protected Color groundInfoColor = Color.white;

	[SerializeField]
	protected bool drawCellarBoundary;

	[SerializeField]
	protected Color cellarBoundaryColor = Color.yellow;

	[SerializeField]
	protected bool drawTerrainLayerInfo;

	[SerializeField]
	protected Color terrainLayerColor = Color.white;

	[SerializeField]
	protected TerrainLayerName terrainLayer = TerrainLayerName.ResourceTree;

	[SerializeField]
	protected bool drawResourceGridOther = true;

	[SerializeField]
	protected Color resourceGridOtherColor = Color.white;

	[SerializeField]
	protected bool drawResourceGridTree = true;

	[SerializeField]
	protected Color resourceGridTreeColor = Color.white;

	[SerializeField]
	protected bool drawResourceConstraintSet = true;

	[SerializeField]
	protected Color resourceConstraintSetColor = Color.white;

	[SerializeField]
	protected DungeonResourceConstraintType resourceConstraintType;

	[SerializeField]
	protected bool drawEnvObstacles = true;

	[SerializeField]
	protected Color envObstacleColor = Color.red;

	[SerializeField]
	protected EnvObstacleType envObstacleType;

	[SerializeField]
	protected bool drawBuildingGrid = true;

	[SerializeField]
	protected Color buildingGridColor = Color.white;

	[SerializeField]
	protected bool drawBuildingToCellar;

	[SerializeField]
	protected Color buildingToCellarColor = Color.yellow;

	[SerializeField]
	protected bool drawEquipmentGrid = true;

	[SerializeField]
	protected Color equipmentGridColor = Color.white;

	[SerializeField]
	protected bool drawPlatformGrid = true;

	[SerializeField]
	protected Color platformGridColor = Color.white;

	[SerializeField]
	protected bool drawColumnGrid = true;

	[SerializeField]
	protected Color columnGridColor = Color.white;

	[SerializeField]
	protected bool drawVegetationGrid = true;

	[SerializeField]
	protected Color vegetationGridColor = Color.white;

	[SerializeField]
	protected bool drawVegetationConstraintSet = true;

	[SerializeField]
	protected Color vegetationConstraintSetColor = Color.white;

	[SerializeField]
	protected VegetationConstraintType vegetationConstraintType = VegetationConstraintType.Environment;

	public string CurrentRoomName
	{
		get
		{
			if (CurrentRoom == null)
			{
				return "不存在于任何房间";
			}
			return CurrentRoom.SceneShortName;
		}
	}

	public Room CurrentRoom { get; set; }

	protected void OnDrawAllChanged()
	{
		drawCameraRect = DrawAll;
		drawTerrainLayerInfo = DrawAll;
		drawTerrainInfo = DrawAll;
		drawGroundInfo = DrawAll;
		drawResourceGridOther = DrawAll;
		drawResourceGridTree = DrawAll;
		drawResourceConstraintSet = DrawAll;
		drawBuildingGrid = DrawAll;
		drawEquipmentGrid = DrawAll;
		drawVegetationConstraintSet = DrawAll;
		drawVegetationGrid = DrawAll;
		drawPlatformGrid = DrawAll;
		drawBuildingToCellar = DrawAll;
	}

	protected void OnDrawGizmos()
	{
		if (CurrentRoom == null || !DolocAPI.IsDataLoaded)
		{
			return;
		}
		if (drawCellarBoundary)
		{
			if (CurrentRoom == DolocAPI.archiveHandle.MainFarm)
			{
				float cellarRightBorderWorldPositionX = CellarExtension.GetCellarRightBorderWorldPositionX();
				Vector2 vector = new Vector2(cellarRightBorderWorldPositionX, 0f);
				Vector2 vector2 = new Vector2(cellarRightBorderWorldPositionX, DolocAPI.archiveHandle.MainFarm.SceneSize.y);
				Gizmos.color = buildingToCellarColor;
				Gizmos.DrawLine(vector, vector2);
			}
			if (CurrentRoom is TemplateRoomInHouse templateRoomInHouse && templateRoomInHouse.Building.proto.Id.Contains("cellar"))
			{
				float x = templateRoomInHouse.RoomPosition.x + templateRoomInHouse.RoomSize.x;
				Vector2 vector3 = new Vector2(x, 0f);
				Vector2 vector4 = new Vector2(x, templateRoomInHouse.SceneSize.y);
				Gizmos.color = cellarBoundaryColor;
				Gizmos.DrawLine(vector3, vector4);
			}
		}
		DrawRoomCameraRect(CurrentRoom);
		DrawRoomGeometry(CurrentRoom);
		DrawRoomTerrainInfo(CurrentRoom);
		DrawDungeonResource(CurrentRoom);
		DrawBuilding(CurrentRoom);
		DrawEquipment(CurrentRoom);
		DrawVegetation(CurrentRoom);
		DrawPlatform(CurrentRoom);
	}

	private void DrawRoomCameraRect(Room room)
	{
		if (drawCameraRect)
		{
			DrawRect(room.ScenePosition, room.SceneSize, cameraRectPadding, cameraRectColor);
		}
		if (drawRoomRect)
		{
			DrawRect(room.RoomPosition, room.RoomSize, padding, roomRectColor);
		}
	}

	private void DrawRoomGeometry(Room room)
	{
		if (drawTerrainInfo)
		{
			DrawPositions(room.Geometry.obstacles, room.RoomPosition, 1.5f, padding, terrainInfoColor);
		}
		if (drawGroundInfo)
		{
			DrawPositions(room.Geometry.groundPositions, room.RoomPosition, 1.5f, padding, groundInfoColor);
		}
	}

	private void DrawRoomTerrainInfo(Room room)
	{
		if (drawTerrainLayerInfo)
		{
			IEnumerable<Vector2Int> occupiedPositions = room.DM_terrain.GetOccupiedPositions(terrainLayer);
			DrawPositions(occupiedPositions, room.RoomPosition, 1.5f, padding, terrainLayerColor);
		}
	}

	private void DrawDungeonResource(Room room)
	{
		if (room == null)
		{
			return;
		}
		Vector2 roomPos = (Vector2)room.RoomGridPos * 1.5f;
		if (drawResourceGridTree)
		{
			DrawPositions(room.DM_terrain.GetOccupiedPositions(TerrainLayerName.ResourceTree), roomPos, 1.5f, padding, resourceGridTreeColor);
		}
		if (drawResourceGridOther)
		{
			DrawPositions(room.DM_terrain.GetOccupiedPositions(TerrainLayerName.ResourceOther), roomPos, 1.5f, padding, resourceGridOtherColor);
		}
		if (drawResourceConstraintSet)
		{
			Vector2Int[] positions = ((IDungeonResourceHost)room).ResourceGenInfo.constraint[resourceConstraintType];
			DrawPositions(positions, roomPos, 1.5f, padding, resourceConstraintSetColor);
		}
		if (drawEnvObstacles)
		{
			Vector2Int[] array = ((IBaseHost)room).CurrentRoom.baseProto.envObstacles[envObstacleType];
			if (array.Length != 0)
			{
				DrawPositions(array, roomPos, 1.5f, padding, envObstacleColor);
			}
		}
	}

	private void DrawBuilding(Room room)
	{
		if (drawBuildingGrid)
		{
			if (room != null)
			{
				DrawPositions(((IBaseHost)room).DM_terrain.GetOccupiedPositions(TerrainLayerName.Building), room.RoomPosition, 1.5f, padding, buildingGridColor);
			}
		}
	}

	private void DrawEquipment(Room room)
	{
		if (drawEquipmentGrid)
		{
			if (room != null)
			{
				DrawPositions(((IBaseHost)room).DM_terrain.GetOccupiedPositions(TerrainLayerName.Equipment), room.RoomPosition, 1.5f, padding, equipmentGridColor);
			}
		}
	}

	private void DrawVegetation(Room room)
	{
		if (room != null)
		{
			if (drawVegetationGrid)
			{
				DrawPositions(((IBaseHost)room).DM_terrain.GetOccupiedPositions(TerrainLayerName.Vegetation), room.RoomPosition, 1.5f, padding, vegetationGridColor);
			}
			if (drawVegetationConstraintSet)
			{
				Vector2Int[] positions = ((IVegetationHost)room).VegetationGenInfo.constraint[vegetationConstraintType];
				DrawPositions(positions, room.RoomPosition, 1.5f, padding, vegetationConstraintSetColor);
			}
		}
	}

	private void DrawPlatform(Room room)
	{
		if (!drawPlatformGrid)
		{
			return;
		}
		if (room != null)
		{
			DrawPositions(((IBaseHost)room).DM_terrain.GetOccupiedPositions(TerrainLayerName.PlatformSurface), room.RoomPosition, 1.5f, padding, platformGridColor);
			if (drawColumnGrid)
			{
				DrawPositions(((IBaseHost)room).DM_terrain.GetOccupiedPositions(TerrainLayerName.PlatformColumn), room.RoomPosition, 1.5f, padding, columnGridColor);
			}
		}
	}

	private void DrawPositions(IEnumerable<Vector2Int> positions, Vector2 roomPos, float cellsize, Vector2 padding, Color color)
	{
		if (positions == null)
		{
			return;
		}
		Gizmos.color = color;
		Vector2 size = new Vector2(cellsize, cellsize) - padding * 2f;
		Vector2 vector = padding + roomPos;
		foreach (Vector2Int position in positions)
		{
			GizmosHelper.DrawBoxLB(new Vector2(position.x, position.y) * cellsize + vector, size);
		}
	}

	private void DrawRect(Vector2 pos, Vector2 size, Vector2 padding, Color color)
	{
		Gizmos.color = color;
		GizmosHelper.DrawBoxLB(pos + padding, size - padding * 2f);
	}
}
