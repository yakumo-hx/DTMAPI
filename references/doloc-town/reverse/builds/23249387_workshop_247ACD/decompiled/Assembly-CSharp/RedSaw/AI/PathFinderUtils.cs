using System.Collections.Generic;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public static class PathFinderUtils
{
	private static Vector2Int[] aroundPoints;

	private static readonly AStar astar = new AStar(constraintDiagonal: true);

	private static readonly AStar astarDiagonal = new AStar();

	private static Vector2Int[] AroundPoints
	{
		get
		{
			if (aroundPoints == null)
			{
				aroundPoints = GetAroundPoints(4);
			}
			return aroundPoints;
		}
	}

	private static Vector2Int[] GetAroundPoints(int maxDistance)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 1; i <= maxDistance; i++)
		{
			Vector2Int[] neighbours = PathFinderHelper.Neighbours8;
			foreach (Vector2Int vector2Int in neighbours)
			{
				list.Add(vector2Int * i);
			}
		}
		return list.ToArray();
	}

	public static bool GetAroundPoint(IGameMap gameMap, Vector2Int pos, out Vector2Int result)
	{
		Vector2Int[] array = AroundPoints.Shuffle();
		foreach (Vector2Int vector2Int in array)
		{
			Vector2Int vector2Int2 = pos + vector2Int;
			if (gameMap.IsEmpty(vector2Int2))
			{
				result = vector2Int2;
				return true;
			}
		}
		result = default(Vector2Int);
		return false;
	}

	public static bool IsUnblocked(Vector2 L, Vector2 R, LayerMask mask)
	{
		Vector2 vector = R - L;
		return Physics2D.Raycast(L, vector.normalized, vector.magnitude, mask).collider == null;
	}

	public static Vector2[] SmoothPath(Vector2[] sourcePath, LayerMask mask)
	{
		if (sourcePath.Length < 3)
		{
			return sourcePath;
		}
		List<Vector2> list = new List<Vector2> { sourcePath[0] };
		int num = 0;
		while (num < sourcePath.Length - 1)
		{
			int i;
			for (i = num + 1; i < sourcePath.Length - 1 && IsUnblocked(sourcePath[num], sourcePath[i + 1], mask); i++)
			{
			}
			list.Add(sourcePath[i]);
			num = i;
		}
		return list.ToArray();
	}

	public static Vector2Int[] SimplifyGroundPath(Vector2Int[] path)
	{
		if (path == null || path.Length <= 2)
		{
			return path;
		}
		Vector2Int vector2Int = path[0];
		Vector2Int vector2Int2 = vector2Int;
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 1; i < path.Length; i++)
		{
			Vector2Int vector2Int3 = path[i];
			if (vector2Int3.y != vector2Int.y)
			{
				list.Add(vector2Int);
				if (vector2Int != vector2Int2)
				{
					list.Add(vector2Int2);
				}
				vector2Int = vector2Int3;
			}
			vector2Int2 = vector2Int3;
		}
		list.Add(vector2Int);
		if (vector2Int != vector2Int2)
		{
			list.Add(vector2Int2);
		}
		return list.ToArray();
	}

	public static Vector2Int[] SimplifyPath(Vector2Int[] origin)
	{
		if (origin == null || origin.Length <= 2)
		{
			return origin;
		}
		List<Vector2Int> list = new List<Vector2Int>();
		Vector2Int vector2Int = origin[0];
		Vector2Int vector2Int2 = origin[1];
		Vector2Int vector2Int3 = vector2Int2 - vector2Int;
		for (int i = 2; i < origin.Length; i++)
		{
			Vector2Int vector2Int4 = origin[i];
			Vector2Int vector2Int5 = vector2Int4 - vector2Int2;
			if (vector2Int3 != vector2Int5)
			{
				list.Add(vector2Int);
				vector2Int = vector2Int2;
				vector2Int3 = vector2Int5;
			}
			vector2Int2 = vector2Int4;
		}
		list.Add(vector2Int);
		list.Add(vector2Int2);
		return list.ToArray();
	}

	public static Vector2Int[] SimplifyGroundPath(IGameMap map, Vector2Int[] path)
	{
		if (path == null || path.Length <= 2)
		{
			return path;
		}
		Vector2Int vector2Int = path[0];
		Vector2Int vector2Int2 = vector2Int;
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 1; i < path.Length; i++)
		{
			Vector2Int vector2Int3 = path[i];
			if (!IsConnected(vector2Int, vector2Int3))
			{
				list.Add(vector2Int);
				if (vector2Int != vector2Int2)
				{
					list.Add(vector2Int2);
				}
				vector2Int = vector2Int3;
			}
			vector2Int2 = vector2Int3;
		}
		list.Add(vector2Int);
		if (vector2Int != vector2Int2)
		{
			list.Add(vector2Int2);
		}
		return list.ToArray();
		bool IsConnected(Vector2Int p1, Vector2Int p2)
		{
			if (p1.y != p2.y)
			{
				return false;
			}
			if (p1.x == p2.x)
			{
				return false;
			}
			int num = Mathf.Max(p2.x, p1.x);
			int num2 = Mathf.Min(p2.x, p1.x);
			int y = p1.y - 1;
			for (int j = num2 + 1; j < num; j++)
			{
				if (!map.IsObstacle(new Vector2Int(j, y)))
				{
					return false;
				}
			}
			return true;
		}
	}
}
