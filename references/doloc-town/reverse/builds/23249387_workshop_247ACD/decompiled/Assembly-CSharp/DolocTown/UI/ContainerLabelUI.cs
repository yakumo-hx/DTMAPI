using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ContainerLabelUI : DolocHorizontalUI<ContainerLabelSlot>
{
	[SerializeField]
	private Color normalColor;

	[SerializeField]
	private Color highLightColor;

	private DolocNavigationButton currentHighlightSlot;

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_NAV_BUTTON_LABEL);

	protected override void OnSlotClick(ContainerLabelSlot slot)
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

	public void Render(string[] titles)
	{
		SetCapacity(titles.Length);
		for (int i = 0; i < base.slotCount; i++)
		{
			base.slots[i].SetLabel(titles[i]);
		}
	}
}
