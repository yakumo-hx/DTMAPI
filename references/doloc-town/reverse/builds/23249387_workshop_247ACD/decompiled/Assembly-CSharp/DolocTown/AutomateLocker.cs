using System.Collections.Generic;
using RedSaw;

namespace DolocTown;

public class AutomateLocker<T> where T : IHasIndex
{
	private readonly Dictionary<T, AutomateBot> lockedThings = new Dictionary<T, AutomateBot>();

	private readonly Queue<T> _clearQueue = new Queue<T>();

	public bool IsLocked(T thing)
	{
		return lockedThings.ContainsKey(thing);
	}

	public void Lock(T thing, AutomateBot bot)
	{
		if (thing != null && bot != null && thing.index >= 0)
		{
			lockedThings.TryAdd(thing, bot);
		}
	}

	public void Unlock(T thing)
	{
		if (thing != null && lockedThings.ContainsKey(thing))
		{
			lockedThings.Remove(thing);
		}
	}

	public void ClearInvalidThings()
	{
		_clearQueue.Clear();
		foreach (T key in lockedThings.Keys)
		{
			if (key.index < 0)
			{
				_clearQueue.Enqueue(key);
			}
		}
		while (_clearQueue.Count > 0)
		{
			lockedThings.Remove(_clearQueue.Dequeue());
		}
	}

	public void ClearLockedThingsOfBot(AutomateBot bot)
	{
		_clearQueue.Clear();
		foreach (KeyValuePair<T, AutomateBot> lockedThing in lockedThings)
		{
			if (lockedThing.Value == bot)
			{
				_clearQueue.Enqueue(lockedThing.Key);
			}
		}
		while (_clearQueue.Count > 0)
		{
			lockedThings.Remove(_clearQueue.Dequeue());
		}
	}
}
