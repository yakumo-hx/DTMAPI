using DolocTown.Config.Asset;
using UnityEngine.Tilemaps;

namespace DolocTown;

public class TileAsset : AssetBase<Tile>
{
	public TileAsset(CfgTileAsset tileAsset)
		: base(tileAsset.Url)
	{
	}

	protected override bool TryLoadAsset(string url, out Tile asset)
	{
		asset = DolocAPI.GetAsset<Tile>(url);
		return asset != null;
	}
}
