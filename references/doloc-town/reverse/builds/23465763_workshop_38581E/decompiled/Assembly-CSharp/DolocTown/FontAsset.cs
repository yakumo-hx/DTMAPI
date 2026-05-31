using UnityEngine;

namespace DolocTown;

public class FontAsset : AssetBase<Font>
{
	public FontAsset(string assetUrl)
		: base(assetUrl)
	{
	}

	protected override bool TryLoadAsset(string url, out Font asset)
	{
		asset = DolocAPI.GetAsset<Font>(url);
		return asset != null;
	}
}
