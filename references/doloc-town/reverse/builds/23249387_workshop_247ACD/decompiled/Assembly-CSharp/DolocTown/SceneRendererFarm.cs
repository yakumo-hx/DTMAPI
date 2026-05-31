using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

public class SceneRendererFarm : SceneRenderer
{
	public readonly Tilemap RM_platformTilemap;

	public readonly TilemapGroupManager RM_supportTilemaps;

	public SceneRendererFarm(Transform container)
		: base(container)
	{
		RM_platformTilemap = container.GetComponentInChildren<Tilemap>(includeInactive: true);
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.GAME_ENTITY_SUPPORT_TILEMAPS);
		RM_supportTilemaps = Object.Instantiate(asset, container).GetComponent<TilemapGroupManager>();
		RM_supportTilemaps.Init();
	}

	protected override void ClearRenderers()
	{
		RM_platformTilemap.ClearAllTiles();
		RM_supportTilemaps.ClearAllTiles();
	}
}
