using UnityEngine;

public static class DolocTransform
{
	public const float PX_WORLD = 0.125f;

	public const float PX_WORLD_HALF = 0.0625f;

	public const float PX_TILE = 0.08333f;

	public const float PX_UI = 4f;

	public const float TILE_WORLD = 1.5f;

	public const float TILE_WORLD_RECIPROCAL = 0.66667f;

	public static Vector2 TILE_WORLD_SIZE = new Vector2(1.5f, 1.5f);

	public const int TILE_PX = 12;

	public const float UI_WORLD = 1f / 32f;

	public const float PixelsPerUnit = 8f;

	public static Vector2 CalcWorldSize(Vector2Int size)
	{
		return new Vector2((float)size.x * 1.5f, (float)size.y * 1.5f);
	}
}
