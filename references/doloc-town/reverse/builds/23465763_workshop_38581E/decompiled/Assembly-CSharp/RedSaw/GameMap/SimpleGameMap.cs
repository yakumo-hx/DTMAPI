using System.Collections.Generic;
using UnityEngine;

namespace RedSaw.GameMap;

public class SimpleGameMap : IGameMap
{
	private readonly GameMapGround _groundMap;

	private readonly int[,] mapInfo;

	private readonly Vector2Int[] empties;

	private readonly Vector2Int[] obstacles;

	private readonly Vector2Int[] grounds;

	private readonly Vector2Int _size;

	private readonly Vector2 _cellSize;

	private readonly Vector2 _cellSizeReciprocal;

	private readonly Vector2 _cellSizeHalf;

	private readonly Vector2 _roomPosition;

	public int Width => _size.x;

	public int Height => _size.y;

	public Vector2 CellSize => _cellSize;

	public Vector2 CellSizeReciprocal => _cellSizeReciprocal;

	public Vector2 CellSizeHalf => _cellSizeHalf;

	public Vector2 RoomPosition => _roomPosition;

	public IEnumerable<Vector2Int> AllObstacles => obstacles;

	public Vector2Int[] GroundPositions => grounds;

	public SimpleGameMap(Vector2Int size, Vector2Int[] obstacles, Vector2 roomPosition, Vector2 cellSize)
	{
		this.obstacles = obstacles;
		_size = size;
		_roomPosition = roomPosition;
		_cellSize = cellSize;
		_cellSizeReciprocal = new Vector2(1f / cellSize.x, 1f / cellSize.y);
		_cellSizeHalf = cellSize * 0.5f;
		grounds = obstacles.SearchGround(size, handleFloorLine: true);
		mapInfo = BuildMap(_size, obstacles, grounds);
		empties = GetEmpties(_size, mapInfo);
		_groundMap = new GameMapGround(this);
	}

	private int[,] BuildMap(Vector2Int size, Vector2Int[] obstacles, Vector2Int[] grounds)
	{
		int[,] array = new int[size.x, size.y];
		Vector2Int[] array2 = obstacles;
		for (int i = 0; i < array2.Length; i++)
		{
			Vector2Int vector2Int = array2[i];
			array[vector2Int.x, vector2Int.y] = 1;
		}
		array2 = this.grounds;
		for (int i = 0; i < array2.Length; i++)
		{
			Vector2Int vector2Int2 = array2[i];
			array[vector2Int2.x, vector2Int2.y] = 2;
		}
		return array;
	}

	private Vector2Int[] GetEmpties(Vector2Int size, int[,] mapInfo)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < size.x; i++)
		{
			for (int j = 0; j < size.y; j++)
			{
				if (mapInfo[i, j] == 0)
				{
					list.Add(new Vector2Int(i, j));
				}
			}
		}
		return list.ToArray();
	}

	public Vector2Int GetPosition(int idx)
	{
		if (idx >= 0 && idx < empties.Length)
		{
			return empties[idx];
		}
		return empties[0];
	}

	public Vector2Int[] GetJumpPoints(Vector2Int pos, int hRange, int vRange, int jumpTolerance = 3, int touchTolerance = 2)
	{
		return _groundMap.GetJumpPoints(pos, hRange, vRange, jumpTolerance, touchTolerance);
	}

	private bool IsValidPosition(Vector2Int pos)
	{
		if (pos.x >= 0 && pos.x < _size.x && pos.y >= 0)
		{
			return pos.y < _size.y;
		}
		return false;
	}

	public bool IsEmpty(Vector2Int pos)
	{
		if (IsValidPosition(pos))
		{
			return mapInfo[pos.x, pos.y] == 0;
		}
		return false;
	}

	public bool IsObstacle(Vector2Int pos)
	{
		if (IsValidPosition(pos))
		{
			return mapInfo[pos.x, pos.y] == 1;
		}
		return true;
	}

	public bool IsGround(Vector2Int pos)
	{
		if (IsValidPosition(pos))
		{
			return mapInfo[pos.x, pos.y] == 2;
		}
		return false;
	}

	public bool IsEmpty(int x, int y)
	{
		return IsEmpty(new Vector2Int(x, y));
	}

	public bool IsObstacle(int x, int y)
	{
		return IsObstacle(new Vector2Int(x, y));
	}

	public Vector2Int GetRandomEmptyPosition()
	{
		int num = Random.Range(0, empties.Length);
		return empties[num];
	}

	public Vector2 GetRandomEmptyPositionWS(Vector2 pivot)
	{
		int num = Random.Range(0, empties.Length);
		Vector2 vector = empties[num];
		return RoomPosition + (vector + pivot) * CellSize;
	}

	public Vector2 GetRandomGroundPositionWS(Vector2 pivot)
	{
		int num = Random.Range(0, grounds.Length);
		Vector2 vector = grounds[num];
		return RoomPosition + (pivot + vector) * CellSize;
	}
}
