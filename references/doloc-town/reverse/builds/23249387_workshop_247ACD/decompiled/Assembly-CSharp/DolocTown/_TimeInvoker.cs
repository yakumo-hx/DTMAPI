using System;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class _TimeInvoker
{
	private readonly RSTimer _timer = new RSTimer();

	private Action _callback;

	private int _count;

	public void Start(float interval, Action callback, int count)
	{
		_timer.SetInterval(interval);
		_callback = callback;
		_count = Mathf.Max(0, count - 1);
		callback?.Invoke();
	}

	public bool Update(float dt)
	{
		if (_timer.Tick(dt))
		{
			_callback?.Invoke();
			return --_count <= 0;
		}
		return false;
	}
}
