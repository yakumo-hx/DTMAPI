using UnityEngine;

namespace DolocTown;

public readonly struct BezierCurveParabolic
{
	private readonly Vector2 from;

	private readonly Vector2 to;

	private readonly Vector2 control1;

	private readonly Vector2 control2;

	private static Vector2 GetPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
	{
		t = Mathf.Clamp01(t);
		float num = 1f - t;
		return num * num * num * p0 + 3f * num * num * t * p1 + 3f * num * t * t * p2 + t * t * t * p3;
	}

	public BezierCurveParabolic(Vector2 from, Vector2 to, float controlDst = 0.24f, float controlHeight = 2f)
	{
		this.from = from;
		this.to = to;
		float num = to.x - from.x;
		float y = Mathf.Max(from.y, to.y) + controlHeight;
		control1 = new Vector2(from.x + num * controlDst, y);
		control2 = new Vector2(to.x - num * controlDst, y);
	}

	public Vector2 GetPosition(float t)
	{
		return GetPoint(from, control1, control2, to, t);
	}

	public Vector2 GetPositionEx(float t)
	{
		Vector2 position = GetPosition(t);
		return new Vector2((to.x - from.x) * t + from.x, position.y);
	}
}
