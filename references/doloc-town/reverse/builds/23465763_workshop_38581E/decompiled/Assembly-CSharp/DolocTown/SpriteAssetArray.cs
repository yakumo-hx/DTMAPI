using System.Linq;
using DolocTown.Config.Asset;
using UnityEngine;

namespace DolocTown;

public class SpriteAssetArray : AssetArrayBase<SpriteAsset, Sprite>
{
	public SpriteAssetArray(CfgSpriteAssetArray assetGroup)
	{
		base.refs = assetGroup.Array;
	}

	protected override bool GetAssets(out Sprite[] assets)
	{
		assets = base.refs.Select((SpriteAsset x) => x.Asset).ToArray();
		return assets.All((Sprite x) => x != null);
	}
}
