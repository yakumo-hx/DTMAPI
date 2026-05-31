using UnityEngine;

namespace DolocTown;

public abstract class AssetBase<T>
{
	private T _asset;

	private bool _isLoaded;

	public string AssetUrl { get; private set; }

	public virtual T Asset
	{
		get
		{
			if (_isLoaded)
			{
				return _asset;
			}
			if (AssetUrl.IsNullOrEmpty())
			{
				return default(T);
			}
			if (TryLoadAsset(AssetUrl, out _asset))
			{
				_isLoaded = true;
				return _asset;
			}
			Debug.LogError("<" + GetType().Name + ">资源获取失败: " + AssetUrl);
			LoadDefaultAsset(out _asset);
			return _asset;
		}
	}

	protected abstract bool TryLoadAsset(string url, out T asset);

	protected virtual bool LoadDefaultAsset(out T asset)
	{
		asset = default(T);
		return false;
	}

	public AssetBase(string assetUrl)
	{
		AssetUrl = assetUrl;
	}
}
