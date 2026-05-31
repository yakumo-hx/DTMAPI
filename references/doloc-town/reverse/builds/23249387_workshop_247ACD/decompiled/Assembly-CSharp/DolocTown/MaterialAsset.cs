using System;
using UnityEngine;

namespace DolocTown;

public class MaterialAsset : AssetBase<Material>
{
	public MaterialAsset(string assetUrl)
		: base(assetUrl)
	{
	}

	protected override bool TryLoadAsset(string url, out Material asset)
	{
		if (url.IsNullOrEmpty())
		{
			asset = null;
			return false;
		}
		if (Enum.TryParse<DolocGameAssets>(url, ignoreCase: true, out var result))
		{
			asset = DolocAPI.GetAsset<Material>(result);
			return asset != null;
		}
		asset = null;
		return false;
	}
}
