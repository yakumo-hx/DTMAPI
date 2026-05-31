using System;

namespace DolocTown;

public class GeneralSubmitUiState : SubmitSingleItemsUiStateBase
{
	private Func<Item, bool> itemFilter;

	private Func<bool> submitConditionChecker;

	private Func<Item, int> itemSubmitCountGetter;

	private Func<Item, int, string> confirmTextGetter;

	private Action<int> onFailedSubmit;

	private Action<int, int> onSubmit;

	public bool HandleStartUpArgs(Func<Item, bool> itemFilter, Func<bool> submitConditionChecker = null, Func<Item, int> itemSubmitCountGetter = null, Func<Item, int, string> confirmTextGetter = null, Action<int> onFailedSubmit = null, Action<int, int> onSubmit = null)
	{
		this.itemFilter = itemFilter;
		this.submitConditionChecker = submitConditionChecker ?? ((Func<bool>)(() => true));
		this.itemSubmitCountGetter = itemSubmitCountGetter ?? ((Func<Item, int>)((Item x) => x.count));
		this.confirmTextGetter = confirmTextGetter ?? ((Func<Item, int, string>)((Item _, int _) => string.Empty));
		this.onFailedSubmit = onFailedSubmit;
		this.onSubmit = onSubmit ?? ((Action<int, int>)delegate(int index, int count)
		{
			DolocAPI.CostItemAt(index, count);
		});
		return this.itemFilter != null;
	}

	protected override int ItemCountCanSubmit(Item item)
	{
		return itemSubmitCountGetter(item);
	}

	protected override bool ItemFilter(Item item)
	{
		return itemFilter(item);
	}

	protected override void OnItemSlotClick(int index)
	{
		if (base.panel.GetSlot(index).grayed)
		{
			onFailedSubmit?.Invoke(index);
		}
		else
		{
			base.OnItemSlotClick(index);
		}
	}

	protected override void OnSubmit(int index, int submitCount)
	{
		if (submitConditionChecker())
		{
			onSubmit(index, submitCount);
		}
	}

	protected override string GetQuestionText(Item item, int submitCount)
	{
		return confirmTextGetter?.Invoke(item, submitCount);
	}
}
