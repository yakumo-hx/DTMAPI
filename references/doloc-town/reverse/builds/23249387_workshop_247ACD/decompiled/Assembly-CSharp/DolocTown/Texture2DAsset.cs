using DolocTown.Config.Asset;
using UnityEngine;

namespace DolocTown;

public class Texture2DAsset : AssetBase<Texture2D>
{
	public Texture2DAsset(CfgTexture2DAsset uiSpriteAsset)
		: base(uiSpriteAsset.Url)
	{
	}

	protected override bool TryLoadAsset(string url, out Texture2D asset)
	{
		asset = DolocAPI.GetAsset<Texture2D>(url);
		return asset != null;
	}
}
