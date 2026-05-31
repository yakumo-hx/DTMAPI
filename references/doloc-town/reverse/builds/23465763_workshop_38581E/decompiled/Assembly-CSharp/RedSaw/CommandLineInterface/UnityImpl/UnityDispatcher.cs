using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace RedSaw.CommandLineInterface.UnityImpl;

public class UnityDispatcher : MonoBehaviour
{
	private static readonly ConcurrentBag<Action> pending = new ConcurrentBag<Action>();

	public void Invoke(Action fn)
	{
		pending.Add(fn);
	}

	private void Update()
	{
		InvokePending();
	}

	private void InvokePending()
	{
		while (!pending.IsEmpty)
		{
			pending.TryTake(out var result);
			try
			{
				result();
			}
			catch (Exception message)
			{
				Debug.LogError("Error happened during invoking action with Dispatcher. Error : ");
				Debug.LogError(message);
			}
		}
	}
}
