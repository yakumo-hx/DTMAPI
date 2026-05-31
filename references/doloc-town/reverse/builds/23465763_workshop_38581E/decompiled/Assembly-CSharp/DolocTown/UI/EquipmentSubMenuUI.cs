using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EquipmentSubMenuUI : DolocHorizontalUI<DolocNavigationButton>
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

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_NAV_BUTTON_EQUIPMENT_MENU);

	protected override void __Init()
	{
		base.__Init();
		leftBtn.Init();
		rightBtn.Init();
	}

	protected override void OnSlotClick(DolocNavigationButton slot)
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

	public override void BuildNavigation()
	{
	}

	public void Render(Sprite[] icons)
	{
		SetCapacity(icons.Length);
		for (int i = 0; i < base.slotCount; i++)
		{
			base.slots[i].iconSprite = icons[i];
		}
	}
}
