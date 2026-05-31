using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

[GameEntityManager("/tilemap_group/support_tilemap", DolocGameAssets.GAME_ENTITY_SUPPORT_TILEMAPS)]
public class TilemapGroupManager : GameEntity
{
	private Tilemap[] layers;

	private TilemapRenderer[] tilemapRenderers;

	protected override void __Init()
	{
		base.__Init();
		layers = GetComponentsInChildren<Tilemap>();
		if (!base.gameObject.TryGetComponent<Grid>(out var component))
		{
			component = base.gameObject.AddComponent<Grid>();
		}
		component.cellSize = new Vector3(1.5f, 1.5f, -1f);
		tilemapRenderers = GetComponentsInChildren<TilemapRenderer>();
		SetVisible(value: true);
	}

	public void SetMaterial(Material material)
	{
		TilemapRenderer[] array = tilemapRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].material = new Material(material);
		}
	}

	public void SetTiles(int layerIndex, Vector3Int[] positions, Tile[] tiles)
	{
		if (layerIndex >= 0 && layerIndex <= layers.Length)
		{
			layers[layerIndex].SetTiles(positions, tiles);
		}
	}

	public void ClearAllTiles()
	{
		Tilemap[] array = layers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ClearAllTiles();
		}
	}

	public void SetMaterialColor(string name, Color c)
	{
		TilemapRenderer[] array = tilemapRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			Material material = array[i].material;
			if (material != null)
			{
				material.SetColor(name, c);
			}
		}
	}

	private void OnDestroy()
	{
		TilemapRenderer[] array = tilemapRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			Object.Destroy(array[i].material);
		}
	}
}
