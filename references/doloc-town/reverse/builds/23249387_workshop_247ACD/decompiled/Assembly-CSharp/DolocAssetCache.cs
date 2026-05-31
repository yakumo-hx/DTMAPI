using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DolocTown;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

public class DolocAssetCache
{
	private readonly AssetLabelReference label;

	private readonly Dictionary<string, Dictionary<string, UnityEngine.Object>> assets = new Dictionary<string, Dictionary<string, UnityEngine.Object>>();

	public DolocAssetCache(AssetLabelReference label)
	{
		this.label = label;
	}

	public IEnumerator Load(Action callback)
	{
		AsyncOperationHandle<IList<UnityEngine.Object>> handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(label, AddAsset);
		handle.Completed += delegate(AsyncOperationHandle<IList<UnityEngine.Object>> h)
		{
			CheckHandleState(h);
			callback?.Invoke();
		};
		while (!handle.IsDone)
		{
			yield return null;
		}
	}

	private void AddAsset(UnityEngine.Object asset)
	{
		if (asset is SpriteAtlas spriteAtlas)
		{
			AddSpriteAtlas(spriteAtlas);
			return;
		}
		string key = ((!(asset is RuntimeAnimatorController)) ? asset.GetType().Name : "RuntimeAnimatorController");
		if (!assets.ContainsKey(key))
		{
			assets.Add(key, new Dictionary<string, UnityEngine.Object>());
		}
		assets[key][GetAssetName(asset)] = asset;
	}

	private void AddSpriteAtlas(SpriteAtlas spriteAtlas)
	{
		string key = "Sprite";
		if (!assets.ContainsKey(key))
		{
			assets.Add(key, new Dictionary<string, UnityEngine.Object>());
		}
		Sprite[] array = new Sprite[spriteAtlas.spriteCount];
		spriteAtlas.GetSprites(array);
		Sprite[] array2 = array;
		foreach (Sprite sprite in array2)
		{
			assets[key][GetAssetName(sprite)] = sprite;
		}
	}

	private string GetAssetName(UnityEngine.Object obj)
	{
		return obj.name.Replace("(Clone)", "");
	}

	private void CheckHandleState<T>(AsyncOperationHandle<IList<T>> handle)
	{
		if (handle.Status == AsyncOperationStatus.Succeeded)
		{
			DolocAPI.outputSuccess($"{label}.{typeof(T)}加载成功!");
		}
		else
		{
			DolocAPI.outputError($"{label}.{typeof(T)}加载失败..");
		}
	}

	public TAsset[] GetAllAssetsOfType<TAsset>() where TAsset : UnityEngine.Object
	{
		string name = typeof(TAsset).Name;
		if (!assets.TryGetValue(name, out var _))
		{
			return Array.Empty<TAsset>();
		}
		return assets[name].Values.Select((UnityEngine.Object x) => x as TAsset).ToArray();
	}

	public bool CheckAsset<TAsset>(string address) where TAsset : UnityEngine.Object
	{
		if (address.IsNullOrEmpty())
		{
			return false;
		}
		string name = typeof(TAsset).Name;
		if (DolocAPI.UseMods && typeof(TAsset) == typeof(Sprite) && DolocAPI.modManager.LoadSpriteFromFile(address, out var _))
		{
			return true;
		}
		if (!assets.TryGetValue(name, out var value) || !value.TryGetValue(address, out var value2) || !(value2 is TAsset val))
		{
			return false;
		}
		return val != null;
	}

	public TAsset GetAsset<TAsset>(string address, bool useLog = true) where TAsset : UnityEngine.Object
	{
		if (address.IsNullOrEmpty())
		{
			return null;
		}
		string name = typeof(TAsset).Name;
		if (DolocAPI.UseMods && typeof(TAsset) == typeof(Sprite) && DolocAPI.modManager.LoadSpriteFromFile(address, out var asset))
		{
			return asset as TAsset;
		}
		if (!assets.TryGetValue(name, out var value) || !value.TryGetValue(address, out var value2) || !(value2 is TAsset result))
		{
			if (useLog)
			{
				Debug.LogError("获取资源失败！typeName: <" + name + ">, address: <" + address + ">");
			}
			return null;
		}
		return result;
	}
}
