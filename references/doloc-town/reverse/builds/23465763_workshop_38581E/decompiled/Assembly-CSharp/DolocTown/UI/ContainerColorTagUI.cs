using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ContainerColorTagUI : DolocHorizontalUI<ContainerColorTagSlot>, INavPanel
{
	private DolocNavigationButton currentHighlightSlot;

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_NAV_BUTTON_COLOR);

	public Selectable[] allSelectablesArray => ((IEnumerable<ContainerColorTagSlot>)base.slots).Select((Func<ContainerColorTagSlot, Selectable>)((ContainerColorTagSlot x) => x.button)).ToArray();

	public int allSelectableCount => allSelectablesArray.Length;

	protected override void OnSlotClick(ContainerColorTagSlot slot)
	{
		base.OnSlotClick(slot);
		slot.highLighted = false;
		slot.highLighted = true;
		if (!(currentHighlightSlot == slot))
		{
			if (currentHighlightSlot != null)
			{
				currentHighlightSlot.highLighted = false;
			}
			currentHighlightSlot = slot;
		}
	}

	public void Render(Color[] colors)
	{
		SetCapacity(colors.Length);
		for (int i = 0; i < base.slotCount; i++)
		{
			base.slots[i].iconColor = colors[i];
		}
	}

	protected override void OnFinishHide()
	{
		base.OnFinishHide();
		SetCapacity(0);
	}
}
