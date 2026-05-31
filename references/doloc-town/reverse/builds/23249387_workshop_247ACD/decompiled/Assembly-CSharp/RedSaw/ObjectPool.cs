using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedSaw;

public class ObjectPool
{
	private readonly PoolCore core;

	private readonly Queue<GameObject> pool;

	private readonly List<GameObject> instances;

	public int ActiveCount => instances.Count;

	public GameObject this[int index] => instances[index];

	public Action<GameObject> OnCreate
	{
		get
		{
			return core.OnCreated;
		}
		set
		{
			core.OnCreated = value;
		}
	}

	public Action<GameObject> OnRecycle { get; set; }

	public virtual GameObject Next
	{
		get
		{
			GameObject gameObject = ((pool.Count > 0) ? pool.Dequeue() : core.NewInstance);
			instances.Add(gameObject);
			gameObject.SetActive(value: true);
			return gameObject;
		}
	}

	public ObjectPool(GameObject pfb, Transform container)
	{
		core = new PoolCore(pfb, container);
		pool = new Queue<GameObject>();
		instances = new List<GameObject>();
	}

	public GameObject[] CopyToArray()
	{
		return instances.ToArray();
	}

	public T[] CopyToArray<T>() where T : MonoBehaviour
	{
		T[] array = new T[instances.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = instances[i].GetComponent<T>();
		}
		return array;
	}

	private void InvokeOnRecycle(GameObject obj)
	{
		try
		{
			OnRecycle?.Invoke(obj);
		}
		catch (Exception ex)
		{
			Debug.LogError("Recycle Error: " + ex.Message);
			Debug.LogException(ex);
		}
	}

	public virtual void Recycle(GameObject value)
	{
		if (!(value == null) && !pool.Contains(value))
		{
			if (instances.Contains(value))
			{
				value.SetActive(value: false);
				pool.Enqueue(value);
				instances.Remove(value);
				InvokeOnRecycle(value);
			}
			else
			{
				UnityEngine.Object.Destroy(value);
			}
		}
	}

	public void RecycleAll()
	{
		GameObject[] array = instances.ToArray();
		foreach (GameObject gameObject in array)
		{
			gameObject.SetActive(value: false);
			pool.Enqueue(gameObject);
			instances.Remove(gameObject);
			InvokeOnRecycle(gameObject);
		}
	}

	public void Iterate(Action<GameObject> handle)
	{
		foreach (GameObject instance in instances)
		{
			handle(instance);
		}
	}

	public void Iterate(Action<GameObject, int> handle)
	{
		for (int i = 0; i < instances.Count; i++)
		{
			handle(instances[i], i);
		}
	}

	public void CheckCount(int count)
	{
		if (count == instances.Count)
		{
			return;
		}
		int num = instances.Count - count;
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				Recycle(instances[count]);
			}
			return;
		}
		num *= -1;
		for (int j = 0; j < num; j++)
		{
			ActiveNext();
		}
	}

	public void ActiveNext()
	{
		_ = Next;
	}
}
public class ObjectPool<T> : IEnumerable<T>, IEnumerable where T : MonoBehaviour, IRecyclable
{
	protected readonly PoolCore<T> core;

	protected readonly Queue<T> pool;

	public List<T> Instances { get; private set; }

	public int ActiveCount => Instances.Count;

	public int CacheCount => pool.Count;

	public T this[int index] => Instances[index];

	public Transform Container => core.Container;

	public GameObject Prefab => core.Prefab;

	public Action<T> OnCreate
	{
		get
		{
			return core.OnCreated;
		}
		set
		{
			core.OnCreated = value;
		}
	}

	public Action<T> OnRecycle { get; set; }

	public Action<T> OnReuse { get; set; }

	public virtual T Next
	{
		get
		{
			T val = ((pool.Count > 0) ? pool.Dequeue() : core.NewInstance);
			Instances.Add(val);
			val.OnReuseProtected();
			InvokeOnReuse(val);
			return val;
		}
	}

	public ObjectPool(GameObject pfb, Transform container, bool usePreset = false, bool disableRecycleOnInit = false)
	{
		core = new PoolCore<T>(pfb, container);
		pool = new Queue<T>();
		Instances = new List<T>();
		if (!usePreset)
		{
			return;
		}
		T[] componentsInChildren = container.GetComponentsInChildren<T>(includeInactive: true);
		core.Spawn(componentsInChildren);
		T[] array = componentsInChildren;
		foreach (T val in array)
		{
			Instances.Add(val);
			if (disableRecycleOnInit)
			{
				if (val != null)
				{
					val.OnCreatedProtected();
				}
				try
				{
					OnCreate?.Invoke(val);
				}
				catch (Exception ex)
				{
					Debug.LogError("Created Error: " + ex.Message);
					Debug.LogException(ex);
				}
			}
			else
			{
				Recycle(val);
			}
		}
	}

	protected void InvokeOnRecycle(T obj)
	{
		try
		{
			OnRecycle?.Invoke(obj);
		}
		catch (Exception ex)
		{
			Debug.LogError("Recycle Error: " + ex.Message);
			Debug.LogException(ex);
		}
	}

	protected void InvokeOnReuse(T obj)
	{
		try
		{
			OnReuse?.Invoke(obj);
		}
		catch (Exception ex)
		{
			Debug.LogError("Reuse Error: " + ex.Message);
			Debug.LogException(ex);
		}
	}

	public void Sort()
	{
		for (int i = 0; i < Instances.Count; i++)
		{
			Instances[i].transform.SetSiblingIndex(i);
		}
	}

	public virtual void Recycle(T value)
	{
		if (!(value == null) && !pool.Contains(value))
		{
			if (!Instances.Contains(value))
			{
				UnityEngine.Object.Destroy(value.gameObject);
				return;
			}
			pool.Enqueue(value);
			Instances.Remove(value);
			value.OnRecycleProtected();
			InvokeOnRecycle(value);
		}
	}

	public virtual void RecycleAll()
	{
		T[] array = Instances.ToArray();
		foreach (T value in array)
		{
			Recycle(value);
		}
	}

	public void ForEach(Action<T> handle)
	{
		foreach (T instance in Instances)
		{
			handle(instance);
		}
	}

	public void ForEach(Action<T, int> handle)
	{
		for (int i = 0; i < Instances.Count; i++)
		{
			handle(Instances[i], i);
		}
	}

	public void CheckCount(int count)
	{
		if (count == Instances.Count)
		{
			return;
		}
		int num = Instances.Count - count;
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				Recycle(Instances[count]);
			}
		}
		else
		{
			for (int j = 0; j < -num; j++)
			{
				ActiveNext();
			}
		}
	}

	public void ActiveNext()
	{
		_ = Next;
	}

	public void Destroy()
	{
		RecycleAll();
		DestroyGarbage();
	}

	public void DestroyGarbage()
	{
		while (pool.Count > 0)
		{
			T val = pool.Dequeue();
			if (val != null && val.gameObject != null)
			{
				UnityEngine.Object.Destroy(val.gameObject);
			}
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		return Instances.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return Instances.GetEnumerator();
	}
}
