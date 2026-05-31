using DolocTown.Config;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class PositionTip : DolocBasicTip
{
	[SerializeField]
	private Text text;

	public string PositionText
	{
		get
		{
			return text.text;
		}
		set
		{
			text.text = value;
		}
	}

	protected override void OnClick()
	{
		base.OnClick();
		DolocAPI.OpenMap();
	}

	protected override void OnHover()
	{
		if (DolocAPI.CanOpenMap())
		{
			this.HoverTextSmall(DolocConfig.StaticTexts.UiTipOpenMap);
		}
	}
}
