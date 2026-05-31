using DolocTown.GameData;

namespace DolocTown;

public class EffectGroupAsset : AssetBase<EffectsConfigSO>
{
	public EffectGroupAsset(string assetUrl)
		: base(assetUrl)
	{
	}

	protected override bool TryLoadAsset(string url, out EffectsConfigSO asset)
	{
		asset = DolocAPI.GetAsset<EffectsConfigSO>(url);
		return asset != null;
	}
}
