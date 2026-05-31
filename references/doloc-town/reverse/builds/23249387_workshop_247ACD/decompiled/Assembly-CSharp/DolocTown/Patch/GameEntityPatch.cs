using UnityEngine;

namespace DolocTown.Patch;

public static class GameEntityPatch
{
	public static TerrainTilemapRenderer NextTilemapRenderer(this GameEntitySystem<GEMGameObjectAttribute> system, Vector2 position, Color color, string alias, float rollSpeed = -1f)
	{
		TerrainTilemapRenderer terrainTilemapRenderer = system.Next<TerrainTilemapRenderer>();
		terrainTilemapRenderer.position = position;
		terrainTilemapRenderer.SetColor(color);
		if (rollSpeed >= 0f)
		{
			terrainTilemapRenderer.SetRollSpeed(rollSpeed);
		}
		return terrainTilemapRenderer;
	}
}
