using System.Linq;
using DolocTown.Config.Asset;
using UnityEngine.Tilemaps;

namespace DolocTown;

public class TileAssetArray : AssetArrayBase<TileAsset, Tile>
{
	public TileAssetArray(CfgTileAssetArray assetGroup)
	{
		base.refs = assetGroup.Array;
	}

	protected override bool GetAssets(out Tile[] assets)
	{
		assets = base.refs.Select((TileAsset x) => x.Asset).ToArray();
		return assets.All((Tile x) => !x.name.IsNullOrEmpty());
	}
}
