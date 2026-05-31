using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedSaw;

public class PoolCore
{
	private readonly Transform container;

	private readonly GameObject pfb;

	public Action<GameObject> OnCreated { get; set; }

	public Transform Container => container;

	public GameObject NewInstance
	{
		get
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(pfb, container);
			InvokeOnCreated(gameObject);
			return gameObject;
		}
	}

	public PoolCore(GameObject pfb, Transform container)
	{
		this.pfb = pfb;
		this.container = container;
	}

	private void InvokeOnCreated(GameObject instance)
	{
		try
		{
			OnCreated?.Invoke(instance);
		}
		catch (Exception ex)
		{
			Debug.LogError("Created Error: " + ex.Message);
			Debug.LogException(ex);
		}
	}
}
public class PoolCore<T> where T : MonoBehaviour, IRecyclable
{
	private readonly Transform container;

	private readonly GameObject pfb;

	public Action<T> OnCreated { get; set; }

	public Transform Container => container;

	public GameObject Prefab => pfb;

	public T NewInstance
	{
		get
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(pfb, container);
			T val = gameObject.GetComponent<T>();
			if (val == null)
			{
				val = gameObject.AddComponent<T>();
			}
			val.OnCreatedProtected();
			InvokeOnCreated(val);
			return val;
		}
	}

	public PoolCore(GameObject pfb, Transform container)
	{
		this.pfb = pfb;
		this.container = container;
	}

	private void InvokeOnCreated(T instance)
	{
		try
		{
			OnCreated?.Invoke(instance);
		}
		catch (Exception ex)
		{
			Debug.LogError("Created Error: " + ex.Message);
			Debug.LogException(ex);
		}
	}

	public void Spawn(IEnumerable<T> components)
	{
		foreach (T component in components)
		{
			component.OnCreatedProtected();
			InvokeOnCreated(component);
		}
	}
}
