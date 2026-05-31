using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class RoutineManager
{
	private readonly List<IMessageReceiver> _routines = new List<IMessageReceiver>();

	public bool RegisterRoutine(IMessageReceiver receiver)
	{
		if (_routines.Contains(receiver))
		{
			return false;
		}
		_routines.Add(receiver);
		return true;
	}

	public void RemoveRoutine(IMessageReceiver receiver)
	{
		if (_routines.Contains(receiver))
		{
			_routines.Remove(receiver);
		}
	}

	public void SendMessage(GameMessage message)
	{
		foreach (IMessageReceiver routine in _routines)
		{
			routine.SendMessage(message);
		}
	}

	public void StartAll(IEnumerable<IMessageReceiver> routines)
	{
		if (routines == null)
		{
			return;
		}
		foreach (IMessageReceiver routine in routines)
		{
			RegisterRoutine(routine);
		}
		Debug.Log($"当前共注册{_routines.Count}个Routine计划表");
	}
}
