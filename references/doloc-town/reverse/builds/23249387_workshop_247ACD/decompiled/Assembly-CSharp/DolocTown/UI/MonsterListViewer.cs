using UnityEngine;

namespace DolocTown.UI;

public class MonsterListViewer : DolocScrollGridUI<BookMonsterSlot, BookMonsterData>
{
	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_BOOK_MONSTER_SLOT);

	protected override void RenderSlot(BookMonsterSlot slot, BookMonsterData data)
	{
		slot.Render(data);
	}

	protected override void OnSlotSelect(BookMonsterSlot slot)
	{
		base.OnSlotSelect(slot);
		DolocAPI.UIRaiseRoll();
	}
}
