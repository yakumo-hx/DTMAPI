using System.Collections.Generic;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public class VoidPathFinder : IPathFinder
{
	public Vector2Int[] FindPath(IGameMap gameMap, Vector2Int f, Vector2Int to)
	{
		return FindPath(f, to);
	}

	public Vector2Int[] FindPath(Vector2Int from, Vector2Int to)
	{
		if (from == to)
		{
			return new Vector2Int[1] { from };
		}
		if (from.y == to.y)
		{
			return Horizontal(from.x, to.x, from.y);
		}
		if (from.x == to.x)
		{
			return Vertical(from.y, to.y, from.x);
		}
		return Diagonal(from, to);
	}

	private Vector2Int[] Diagonal(Vector2Int A, Vector2Int B)
	{
		int num = B.x - A.x;
		int num2 = B.y - A.y;
		int num3 = Mathf.Abs(num);
		int num4 = Mathf.Abs(num2);
		Vector2Int vector2Int = new Vector2Int((num > 0) ? 1 : (-1), (num2 > 0) ? 1 : (-1));
		List<Vector2Int> list = new List<Vector2Int> { A };
		Vector2Int item = A;
		if (num3 > num4)
		{
			while (item.y != B.y)
			{
				item += vector2Int;
				list.Add(item);
			}
			list.AddRange(Horizontal(item.x, B.x, item.y));
		}
		else
		{
			while (item.x != B.x)
			{
				item += vector2Int;
				list.Add(item);
			}
			list.AddRange(Vertical(item.y, B.y, item.x));
		}
		return list.ToArray();
	}

	private Vector2Int[] Horizontal(int x1, int x2, int y)
	{
		int num = x2 - x1;
		Vector2Int[] array = new Vector2Int[Mathf.Abs(num) + 1];
		int num2 = ((num > 0) ? 1 : (-1));
		int num3 = x1;
		for (int i = 0; i < array.Length; i++)
		{
			num3 += num2;
			array[i] = new Vector2Int(num3, y);
		}
		return array;
	}

	private Vector2Int[] Vertical(int y1, int y2, int x)
	{
		int num = y2 - y1;
		Vector2Int[] array = new Vector2Int[Mathf.Abs(num) + 1];
		int num2 = ((num > 0) ? 1 : (-1));
		int num3 = y1;
		for (int i = 0; i < array.Length; i++)
		{
			num3 += num2;
			array[i] = new Vector2Int(x, num3);
		}
		return array;
	}
}
