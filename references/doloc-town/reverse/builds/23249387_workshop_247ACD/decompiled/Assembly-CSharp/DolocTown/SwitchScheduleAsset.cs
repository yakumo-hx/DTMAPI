using DolocTown.NodeCanvas;

namespace DolocTown;

public class SwitchScheduleAsset : AssetBase<SwitchScheduleGraph>
{
	public SwitchScheduleAsset(string assetUrl)
		: base(assetUrl)
	{
	}

	protected override bool TryLoadAsset(string url, out SwitchScheduleGraph asset)
	{
		asset = DolocAPI.assets.cache.GetAsset<SwitchScheduleGraph>(url);
		return asset != null;
	}
}
