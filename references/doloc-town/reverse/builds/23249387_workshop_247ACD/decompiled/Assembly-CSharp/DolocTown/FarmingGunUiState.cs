using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class FarmingGunUiState : ContainerBaseUiState
{
	protected override UnityEvent OnCloseButtonClick => base.backpackPanel.OnCloseButtonClick;

	protected override void OnBackpackItemClick(int index)
	{
		HandlePlaceToOtherSide(index);
	}

	protected override void OnContainerItemClick(int index)
	{
		HandlePlaceToOtherSide(index);
	}

	protected override void HandleSwapOneItem(int index)
	{
		Item item = base.selectedItem;
		if (inBackpack)
		{
			if (!base.container.ContentFilter(item))
			{
				if (item != null)
				{
					DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.InventoryPanelCannotPutIn);
				}
				return;
			}
			Item item2 = item.Clone(1);
			if (base.containerInventory.CanPlaceIn(item2) && base.backpackInventory.TryCostAtIndex(base.currentIndex, 1))
			{
				base.containerInventory.PlaceItem(item2);
			}
			else
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.InventoryPanelContainerFull, base.container.title));
			}
		}
		else if (!base.containerInventory.isEmpty)
		{
			Item item3 = base.containerInventory.Read(index).Clone(1);
			if (!base.backpackInventory.CanPlaceIn(item3))
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrBackpackIsFull);
			}
			else if (base.containerInventory.TryCostAtIndex(index, 1))
			{
				base.backpackInventory.PlaceItem(item3);
			}
		}
	}

	protected override void HandlePlaceToOtherSide(int index)
	{
		if (inBackpack)
		{
			Item item = base.selectedItem;
			if (!base.buffer.IsEmpty)
			{
				base.OnBackpackItemClick(index);
				return;
			}
			if (!base.container.ContentFilter(item))
			{
				if (item != null)
				{
					DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.InventoryPanelCannotPutIn);
				}
				return;
			}
			Item firstItem = base.containerInventory.FirstItem;
			if (firstItem == null || !firstItem.IsSame(item))
			{
				base.containerInventory.SwapItem(0, item);
				base.backpackInventory.SwapItem(index, firstItem);
			}
			else
			{
				base.HandlePlaceToOtherSide(index);
			}
		}
		else
		{
			base.HandlePlaceToOtherSide(index);
		}
	}

	protected override void HandleSwapHalfItems(int index)
	{
	}

	protected override void PutAll()
	{
		QuickPut();
	}

	protected override void PutMax()
	{
		QuickPut();
	}

	private void QuickPut()
	{
		Item item = base.containerInventory.FirstItem;
		if (item == null)
		{
			for (int i = 0; i < base.backpackInventory.capacity; i++)
			{
				Item item2 = base.backpackInventory.Read(i);
				if (base.container.ContentFilter(item2))
				{
					item = item2;
					break;
				}
			}
		}
		if (item != null)
		{
			LinearInventory[] backpackWithInsideBoxes = DolocAPI.GetBackpackWithInsideBoxes(useSharedContainer: false);
			int num = backpackWithInsideBoxes.CountItem(item, shouldEqualAsItem: true);
			if (num != 0)
			{
				int count = Mathf.Min(item.overlay - item.count, num);
				backpackWithInsideBoxes.MaxCostItem(item, count, shouldEqualAsItem: true);
				base.containerInventory.PlaceItem(item.Clone(count));
			}
		}
	}

	protected override void Show()
	{
		base.Show();
		base.backpackPanel.SetCloseButtonVisible(value: true);
		base.containerWidget.SetCloseButtonVisible(value: false);
	}

	protected override string[] GetTipInBackpack()
	{
		return new string[3]
		{
			base.staticTexts.UiTipQuickPutAll,
			base.staticTexts.UiTipQuickPutOne,
			base.staticTexts.UiTipPutAllTap
		};
	}

	protected override string[] GetTipInContainer()
	{
		return new string[3]
		{
			base.staticTexts.UiTipQuickTakeAll,
			base.staticTexts.UiTipQuickTakeOne,
			base.staticTexts.UiTipPutAllTap
		};
	}
}
