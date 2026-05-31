using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RedSaw;

public static class Grid2D
{
	public static IEnumerable<Vector2Int> IterateGrid(this Vector2Int size)
	{
		if (size.x <= 0 || size.y <= 0)
		{
			yield break;
		}
		for (int x = 0; x < size.x; x++)
		{
			for (int y = 0; y < size.y; y++)
			{
				yield return new Vector2Int(x, y);
			}
		}
	}

	public static IEnumerable<Vector2Int> IterateGrid(this Vector2Int size, Vector2Int anchor)
	{
		if (size.x <= 0 || size.y <= 0)
		{
			yield break;
		}
		for (int x = anchor.x; x < anchor.x + size.x; x++)
		{
			for (int y = anchor.y; y < anchor.y + size.y; y++)
			{
				yield return new Vector2Int(x, y);
			}
		}
	}

	public static Vector2Int SnapToGrid(this Vector2 v, Vector2 cellSize)
	{
		if (cellSize.x <= 0f || cellSize.y <= 0f)
		{
			throw new ArgumentException("cellSize must be positive");
		}
		return new Vector2Int(Mathf.RoundToInt(v.x / cellSize.x), Mathf.RoundToInt(v.y / cellSize.y));
	}

	public static Vector2Int SnapToGridCeil(this Vector2 v, Vector2 cellSize)
	{
		if (cellSize.x <= 0f || cellSize.y <= 0f)
		{
			throw new ArgumentException("cellSize must be positive");
		}
		return new Vector2Int(Mathf.CeilToInt(v.x / cellSize.x), Mathf.CeilToInt(v.y / cellSize.y));
	}

	public static IEnumerable<Vector2Int> Offset(this Vector2Int[] positions, Vector2Int offset)
	{
		if (positions != null && positions.Length != 0)
		{
			for (int i = 0; i < positions.Length; i++)
			{
				Vector2Int vector2Int = positions[i];
				yield return new Vector2Int(vector2Int.x + offset.x, vector2Int.y + offset.y);
			}
		}
	}

	public static int ManhattenDistance(this Vector2Int from, Vector2Int to)
	{
		return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
	}

	public static IEnumerable<Vector2Int> Cover(Vector2Int pos, Vector2Int size)
	{
		if (size.x * size.y == 0)
		{
			yield break;
		}
		for (int x = pos.x; x < pos.x + size.x; x++)
		{
			for (int y = pos.y; y < pos.y + size.y; y++)
			{
				yield return new Vector2Int(x, y);
			}
		}
	}

	public static Vector2Int[] CoverArray(Vector2Int pos, Vector2Int size)
	{
		return Cover(pos, size).ToArray();
	}

	public static IEnumerable<Vector2Int> CoverH(Vector2Int pos, int width)
	{
		for (int x = pos.x; x < pos.x + width; x++)
		{
			yield return new Vector2Int(x, pos.y);
		}
	}

	public static Vector2Int[] CoverHArray(Vector2Int pos, int width)
	{
		return CoverH(pos, width).ToArray();
	}

	public static IEnumerable<Vector2Int> CoverV(Vector2Int pos, int height)
	{
		for (int y = pos.y; y < pos.y + height; y++)
		{
			yield return new Vector2Int(pos.x, y);
		}
	}

	public static Vector2Int[] CoverVArray(Vector2Int pos, int height)
	{
		return CoverV(pos, height).ToArray();
	}

	public static IEnumerable<Vector2Int> Ring(Vector2Int pos, Vector2Int size)
	{
		int startX = pos.x - 1;
		int endX = pos.x + size.x + 1;
		int startY = pos.y - 1;
		int endY = pos.y + size.y + 1;
		for (int x2 = startX; x2 < endX; x2++)
		{
			yield return new Vector2Int(x2, startY);
			yield return new Vector2Int(x2, endY);
		}
		for (int x2 = pos.y; x2 < pos.y + size.y; x2++)
		{
			yield return new Vector2Int(startX, x2);
			yield return new Vector2Int(endX, x2);
		}
	}

	public static IEnumerable<Vector2Int> Ring(Vector2Int pos, Vector2Int size, int n)
	{
		if (n < 0)
		{
			return Array.Empty<Vector2Int>();
		}
		int num = 2 * n;
		pos = new Vector2Int(pos.x - n, pos.y - n);
		size = new Vector2Int(size.x + num, size.y + num);
		return Ring(pos, size);
	}

	public static Vector2Int[] RingArray(Vector2Int pos, Vector2Int size)
	{
		return Ring(pos, size).ToArray();
	}

	public static Vector2Int[] SearchGround(this Vector2Int[] obstacles, Vector2Int size, bool handleFloorLine = false)
	{
		if (obstacles == null)
		{
			return Array.Empty<Vector2Int>();
		}
		int[,] array = new int[size.x, size.y];
		for (int i = 0; i < obstacles.Length; i++)
		{
			Vector2Int vector2Int = obstacles[i];
			array[vector2Int.x, vector2Int.y] = 1;
		}
		int[,] array2 = new int[size.x, size.y];
		for (int j = 1; j < size.y; j++)
		{
			for (int k = 0; k < size.x; k++)
			{
				array2[k, j] = ((array[k, j - 1] - array[k, j] > 0) ? 1 : 0);
			}
		}
		if (handleFloorLine)
		{
			for (int l = 0; l < size.x; l++)
			{
				array2[l, 0] = ((1 - array[l, 0] > 0) ? 1 : 0);
			}
		}
		List<Vector2Int> list = new List<Vector2Int>();
		for (int m = 0; m < size.y; m++)
		{
			for (int n = 0; n < size.x; n++)
			{
				if (array2[n, m] != 0)
				{
					list.Add(new Vector2Int(n, m));
				}
			}
		}
		return list.ToArray();
	}

	public static Dictionary<int, Vector2Int[]> ToHeightGroup(this Vector2Int[] positions)
	{
		Dictionary<int, List<Vector2Int>> dictionary = new Dictionary<int, List<Vector2Int>>();
		for (int i = 0; i < positions.Length; i++)
		{
			Vector2Int item = positions[i];
			int y = item.y;
			if (!dictionary.TryGetValue(y, out var value))
			{
				value = (dictionary[y] = new List<Vector2Int>());
			}
			value.Add(item);
		}
		return dictionary.ToDictionary((KeyValuePair<int, List<Vector2Int>> kvp) => kvp.Key, (KeyValuePair<int, List<Vector2Int>> kvp) => kvp.Value.ToArray());
	}

	public static IEnumerable<Vector2Int> AroundPositions(this Vector2Int center, int maxDistance, Func<Vector2Int, Vector2Int, float> dstFunc)
	{
		if (maxDistance <= 0)
		{
			yield return center;
			yield break;
		}
		for (int dx = -maxDistance; dx <= maxDistance; dx++)
		{
			for (int dy = -maxDistance; dy <= maxDistance; dy++)
			{
				Vector2Int vector2Int = new Vector2Int(dx, dy);
				if (!(dstFunc(vector2Int, Vector2Int.zero) > (float)maxDistance))
				{
					yield return center + vector2Int;
				}
			}
		}
	}

	public static IEnumerable<Vector2Int> AroundPositionsManhatten(this Vector2Int center, int maxDistance)
	{
		return center.AroundPositions(maxDistance, (Vector2Int x, Vector2Int y) => x.ManhattenDistance(y));
	}

	public static IEnumerable<Vector2Int> AroundPositionsEuclidean(this Vector2Int center, int maxDistance)
	{
		return center.AroundPositions(maxDistance, (Vector2Int x, Vector2Int y) => Vector2.Distance(x, y));
	}
}
