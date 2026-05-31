using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown;
using UnityEngine;

public class BoxUiState : ContainerBaseUiState
{
	private int currentSkinIndex;

	protected override SoundEvents SoundEventShow => SoundEvents.PLAY_ITEM_BOX;

	private List<ItemBox> boxes => currentBoxItem.GetAllBoxInBackpack();

	private int currentBoxIndex => base.backpackInventory.IndexOf(currentBoxItem);

	private ItemBox currentBoxItem => base.container as ItemBox;

	private bool canBoxPutIn => currentBoxItem.canBoxPutIn;

	private bool isBoxUsedUp => currentBoxItem.isBoxUsedUp;

	protected override bool ignoreSubmitWhenClick => true;

	protected override bool disableItemLocked => false;

	public override bool HandleStartUpArgs(IContainer box, Action onExit = null)
	{
		if (!(box is ItemBox))
		{
			return false;
		}
		if (!base.HandleStartUpArgs(box, onExit))
		{
			return false;
		}
		return currentBoxIndex >= 0;
	}

	protected override string[] GetTipInBackpack()
	{
		return base.GetTipInBackpack().Concat(new string[1] { base.staticTexts.UiTipSwitchBox }).ToArray();
	}

	protected override string[] GetTipInContainer()
	{
		return base.GetTipInContainer().Concat(new string[1] { base.staticTexts.UiTipSwitchBox }).ToArray();
	}

	protected override void PutAll()
	{
		if (inBackpack && !canBoxPutIn)
		{
			ShowBoxUsedUpWarning();
		}
		else
		{
			base.PutAll();
		}
	}

	private void UpdateBoxTitle()
	{
		Color color = ((currentBoxItem.currentDurability > 1) ? DolocUiColor.SLIENTCOLOR_GREEN : DolocUiColor.SLIENTCOLOR_RED);
		int num = Mathf.Max(0, currentBoxItem.currentDurability);
		base.containerWidget.SetInfo(DolocUtils.Format(base.staticTexts.BoxPanelRestCount, num.ToString().Colored(color), currentBoxItem.maxDurability));
	}

