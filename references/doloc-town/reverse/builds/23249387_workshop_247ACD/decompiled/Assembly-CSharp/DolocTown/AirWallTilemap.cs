using DolocTown.Config;
using UnityEngine.Tilemaps;

namespace DolocTown;

public class AirWallTilemap : InteractableObject
{
	public bool showTips;

	private Tilemap tilemap;

	protected override void __Init()
	{
		base.__Init();
		tilemap = GetComponent<Tilemap>();
		tilemap.color = DolocColor.empty;
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
