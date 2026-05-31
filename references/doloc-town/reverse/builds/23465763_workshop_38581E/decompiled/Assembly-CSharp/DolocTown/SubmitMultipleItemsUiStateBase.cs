using System.Collections.Generic;
using System.Linq;
using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public abstract class SubmitMultipleItemsUiStateBase : DolocUiState<BackpackBottomPanel>
{
	protected InventorySystem inventorySystem => DolocAPI.archiveHandle.InventorySystem;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	protected override void Show()
	{
		base.Show();
		SetItemSlotGrayed();
		base.panel.Show();
		base.panel.Select(DolocAPI.SelectedItemIndex);
		base.panel.operationTip.SetTextKey(new string[2]
		{
			base.staticTexts.UiTipChoice,
			base.staticTexts.UiTipSubmitAll
		});
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide(useTween: true, ClearItemSlot);
	}

	protected override void Register()
	{
		base.panel.SetClickCallbacks(OnItemSlotClick);
		base.panel.BindInventory(inventorySystem.inventory);
		base.panel.OnCloseButtonClick.AddListener(OnCancel);
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseSubmitItem)
		{
			if (base.panel.slots.Any((ItemNavSlot x) => x.highLighted))
			{
				SubmitItems();
				gameController.PopState();
			}
		}
		else if (userInput.BaseIsCancelPressed)
		{
			OnCancel();
			gameController.PopState();
		}
	}

	protected override void Unregister()
	{
		base.panel.Clear();
		base.panel.OnCloseButtonClick.RemoveListener(OnCancel);
	}

	protected override void ClickCloseButton()
	{
		OnCancel();
		base.ClickCloseButton();
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

	private void SubmitItems()
	{
		List<Item> list = new List<Item>();
		List<Item> list2 = new List<Item>();
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (ItemNavSlot slot in base.panel.slots)
		{
			if (slot.highLighted)
			{
				Item item = base.panel.itemGetter(slot.index);
				if (dictionary.TryAdd(item.name, 0))
				{
					dictionary[item.name] += ItemCountCanSubmit(item);
				}
			}
		}
		foreach (ItemNavSlot slot2 in base.panel.slots)
		{
			if (!slot2.highLighted)
			{
				continue;
			}
			Item item2 = base.panel.itemGetter(slot2.index);
			if (dictionary.TryGetValue(item2.name, out var value))
			{
				dictionary.Remove(item2.name);
				int num = DolocAPI.CountItem(item2.name) - value;
				if (value > 0)
				{
					DolocAPI.CostItemAt(slot2.index, value);
					DolocAPI.RaiseUiSpriteFadeUp(slot2.position, slot2.iconSprite);
					list.Add(item2.Clone(value));
				}
				if (num > 0)
				{
					list2.Add(item2.Clone(num));
				}
			}
		}
		AfterSubmit(list.ToArray(), list2.ToArray());
	}

	protected virtual void OnCancel()
	{
	}

	protected virtual void AfterSubmit(Item[] submitItems, Item[] overflowItems)
	{
	}

	protected virtual void OnItemSlotClick(int index)
	{
		ItemNavSlot slot = base.panel.GetSlot(index);
		if (!slot.grayed)
		{
			slot.highLighted = !slot.highLighted;
		}
	}

	protected abstract int ItemCountCanSubmit(Item item);

	protected abstract bool ItemFilter(Item item);
}
