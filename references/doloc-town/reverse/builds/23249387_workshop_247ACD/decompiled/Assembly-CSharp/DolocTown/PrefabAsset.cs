using DolocTown.Config.Asset;
using UnityEngine;

namespace DolocTown;

public class PrefabAsset : AssetBase<GameObject>
{
	public PrefabAsset(CfgPrefabAsset assetUrl)
		: base(assetUrl.Url)
	{
	}

	protected override bool TryLoadAsset(string url, out GameObject asset)
	{
		asset = DolocAPI.assets.cache.GetAsset<GameObject>(url);
		return asset != null;
	}
}
