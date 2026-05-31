using System;
using System.Collections.Generic;
using System.Linq;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public abstract class GameEntityManager
{
	public readonly Transform transform;

	public readonly bool customManaged;

	public readonly string alias;

	protected bool hasNestedPool;

	public Transform Container => transform;

	public abstract int ActivedGameEntityCount { get; }

	public bool HasNestedPool => hasNestedPool;

	public abstract GameObject Prefab { get; }

	public abstract int CacheCount { get; }

	public abstract string RecycleCounterInfo { get; }

	public GameEntityManager(Transform transform, bool customManaged, string alias = null)
	{
		this.transform = transform;
		this.customManaged = customManaged;
		this.alias = alias ?? string.Empty;
	}

	public abstract Transform GetContainer(string alias);

	public abstract bool ComposeManager(GameEntityManager manager, string alias);

	public T NextGameEntity<T>() where T : GameEntity
	{
		return (T)NextGameEntity();
	}

	public T NextGameEntityWithLimitation<T>(int limitation) where T : GameEntity
	{
		return (T)NextGameEntityLimited(limitation);
	}

	public T NextGameEntity<T>(string alias) where T : GameEntity
	{
		return (T)NextGameEntity(alias);
	}

	public IEnumerable<T> NextGameEntities<T>(int count) where T : GameEntity
	{
		return NextGameEntities(count).Cast<T>();
	}

	public IEnumerable<T> NextGameEntities<T>(int count, string alias) where T : GameEntity
	{
		return NextGameEntitiesEx(count, alias).Cast<T>();
	}

	public abstract GameEntity NextGameEntity();

	public abstract GameEntity NextGameEntityLimited(int limit);

	public abstract GameEntity NextGameEntity(string alias);

	public abstract IEnumerable<GameEntity> NextGameEntities(int count);

	public abstract IEnumerable<GameEntity> NextGameEntitiesEx(int count, string alias);

	public abstract void Recycle(GameEntity entity);

	public abstract void Recycle(GameEntity entity, string alias);

	public abstract void Clear();

	public abstract void Clear(string alias);
}
public class GameEntityManager<T> : GameEntityManager where T : GameEntity
{
	private readonly NashObjectPoolEx<T> pool;

	private readonly Dictionary<string, NashObjectPoolEx<T>> nestedPools = new Dictionary<string, NashObjectPoolEx<T>>();

	public NashObjectPoolEx<T> Pool => pool;

	public override GameObject Prefab => pool.Prefab;

	public override int CacheCount => pool.CacheCount;

	public override string RecycleCounterInfo => pool.RecycleCounterInfo;

	public override int ActivedGameEntityCount
	{
		get
		{
			int num = pool.ActiveCount;
			foreach (NashObjectPoolEx<T> value in nestedPools.Values)
			{
				num += value.ActiveCount;
			}
			return num;
		}
	}

	public GameEntityManager(Transform transform, GameObject prefab, int threshold = 5, int frequency = 3, bool customManagement = false, string alias = null)
		: base(transform, customManagement, alias)
	{
		pool = new NashObjectPoolEx<T>(prefab, transform, frequency);
		transform.gameObject.AddComponent<GameEntityManagerInfo>().gameEntityManager = this;
	}

	public void SetCreatedCallback(Action<T> callback)
	{
		pool.OnCreate = callback;
	}

	public void SetCreatedCallback(Action<T> callback, string alias)
	{
		if (!string.IsNullOrEmpty(alias) && nestedPools.TryGetValue(alias, out var value))
		{
			value.OnCreate = callback;
		}
	}

	public override Transform GetContainer(string alias)
	{
		if (string.IsNullOrEmpty(alias))
		{
			return transform;
		}
		if (nestedPools.TryGetValue(alias, out var value))
		{
			return value.Container;
		}
		return null;
	}

	public override bool ComposeManager(GameEntityManager manager, string alias)
	{
		if (string.IsNullOrEmpty(alias))
		{
			return false;
		}
		if (manager is GameEntityManager<T> gameEntityManager && !nestedPools.ContainsKey(alias))
		{
			hasNestedPool = true;
			nestedPools.Add(alias, gameEntityManager.pool);
			return true;
		}
		return false;
	}

	public override GameEntity NextGameEntity()
	{
		return pool.Next;
	}

	public override GameEntity NextGameEntityLimited(int limit)
	{
		return (pool.ActiveCount < limit) ? pool.Next : null;
	}

