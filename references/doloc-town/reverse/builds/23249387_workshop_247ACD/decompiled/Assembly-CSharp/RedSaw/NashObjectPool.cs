using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedSaw;

public class NashObjectPool<T> : ObjectPool<T> where T : MonoBehaviour, IRecyclable
{
	private readonly int frequency;

	private readonly int threshold;

	private readonly Queue<T> garbage = new Queue<T>();

	private readonly List<T> buffer = new List<T>();

	private int store;

	private bool isLocked;

	public override T Next
	{
		get
		{
			T val = ((garbage.Count > 0) ? garbage.Dequeue() : ((pool.Count > 0) ? pool.Dequeue() : core.NewInstance));
			base.Instances.Add(val);
			val.OnReuseProtected();
			InvokeOnReuse(val);
			return val;
		}
	}

	public NashObjectPool(GameObject pfb, Transform container, int threshold = 5, int frequency = 3, bool usePreset = false)
		: base(pfb, container, usePreset, disableRecycleOnInit: false)
	{
		this.threshold = threshold;
		this.frequency = frequency;
	}

	private bool ContainsObj(T value)
	{
		if (pool == null || pool.Count == 0)
		{
			return false;
		}
		try
		{
			return pool.Contains(value);
		}
		catch (ArgumentOutOfRangeException)
		{
			return false;
		}
	}

	public override void Recycle(T value)
	{
		if (value == null || ContainsObj(value))
		{
			return;
		}
		if (!base.Instances.Contains(value))
		{
			UnityEngine.Object.Destroy(value.gameObject);
			return;
		}
		if (isLocked)
		{
			Debug.Log("<color=red>对象池递归回收</color>");
			buffer.Add(value);
			return;
		}
		isLocked = true;
		base.Instances.Remove(value);
		value.OnRecycleProtected();
		InvokeOnRecycle(value);
		if (pool.Count < threshold)
		{
			pool.Enqueue(value);
			UpdateGarbageQueue();
			Unlock();
		}
		else if (--store <= 0)
		{
			store = frequency;
			UnityEngine.Object.Destroy(value.gameObject);
			Unlock();
		}
		else
		{
			garbage.Enqueue(value);
			Unlock();
		}
	}

	public void Unlock()
	{
		if (!isLocked)
		{
			return;
		}
		isLocked = false;
		foreach (T item in buffer)
		{
			Recycle(item);
		}
		buffer.Clear();
	}

	protected void UpdateGarbageQueue()
	{
		if (garbage.Count != 0 && --store <= 0)
		{
			store = frequency;
			UnityEngine.Object.Destroy(garbage.Dequeue().gameObject);
		}
	}

	public override void RecycleAll()
	{
		T[] array = base.Instances.ToArray();
		foreach (T value in array)
		{
			Recycle(value);
		}
	}
}
