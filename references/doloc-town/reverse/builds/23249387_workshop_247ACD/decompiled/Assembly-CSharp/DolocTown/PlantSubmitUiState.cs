namespace DolocTown;

public class PlantSubmitUiState : SubmitMultipleItemsUiStateBase
{
	private DocumentManager docMgr => DolocAPI.archiveHandle.cityData.documentManager;

	protected override int ItemCountCanSubmit(Item item)
	{
		return docMgr.plantDocMgr.SubmitItemAsPlant(item);
	}

	protected override bool ItemFilter(Item item)
	{
		return docMgr.plantDocMgr.CanSubmitItemAsPlant(item);
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
