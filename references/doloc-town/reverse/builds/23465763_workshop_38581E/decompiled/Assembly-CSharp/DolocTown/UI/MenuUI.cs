using UnityEngine;

namespace DolocTown.UI;

public class MenuUI : DolocHorizontalUI<MenuButton>
{
	[SerializeField]
	protected CanvasGroup titleCanvasGroup;

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_MENU_BUTTON);

	public override void SetCapacity(int totalCapacity)
	{
		base.SetCapacity(totalCapacity);
		BuildNavigation();
	}

	public void Render(string[] titles, Sprite[] icons)
	{
		DolocAssert.IsTrue(titles.Length == icons.Length);
		SetCapacity(icons.Length);
		for (int i = 0; i < base.slotCount; i++)
		{
			base.slots[i].SetVisible(value: true);
			base.slots[i].title = titles[i];
			base.slots[i].iconSprite = icons[i];
		}
	}

	protected override void OnFinishShow()
	{
		base.OnFinishShow();
		for (int i = 0; i < base.slotCount; i++)
		{
			base.slots[i].IgnoreParentCanvasGroups(value: true);
		}
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		for (int i = 0; i < base.slotCount; i++)
		{
			base.slots[i].IgnoreParentCanvasGroups(value: false);
		}
	}
}
