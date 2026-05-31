using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public static class TerrainPatch
{
	public static bool IsSingle(this TerrainLayerName n)
	{
		if (n != 0)
		{
			return (n & (n - 1)) == 0;
		}
		return false;
	}

	public static bool IsUnion(this TerrainLayerName n)
	{
		if (n != 0)
		{
			return (n & (n - 1)) != 0;
		}
		return true;
	}

	public static void FillGroundTerrain(this Terrain terrain, Vector2Int[] terrainPositions, Vector2Int[] extraObstaclePositions)
	{
		terrain.FillContent(new TerrainGround(terrainPositions, extraObstaclePositions));
	}

	public static void RemoveGroundTerrain(this Terrain terrain)
	{
		foreach (TerrainGround item in terrain.GetContentsOfType<TerrainGround>())
		{
			terrain.RemoveContent(item);
		}
	}

	public static bool IsStructureConstructable(this Terrain terrain, Vector2Int pos)
	{
		return terrain.IsEmpty(pos, TerrainLayerName.Structure);
	}

	public static bool IsEquipmentConstructable(this Terrain terrain, IEnumerable<Vector2Int> cvPositions, IEnumerable<Vector2Int> gdPositions, bool forceOnGround = false)
	{
		TerrainLayerName layerMask = TerrainLayerName.Resource | TerrainLayerName.Ground | TerrainLayerName.Equipment | TerrainLayerName.Door;
		if (!terrain.AllEmpty(cvPositions, layerMask))
		{
			return false;
		}
		if (forceOnGround)
		{
			return terrain.AllFilled(gdPositions, TerrainLayerName.Ground);
		}
		return terrain.AllPositionsFilledInAnyLayer(gdPositions, TerrainLayerName.Structure | TerrainLayerName.CeilingFront);
	}

	public static bool IsEquipmentConstructable(this Terrain terrain, IEnumerable<Vector2Int> cvPositions)
	{
		return !terrain.AnyPositionFilledInAnyLayer(cvPositions, TerrainLayerName.Resource | TerrainLayerName.Ground | TerrainLayerName.Equipment | TerrainLayerName.Door);
	}
}
