using UnityEngine;

namespace DolocTown.Config.Item;

public struct BoxSkin
{
	public int skinIndex;

	public Color color;

	public SpriteAsset emptySprite;

	public SpriteAsset fullSprite;

	public BoxSkin(int skinIndex, Color color, SpriteAsset emptySprite, SpriteAsset fullSprite)
	{
		this.skinIndex = skinIndex;
		this.color = color;
		this.emptySprite = emptySprite;
		this.fullSprite = fullSprite;
	}
}
