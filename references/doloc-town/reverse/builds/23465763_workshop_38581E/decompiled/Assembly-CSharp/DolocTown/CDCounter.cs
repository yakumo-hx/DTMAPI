using System;
using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class CDCounter<T> where T : Enum
{
	private readonly RSTimer[] timers = new RSTimer[Enum.GetValues(typeof(T)).Length];

	private readonly List<int> currentCdList = new List<int>();

	private readonly Queue<Vector2Int> changes = new Queue<Vector2Int>();

	private bool IsProtected;

	public void ResetCounter(T type, float duration)
	{
		int hashCode = type.GetHashCode();
		if (timers[hashCode] == null)
		{
			timers[hashCode] = new RSTimer(duration);
		}
		else
		{
			timers[hashCode].SetInterval(duration);
		}
	}

	public bool IsAvailable(T type)
	{
		int hashCode = type.GetHashCode();
		if (timers[hashCode] != null)
		{
			return !currentCdList.Contains(hashCode);
		}
		return false;
	}

	public void Use(T type)
	{
		int hashCode = type.GetHashCode();
		if (timers[hashCode] != null && !currentCdList.Contains(hashCode))
		{
			if (IsProtected)
			{
				changes.Enqueue(new Vector2Int(hashCode, 1));
			}
			else
			{
				currentCdList.Add(hashCode);
			}
		}
	}

	public void ReduceCD(T type, float t = 1f)
	{
		int hashCode = type.GetHashCode();
		if (timers[hashCode] != null && currentCdList.Contains(hashCode))
		{
			timers[hashCode].Offset(t);
		}
	}

	public void Update(float dt)
	{
		if (currentCdList.Count == 0)
		{
			return;
		}
		IsProtected = true;
		currentCdList.ForEach(delegate(int idx)
		{
			if (timers[idx].Tick(dt))
			{
				changes.Enqueue(new Vector2Int(idx, 0));
			}
		});
		IsProtected = false;
		while (changes.Count > 0)
		{
			Vector2Int vector2Int = changes.Dequeue();
			if (vector2Int.y == 0)
			{
				currentCdList.Remove(vector2Int.x);
			}
			else
			{
				currentCdList.Add(vector2Int.x);
			}
		}
	}
}
