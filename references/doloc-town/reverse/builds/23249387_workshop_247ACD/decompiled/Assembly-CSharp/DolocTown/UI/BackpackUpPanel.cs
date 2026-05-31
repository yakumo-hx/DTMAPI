using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BackpackUpPanel : DolocUIPanel
{
	[SerializeField]
	private Text desc;

	[SerializeField]
	private Text info;

	[SerializeField]
	private Text goldNum;

	[SerializeField]
	public DolocButtonComponent confirmBtn;

	[SerializeField]
	public DolocButtonComponent cancelBtn;

	protected override void __Init()
	{
		base.__Init();
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
	}

	public void Render(int originCount, int targetCount, int goldNum, bool canAfford)
	{
		desc.text = base.staticTexts.UiTipBuyBackpack.Format(targetCount - originCount);
		info.text = base.staticTexts.UiTipBuyBackpackInfo.Format(originCount);
		this.goldNum.text = (canAfford ? goldNum.ToString() : goldNum.ToString().Colored(DolocUiColor.SLIENTCOLOR_RED));
	}
}
