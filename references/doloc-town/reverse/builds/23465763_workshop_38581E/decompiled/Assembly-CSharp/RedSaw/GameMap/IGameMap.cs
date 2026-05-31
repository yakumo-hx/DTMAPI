using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedSaw.GameMap;

public interface IGameMap
{
	int Width { get; }

	int Height { get; }

	Vector2 CellSize { get; }

	Vector2 CellSizeReciprocal { get; }

	Vector2 CellSizeHalf { get; }

	Vector2 RoomPosition { get; }

	Vector2Int[] GroundPositions { get; }

	Vector2Int FirstEmptyPosition => GetPosition(0);

	IEnumerable<Vector2Int> AllObstacles { get; }

	Vector2Int GetPosition(int idx);

	bool IsGround(Vector2Int pos);

	Vector2Int[] GetJumpPoints(Vector2Int pos, int hRange, int vRange, int jumpTolerance = 3, int touchTolerance = 2);

	bool RaycastToGround(Vector2 positionWS, out Vector2 result)
	{
		result = default(Vector2);
		Vector2Int vector2Int = WorldToCell(positionWS);
		for (int num = vector2Int.y; num > 0; num--)
		{
			if (IsGround(new Vector2Int(vector2Int.x, num)))
			{
				result = CellToWorldPivot(new Vector2Int(vector2Int.x, num), new Vector2(0.5f, 0f));
				return true;
			}
		}
		result = default(Vector2);
		return false;
	}

	bool IsEmpty(Vector2Int pos);

	bool IsObstacle(Vector2Int pos);

	bool IsEmpty(int x, int y);

	bool IsObstacle(int x, int y);

	bool IsValidPosition(Vector2Int pos)
	{
		if (pos.x >= 0 && pos.x < Width && pos.y >= 0)
		{
			return pos.y < Height;
		}
		return false;
	}

	bool IsObstacleOrInvalid(Vector2Int pos)
	{
		if (IsValidPosition(pos))
		{
			return IsObstacle(pos);
		}
		return true;
	}

	Vector2 GetRandomEmptyPositionWS(Vector2 pivot = default(Vector2));

	Vector2 GetRandomGroundPositionWS(Vector2 pivot = default(Vector2));

	bool GetAroundPositionWS(Vector2 pos, int range, out Vector2 result)
	{
		Vector2Int pos2 = WorldToCell(pos);
		Vector2Int[] array = AroundGrids(pos2, range);
		if (array.Length == 0)
		{
			result = default(Vector2);
			return false;
		}
		int num = ((array.Length != 1) ? UnityEngine.Random.Range(0, array.Length) : 0);
		result = CellToWorldPivot(array[num], new Vector2(0.5f, 0.5f));
		return true;
	}

	Vector2Int GetNearestEmptyPos(Vector2Int pos, int rangeLimit = 10)
	{
		if (IsEmpty(pos))
		{
			return pos;
		}
		for (int i = 0; i < rangeLimit; i++)
		{
			Vector2Int[] array = Ring(pos, i);
			foreach (Vector2Int vector2Int in array)
			{
				if (IsEmpty(vector2Int))
				{
					return vector2Int;
				}
			}
		}
		return FirstEmptyPosition;
	}

	bool SearchNearestEmptyPos(Vector2Int pos, out Vector2Int result, int rangeLimit = 5)
	{
		if (IsEmpty(pos))
		{
			result = pos;
			return true;
		}
		for (int i = 1; i < rangeLimit; i++)
		{
			Vector2Int[] array = Ring(pos, i);
			foreach (Vector2Int vector2Int in array)
			{
				if (IsEmpty(vector2Int))
				{
					result = vector2Int;
					return true;
				}
			}
		}
		result = default(Vector2Int);
		return false;
	}

