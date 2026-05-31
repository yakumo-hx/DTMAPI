using System;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class GameEntitySlotUI<T> where T : DolocUiRecyclableObject
{
	private readonly bool inScene;

	private readonly Func<T> __apply_for;

	private T entity;

	public T Entity
	{
		get
		{
			if (entity != null)
			{
				return entity;
			}
			entity = __apply_for();
			if (entity == null)
			{
				Debug.LogWarning("Failed to create UI: " + typeof(T).Name);
			}
			return entity;
		}
	}

	public GameEntitySlotUI(bool inScene = true)
	{
		this.inScene = inScene;
		__apply_for = __ApplyFor;
		entity = null;
	}

	private T __ApplyFor()
	{
		return DolocAPI.uiSystem.GetFromPool<T>(inScene);
	}

	public void Do(Action<T> callback)
	{
		T val = Entity;
		if (val == null)
		{
			Debug.LogWarning("<color=red>GameEntitySlotUI.Do: Failed to get UI: " + typeof(T).Name + "</color>");
		}
		else
		{
			callback?.Invoke(val);
		}
	}

	public void DoIfExist(Action<T> callback)
	{
		if (!(entity == null))
		{
			callback?.Invoke(entity);
		}
	}

	public void Release()
	{
		DolocAPI.uiSystem.RecycleToPoolInScene(entity);
		entity = null;
	}

	public T Fetch()
	{
		return entity;
	}
}
