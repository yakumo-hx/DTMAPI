using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ArchiveListViewer : DolocScrollGridUI<DocumentSlot, DocumentData>
{
	[SerializeField]
	private Text emptyHint;

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_BOOK_ARCHIVE_SLOT);

	protected override void RenderSlot(DocumentSlot slot, DocumentData data)
	{
		slot.title = data.title;
	}

	protected override void OnSlotSelect(DocumentSlot slot)
	{
		base.OnSlotSelect(slot);
		DolocAPI.UIRaiseRoll();
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		emptyHint.text = base.staticTexts.DocumentEmpty;
	}

	protected override void OnRefreshView()
	{
		base.OnRefreshView();
		emptyHint.gameObject.SetActive(base.slots.Count == 0);
	}
}
