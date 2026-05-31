using UnityEngine;

namespace RedSaw;

public static class Geometry
{
	public static bool CollideWith(this Rect L, Rect R)
	{
		float x = L.x;
		float num = R.x + R.width;
		if (x > num)
		{
			return false;
		}
		float num2 = L.x + L.width;
		if (R.x > num2)
		{
			return false;
		}
		float y = L.y;
		float num3 = R.y + R.height;
		if (y > num3)
		{
			return false;
		}
		float num4 = L.y + L.height;
		if (R.y > num4)
		{
			return false;
		}
		return true;
	}

	public static Vector2[] Offset(this Vector2[] polygon, Vector2 offset)
	{
		if (polygon == null || polygon.Length == 0)
		{
			return polygon;
		}
		Vector2[] array = new Vector2[polygon.Length];
		for (int i = 0; i < polygon.Length; i++)
		{
			array[i] = polygon[i] + offset;
		}
		return array;
	}

	public static bool GetClosestPointOnPolygon(Vector2[] polygon, Vector2 p, out Vector2 closestPoint)
	{
		closestPoint = default(Vector2);
		if (polygon == null || polygon.Length < 3)
		{
			return false;
		}
		float num = float.MaxValue;
		for (int i = 0; i < polygon.Length; i++)
		{
			Vector2 a = polygon[i];
			Vector2 b = polygon[(i + 1) % polygon.Length];
			Vector2 closestPointOnLineSegment = GetClosestPointOnLineSegment(a, b, p);
			float num2 = Vector2.Distance(p, closestPointOnLineSegment);
			if (num2 < num)
			{
				num = num2;
				closestPoint = closestPointOnLineSegment;
			}
		}
		return true;
	}

	public static Vector2 GetClosestPointOnLineSegment(Vector2 A, Vector2 B, Vector2 P)
	{
		Vector2 lhs = P - A;
		Vector2 vector = B - A;
		float sqrMagnitude = vector.sqrMagnitude;
		if (sqrMagnitude == 0f)
		{
			return A;
		}
		float value = Vector2.Dot(lhs, vector) / sqrMagnitude;
		value = Mathf.Clamp01(value);
		return A + vector * value;
	}

	public static RectInt GetRectFromPositions(Vector2Int[] positions)
	{
		if (positions == null || positions.Length == 0)
		{
			return new RectInt(0, 0, 0, 0);
		}
		int x = positions[0].x;
		int x2 = positions[0].x;
		int y = positions[0].y;
		int y2 = positions[0].y;
		for (int i = 0; i < positions.Length; i++)
		{
			Vector2Int vector2Int = positions[i];
			if (vector2Int.x < x)
			{
				x = vector2Int.x;
			}
			if (vector2Int.x > x2)
			{
				x2 = vector2Int.x;
			}
			if (vector2Int.y < y)
			{
				y = vector2Int.y;
			}
			if (vector2Int.y > y2)
			{
				y2 = vector2Int.y;
			}
		}
		return new RectInt(x, y, x2 - x + 1, y2 - y + 1);
	}

	public static RectInt CombineRects(this RectInt[] rects)
	{
		if (rects == null || rects.Length == 0)
		{
			return default(RectInt);
		}
		if (rects.Length == 1)
		{
			return rects[0];
		}
		Vector2Int position = rects[0].position;
		Vector2Int vector2Int = rects[0].position + rects[0].size;
		for (int i = 1; i < rects.Length; i++)
		{
			RectInt rectInt = rects[i];
			Vector2Int vector2Int2 = rectInt.position + rectInt.size;
			if (rectInt.position.x < position.x)
			{
				position.x = rectInt.position.x;
			}
			if (rectInt.position.y < position.y)
			{
				position.y = rectInt.position.y;
			}
			if (vector2Int2.x > vector2Int.x)
			{
				vector2Int.x = vector2Int2.x;
			}
			if (vector2Int2.y > vector2Int.y)
			{
				vector2Int.y = vector2Int2.y;
			}
		}
		return new RectInt(position, vector2Int - position);
	}
}
