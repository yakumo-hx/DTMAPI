namespace DolocTown;

public abstract class AssetArrayBase<TRef, TAsset> where TRef : AssetBase<TAsset>
{
	protected bool loaded;

	private TAsset[] _assets;

	public TRef[] refs { get; protected set; }

	public TAsset[] assets
	{
		get
		{
			if (loaded)
			{
				return _assets;
			}
			loaded = GetAssets(out _assets);
			return _assets;
		}
	}

	protected abstract bool GetAssets(out TAsset[] assets);

	public TAsset GetByIndex(int index)
	{
		if (assets.IsNullOrEmpty() || index < 0 || index > assets.Length - 1)
		{
			return default(TAsset);
		}
		return assets[index];
	}
}
