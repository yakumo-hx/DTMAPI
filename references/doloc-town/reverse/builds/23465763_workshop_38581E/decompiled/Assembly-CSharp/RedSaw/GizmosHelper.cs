using UnityEngine;

namespace RedSaw;

public static class GizmosHelper
{
	public static void DrawColliderBox(Collider2D collider, Color color)
	{
		Vector2 centerPosition = collider.bounds.center;
		Vector2 size = collider.bounds.size;
		DrawBoxMM(centerPosition, size, color);
	}

	public static void DrawBounds(BoundsInt bounds, Color color)
	{
		Gizmos.color = color;
		Vector2 p = new Vector2(bounds.position.x, bounds.position.y);
		Vector2 p2 = new Vector2(bounds.position.x, bounds.position.y + bounds.size.y);
		Vector2 p3 = new Vector2(bounds.position.x + bounds.size.x, bounds.position.y + bounds.size.y);
		Vector2 p4 = new Vector2(bounds.position.x + bounds.size.x, bounds.position.y);
		DrawBox(p, p2, p3, p4);
	}

	public static void DrawBounds(Bounds bounds, Color color)
	{
		DrawBoxMM(bounds.center, bounds.size, color);
	}

	public static void DrawRect(Rect rect, Color? color = null)
	{
		if (color.HasValue)
		{
			Gizmos.color = color.Value;
		}
		Vector2 p = new Vector2(rect.x, rect.y);
		Vector2 p2 = new Vector2(rect.x, rect.y + rect.height);
		Vector2 p3 = new Vector2(rect.x + rect.width, rect.y + rect.height);
		Vector2 p4 = new Vector2(rect.x + rect.width, rect.y);
		DrawBox(p, p2, p3, p4);
	}

	public static void DebugDrawBoxLB(Vector2 lb, Vector2 size, Color color, float dur = 1f)
	{
		Vector2 vector = lb;
		Vector2 vector2 = new Vector2(lb.x, lb.y + size.y);
		Vector2 vector3 = new Vector2(lb.x + size.x, lb.y + size.y);
		Vector2 vector4 = new Vector2(lb.x + size.x, lb.y);
		Debug.DrawLine(vector, vector2, color, dur);
		Debug.DrawLine(vector2, vector3, color, dur);
		Debug.DrawLine(vector3, vector4, color, dur);
		Debug.DrawLine(vector4, vector, color, dur);
	}

	public static void DebugDrawBoxMM(Vector2 center, Vector2 size, Color color, float dur = 1f)
	{
		DebugDrawBoxLB(center - size * 0.5f, size, color, dur);
	}

	public static void DrawBoxMM(Vector2 centerPosition, Vector2 size, Color color)
	{
		DrawBoxLB(centerPosition - size * 0.5f, size, color);
	}

	public static void DrawBoxMM(Vector2 centerPosition, Vector2 size)
	{
		DrawBoxLB(centerPosition - size * 0.5f, size);
	}

	public static void DrawBoxLB(Vector2 LB, Vector2 size, Color color)
	{
		Gizmos.color = color;
		Vector2 p = LB;
		Vector2 p2 = new Vector2(LB.x, LB.y + size.y);
		Vector2 p3 = new Vector2(LB.x + size.x, LB.y + size.y);
		Vector2 p4 = new Vector2(LB.x + size.x, LB.y);
		DrawBox(p, p2, p3, p4);
	}

	public static void DrawBoxLB(Vector2 LB, Vector2 size)
	{
		Vector2 p = LB;
		Vector2 p2 = new Vector2(LB.x, LB.y + size.y);
		Vector2 p3 = new Vector2(LB.x + size.x, LB.y + size.y);
		Vector2 p4 = new Vector2(LB.x + size.x, LB.y);
		DrawBox(p, p2, p3, p4);
	}

	public static void DrawBoxMB(Vector2 MB, Vector2 size)
	{
		float num = size.x * 0.5f;
		Vector2 p = new Vector2(MB.x - num, MB.y);
		Vector2 p2 = new Vector2(p.x, p.y + size.y);
		Vector2 p3 = new Vector2(MB.x + num, p2.y);
		DrawBox(p4: new Vector2(p3.x, p.y), p1: p, p2: p2, p3: p3);
	}

	public static void DrawBoxMB(Vector2 MB, Vector2 size, Color color)
	{
		Gizmos.color = color;
		float num = size.x * 0.5f;
		Vector2 p = new Vector2(MB.x - num, MB.y);
		Vector2 p2 = new Vector2(p.x, p.y + size.y);
		Vector2 p3 = new Vector2(MB.x + num, p2.y);
		DrawBox(p4: new Vector2(p3.x, p.y), p1: p, p2: p2, p3: p3);
	}

	public static void DrawBox2Point(Vector2 LB, Vector2 RT)
	{
		Vector2 p = LB;
		Vector2 p2 = new Vector2(LB.x, RT.y);
		Vector2 p3 = RT;
		Vector2 p4 = new Vector2(RT.x, LB.y);
		DrawBox(p, p2, p3, p4);
	}

	private static void DrawBox(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
	{
		Gizmos.DrawLine(p1, p2);
		Gizmos.DrawLine(p2, p3);
		Gizmos.DrawLine(p3, p4);
		Gizmos.DrawLine(p4, p1);
	}
}
