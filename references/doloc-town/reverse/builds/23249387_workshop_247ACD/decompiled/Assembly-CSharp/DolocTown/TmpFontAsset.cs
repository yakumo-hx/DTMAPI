using TMPro;

namespace DolocTown;

public class TmpFontAsset : AssetBase<TMP_FontAsset>
{
	public TmpFontAsset(string assetUrl)
		: base(assetUrl)
	{
	}

	protected override bool TryLoadAsset(string url, out TMP_FontAsset asset)
	{
		asset = DolocAPI.GetAsset<TMP_FontAsset>(url);
		return asset != null;
	}
}
