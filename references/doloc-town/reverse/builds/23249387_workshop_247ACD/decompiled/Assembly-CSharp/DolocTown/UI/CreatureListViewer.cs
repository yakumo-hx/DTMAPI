using UnityEngine;

namespace DolocTown.UI;

public class CreatureListViewer : DolocScrollGridUI<BookCreatureSlot, BookCreatureData>
{
	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_BOOK_CREATURE_SLOT);

	protected override void RenderSlot(BookCreatureSlot slot, BookCreatureData data)
	{
		slot.Render(data);
	}

	protected override void OnSlotSelect(BookCreatureSlot slot)
	{
		base.OnSlotSelect(slot);
		DolocAPI.UIRaiseRoll();
	}
}
