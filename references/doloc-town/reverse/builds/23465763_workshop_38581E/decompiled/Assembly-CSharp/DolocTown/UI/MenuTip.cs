using DolocTown.Config;

namespace DolocTown.UI;

public class MenuTip : DolocBasicTip
{
	protected override void OnHover()
	{
		this.HoverTextSmall(DolocConfig.StaticTexts.UiTipOpenMenu, UIAlignmentType.RightTop, UIAlignmentType.RightBottom);
	}

	protected override void OnClick()
	{
		base.OnClick();
		DolocAPI.EnterUI<MainMenuUiState>();
	}
}
