using DolocTown;
using DolocTown.UI;
using RedSaw;
using UnityEngine;

public static class DolocGameAssetsPatch
{
	public static Material LoadMaterial(this DolocGameAssets assetId)
	{
		return DolocAPI.GetAsset<Material>(assetId);
	}

	public static GameObject CreateEntity(this DolocGameAssets assetId, Transform parent = null)
	{
		GameObject asset = DolocAPI.GetAsset<GameObject>(assetId);
		if (asset == null)
		{
			return null;
		}
		if (parent != null)
		{
			return Object.Instantiate(asset, parent);
		}
		return Object.Instantiate(asset);
	}

	public static T CreateEntity<T>(this DolocGameAssets assetId, Transform parent = null) where T : MonoBehaviour
	{
		GameObject gameObject = assetId.CreateEntity(parent);
		if (gameObject == null)
		{
			return null;
		}
		T component = gameObject.GetComponent<T>();
		if (component is DolocObject dolocObject)
		{
			dolocObject.Init();
		}
		else if (component is DolocUiObject dolocUiObject)
		{
			dolocUiObject.Init();
		}
		return gameObject.GetComponent<T>();
	}

	public static NashObjectPoolEx<T> CreateNashPool<T>(this DolocGameAssets assetsId, Transform container, int frequency = 3) where T : DolocRecyclableObject
	{
		return new NashObjectPoolEx<T>(DolocAPI.GetAsset<GameObject>(assetsId), container, frequency);
	}

	public static ObjectPool<T> CreatePool<T>(this DolocGameAssets assetsId, Transform container, bool usePreset = false) where T : DolocRecyclableObject
	{
		return new ObjectPool<T>(DolocAPI.GetAsset<GameObject>(assetsId), container, usePreset);
	}
}
