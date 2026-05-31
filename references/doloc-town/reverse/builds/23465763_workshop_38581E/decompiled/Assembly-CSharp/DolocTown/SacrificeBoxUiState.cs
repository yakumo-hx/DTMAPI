using UnityEngine.Events;

namespace DolocTown;

public class SacrificeBoxUiState : ContainerBaseUiState
{
	protected override bool singleOption => true;

	protected override bool disablePutMax => true;

	protected override bool disableUpdateTip => true;

	protected override UnityEvent OnCloseButtonClick => base.backpackPanel.OnCloseButtonClick;

	protected override void OnBackpackItemClick(int index)
	{
		if (base.selectedItem != null && base.container.ContentFilter(base.selectedItem) && base.containerInventory.isEmpty)
		{
			base.containerInventory.PlaceItem(base.backpackInventory.Read(index).Clone(1));
			base.backpackInventory.TryCostAtIndex(base.currentIndex, 1);
		}
		else if (base.selectedItem != null)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.InventoryPanelCannotPutIn);
		}
	}

	protected override void OnContainerItemClick(int index)
	{
		PlaceToOtherSide();
	}

	protected override void Sort()
	{
		base.backpackInventory.Sort();
	}

	protected override void PutAll()
	{
		for (int i = 0; i < base.containerInventory.capacity; i++)
		{
			Item target = base.containerInventory.Read(i);
			if (base.backpackInventory.CanPlaceIn(target))
			{
				base.backpackInventory.PlaceItem(base.containerInventory.Take(i));
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
		return new string[2]
		{
			base.staticTexts.UiTipTakeOutAll,
			base.staticTexts.UiTipDestroyItem
		};
	}
}
