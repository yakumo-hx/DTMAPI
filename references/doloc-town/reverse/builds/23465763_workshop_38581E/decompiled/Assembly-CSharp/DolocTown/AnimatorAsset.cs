using UnityEngine;

namespace DolocTown;

public class AnimatorAsset : AssetBase<RuntimeAnimatorController>
{
	public AnimatorAsset(string assetUrl)
		: base(assetUrl)
	{
	}

	protected override bool TryLoadAsset(string url, out RuntimeAnimatorController asset)
	{
		asset = DolocAPI.GetAsset<RuntimeAnimatorController>(url);
		return asset != null;
	}
}
