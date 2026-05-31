using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

[GameEntityManager("/sub_entity/platform_indicator", DolocGameAssets.GAME_ENTITY_PLATFORM_INDICATOR)]
public class PlatformIndicator : GameEntity
{
	[SerializeField]
	public Tilemap tilemap;

	[SerializeField]
	private TilemapRenderer tilemapRenderer;

	private Material clonedMaterial;

	protected override void __Init()
	{
		base.__Init();
		clonedMaterial = tilemapRenderer.material;
	}

	public void SetMaterialColor(string propertyName, Color c)
	{
		if (clonedMaterial != null)
		{
			clonedMaterial.SetColor(propertyName, c);
		}
	}

	public void SetTile(Vector2Int pos, Tile tile)
	{
		tilemap.SetTile((Vector3Int)pos, tile);
	}

	public void Clear()
	{
		tilemap.ClearAllTiles();
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		tilemap.ClearAllTiles();
	}

	public void OnDestroy()
	{
		if (clonedMaterial != null)
		{
			Object.Destroy(clonedMaterial);
		}
	}
}
