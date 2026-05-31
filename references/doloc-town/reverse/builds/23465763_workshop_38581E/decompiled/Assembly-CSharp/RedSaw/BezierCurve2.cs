using UnityEngine;

namespace RedSaw;

public readonly struct BezierCurve2
{
	private readonly Vector2 _from;

	private readonly Vector2 err1;

	private readonly Vector2 err2;

	public BezierCurve2(Vector2 from, Vector2 to, Vector2 control_01)
	{
		_from = from;
		err1 = control_01 - from;
		err2 = to - control_01;
	}

	public Vector2 GetPosition(float t)
	{
		Vector2 vector = _from + err1 * t;
		Vector2 vector2 = vector + err2 * t;
		return vector + (vector2 - vector) * t;
	}
}
