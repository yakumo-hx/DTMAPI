using System;

namespace RedSaw;

public class DoubleTimeOperation
{
	private readonly RSTimer _timer;

	private readonly Action<bool> _latchCallback;

	private bool _latch;

	public DoubleTimeOperation(Action<bool> latchCallback, float interval = 2f)
	{
		_timer = new RSTimer(interval);
		_latchCallback = latchCallback;
	}

	public void Update(float dt)
	{
		if (_latch && _timer.Tick(dt))
		{
			_latch = false;
		}
	}

	public void Invoke()
	{
		if (_latch)
		{
			_latchCallback(obj: true);
			_latch = false;
		}
		else
		{
			_latchCallback(obj: false);
			_latch = true;
			_timer.Reset();
		}
	}
}
