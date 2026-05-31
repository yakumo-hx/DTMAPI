namespace DolocTown;

public class GiveGiftToNPCUiState : SubmitSingleItemsUiStateBase
{
	private string targetNpc;

	private static string lastGiftItemName;

	protected override int ItemCountCanSubmit(Item item)
	{
		return 1;
	}

	protected override bool ItemFilter(Item item)
	{
		if (DolocAPI.IsItemDisposable(item))
		{
			return DolocAPI.IsItemSalable(item);
		}
		return false;
	}

	protected override void OnSubmit(int index, int submitCount)
	{
		Item item = base.inventorySystem.inventory.Read(index);
		lastGiftItemName = item?.name;
		DolocAPI.TryGiftItemToNpc(targetNpc, item, index);
	}

	protected override string GetQuestionText(Item item, int submitCount)
	{
		return string.Format(base.staticTexts.UiGiftItemConfirm, item.title, DolocAPI.GetNpcTitle(targetNpc));
	}

	protected override void Show()
	{
		base.Show();
		lastGiftItemName = string.Empty;
		targetNpc = string.Empty;
		if (DolocAPI.userInput.CurrentState is DialogueState dialogueState)
		{
			targetNpc = dialogueState.interactedNpc.EntityId;
		}
	}

	public static string GetLastGiftItem()
	{
		return lastGiftItemName;
	}
}
