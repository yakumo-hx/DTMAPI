using DolocTown.NodeCanvas;

namespace DolocTown;

public class NpcScheduleAsset : AssetBase<NpcScheduleGraph>
{
	public NpcScheduleAsset(string assetUrl)
		: base(assetUrl)
	{
	}

	protected override bool TryLoadAsset(string url, out NpcScheduleGraph asset)
	{
		asset = DolocAPI.assets.cache.GetAsset<NpcScheduleGraph>(url);
		return asset != null;
	}
}
