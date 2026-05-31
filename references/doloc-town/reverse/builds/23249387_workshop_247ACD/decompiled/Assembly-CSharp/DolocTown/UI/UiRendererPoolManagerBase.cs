using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.UI;
using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public abstract class UiRendererPoolManagerBase : DolocUiEntity
{
	private readonly Dictionary<string, NashObjectPoolEx<DolocUiRecyclableObject>> pools = new Dictionary<string, NashObjectPoolEx<DolocUiRecyclableObject>>();

	private readonly Dictionary<string, Transform> containers = new Dictionary<string, Transform>();

	protected override void __Init()
	{
		base.__Init();
		SetVisible(value: true);
	}

	private Transform GetContainer(string poolName)
	{
		if (!containers.TryGetValue(poolName, out var value))
		{
			value = new GameObject(poolName).AddComponent<RectTransform>();
			value.SetParent(base.transform);
			value.transform.localScale = Vector3.one;
			containers[poolName] = value;
		}
		return value;
	}

	private NashObjectPoolEx<DolocUiRecyclableObject> GetPool(string key)
	{
		if (!pools.TryGetValue(key, out var value))
		{
			UIPooledObjectInfo orDefault = DolocConfig.Tables.TbUIPooledObject.GetOrDefault(key);
			if (orDefault == null)
			{
				Debug.LogError("池对象<" + key + ">没有对应配置");
				return null;
			}
			GameObject asset = orDefault.PrefabAsset.Asset;
			if (asset == null)
			{
				Debug.LogError("加载资源<" + key + ">失败");
				return null;
			}
			Transform container = GetContainer(orDefault.PoolName);
			value = new NashObjectPoolEx<DolocUiRecyclableObject>(asset, container);
			pools.Add(key, value);
		}
		return value;
	}

	public T GetFromPool<T>() where T : DolocUiRecyclableObject
	{
		NashObjectPoolEx<DolocUiRecyclableObject> pool = GetPool(typeof(T).Name);
		T val = ((pool != null) ? pool.Next.GetComponent<T>() : null);
		if (val != null)
		{
			val.SetVisible(value: true);
		}
		return val;
	}

	public void RecycleToPool<T>(T value) where T : DolocUiRecyclableObject
	{
		NashObjectPoolEx<DolocUiRecyclableObject> pool = GetPool(typeof(T).Name);
		if (pool != null)
		{
			pool.Recycle(value);
		}
		else
		{
			Object.Destroy(value.gameObject);
		}
	}

	public void Clear()
	{
		foreach (NashObjectPoolEx<DolocUiRecyclableObject> value in pools.Values)
		{
			value.RecycleAll();
		}
	}

	public Transform GetContainer<T>()
	{
		UIPooledObjectInfo orDefault = DolocConfig.Tables.TbUIPooledObject.GetOrDefault(typeof(T).Name);
		if (orDefault == null)
		{
			return null;
		}
		containers.TryGetValue(orDefault.PoolName, out var value);
		return value;
	}
}
