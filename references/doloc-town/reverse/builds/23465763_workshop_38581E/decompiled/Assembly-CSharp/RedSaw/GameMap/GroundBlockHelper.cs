using System.Collections.Generic;
using UnityEngine;

namespace RedSaw.GameMap;

public static class GroundBlockHelper
{
	public static bool IsManhattanPathValid(Vector2Int A, Vector2Int B, IGameMap gameMap)
	{
		if (!GetManhattanPath(A, B, out var path))
		{
			return false;
		}
		Vector2Int[] array = path;
		foreach (Vector2Int pos in array)
		{
			if (gameMap.IsObstacle(pos))
			{
				return false;
			}
		}
		return true;
	}

	public static bool GetManhattanPath(Vector2Int A, Vector2Int B, out Vector2Int[] path)
	{
		int num = A.x - B.x;
		if (num == 0)
		{
			path = null;
			return false;
		}
		int num2 = ((num > 0) ? B.x : A.x);
		int num3 = num2 + num;
		int num4 = A.y - B.y;
		int num5 = ((num4 > 0) ? B.y : A.y);
		int num6 = num5 + num4;
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = num5; i <= num6; i++)
		{
			list.Add(new Vector2Int(num2, i));
		}
		for (int j = num2; j <= num3; j++)
		{
			list.Add(new Vector2Int(j, num6));
		}
		path = list.ToArray();
		return true;
	}
}
