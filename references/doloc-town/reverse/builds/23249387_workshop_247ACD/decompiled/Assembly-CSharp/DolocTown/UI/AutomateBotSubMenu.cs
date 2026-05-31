using UnityEngine;

namespace DolocTown.UI;

public class AutomateBotSubMenu : DolocVerticalUI<AutomateBotSlot>
{
	private AutomateBotSlot currentHighlightSlot;

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_BOT_SLOT);

	protected override void OnSlotClick(AutomateBotSlot slot)
	{
		base.OnSlotClick(slot);
		if (currentHighlightSlot != null)
		{
			currentHighlightSlot.backgroundColor = (currentHighlightSlot.Empty ? slot.unplacedColor : slot.normalColor);
		}
		currentHighlightSlot = slot;
		currentHighlightSlot.backgroundColor = slot.highLightColor;
	}

	public void Render(int index, Sprite icon, bool isLowPower, bool isPause)
	{
		if (index <= base.slots.Count - 1)
		{
			base.slots[index].Render(icon, isLowPower, isPause);
		}
	}

	public void RenderEmpty(int index)
	{
		if (index <= base.slots.Count - 1)
		{
			base.slots[index].RenderEmpty();
		}
	}

	public override void BuildNavigation()
	{
	}
}
