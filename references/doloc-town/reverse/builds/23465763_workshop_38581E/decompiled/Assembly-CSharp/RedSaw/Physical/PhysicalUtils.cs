using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedSaw.Physical;

public static class PhysicalUtils
{
	public static Vector2[] SimplifyPolyLine(Vector2[] points, float epsilon, bool sortByX = true)
	{
		if (points == null || points.Length < 3)
		{
			return points;
		}
		List<Vector2> list = new List<Vector2>();
		int num = 0;
		int num2 = points.Length - 1;
		list.Add(points[num]);
		list.Add(points[num2]);
		SimplifyRecursive(points, num, num2, epsilon, list);
		Vector2[] array = list.ToArray();
		if (sortByX)
		{
			Array.Sort(array, (Vector2 a, Vector2 b) => a.x.CompareTo(b.x));
		}
		return array;
	}

	private static void SimplifyRecursive(Vector2[] points, int startIdx, int endIdx, float epsilon, List<Vector2> result)
	{
		float num = 0f;
		int num2 = 0;
		for (int i = startIdx + 1; i < endIdx; i++)
		{
			float num3 = PerpendicularDistance(points[i], points[startIdx], points[endIdx]);
			if (num3 > num)
			{
				num = num3;
				num2 = i;
			}
		}
		if (num > epsilon)
		{
			result.Add(points[num2]);
			SimplifyRecursive(points, startIdx, num2, epsilon, result);
			SimplifyRecursive(points, num2, endIdx, epsilon, result);
		}
	}

	private static float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
	{
		float num = Vector2.Distance(lineStart, lineEnd);
		return Mathf.Abs((point.x - lineStart.x) * (lineEnd.y - lineStart.y) - (point.y - lineStart.y) * (lineEnd.x - lineStart.x)) / num;
	}
}
