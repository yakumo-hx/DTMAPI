using UnityEngine;

namespace RedSaw;

public class CastTimer
{
	private readonly float _interval;

	private readonly float _intervalReci;

	private readonly float _perfectTimingLength;

	private bool _isIncrease;

	private float _value;

	private float _currentPerfectTimingRecorder;

	public float Progress => _value * _intervalReci;

	public bool IsPerfect
	{
		get
		{
			if (Mathf.Abs(_value - _interval) < 0.01f)
			{
				return _currentPerfectTimingRecorder > 0f;
			}
			return false;
		}
	}

	public CastTimer(float interval, float perfectTimingLength = 0.3f)
	{
		_value = 0f;
		_isIncrease = true;
		_interval = interval;
		_perfectTimingLength = perfectTimingLength;
		_currentPerfectTimingRecorder = perfectTimingLength;
		_intervalReci = 1f / _interval;
	}

	public void Reset()
	{
		_value = 0f;
		_isIncrease = true;
		_currentPerfectTimingRecorder = _perfectTimingLength;
	}

	private void UpdatePerfectTiming(float dt)
	{
		if (_currentPerfectTimingRecorder > 0f)
		{
			_currentPerfectTimingRecorder -= dt;
			if (_currentPerfectTimingRecorder <= 0f)
			{
				_currentPerfectTimingRecorder = _perfectTimingLength;
				_isIncrease = false;
			}
		}
	}

	public void Tick(float dt)
	{
		if (_isIncrease)
		{
			_value += dt;
			if (_value >= _interval)
			{
				_value = _interval;
				UpdatePerfectTiming(dt);
			}
		}
		else
		{
			_value -= dt;
			if (_value <= 0f)
			{
				_value = 0f;
				_isIncrease = true;
			}
		}
	}
}
