using System;
using UnityEngine;

namespace DolocTown.UI;

public class InputItemViewer : DolocHorizontalUI<RecipeSubItemSlot>
{
	[SerializeField]
	private int fixedSlotCount;

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_SLOT_COSTITEMICON);

	public Func<int, Item> itemGetter { get; set; }

	public void Render(ItemSimpleData[] simpleDatas)
	{
		int num = Mathf.Max(simpleDatas.Length, fixedSlotCount);
		SetCapacity(num);
		for (int i = 0; i < num; i++)
		{
			base.slots[i].Render((i < simpleDatas.Length) ? simpleDatas[i] : default(ItemSimpleData));
		}
	}

	public override void BuildNavigation()
	{
	}

	protected override void OnSlotSelect(RecipeSubItemSlot slot)
	{
		base.OnSlotSelect(slot);
		HoverItemViewerAt(slot);
	}

	protected override void OnSlotDeselect(RecipeSubItemSlot slot)
	{
		base.OnSlotDeselect(slot);
		DolocAPI.HideHoverBox();
	}

	protected override void OnSlotPointerEnter(RecipeSubItemSlot slot)
	{
		base.OnSlotPointerEnter(slot);
		HoverItemViewerAt(slot);
	}

	protected override void OnSlotPointerExit(RecipeSubItemSlot slot)
	{
		base.OnSlotPointerExit(slot);
		DolocAPI.HideHoverBox();
	}

	private void HoverItemViewerAt(RecipeSubItemSlot slot)
	{
		Item item = itemGetter?.Invoke(slot.index);
		if (item != null)
		{
			slot.HoverItemViewer(new ItemData(item));
		}
	}
}
