using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public static class AutomateUtils
{
	private static Vector2Int[] SimplifyPath(Vector2Int[] path, int step)
	{
		if (step <= 1)
		{
			return path;
		}
		if (path.Length <= 2)
		{
			return new Vector2Int[1] { path[^1] };
		}
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < path.Length - 1; i++)
		{
			if (i % step == 0)
			{
				list.Add(path[i]);
			}
		}
		list.Add(path[^1]);
		return list.ToArray();
	}

	public static Vector2Int[] VoidPath(Vector2Int from, Vector2Int to)
	{
		if (from == to)
		{
			return new Vector2Int[1] { from };
		}
		if (from.y == to.y)
		{
			return HorizontalLine(from.x, to.x, from.y);
		}
		if (from.x == to.x)
		{
			return VerticalLine(from.y, to.y, from.x);
		}
		return DiagonalLine(from, to);
	}

	public static Vector2Int[] VoidPathStep(Vector2Int from, Vector2Int to, int step = 2)
	{
		return SimplifyPath(VoidPath(from, to), step);
	}

	private static Vector2Int[] DiagonalLine(Vector2Int A, Vector2Int B)
	{
		int num = B.x - A.x;
		int num2 = B.y - A.y;
		int num3 = Mathf.Abs(num);
		int num4 = Mathf.Abs(num2);
		Vector2Int vector2Int = new Vector2Int((num > 0) ? 1 : (-1), (num2 > 0) ? 1 : (-1));
		List<Vector2Int> list = new List<Vector2Int> { A };
		Vector2Int vector2Int2 = A;
		if (num3 == num4)
		{
			while (vector2Int2 != B)
			{
				vector2Int2 += vector2Int;
				list.Add(vector2Int2);
			}
			return list.ToArray();
		}
		if (num3 > num4)
		{
			while (vector2Int2.y != B.y)
			{
				vector2Int2 += vector2Int;
				list.Add(vector2Int2);
			}
			list.AddRange(HorizontalLine(vector2Int2.x, B.x, vector2Int2.y));
		}
		else
		{
			while (vector2Int2.x != B.x)
			{
				vector2Int2 += vector2Int;
				list.Add(vector2Int2);
			}
			list.AddRange(VerticalLine(vector2Int2.y, B.y, vector2Int2.x));
		}
		return list.ToArray();
	}

	private static Vector2Int[] HorizontalLine(int x1, int x2, int y)
	{
		int num = x2 - x1;
		Vector2Int[] array = new Vector2Int[Mathf.Abs(num)];
		int num2 = ((num > 0) ? 1 : (-1));
		int num3 = x1;
		for (int i = 0; i < array.Length; i++)
		{
			num3 += num2;
			array[i] = new Vector2Int(num3, y);
		}
		return array;
	}

	private static Vector2Int[] VerticalLine(int y1, int y2, int x)
	{
		int num = y2 - y1;
		Vector2Int[] array = new Vector2Int[Mathf.Abs(num)];
		int num2 = ((num > 0) ? 1 : (-1));
		int num3 = y1;
		for (int i = 0; i < array.Length; i++)
		{
			num3 += num2;
			array[i] = new Vector2Int(x, num3);
		}
		return array;
	}

	public static (Case, T[]) ClipMissions<T>(Dictionary<Case, List<T>> plantMissions)
	{
		int num = 0;
		(Case, T[]) result = default((Case, T[]));
		foreach (var (item, list2) in plantMissions)
		{
			if (list2.Count > num)
			{
				num = list2.Count;
				result = (item, list2.ToArray());
			}
		}
		return result;
	}
}
