using UnityEngine;

namespace DolocTown.UI;

public class ResourceListViewer : DolocScrollGridUI<BookResourceSlot, BookResourceData>
{
	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_BOOK_RESOURCE_SLOT);

	protected override void RenderSlot(BookResourceSlot slot, BookResourceData data)
	{
		slot.Render(data);
	}

	protected override void OnSlotSelect(BookResourceSlot slot)
	{
		base.OnSlotSelect(slot);
		DolocAPI.UIRaiseRoll();
	}
}
