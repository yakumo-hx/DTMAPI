using UnityEngine;

namespace DolocTown.UI;

public class TechTreeTitleMenu : HorizontalTextMenu
{
	[SerializeField]
	private DolocButtonComponent prevButton;

	[SerializeField]
	private DolocButtonComponent nextButton;

	protected override GameObject pfb => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_TECH_MENU_SLOT);

	protected override void __Init()
	{
		base.__Init();
		prevButton.onClick.AddListener(ClickPrev);
		nextButton.onClick.AddListener(ClickNext);
	}

	private void ClickPrev()
	{
		int index = (base.selectIndex + slotPool.ActiveCount - 1) % slotPool.ActiveCount;
		slotPool[index].FireClick(fireSelect: true);
	}

	private void ClickNext()
	{
		int index = (base.selectIndex + 1) % slotPool.ActiveCount;
		slotPool[index].FireClick(fireSelect: true);
	}
}
