using System;
using UnityEngine;

namespace RedSaw;

public class NashObjectPoolGroup<T1, T2> where T1 : Enum where T2 : MonoBehaviour, IRecyclable
{
	private readonly NashObjectPool<T2>[] pools;

	private readonly Transform container;

	private readonly int threshold;

	private readonly int frequency;

	public NashObjectPoolGroup(Transform container, int threshold = 10, int frequency = 5)
	{
		pools = new NashObjectPool<T2>[Enum.GetNames(typeof(T1)).Length];
		this.container = container;
		this.threshold = threshold;
		this.frequency = frequency;
	}

	public NashObjectPool<T2> AppendPool(T1 type, GameObject pfb)
	{
		NashObjectPool<T2> nashObjectPool = new NashObjectPool<T2>(pfb, container, threshold, frequency);
		pools[type.GetHashCode()] = nashObjectPool;
		return nashObjectPool;
	}

	public T2 Next(T1 type)
	{
		NashObjectPool<T2> obj = pools[type.GetHashCode()];
		if (obj == null)
		{
			return null;
		}
		return obj.Next;
	}

	public void Recycle(T1 type, T2 value)
	{
		pools[type.GetHashCode()]?.Recycle(value);
	}

	public void RecycleAll()
	{
		NashObjectPool<T2>[] array = pools;
		foreach (NashObjectPool<T2> nashObjectPool in array)
		{
			if (nashObjectPool != null)
			{
				nashObjectPool.RecycleAll();
				nashObjectPool.DestroyGarbage();
			}
		}
	}
}
