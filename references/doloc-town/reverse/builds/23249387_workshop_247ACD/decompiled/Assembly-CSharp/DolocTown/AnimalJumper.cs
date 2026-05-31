using UnityEngine;

namespace DolocTown;

public class AnimalJumper
{
	private BezierCurveParabolic _curve;

	private Vector2 _start;

	private Vector2 _end;

	private float _duration;

	private float _totalDuration;

	public Vector2 destination => _end;

	public void SetJumpInfo(Vector2 start, Vector2 end, float duration, float height)
	{
		_start = start;
		_end = end;
		_totalDuration = duration;
		_duration = 0f;
		_curve = new BezierCurveParabolic(_start, _end, 0.24f, height);
	}

	public Vector2 Update(float dt, out bool isDone)
	{
		_duration += dt;
		if (_duration >= _totalDuration)
		{
			_duration = _totalDuration;
			isDone = true;
			return _end;
		}
		isDone = false;
		float t = _duration / _totalDuration;
		return _curve.GetPosition(t);
	}
}