	public override IEnumerable<GameEntity> NextGameEntities(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return pool.Next;
		}
	}

	public override GameEntity NextGameEntity(string alias)
	{
		if (alias == null)
		{
			return NextGameEntity();
		}
		if (nestedPools.TryGetValue(alias, out var value))
		{
			return value.Next;
		}
		return null;
	}

	public override IEnumerable<GameEntity> NextGameEntitiesEx(int count, string alias)
	{
		if (alias == null)
		{
			return NextGameEntities(count);
		}
		if (nestedPools.TryGetValue(alias, out var value))
		{
			return NextGameEntitiesFromPool(value, count);
		}
		return null;
	}

	public override void Recycle(GameEntity entity)
	{
		if (entity is T value)
		{
			pool.Recycle(value);
		}
		else
		{
			UnityEngine.Object.Destroy(entity.gameObject);
		}
	}

	public override void Recycle(GameEntity entity, string alias)
	{
		NashObjectPoolEx<T> value2;
		if (string.IsNullOrEmpty(alias))
		{
			Recycle(entity);
		}
		else if (entity is T value && nestedPools.TryGetValue(alias, out value2))
		{
			value2.Recycle(value);
		}
		else
		{
			UnityEngine.Object.Destroy(entity.gameObject);
		}
	}

	public override void Clear()
	{
		pool.RecycleAll();
		foreach (NashObjectPoolEx<T> value in nestedPools.Values)
		{
			value.RecycleAll();
		}
	}

	public override void Clear(string alias)
	{
		NashObjectPoolEx<T> value;
		if (string.IsNullOrEmpty(alias))
		{
			pool.RecycleAll();
		}
		else if (nestedPools.TryGetValue(alias, out value))
		{
			value.RecycleAll();
		}
	}

	public T NextEntity()
	{
		return pool.Next;
	}

	public IEnumerable<T> NextEntities(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return pool.Next;
		}
	}

	public void Recycle(T entity)
	{
		pool.Recycle(entity);
	}

	public void SetupAll<TData>(IEnumerable<TData> datas, Action<T, TData> func) where TData : class
	{
		foreach (TData data in datas)
		{
			func(pool.Next, data);
		}
	}

	public void HandleAll(Action<T> func)
	{
		foreach (T instance in pool.Instances)
		{
			func(instance);
		}
	}

	public T NextEntity(string alias)
	{
		if (string.IsNullOrEmpty(alias))
		{
			return pool.Next;
		}
		if (nestedPools.TryGetValue(alias, out var value))
		{
			return value.Next;
		}
		return null;
	}

	public IEnumerable<T> NextEntities(int count, string alias)
	{
		if (string.IsNullOrEmpty(alias))
		{
			return NextEntities(count);
		}
		if (nestedPools.TryGetValue(alias, out var value))
		{
			return NextEntitiesFromPool(value, count);
		}
		return null;
	}

	public void Recycle(T entity, string alias)
	{
		NashObjectPoolEx<T> value;
		if (string.IsNullOrEmpty(alias))
		{
			pool.Recycle(entity);
		}
		else if (nestedPools.TryGetValue(alias, out value))
		{
			value.Recycle(entity);
		}
		else
		{
			UnityEngine.Object.Destroy(entity.gameObject);
		}
	}

	public void SetupAll<TData>(IEnumerable<TData> datas, Action<T, TData> func, string alias) where TData : class
	{
		if (string.IsNullOrEmpty(alias))
		{
			SetupAll(datas, func);
		}
		else
		{
			if (!nestedPools.TryGetValue(alias, out var value))
			{
				return;
			}
			foreach (TData data in datas)
			{
				func(value.Next, data);
			}
		}
	}

	public void HandleAll(Action<T> func, string alias)
	{
		if (string.IsNullOrEmpty(alias))
		{
			HandleAll(func);
		}
		else
		{
			if (!nestedPools.TryGetValue(alias, out var value))
			{
				return;
			}
			foreach (T instance in value.Instances)
			{
				func(instance);
			}
		}
	}

	private IEnumerable<T> NextEntitiesFromPool(NashObjectPoolEx<T> pool, int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return pool.Next;
		}
	}

	private IEnumerable<GameEntity> NextGameEntitiesFromPool(NashObjectPoolEx<T> pool, int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return pool.Next;
		}
	}
}
