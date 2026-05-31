using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DolocTown;

public class GameEntitySystem<TAttr> where TAttr : GameEntityManagerBaseAttribute
{
	public readonly Transform root;

	private readonly Dictionary<Type, GameEntityManager> gems;

	private static Dictionary<Type, GameEntityManager> LoadGameEntityManagers(Transform root)
	{
		Dictionary<Type, GameEntityManager> dictionary = new Dictionary<Type, GameEntityManager>();
		Dictionary<string, Transform> pathCache = new Dictionary<string, Transform>();
		Type[] types = Assembly.GetExecutingAssembly().GetTypes();
		Type typeFromHandle = typeof(GameEntity);
		Type[] array = types;
		foreach (Type type in array)
		{
			if (!type.IsAbstract && typeFromHandle.IsAssignableFrom(type) && CreateManagersFromType(type, root, pathCache, out var gem))
			{
				dictionary.Add(type, gem);
			}
		}
		return dictionary;
	}

	private static bool CreateManagersFromType(Type type, Transform root, Dictionary<string, Transform> pathCache, out GameEntityManager gem)
	{
		IEnumerable<TAttr> customAttributes = type.GetCustomAttributes<TAttr>();
		gem = null;
		Dictionary<string, GameEntityManager> dictionary = new Dictionary<string, GameEntityManager>();
		foreach (TAttr item in customAttributes)
		{
			Transform transformFromPath = GetTransformFromPath(root, item.containerPath, pathCache);
			if (!CreateManagerFromAttribute(type, transformFromPath, item, out var gem2))
			{
				continue;
			}
			if (item.Alias.Length == 0)
			{
				if (gem != null)
				{
					Debug.LogError("GameEntitySystem.cs: duplicate game entity type " + type);
				}
				else
				{
					gem = gem2;
				}
			}
			else if (dictionary.ContainsKey(item.Alias))
			{
				Debug.LogError("GameEntitySystem.cs: duplicate alias " + item.Alias);
			}
			else
			{
				dictionary.Add(item.Alias, gem2);
			}
		}
		if (gem == null && dictionary.Count == 0)
		{
			return false;
		}
		foreach (KeyValuePair<string, GameEntityManager> item2 in dictionary)
		{
			if (gem == null)
			{
				gem = item2.Value;
			}
			if (!gem.ComposeManager(item2.Value, item2.Key))
			{
				Debug.LogWarning("GameEntitySystem.cs: compose manager " + type.Name + " failed " + item2.Key);
			}
		}
		return true;
	}

	private static bool CreateManagerFromAttribute(Type entityType, Transform container, GameEntityManagerBaseAttribute attr, out GameEntityManager gem)
	{
		GameObject gameObject = attr.LoadPrefab();
		if (gameObject == null)
		{
			Debug.LogError("GameEntitySystem.cs: load prefab failed \"" + attr.PrefabPath + "\"");
			gem = null;
			return false;
		}
		gem = (GameEntityManager)Activator.CreateInstance(typeof(GameEntityManager<>).MakeGenericType(entityType), container, gameObject, attr.Threshold, attr.Frequency, attr.CustomManagement, attr.Alias);
		attr.OnCreated(gem);
		return true;
	}

	private static Transform GetTransformFromPath(Transform transform, string path, Dictionary<string, Transform> cache)
	{
		if (path.Length == 0)
		{
			return transform;
		}
		if (cache.ContainsKey(path))
		{
			return cache[path];
		}
		if (!path.Contains('/'))
		{
			Transform transform2 = RetrieveChild(transform, path);
			cache.Add(path, transform2);
			return transform2;
		}
		string[] array = path.Split('/');
		for (int i = 0; i < array.Length; i++)
		{
			transform = RetrieveChild(transform, array[i]);
		}
		cache.Add(path, transform);
		return transform;
	}

	private static Transform RetrieveChild(Transform transform, string name)
	{
		name = name.Trim();
		if (name.Length == 0)
		{
			return transform;
		}
		Transform transform2 = transform.Find(name);
		if (transform2 == null)
		{
			transform2 = new GameObject(name).transform;
			transform2.SetParent(transform);
		}
		return transform2;
	}