	protected override bool TryPlaceBuffer(int index)
	{
		if (inBackpack && index == currentBoxIndex)
		{
			if (base.buffer.CurrentItem == null)
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.BoxPanelAlreadyOpen);
			}
			return true;
		}
		if (!inBackpack && !canBoxPutIn && base.buffer.CurrentItem != null)
		{
			ShowBoxUsedUpWarning();
			return true;
		}
		return base.TryPlaceBuffer(index);
	}

	protected override void PlaceToOtherSide()
	{
		if (inBackpack && !canBoxPutIn && base.selectedItem != null)
		{
			ShowBoxUsedUpWarning();
		}
		else
		{
			base.PlaceToOtherSide();
		}
	}

	private void ShowBoxUsedUpWarning()
	{
		DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.BoxPanelUsedUpWarning);
	}

	protected override void OnBackpackItemRender(int index, Item item)
	{
		if (item != currentBoxItem)
		{
			base.OnBackpackItemRender(index, item);
		}
		else
		{
			base.backpackPanel.GetSlot(index).highLighted = item == currentBoxItem;
		}
	}

	protected override bool HandleInput(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
			return true;
		}
		if (ContinuouslyPressLast(deltaTime, delegate
		{
			SetBoxIndex(-1);
		}))
		{
			return true;
		}
		if (ContinuouslyPressNext(deltaTime, delegate
		{
			SetBoxIndex(1);
		}))
		{
			return true;
		}
		if (isItemSlotSelected && base.HandleInput(deltaTime))
		{
			return true;
		}
		return false;
	}

	private void SetBoxIndex(int offset)
	{
		List<ItemBox> list = boxes;
		if (list == null || list.Count <= 1)
		{
			return;
		}
		int index = (list.IndexOf(currentBoxItem) + offset + list.Count) % list.Count;
		Unregister();
		base.container = list[index];
		DolocAPI.DelayFrame(delegate
		{
			RefreshColorTag();
			base.panel.RebuildNavigation();
			Register();
			if (!isItemSlotSelected)
			{
				base.panel.containerWidget.Select(0);
			}
		});
		UpdateBoxTitle();
		if (!canBoxPutIn)
		{
			ShowBoxUsedUpWarning();
		}
	}

	protected override void DisposeItem()
	{
		if (base.selectedItem == currentBoxItem)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.BoxPanelAlreadyOpen);
		}
		else
		{
			base.DisposeItem();
		}
	}

	protected override void DestroyItem()
	{
		if (base.selectedItem == currentBoxItem)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.BoxPanelAlreadyOpen);
		}
		else
		{
			base.DestroyItem();
		}
	}

	protected override void Register()
	{
		base.Register();
		base.containerWidget.containerColorTagUI.SetClickCallbacks(OnColorTagClick);
	}

	protected override void Unregister()
	{
		base.Unregister();
		base.containerWidget.containerColorTagUI.RemoveCallbacks();
	}

	protected override void Show()
	{
		base.Show();
		RefreshColorTag();
		base.panel.RebuildNavigation();
		base.containerWidget.SetRepairCallback(OnRepairButtonClick);
		UpdateBoxTitle();
		if (!canBoxPutIn)
		{
			ShowBoxUsedUpWarning();
		}
	}

	protected override void Hide()
	{
		HandleBuffer();
		base.Hide();
		base.container = null;
	}

	private void RefreshColorTag()
	{
		currentSkinIndex = currentBoxItem.skinIndex;
		if (currentBoxItem.func.SkinCount > 1)
		{
			base.containerWidget.containerColorTagUI.Render(currentBoxItem.func.SkinColor);
			base.containerWidget.containerColorTagUI.Show();
			base.containerWidget.containerColorTagUI.FireClick(currentSkinIndex);
			base.panel.RebuildLayout();
		}
		else
		{
			base.containerWidget.containerColorTagUI.Hide();
		}
	}

	private void HandleBuffer()
	{
		Item item = base.buffer.Take();
		if (item != null)
		{
			Item item2 = DolocAPI.PlaceItem(item, checkBox: true);
			bool flag = DolocAPI.IsItemDisposable(item2);
			if (currentBoxItem != null && item2 != null && currentBoxItem.ContentFilter(item2, !flag))
			{
				item2 = currentBoxItem.inventory.PlaceItem(item2);
			}
			if (item2 != null)
			{
				DolocAPI.GenerateDropItem(DolocAPI.archiveHandle.currentRoom, item2, DolocAPI.AgentPosition);
			}
		}
	}

	private void OnRepairButtonClick()
	{
		if (currentBoxItem.currentDurability == currentBoxItem.maxDurability)
		{
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.BoxPanelNoNeedRepair);
			return;
		}
		CountItem repairItem = currentBoxItem.func.RepairItem;
		if (repairItem.isValid)
		{
			if (DolocAPI.CountItem(repairItem.itemName, checkBox: true) < repairItem.itemCount)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.BoxPanelNoRepairCost, DolocAPI.GetItemTitle(repairItem.itemName)));
			}
			else if (DolocAPI.CostItem(repairItem.itemName, repairItem.itemCount, checkBox: true))
			{
				DolocAPI.ShowMessageBoxSmall(base.staticTexts.BoxPanelRepairInfo);
				base.containerWidget.FadeRepairSprite(DolocAPI.GetItemSprite(repairItem.itemName));
				currentBoxItem.Repair();
				UpdateBoxTitle();
			}
		}
	}

	private void OnColorTagClick(int index)
	{
		currentBoxItem.SetSkinIndex(index);
		base.backpackInventory.ReEmit(currentBoxIndex);
	}
}
