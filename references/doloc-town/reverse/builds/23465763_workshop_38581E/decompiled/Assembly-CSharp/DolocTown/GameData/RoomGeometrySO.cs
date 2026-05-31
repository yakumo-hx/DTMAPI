using System;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public struct RoomGeometrySO
{
	[SerializeField]
	public Vector2 scenePosition;

	[SerializeField]
	public Vector2 sceneSize;

	[SerializeField]
	public Vector4 cameraPadding;

	[SerializeField]
	public Vector2 roomPosition;

	[SerializeField]
	public Vector2 roomSize;

	[SerializeField]
	public Vector2Int gridPos;

	[SerializeField]
	public Vector2Int gridSize;

	[SerializeField]
	public Vector2 defaultEntryPosition;

	[SerializeField]
	public Vector2Int[] obstacles;

	[SerializeField]
	public Vector2Int[] groundPositions;

	[SerializeField]
	public Vector2Int[] extraObstacles;

	[SerializeField]
	public Vector2[] polygon;

	public Vector2Int[] LeftWall
	{
		get
		{
			Vector2Int item = gridPos;
			List<Vector2Int> list = new List<Vector2Int>();
			for (int i = 0; i < gridSize.y; i++)
			{
				list.Add(item);
				item.y++;
			}
			return list.ToArray();
		}
	}

	public Vector2Int[] RightWall
	{
		get
		{
			Vector2Int item = gridPos + new Vector2Int(gridSize.x - 1, 0);
			List<Vector2Int> list = new List<Vector2Int>();
			for (int i = 0; i < gridSize.y; i++)
			{
				list.Add(item);
				item.y++;
			}
			return list.ToArray();
		}
	}

	public Vector2Int[] Floor
	{
		get
		{
			Vector2Int item = gridPos;
			List<Vector2Int> list = new List<Vector2Int>();
			for (int i = 0; i < gridSize.x; i++)
			{
				list.Add(item);
				item.x++;
			}
			return list.ToArray();
		}
	}

	public Vector2Int[] Ceiling
	{
		get
		{
			Vector2Int item = gridPos + new Vector2Int(0, gridSize.y - 1);
			List<Vector2Int> list = new List<Vector2Int>();
			for (int i = 0; i < gridSize.x; i++)
			{
				list.Add(item);
				item.x++;
			}
			return list.ToArray();
		}
	}

	public Rect SceneRect => new Rect(scenePosition, sceneSize);

	public Rect ExtendSceneRect(float padding = 3f)
	{
		Vector2 vector = new Vector2(padding, padding);
		return new Rect(scenePosition - vector, sceneSize + vector * 2f);
	}

	public RoomGeometrySO(Vector2 scenePosition, Vector2 sceneSize, Vector4 cameraPadding, Vector2 roomPosition, Vector2 roomSize, Vector2Int gridPos, Vector2Int gridSize, Vector2 defaultEntryPosition, Vector2Int[] obstacles, Vector2Int[] groundPositions, Vector2Int[] extraObstacles, Vector2[] polygon = null)
	{
		this.scenePosition = scenePosition;
		this.sceneSize = sceneSize;
		this.cameraPadding = cameraPadding;
		this.roomPosition = roomPosition;
		this.roomSize = roomSize;
		this.gridPos = gridPos;
		this.gridSize = gridSize;
		this.defaultEntryPosition = defaultEntryPosition;
		this.obstacles = obstacles;
		this.groundPositions = groundPositions;
		this.extraObstacles = extraObstacles ?? Array.Empty<Vector2Int>();
		this.polygon = polygon ?? Array.Empty<Vector2>();
	}

	public bool CreateProto(out RoomGeometry geometry)
	{
		geometry = default(RoomGeometry);
		if (groundPositions == null)
		{
			return false;
		}
		if (obstacles == null)
		{
			return false;
		}
		geometry = new RoomGeometry(scenePosition, sceneSize, cameraPadding, roomPosition, roomSize, gridPos, gridSize, defaultEntryPosition, obstacles, groundPositions, extraObstacles, polygon);
		return true;
	}

	private int[,] LoadMapInfo(Vector2Int gridSize, Vector2Int[] groundPositions, Vector2Int[] obstacles)
	{
		int[,] array = new int[gridSize.x, gridSize.y];
		Vector2Int[] array2 = groundPositions;
		for (int i = 0; i < array2.Length; i++)
		{
			Vector2Int vector2Int = array2[i];
			array[vector2Int.x, vector2Int.y] = 2;
		}
		array2 = obstacles;
		for (int i = 0; i < array2.Length; i++)
		{
			Vector2Int vector2Int2 = array2[i];
			array[vector2Int2.x, vector2Int2.y] = 1;
		}
		return array;
	}

	public readonly Vector2Int[] GetWalkablePositions()
	{
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>(obstacles);
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < gridSize.x; i++)
		{
			for (int j = 0; j < gridSize.y; j++)
			{
				Vector2Int item = new Vector2Int(i, j);
				if (!hashSet.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		return list.ToArray();
	}
}
