using System;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class GarbageShredderUiState : ContainerBaseUiState
{
	private int maxCraftCount;

	private Action onCraft;

	private bool backupItemName;

	private Func<string> restTimeInfoGetter;

	private string currentTimeInfo => restTimeInfoGetter?.Invoke() ?? "";

	protected override UnityEvent OnCloseButtonClick => base.backpackPanel.OnCloseButtonClick;

	private string GetFullInfo()
	{
		string text = base.staticTexts.RecipePanelMaxCraftCount.Format(maxCraftCount);
		string text2 = currentTimeInfo;
		if (text2.IsNullOrEmpty())
		{
			return text;
		}
		return text + " (" + text2 + ")";
	}

	public bool HandleStartUpArgs(IContainer container, int maxCraftCount, Func<string> restTimeInfoGetter, Action onCraft = null)
	{
		base.container = container;
		this.maxCraftCount = maxCraftCount;
		this.onCraft = onCraft;
		this.restTimeInfoGetter = restTimeInfoGetter;
		if (maxCraftCount > 0)
		{
			base.containerWidget.SetInfo(GetFullInfo());
		}
		return container.inventory != null;
	}

	protected override void OnBackpackItemClick(int index)
	{
		Item item = base.selectedItem;
		if (!base.buffer.IsEmpty)
		{
			base.OnBackpackItemClick(index);
			return;
		}
		int num = ((maxCraftCount <= 0) ? int.MaxValue : ((maxCraftCount - base.containerInventory.FirstItem?.count) ?? maxCraftCount));
		if (num <= 0)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.InventoryPanelContainerFull, base.container.title));
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
		Item item2 = ClearItemEquipmentDirtyData(item.Clone());
		if (base.containerInventory.CanPlaceIn(item2.Clone(1)))
		{
			if (num >= item.count)
			{
				base.backpackInventory.Take(base.currentIndex);
				base.containerInventory.PlaceItem(item2);
			}
			else if (base.backpackInventory.TryCostAtIndex(base.currentIndex, num))
			{
				base.containerInventory.PlaceItem(item2.Clone(num));
			}
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.InventoryPanelContainerFull, base.container.title));
		}
	}

	protected override void OnContainerItemClick(int index)
	{
		base.HandlePlaceToOtherSide(index);
	}

	protected override void HandleSwapOneItem(int index)
	{
		Item item = base.selectedItem;
		if (inBackpack)
		{
			if (((maxCraftCount <= 0) ? int.MaxValue : ((maxCraftCount - base.containerInventory.FirstItem?.count) ?? maxCraftCount)) <= 0)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.InventoryPanelContainerFull, base.container.title));
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
			Item item2 = ClearItemEquipmentDirtyData(item.Clone(1));
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
		int num = ((maxCraftCount <= 0) ? int.MaxValue : ((maxCraftCount - base.containerInventory.FirstItem?.count) ?? maxCraftCount));
		if (num <= 0)
		{
			return;
		}
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
		if (item == null)
		{
			return;
		}
		int num2 = DolocAPI.CountItem(item, checkBox: true, shouldEqualAsItem: true);
		if (num2 == 0)
		{
			return;
		}
		if (num >= num2)
		{
			int count = Mathf.Min(item.overlay, num2);
			if (DolocAPI.CostItem(item, count, checkBox: true, shouldEqualAsItem: true))
			{
				base.containerInventory.PlaceItem(item.Clone(count));
			}
		}
		else if (DolocAPI.CostItem(item, num, checkBox: true, shouldEqualAsItem: true))
		{
			base.containerInventory.PlaceItem(item.Clone(num));
		}
	}

	protected override void Register()
	{
		base.Register();
		base.containerInventory.AddReceiver(OnInventoryChange);
	}

	protected override void Unregister()
	{
		base.Unregister();
		base.containerInventory.RemoveReceiver(OnInventoryChange);
	}

	private void ClearItemEquipmentDirtyDataInContainer()
	{
		if (base.containerInventory.FirstItem is ItemEquipment { IsDirty: not false })
		{
			Item item = base.containerInventory.Take(0);
			base.containerInventory.PlaceItem(DolocAPI.GenerateItem(item.name, item.count));
		}
	}

	private Item ClearItemEquipmentDirtyData(Item item)
	{
		if (item is ItemEquipment { IsDirty: not false })
		{
			return DolocAPI.GenerateItem(item.name, item.count);
		}
		return item;
	}

	protected override void Show()
	{
		base.Show();
		base.backpackPanel.SetCloseButtonVisible(value: true);
		base.containerWidget.SetCloseButtonVisible(value: false);
	}

	protected override void Hide()
	{
		base.Hide();
		onCraft?.Invoke();
	}

	private void OnInventoryChange(int index, Item item, bool _)
	{
		base.containerWidget.SetInfo(GetFullInfo());
		ClearItemEquipmentDirtyDataInContainer();
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