	Vector2Int[] Ring(Vector2Int center, int dst)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		int num = center.y - dst;
		int num2 = center.y + dst;
		int num3 = center.x - dst;
		int num4 = center.x + dst;
		for (int num5 = num2; num5 >= num; num5--)
		{
			list.Add(new Vector2Int(num3, num5));
			list.Add(new Vector2Int(num4, num5));
		}
		num3++;
		for (int i = num3; i < num4; i++)
		{
			list.Add(new Vector2Int(i, num2));
			list.Add(new Vector2Int(i, num));
		}
		return list.ToArray();
	}

	bool GetAroundGroundPositionWS(Vector2 currentPosition, int range, out Vector2 result)
	{
		Vector2Int pos = WorldToCell(currentPosition);
		Vector2Int[] array = AroundGroundGrids(pos, range);
		if (array.Length == 0)
		{
			result = default(Vector2);
			return false;
		}
		Vector2Int positionCS = array[(array.Length != 1) ? UnityEngine.Random.Range(0, array.Length) : 0];
		result = CellToWorldPivot(positionCS, new Vector2(0.5f, 0f));
		return true;
	}

	private Vector2Int[] AroundGrids(Vector2Int pos, int range)
	{
		if (range < 1)
		{
			return Array.Empty<Vector2Int>();
		}
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = -range; i <= range; i++)
		{
			for (int j = -range; j <= range; j++)
			{
				Vector2Int vector2Int = pos + new Vector2Int(i, j);
				if (IsEmpty(vector2Int))
				{
					list.Add(vector2Int);
				}
			}
		}
		list.Remove(pos);
		return list.ToArray();
	}

	private Vector2Int[] AroundGroundGrids(Vector2Int pos, int range)
	{
		if (range < 1)
		{
			return Array.Empty<Vector2Int>();
		}
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = -range; i <= range; i++)
		{
			for (int j = -range; j <= range; j++)
			{
				if (i != 0 || j != 0)
				{
					Vector2Int vector2Int = pos + new Vector2Int(i, j);
					if (IsGround(vector2Int))
					{
						list.Add(vector2Int);
					}
				}
			}
		}
		return list.ToArray();
	}

	Vector2Int WorldToCell(Vector2 positionWS)
	{
		positionWS -= RoomPosition;
		return new Vector2Int(Mathf.RoundToInt(positionWS.x * CellSizeReciprocal.x), Mathf.RoundToInt(positionWS.y * CellSizeReciprocal.y));
	}

	private Vector2 _CellToWorld(Vector2Int positionCS, Vector2 offset)
	{
		return new Vector2((float)positionCS.x * CellSize.x, (float)positionCS.y * CellSize.y) + offset;
	}

	Vector2 CellToWorldPivot(Vector2Int positionCS, Vector2 pivot)
	{
		return _CellToWorld(positionCS, RoomPosition + CellSize * pivot);
	}

	Vector2 CellToWorldCenter(Vector2Int positionCS)
	{
		return _CellToWorld(positionCS, RoomPosition + CellSizeHalf);
	}

	Vector2[] CellToWorld(Vector2Int[] positionCSList, Vector2 offset)
	{
		Vector2[] array = new Vector2[positionCSList.Length];
		for (int i = 0; i < positionCSList.Length; i++)
		{
			array[i] = _CellToWorld(positionCSList[i], offset);
		}
		return array;
	}

	Vector2[] CellToWorldPivot(Vector2Int[] positionCSList, Vector2 pivot)
	{
		Vector2 offset = RoomPosition + CellSize * pivot;
		return CellToWorld(positionCSList, offset);
	}

	Vector2[] CellToWorldCenter(Vector2Int[] positionCSList)
	{
		return CellToWorldPivot(positionCSList, Vector2.one * 0.5f);
	}

	static Vector2Int[] GenerateEmptyPositions(HashSet<Vector2Int> obstacles, Vector2Int size)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < size.x; i++)
		{
			for (int j = 0; j < size.y; j++)
			{
				Vector2Int item = new Vector2Int(i, j);
				if (!obstacles.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		return list.ToArray();
	}

	bool SearchNearestGround(Vector2Int pos, out Vector2Int result, int limit = 5)
	{
		result = pos;
		if (IsGround(pos))
		{
			return true;
		}
		for (int i = 1; i <= limit; i++)
		{
			Vector2Int[] array = Ring(pos, i);
			foreach (Vector2Int vector2Int in array)
			{
				if (IsGround(vector2Int))
				{
					result = vector2Int;
					return true;
				}
			}
		}
		return false;
	}
}
