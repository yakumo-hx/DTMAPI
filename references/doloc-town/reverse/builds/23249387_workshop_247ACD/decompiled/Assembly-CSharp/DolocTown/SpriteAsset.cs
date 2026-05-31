using DolocTown.Config.Asset;
using UnityEngine;

namespace DolocTown;

public class SpriteAsset : AssetBase<Sprite>
{
	public override Sprite Asset
	{
		get
		{
			if (DolocAPI.UseMods && DolocAPI.modManager.LoadSpriteFromFile(base.AssetUrl, out var asset))
			{
				return asset;
			}
			return base.Asset;
		}
	}

	public SpriteAsset(CfgSpriteAsset uiSpriteAsset)
		: base(uiSpriteAsset.Url)
	{
	}

	protected override bool TryLoadAsset(string url, out Sprite asset)
	{
		asset = DolocAPI.LoadSprite(url);
		return asset != null;
	}

	protected override bool LoadDefaultAsset(out Sprite asset)
	{
		asset = LocSprites.SPRITE_EMPTY;
		return true;
	}
}
