using System;
using System.Collections.Generic;

namespace DolocTown;

public class GameEvent
{
	private readonly HashSet<Action<object, GameEventArgs>> _subscribers = new HashSet<Action<object, GameEventArgs>>();

	protected event Action<object, GameEventArgs> OnEventTriggered;

	public void Register(Action<object, GameEventArgs> callback)
	{
		if (callback != null && _subscribers.Add(callback))
		{
			OnEventTriggered += callback;
		}
	}

	public void Unregister(Action<object, GameEventArgs> callback)
	{
		if (callback != null && _subscribers.Remove(callback))
		{
			OnEventTriggered -= callback;
		}
	}

	public void Raise(object sender, GameEventArgs args)
	{
		this.OnEventTriggered?.Invoke(sender, args);
	}
}