	public GameEntitySystem(Transform transform)
	{
		root = transform;
		gems = LoadGameEntityManagers(transform);
	}

	public GameEntitySystem(GameObject gameObject)
		: this(gameObject.transform)
	{
	}

	public void Clear(bool force = false)
	{
		if (force)
		{
			ForceClear();
			return;
		}
		foreach (GameEntityManager value in gems.Values)
		{
			if (!value.customManaged)
			{
				value.Clear();
			}
		}
	}

	private void ForceClear()
	{
		foreach (GameEntityManager value in gems.Values)
		{
			value.Clear();
		}
	}

	public GameEntityManager<T> GetGameEntityManager<T>() where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			return (GameEntityManager<T>)value;
		}
		return null;
	}

	public T Next<T>() where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			return value.NextGameEntity<T>();
		}
		return null;
	}

	public GameEntity Next(Type type)
	{
		if (!typeof(GameEntity).IsAssignableFrom(type))
		{
			return null;
		}
		if (gems.TryGetValue(type, out var value))
		{
			return value.NextGameEntity();
		}
		return null;
	}

	public T NextLimit<T>(int limitation) where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			return value.NextGameEntityWithLimitation<T>(limitation);
		}
		return null;
	}

	public T Next<T>(string alias) where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			return value.NextGameEntity<T>(alias);
		}
		return null;
	}

	public IEnumerable<T> NextGroup<T>(int count) where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			return value.NextGameEntities<T>(count);
		}
		return null;
	}

	public IEnumerable<T> NextGroup<T>(int count, string alias) where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			return value.NextGameEntities<T>(count, alias);
		}
		return null;
	}

	public void Recycle<T>(T entity) where T : GameEntity
	{
		if (!(entity == null))
		{
			if (gems.TryGetValue(entity.GetType(), out var value))
			{
				value.Recycle(entity);
				return;
			}
			Debug.LogWarning($"GameEntitySystem.cs: Recycle entity failed, no manager found for type {entity.GetType()}");
			UnityEngine.Object.Destroy(entity.gameObject);
		}
	}

	public void Recycle<T>(T entity, string alias) where T : GameEntity
	{
		if (!(entity == null))
		{
			if (alias == null)
			{
				Recycle(entity);
				return;
			}
			if (gems.TryGetValue(entity.GetType(), out var value))
			{
				value.Recycle(entity, alias);
				return;
			}
			Debug.LogWarning($"GameEntitySystem.cs: Recycle entity failed, no manager found for type {entity.GetType()}");
			UnityEngine.Object.Destroy(entity.gameObject);
		}
	}

	public void Clear<T>() where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			value.Clear();
		}
	}

	public void Clear<T>(string alias) where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			value.Clear(alias);
		}
	}

	public GameObject GetContainer<T>()
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			return value.transform.gameObject;
		}
		return null;
	}

	public void SetCreatedCallback<T>(Action<T> callback) where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			((GameEntityManager<T>)value).SetCreatedCallback(callback);
		}
	}

	public void SetCreatedCallback<T>(Action<T> callback, string alias) where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			((GameEntityManager<T>)value).SetCreatedCallback(callback, alias);
		}
	}

	public void SetupAll<T, TData>(IEnumerable<TData> datas, Action<T, TData> func) where T : GameEntity where TData : class
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			((GameEntityManager<T>)value).SetupAll(datas, func);
		}
	}

	public void HandleAll<T>(Action<T> func) where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			((GameEntityManager<T>)value).HandleAll(func);
		}
	}

	public void SetupAll<T, TData>(IEnumerable<TData> datas, Action<T, TData> func, string alias) where T : GameEntity where TData : class
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			((GameEntityManager<T>)value).SetupAll(datas, func, alias);
		}
	}

	public void HandleAll<T>(Action<T> func, string alias) where T : GameEntity
	{
		if (gems.TryGetValue(typeof(T), out var value))
		{
			((GameEntityManager<T>)value).HandleAll(func, alias);
		}
	}
}
