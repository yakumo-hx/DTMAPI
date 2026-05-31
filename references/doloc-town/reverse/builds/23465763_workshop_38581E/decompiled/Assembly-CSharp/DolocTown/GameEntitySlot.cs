using System;
using UnityEngine;

namespace DolocTown;

public class GameEntitySlot<T> where T : GameEntity
{
	private readonly string alias;

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
				Debug.LogWarning("Failed to create entity: " + typeof(T).Name);
			}
			return entity;
		}
	}

	public GameEntitySlot(string alias = null)
	{
		this.alias = alias;
		__apply_for = ((alias == null) ? new Func<T>(__ApplyFor) : new Func<T>(__ApplyForAlias));
		entity = null;
	}

	private T __ApplyFor()
	{
		return DolocAPI.EntitySystem.Next<T>();
	}

	private T __ApplyForAlias()
	{
		return DolocAPI.EntitySystem.Next<T>(alias);
	}

	public void Do(Action<T> callback)
	{
		T val = Entity;
		if (val == null)
		{
			Debug.LogWarning("<color=red>GameEntitySlot.Do: Failed to get entity: " + typeof(T).Name + "</color>");
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
		DolocAPI.EntitySystem.Recycle(entity, alias);
		entity = null;
	}

	public T Fetch()
	{
		return entity;
	}
}
