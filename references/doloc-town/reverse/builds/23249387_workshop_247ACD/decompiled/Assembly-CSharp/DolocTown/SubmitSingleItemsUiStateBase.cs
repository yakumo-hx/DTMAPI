using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public abstract class SubmitSingleItemsUiStateBase : DolocUiState<BackpackBottomPanel>
{
	protected InventorySystem inventorySystem => DolocAPI.archiveHandle.InventorySystem;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	protected override void Show()
	{
		base.Show();
		SetItemSlotGrayed();
		base.panel.Show();
		base.panel.Select(DolocAPI.SelectedItemIndex);
		base.panel.operationTip.SetTextKey(base.staticTexts.UiTipSubmit);
	}

	protected override void Hide()
	{
		base.panel.Hide(useTween: true, ClearItemSlot);
		base.Hide();
	}

	protected override void Register()
	{
		base.panel.BindInventory(inventorySystem.inventory);
		base.panel.SetClickCallbacks(OnItemSlotClick);
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			OnCancel();
			gameController.PopState();
		}
	}

	protected override void Unregister()
	{
		base.panel.Clear();
	}

	protected override void ClickCloseButton()
	{
		OnCancel();
		gameController.PopState();
	}

	private void SetItemSlotGrayed()
	{
		foreach (ItemNavSlot slot in base.panel.slots)
		{
			Item item = base.panel.itemGetter?.Invoke(slot.index);
			slot.grayed = !ItemFilter(item);
		}
	}

	private void ClearItemSlot()
	{
		foreach (ItemNavSlot slot in base.panel.slots)
		{
			slot.grayed = false;
			slot.highLighted = false;
		}
	}

	protected virtual string GetQuestionText(Item item, int submitCount)
	{
		return string.Format(base.staticTexts.UiSubmitConfirm, submitCount, item.title);
	}

	private void SubmitItem(int index)
	{
		Item item = base.panel.itemGetter(index);
		int submitCount;
		int overflowCount;
		if (item != null)
		{
			submitCount = ItemCountCanSubmit(item);
			overflowCount = item.count - submitCount;
			string questionText = GetQuestionText(item, submitCount);
			if (!questionText.IsNullOrEmpty())
			{
				DolocAPI.ShowQuestionBox(questionText, Callback);
			}
			else
			{
				Callback();
			}
		}
		void Callback()
		{
			if (submitCount > 0)
			{
				OnSubmit(index, submitCount);
			}
			AfterSubmit(item, submitCount, overflowCount);
			gameController.PopState();
		}
	}

	protected virtual void OnSubmit(int index, int submitCount)
	{
		DolocAPI.CostItemAt(index, submitCount);
		base.panel.GetSlot(index).RaiseUiSpriteFadeUp();
	}

	protected virtual void AfterSubmit(Item item, int submitCount, int overflowCount)
	{
	}

	protected virtual void OnCancel()
	{
	}

	protected virtual void OnItemSlotClick(int index)
	{
		if (!base.panel.GetSlot(index).grayed)
		{
			SubmitItem(index);
		}
	}

	protected abstract int ItemCountCanSubmit(Item item);

	protected abstract bool ItemFilter(Item item);
}
