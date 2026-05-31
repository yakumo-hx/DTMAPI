using UnityEngine;

namespace DolocTown.UI;

public class NpcListViewer : DolocScrollGridUI<BookNpcSlot, BookNpcData>
{
	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_BOOK_NPC_SLOT);

	protected override void RenderSlot(BookNpcSlot slot, BookNpcData data)
	{
		slot.Render(data);
	}

	protected override void OnSlotSelect(BookNpcSlot slot)
	{
		base.OnSlotSelect(slot);
		DolocAPI.UIRaiseRoll();
	}
}
