using UnityEngine;

namespace DolocTown.Config.Equipment;

public struct CaseSkin
{
	public int skinIndex;

	public Color color;

	public SpriteAsset sprite;

	public CaseSkin(int skinIndex, Color color, SpriteAsset sprite)
	{
		this.skinIndex = skinIndex;
		this.color = color;
		this.sprite = sprite;
	}
}
