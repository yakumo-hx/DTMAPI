using UnityEngine;

namespace DolocTown.UI;

public class ItemListViewer : DolocScrollGridUI<BookItemSlot, BookItemData>
{
	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_BOOK_ITEM_SLOT);

	protected override void RenderSlot(BookItemSlot slot, BookItemData data)
	{
		slot.Render(data);
	}

	public override void GetFocus()
	{
		base.GetFocus();
		BookItemSlot slot = GetSlot(base.selectedIndex);
		slot.highLighted = true;
		slot.forceHighLighted = false;
	}

	public override void LoseFocus()
	{
		base.LoseFocus();
		BookItemSlot slot = GetSlot(base.selectedIndex);
		slot.forceHighLighted = true;
		slot.highLighted = true;
	}

	protected override void OnSelectedIndexChange(int oldValue, int newValue)
	{
		BookItemSlot slot = GetSlot(oldValue);
		slot.forceHighLighted = false;
		slot.highLighted = false;
	}

	protected override void OnSlotSelect(BookItemSlot slot)
	{
		base.OnSlotSelect(slot);
		DolocAPI.UIRaiseRoll();
	}
}
