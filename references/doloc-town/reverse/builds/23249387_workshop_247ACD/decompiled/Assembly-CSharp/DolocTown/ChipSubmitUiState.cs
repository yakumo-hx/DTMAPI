namespace DolocTown;

public class ChipSubmitUiState : SubmitMultipleItemsUiStateBase
{
	private DocumentManager docMgr => DolocAPI.archiveHandle.cityData.documentManager;

	protected override int ItemCountCanSubmit(Item item)
	{
		return docMgr.chipDocMgr.SubmitItemCountAsChip(item);
	}

	protected override bool ItemFilter(Item item)
	{
		return docMgr.chipDocMgr.CanSubmitItemAsChip(item);
	}

	protected override void AfterSubmit(Item[] submitItems, Item[] overflowItems)
	{
		docMgr.latestSubmitItems = submitItems;
		docMgr.latestOverflowItems = overflowItems;
	}

	protected override void OnCancel()
	{
		docMgr.latestSubmitItems = null;
		docMgr.latestOverflowItems = null;
	}
}
