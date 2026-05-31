using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedSaw.AI;

public static class JpsUtils
{
	public static readonly Vector2Int up = Vector2Int.up;

	public static readonly Vector2Int down = Vector2Int.down;

	public static readonly Vector2Int left = Vector2Int.left;

	public static readonly Vector2Int right = Vector2Int.right;

	public static readonly Vector2Int upRight = Vector2Int.one;

	public static readonly Vector2Int upLeft = new Vector2Int(-1, 1);

	public static readonly Vector2Int downRight = new Vector2Int(1, -1);

	public static readonly Vector2Int downLeft = new Vector2Int(-1, -1);

	public static readonly Vector2Int[] allDirections = new Vector2Int[8] { up, left, down, right, upRight, upLeft, downLeft, downRight };

	public static Vector2[] loadVector2Int(Vector2Int[] path)
	{
		Vector2[] array = new Vector2[path.Length];
		for (int i = 0; i < path.Length; i++)
		{
			Vector2Int vector2Int = path[i];
			array[i] = new Vector2(vector2Int.x, vector2Int.y);
		}
		return array;
	}

	public static Vector2[] lineSmooth(Vector2[] path, Func<Vector2, Vector2, bool> rayClear)
	{
		List<Vector2> list = new List<Vector2>();
		list.Add(path[0]);
		Vector2 arg = path[0];
		int num = 2;
		while (num < path.Length)
		{
			if (rayClear(arg, path[num]))
			{
				num++;
				continue;
			}
			list.Add(path[num - 1]);
			arg = path[num - 1];
			num++;
		}
		list.Add(path[^1]);
		return list.ToArray();
	}

	public static int Manhattan(Vector2Int p, Vector2Int e)
	{
		return Mathf.Abs(p.x - e.x) + Mathf.Abs(p.y - e.y);
	}

	public static int Euler(Vector2Int p, Vector2Int e)
	{
		int num = p.x - e.x;
		int num2 = p.y - e.y;
		return num * num + num2 * num2;
	}
}
