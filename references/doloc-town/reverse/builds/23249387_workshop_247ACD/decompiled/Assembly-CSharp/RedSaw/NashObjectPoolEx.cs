using UnityEngine;

namespace RedSaw;

public class NashObjectPoolEx<T> : ObjectPool<T> where T : MonoBehaviour, IRecyclable
{
	private readonly Counter counter;

	public string RecycleCounterInfo => counter.ToString();

	public override T Next
	{
		get
		{
			T val = ((pool.Count > 0) ? pool.Dequeue() : core.NewInstance);
			counter.Reset();
			base.Instances.Add(val);
			val.OnReuseProtected();
			InvokeOnReuse(val);
			return val;
		}
	}

	public NashObjectPoolEx(GameObject pfb, Transform container, int frequency = 5)
		: base(pfb, container, usePreset: false, disableRecycleOnInit: false)
	{
		counter = new Counter(frequency);
	}

	private void NashRecycle(T obj, bool tick = false)
	{
		if (!(obj == null) && !pool.Contains(obj))
		{
			if (!base.Instances.Contains(obj))
			{
				Object.Destroy(obj.gameObject);
				return;
			}
			base.Instances.Remove(obj);
			obj.OnRecycleProtected();
			InvokeOnRecycle(obj);
			HandleRecycleEvent(obj, tick);
		}
	}

	public override void Recycle(T value)
	{
		if (!(value == null) && !pool.Contains(value))
		{
			if (!base.Instances.Contains(value))
			{
				Object.Destroy(value.gameObject);
				return;
			}
			base.Instances.Remove(value);
			value.OnRecycleProtected();
			InvokeOnRecycle(value);
			HandleRecycleEvent(value, tick: true);
		}
	}

	private void HandleRecycleEvent(T obj, bool tick)
	{
		pool.Enqueue(obj);
		if (tick && counter.Tick() && pool.Count > 0)
		{
			Object.Destroy(pool.Dequeue().gameObject);
		}
	}

	public override void RecycleAll()
	{
		T[] array = base.Instances.ToArray();
		foreach (T obj in array)
		{
			NashRecycle(obj);
		}
	}
}
