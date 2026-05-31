using DolocTown.Config;
using UnityEngine;

namespace DolocTown;

public class AirWall : InteractableObject
{
	public bool showTips;

	private SpriteRenderer spriteRenderer;

	public override bool OnlyTouch => true;

	protected override void __Init()
	{
		base.__Init();
		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteRenderer.enabled = false;
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		if (showTips)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiTipAirWall);
		}
	}
}
