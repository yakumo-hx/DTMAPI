using DolocTown.Config.Equipment;
using DolocTown.Config.Platform;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public static class BuilderUtils
{
	public static TerrainTilemapRenderer CreatePlatformIndicator(PlatformGeometry geometry, PlatformInfo proto, Vector2 roomPosition)
	{
		TerrainTilemapRenderer terrainTilemapRenderer = DolocAPI.EntitySystem.Next<TerrainTilemapRenderer>();
		terrainTilemapRenderer.position = roomPosition;
		terrainTilemapRenderer.TilemapRenderer.sortingLayerName = "Platform";
		terrainTilemapRenderer.TilemapRenderer.sortingOrder = 1;
		terrainTilemapRenderer.tilemap.SetPlatformDraft(geometry, proto);
		return terrainTilemapRenderer;
	}

	public static Vector2Int GetRealGridPos(Vector2Int anchor, Vector2 roomPosition)
	{
		return (new Vector2((float)anchor.x * 1.5f, (float)anchor.y * 1.5f) + roomPosition).LatticeToGrid(1.5f);
	}

	public static Vector2 GetScreenLatticePosition(Vector2Int cellPos, Vector2 roomPosition)
	{
		return DolocAPI.WorldToScreen((Vector2)cellPos * 1.5f + roomPosition);
	}

	public static AffectorArea GetEquipmentAffectorArea(EquipmentInfo proto)
	{
		if (!(proto.Function is EquipmentFuncAffector equipmentFuncAffector))
		{
			return null;
		}
		return new AffectorArea(equipmentFuncAffector.HorizontalRange, equipmentFuncAffector.VerticalRangeTop, equipmentFuncAffector.VerticalRangeBottom);
	}

	public static void SetGridRendererState(this GridRenderer renderer, Vector2Int gridPosition, Vector2Int size, Color color)
	{
		renderer.SetGridRendererColor(color);
		renderer.GridPosition = gridPosition;
		renderer.GridSize = size;
		renderer.BorderThickness = 0.2f;
		renderer.GridLineThickness = 0f;
		renderer.BorderOffsetSpeed = 2f;
		renderer.CoverColor = new Color(0f, 0f, 0f, 0.15f);
	}

	public static void SetGridRendererColor(this GridRenderer renderer, Color color)
	{
		if (!(renderer == null))
		{
			renderer.BorderColor1 = color;
		}
	}

	public static Vector2Int LatticeToGrid(this Vector2 pos, float size)
	{
		float num = ((pos.x >= 0f) ? 0f : (0f - size));
		float num2 = ((pos.y >= 0f) ? 0f : (0f - size));
		return new Vector2Int((int)((pos.x - pos.x % size + num) / size), (int)((pos.y - pos.y % size + num2) / size));
	}

	public static Vector2 Lattice(this Vector2 pos, float size)
	{
		return (Vector2)pos.LatticeToGrid(size) * size;
	}
}
