using System;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public static class AnimalPathFinderUtils
{
	public static Vector2Int[] OptimizePath(Vector2Int[] originPath)
	{
		if (originPath.IsNullOrEmpty())
		{
			return Array.Empty<Vector2Int>();
		}
		if (originPath.Length == 1)
		{
			return originPath;
		}
		Vector2Int vector2Int = originPath[0];
		Vector2Int vector2Int2 = vector2Int;
		Vector2Int vector2Int3 = vector2Int;
		List<Vector2Int> list = new List<Vector2Int> { vector2Int };
		for (int i = 1; i < originPath.Length; i++)
		{
			vector2Int3 = originPath[i];
			if (vector2Int2.y == vector2Int3.y)
			{
				vector2Int2 = vector2Int3;
				continue;
			}
			if (vector2Int != vector2Int2)
			{
				list.Add(vector2Int2);
			}
			list.Add(vector2Int3);
			vector2Int = vector2Int3;
			vector2Int2 = vector2Int3;
		}
		if (vector2Int3 != vector2Int)
		{
			list.Add(vector2Int3);
		}
		return list.ToArray();
	}
}
