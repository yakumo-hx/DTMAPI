using System;

namespace DolocTown;

public class MessageSystem<T> where T : Enum
{
	private readonly GameEvent[] _events = InitializeEvents<T>();

	private static GameEvent[] InitializeEvents<T>() where T : Enum
	{
		GameEvent[] array = new GameEvent[Enum.GetValues(typeof(T)).Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new GameEvent();
		}
		return array;
	}

	public void Register(T eventType, Action<object, GameEventArgs> callback)
	{
		_events[eventType.GetHashCode()].Register(callback);
	}

	public void Unregister(T eventType, Action<object, GameEventArgs> callback)
	{
		_events[eventType.GetHashCode()].Unregister(callback);
	}

	public void Broadcast(T type, GameEventArgs args, object sender = null)
	{
		_events[type.GetHashCode()].Raise(sender, args);
	}
}
