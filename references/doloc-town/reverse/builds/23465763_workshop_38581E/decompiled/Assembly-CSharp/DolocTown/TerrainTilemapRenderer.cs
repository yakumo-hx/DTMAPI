using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

[GameEntityManager("/tilemaps/indicators", DolocGameAssets.GAME_ENTITY_TERRAIN_INDICATOR)]
public class TerrainTilemapRenderer : GameEntity
{
	private TilemapRenderer tilemapRenderer;

	private Tween anim;

	public Tilemap tilemap { get; private set; }

	public TilemapRenderer TilemapRenderer => tilemapRenderer;

	protected override void __Init()
	{
		base.__Init();
		tilemap = GetComponentInChildren<Tilemap>();
		if (tilemap.layoutGrid == null)
		{
			base.gameObject.AddComponent<Grid>().cellSize = new Vector3(1.5f, 1.5f, 0f);
		}
		tilemapRenderer = GetComponentInChildren<TilemapRenderer>();
		SetVisible(value: true);
	}

	public override void OnRecycle()
	{
		tilemap.ClearAllTiles();
		base.OnRecycle();
	}

	public override void OnReuse()
	{
		base.OnReuse();
		tilemapRenderer.sortingLayerName = "BuildingsForeground";
		tilemapRenderer.sortingOrder = 999;
	}

	public void SetColor(Color c)
	{
		tilemapRenderer.material.SetColor("_BaseColor", c);
		tilemapRenderer.material.SetFloat("_Alpha", c.a);
	}

	public void SetRollSpeed(float speed)
	{
		tilemapRenderer.material.SetFloat("_RollingSpeed", speed);
	}

	public void Shake(float duration = 0.3f, float shakeStrength = 0.1f)
	{
		anim?.Kill();
		anim = base.transform.DOShakePosition(duration, new Vector3(shakeStrength, shakeStrength, 0f));
	}

	private void OnDestroy()
	{
		Object.Destroy(tilemapRenderer.material);
	}
}
