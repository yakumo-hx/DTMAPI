using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

[RequireComponent(typeof(Tilemap))]
public class TerrainLayerDraft : DolocObject
{
	[SerializeField]
	private Tilemap tilemap;

	[SerializeField]
	private Tile groundTile;

	[SerializeField]
	private Tile occupiedTile;

	[SerializeField]
	private Tile draftTile;

	private HashSet<Vector2Int> groundPositions = new HashSet<Vector2Int>();

	private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

	private HashSet<Vector2Int> draftPositions = new HashSet<Vector2Int>();

	public void Clear()
	{
		tilemap.ClearAllTiles();
		groundPositions.Clear();
		occupiedPositions.Clear();
		draftPositions.Clear();
	}

	public void DrawGround(Vector2Int[] groundPositions)
	{
		_Draw(groundPositions, ref this.groundPositions, groundTile);
	}

	public void DrawOccupied(Vector2Int[] occupiedPositions)
	{
		_Draw(occupiedPositions, ref this.occupiedPositions, occupiedTile);
	}

	public void DrawDraft(Vector2Int[] draftPositions)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		Vector2Int[] array = draftPositions;
		foreach (Vector2Int item in array)
		{
			if (!groundPositions.Contains(item) && !occupiedPositions.Contains(item))
			{
				list.Add(item);
			}
		}
		draftPositions = list.ToArray();
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>(draftPositions);
		foreach (Vector2Int draftPosition in this.draftPositions)
		{
			if (!hashSet.Contains(draftPosition))
			{
				tilemap.SetTile((Vector3Int)draftPosition, null);
			}
		}
		array = draftPositions;
		foreach (Vector2Int vector2Int in array)
		{
			if (!this.draftPositions.Contains(vector2Int))
			{
				tilemap.SetTile((Vector3Int)vector2Int, draftTile);
			}
		}
		this.draftPositions = hashSet;
	}

	private void _Draw(Vector2Int[] newPositions, ref HashSet<Vector2Int> loadedPositions, Tile tile)
	{
		if (loadedPositions != null)
		{
			foreach (Vector2Int loadedPosition in loadedPositions)
			{
				tilemap.SetTile((Vector3Int)loadedPosition, null);
			}
		}
		loadedPositions = new HashSet<Vector2Int>(newPositions);
		foreach (Vector2Int vector2Int in newPositions)
		{
			tilemap.SetTile((Vector3Int)vector2Int, tile);
		}
	}
}
