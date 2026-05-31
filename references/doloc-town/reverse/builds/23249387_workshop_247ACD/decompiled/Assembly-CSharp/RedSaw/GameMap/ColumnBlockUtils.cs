using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RedSaw.GameMap;

public static class ColumnBlockUtils
{
	public static Vector2Int[] FindFreeBlocks(this IEnumerable<Vector2Int> availablePositions, int width)
	{
		width = Mathf.Max(1, width);
		if (width == 1)
		{
			return availablePositions.ToArray();
		}
		Dictionary<int, List<int>> dictionary = availablePositions.SortByHeight();
		List<Vector2Int> list = new List<Vector2Int>();
		foreach (KeyValuePair<int, List<int>> item in dictionary)
		{
			int[] array = item.Value.FindBlocks(width);
			foreach (int x in array)
			{
				list.Add(new Vector2Int(x, item.Key));
			}
		}
		return list.ToArray();
	}

	private static Dictionary<int, List<int>> SortByHeight(this IEnumerable<Vector2Int> positions)
	{
		Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
		foreach (Vector2Int position in positions)
		{
			if (dictionary.ContainsKey(position.y))
			{
				dictionary[position.y].Add(position.x);
				continue;
			}
			List<int> value = new List<int> { position.x };
			dictionary.Add(position.y, value);
		}
		return dictionary;
	}

	private static int[] FindBlocks(this List<int> arr, int width)
	{
		if (arr.Count < width)
		{
			return Array.Empty<int>();
		}
		arr.Sort();
		int num = 0;
		int num2 = arr.Count - width;
		List<int> list = new List<int>();
		while (num <= num2)
		{
			if (arr.IsContinues(num, width))
			{
				list.Add(arr[num]);
				num += width;
			}
			else
			{
				num++;
			}
		}
		return list.ToArray();
	}

	private static bool IsContinues(this List<int> arr, int start, int width)
	{
		int num = arr[start];
		for (int i = 1; i < width; i++)
		{
			if (arr[i + start] - num != i)
			{
				return false;
			}
		}
		return true;
	}
}
