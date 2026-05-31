using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class SubMenuPanel : MenuUI
{
	[SerializeField]
	private Color normalColor;

	[SerializeField]
	private Color highLightColor;

	[SerializeField]
	public DolocNavigationButton leftBtn;

	[SerializeField]
	public DolocNavigationButton rightBtn;

	private DolocNavigationButton currentHighlightSlot;

	protected override void __Init()
	{
		base.__Init();
		titleCanvasGroup.alpha = 0f;
		leftBtn.Init();
		rightBtn.Init();
	}

	protected override void OnSlotClick(MenuButton slot)
	{
		base.OnSlotClick(slot);
		if (!(currentHighlightSlot == slot))
		{
			SetColor(currentHighlightSlot, normalColor);
			currentHighlightSlot = slot;
			SetColor(slot, highLightColor);
		}
	}

	private void SetColor(DolocNavigationButton slot, Color color)
	{
		if (!(slot == null))
		{
			ColorBlock colors = slot.button.colors;
			colors.normalColor = color;
			colors.highlightedColor = color;
			colors.selectedColor = color;
			slot.button.colors = colors;
		}
	}

	public void ClearNavigation()
	{
		foreach (MenuButton slot in base.slots)
		{
			slot.button.SetNavigation();
		}
	}
}
