using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public abstract class ContainerBaseUiState : DolocUiState<ContainerPanel>
{
	private Timer timer = new Timer(DolocAPI.GlobalParameter.QuantitySelectTimer_Ref);

	protected bool inBackpack;

	protected bool isItemSlotSelected;

	private bool rightClickHold;

	protected Action onExit;

	private string[] tipTextInBackpack;

	private string[] tipTextInContainer;

	protected virtual SoundEvents SoundEventShow => SoundEvents.PLAY_UI_POP_UP;

	protected virtual SoundEvents SoundEventHide => SoundEvents.PLAY_UI_POP_DOWN;

	protected BackpackBottomPanel backpackPanel => base.panel.backpackPanel;

	protected LinearInventory backpackInventory => DolocAPI.archiveHandle.InventorySystem.inventory;

	protected IContainer container { get; set; }

	protected ContainerWidget containerWidget => base.panel.containerWidget;

	protected LinearInventory containerInventory => container.inventory;

	protected virtual InventoryPanel currentPanel
	{
		get
		{
			if (!inBackpack)
			{
				return containerWidget;
			}
			return backpackPanel;
		}
	}

	protected int currentIndex => currentPanel.selectedIndex;

	protected virtual LinearInventory currentInventory
	{
		get
		{
			if (!inBackpack)
			{
				return containerInventory;
			}
			return backpackInventory;
		}
	}

	protected virtual LinearInventory otherInventory
	{
		get
		{
			if (!inBackpack)
			{
				return backpackInventory;
			}
			return containerInventory;
		}
	}

	protected Item selectedItem => currentInventory.Read(currentIndex);

	protected SingleInventory buffer => DolocAPI.archiveHandle.InventorySystem.buffer;

	private bool noneItemSelected => (buffer.CurrentItem ?? selectedItem) == null;

	protected virtual bool disableItemLocked { get; } = true;


	protected virtual bool disableUpdateTip { get; }

	protected virtual bool disableSort { get; }

	protected virtual bool disablePutAll { get; }

	protected virtual bool disablePutMax { get; }

	protected virtual bool disableDestroy { get; }

	protected virtual bool singleOption { get; }

	protected virtual bool ignoreSubmitWhenClick { get; }

	protected override UnityEvent OnCloseButtonClick => containerWidget.OnCloseButtonClick;

	protected virtual void OnBackpackItemClick(int index)
	{
		if (!ignoreSubmitWhenClick || (!userInput.BaseIsConfirmPressed && !userInput.BaseIsConfirmInProgress))
		{
			HandleSwapAllItems(index);
		}
	}

	protected virtual void OnContainerItemClick(int index)
	{
		if (!ignoreSubmitWhenClick || (!userInput.BaseIsConfirmPressed && !userInput.BaseIsConfirmInProgress))
		{
			HandleSwapAllItems(index);
		}
	}

	protected void HandleSwapAllItems(int index, bool forceSwap = false)
	{
		if (!TryPlaceBuffer(index))
		{
			if (buffer.CurrentItem != null || forceSwap || DolocButtonComponent.latestClickType == ClickType.Mouse)
			{
				DolocAPI.SwapItemFromOutside(currentInventory, index);
			}
			else
			{
				PlaceToOtherSide();
			}
		}
	}

	protected virtual void HandleSwapOneItem(int index)
	{
		if (!TryPlaceBuffer(index))
		{
			DolocAPI.SwapOneItemFromOutside(currentInventory, index);
		}
	}

	private bool HandleSwapOneItemInterrupted(int index)
	{
		if (selectedItem == null)
		{
			return false;
		}
		HandleSwapOneItem(index);
		return true;
	}

	protected virtual void HandlePlaceToOtherSide(int index)
	{
		if (!TryPlaceBuffer(index))
		{
			PlaceToOtherSide();
		}
	}

	protected virtual void HandleSwapHalfItems(int index)
	{
		if (!TryPlaceBuffer(index))
		{
			DolocAPI.SwapHalfItemFromOutside(currentInventory, index);
		}
	}

	protected virtual bool TryPlaceBuffer(int index)
	{
		if (buffer.CurrentItem == null)
		{
			return false;
		}
		if (!inBackpack && !container.ContentFilter(buffer.CurrentItem))
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.InventoryPanelCannotPutIn);
			return true;
		}
		if (selectedItem == null)
		{
			DolocAPI.SwapItemFromOutside(currentInventory, index);
			return true;
		}
		return false;
	}

	protected virtual void PlaceToOtherSide()
	{
		if (selectedItem == null)
		{
			return;
		}
		if (inBackpack)
		{
			if (!container.ContentFilter(selectedItem))
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.InventoryPanelCannotPutIn);
				return;
			}
			if (!containerInventory.CanPlaceIn(selectedItem.Clone(1)))
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.InventoryPanelContainerFull, container.title));
				return;
			}
			Item item = backpackInventory.Take(currentIndex);
			Item item2 = containerInventory.PlaceItem(item);
			backpackInventory.PlaceItemAt(currentIndex, item2);
		}
		else if (!backpackInventory.CanPlaceIn(selectedItem.Clone(1)))
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrBackpackIsFull);
		}
		else
		{
			Item item3 = containerInventory.Take(currentIndex);
			Item item4 = backpackInventory.PlaceItem(item3);
			containerInventory.PlaceItemAt(currentIndex, item4);
		}
	}

	public virtual bool HandleStartUpArgs(IContainer container, Action onExit = null)
	{
		this.container = container;
		this.onExit = onExit;
		return container?.inventory != null;
	}

	protected virtual string[] GetTipInBackpack()
	{
		return new string[6]
		{
			base.staticTexts.UiTipPutMax,
			base.staticTexts.UiTipPutAll,
			base.staticTexts.UiTipDispose,
			base.staticTexts.UiTipDestroyItem,
			base.staticTexts.UiTipTidy,
			base.staticTexts.UiTipLockSlot
		}.Concat(GetBackpackTipsByInput()).ToArray();
	}

	protected IEnumerable<string> GetBackpackTipsByInput()
	{
		if (DolocAPI.UserInput.DeviceType != 0)
		{
			return new string[3]
			{
				base.staticTexts.UiTipPutGamepad,
				base.staticTexts.UiTipPickUpGamepad,
				base.staticTexts.UiTipTakeOutOneGamepad
			};
		}
		return new string[3]
		{
			base.staticTexts.UiTipPut,
			base.staticTexts.UiTipTakeOutOne,
			base.staticTexts.UiTipTakeOutHalf
		};
	}

	protected virtual string[] GetTipInContainer()
	{
		return new string[6]
		{
			base.staticTexts.UiTipTakeOutMax,
			base.staticTexts.UiTipTakeOutAll,
			base.staticTexts.UiTipDispose,
			base.staticTexts.UiTipDestroyItem,
			base.staticTexts.UiTipTidy,
			base.staticTexts.UiTipLockSlot
		}.Concat(GetContainerTipsByInput()).ToArray();
	}

	protected IEnumerable<string> GetContainerTipsByInput()
	{
		if (DolocAPI.UserInput.DeviceType != 0)
		{
			return new string[3]
			{
				base.staticTexts.UiTipTakeOutGamepad,
				base.staticTexts.UiTipPickUpGamepad,
				base.staticTexts.UiTipTakeOutOneGamepad
			};
		}
		return new string[3]
		{
			base.staticTexts.UiTipTakeOut,
			base.staticTexts.UiTipTakeOutOne,
			base.staticTexts.UiTipTakeOutHalf
		};
	}

	protected sealed override void OnUiUpdate(float deltaTime)
	{
		HandleInput(deltaTime);
	}

	protected virtual bool HandleInput(float deltaTime)
	{
		if (userInput.BaseIsSplitPressed)
		{
			timer.ReStart();
			HandleSwapOneItem(currentIndex);
			return true;
		}
		if (userInput.BaseIsSplitInProgress)
		{
			if (timer.Update(deltaTime))
			{
				HandleSwapOneItem(currentIndex);
			}
			return true;
		}
		if (ignoreSubmitWhenClick && userInput.BaseIsConfirmPressed)
		{
			HandleSwapAllItems(currentIndex);
			return true;
		}
		if (ignoreSubmitWhenClick && userInput.BaseIsConfirmHold)
		{
			HandleSwapAllItems(currentIndex, forceSwap: true);
			return true;
		}
		if (!disableSort && userInput.BaseSortItem)
		{
			Sort();
			return true;
		}
		if (!disableItemLocked && userInput.BaseLockItem)
		{
			SwitchSlotLockStatus();
			return true;
		}
		if (!disableDestroy && userInput.BaseDisposeItem)
		{
			DisposeItem();
			return true;
		}
		if (!disableDestroy && userInput.BaseDestroyItem)
		{
			DestroyItem();
			return true;
		}
		if (!disablePutMax && userInput.BasePutMaxItem)
		{
			PutMax();
			return true;
		}
		if (!disablePutAll && userInput.BasePutAllItem)
		{
			PutAll();
			return true;
		}
		if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
			return true;
		}
		return false;
	}

	protected virtual void PutAll()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_BACKPACK_TIDY_UP);
		for (int i = 0; i < currentInventory.capacity; i++)
		{
			Item item = currentInventory.Read(i);
			if ((!inBackpack || container.ContentFilter(item)) && otherInventory.CanPlaceIn(item))
			{
				otherInventory.PlaceItem(currentInventory.Take(i));
			}
		}
	}

	protected virtual void Sort()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_BACKPACK_TIDY_UP);
		currentInventory.Sort();
	}

	protected virtual void SwitchSlotLockStatus()
	{
		currentInventory.SwitchSlotLockStatus(currentIndex);
	}

	protected virtual void PutMax()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_BACKPACK_TIDY_UP);
		for (int i = 0; i < otherInventory.capacity; i++)
		{
			Item item = otherInventory.Read(i);
			if (item == null)
			{
				continue;
			}
			for (int j = 0; j < currentInventory.capacity; j++)
			{
				Item item2 = currentInventory.Read(j);
				if (item2 != null && item.IsSame(item2))
				{
					Item item3 = otherInventory.PlaceItem(currentInventory.Take(j));
					if (item3 != null)
					{
						currentInventory.SwapItem(j, item3);
					}
				}
			}
		}
	}

	protected virtual void DisposeItem()
	{
		if (DolocAPI.DisposeItem(selectedItem))
		{
			currentInventory.Take(currentIndex);
		}
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		isItemSlotSelected = false;
	}

	protected override void Register()
	{
		backpackPanel.BindInventory(backpackInventory, ConvertItemData, OnBackpackItemRender);
		backpackPanel.SetSelectCallbacks(OnBackpackItemSelect);
		backpackPanel.SetDeselectCallbacks(OnBackpackItemDeselect);
		if (singleOption)
		{
			backpackPanel.SetClickCallbacks(OnBackpackItemClick);
		}
		else
		{
			backpackPanel.SetClickCallbacks(OnBackpackItemClick, HandleSwapOneItem, HandlePlaceToOtherSide, HandleSwapHalfItems, HandlePlaceToOtherSide, null, null, HandleSwapOneItemInterrupted);
		}
		containerWidget.lineCapacity = container.lineCapacity;
		containerWidget.BindInventory(containerInventory, ConvertItemData, OnContainerItemRender);
		containerWidget.SetSelectCallbacks(OnContainerItemSelect);
		containerWidget.SetDeselectCallbacks(OnContainerItemDeselect);
		if (singleOption)
		{
			containerWidget.SetClickCallbacks(OnContainerItemClick);
		}
		else
		{
			containerWidget.SetClickCallbacks(OnContainerItemClick, HandleSwapOneItem, HandlePlaceToOtherSide, HandleSwapHalfItems, HandlePlaceToOtherSide, null, null, HandleSwapOneItemInterrupted);
		}
		buffer.onValueChanged.AddListener(OnBufferChanged);
	}

	protected override void Unregister()
	{
		backpackPanel.Clear();
		containerWidget.Clear();
		buffer.onValueChanged.RemoveListener(OnBufferChanged);
	}

	private void OnBufferChanged(Item item)
	{
		backpackPanel.DisableDestroyButton(DisableDestroyButton(), noneItemSelected);
	}

	protected virtual void OnBackpackItemRender(int index, Item item)
	{
		backpackPanel.GetSlot(index).grayed = !container.ContentFilter(item);
	}

	protected virtual ItemData ConvertItemData(Item item)
	{
		return new ItemData(item);
	}

	protected virtual void OnBackpackItemSelect(int index)
	{
		isItemSlotSelected = true;
		if (!inBackpack)
		{
			inBackpack = true;
			if (!disableUpdateTip)
			{
				base.panel.operationTip.SetTextKey(tipTextInBackpack);
			}
		}
		backpackPanel.DisableDestroyButton(DisableDestroyButton(), noneItemSelected);
	}

	protected virtual void OnBackpackItemDeselect(int index)
	{
		isItemSlotSelected = false;
	}

	protected virtual void OnContainerItemRender(int index, Item item)
	{
	}

	protected virtual void OnContainerItemSelect(int index)
	{
		isItemSlotSelected = true;
		backpackPanel.DisableDestroyButton(DisableDestroyButton(), noneItemSelected);
		if (inBackpack)
		{
			inBackpack = false;
			if (!disableUpdateTip)
			{
				base.panel.operationTip.SetTextKey(tipTextInContainer);
			}
		}
	}

	protected virtual void OnContainerItemDeselect(int index)
	{
		isItemSlotSelected = false;
	}

	protected override void Show()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEventShow);
		rightClickHold = false;
		backpackPanel.SetCloseButtonVisible(value: false);
		containerWidget.SetCloseButtonVisible(value: true);
		tipTextInBackpack = GetTipInBackpack();
		tipTextInContainer = GetTipInContainer();
		inBackpack = false;
		backpackPanel.SetDeleteCallback(DestroyItem);
		containerWidget.SetTitle(container.title);
		base.panel.Show();
		if (!disableUpdateTip)
		{
			base.panel.operationTip.SetTextKey(tipTextInBackpack);
		}
	}

	protected override void Hide()
	{
		base.panel.Hide();
		DolocAPI.Sound.PostSoundEvent(SoundEventHide);
		onExit?.Invoke();
		Item item = buffer.Take();
		if (item != null)
		{
			Item item2 = DolocAPI.PlaceItem(item, checkBox: true);
			if (container != null && item2 != null && container.ContentFilter(item2))
			{
				item2 = containerInventory.PlaceItem(item2);
			}
			if (item2 != null)
			{
				DolocAPI.GenerateDropItem(DolocAPI.archiveHandle.currentRoom, item2, DolocAPI.AgentPosition);
			}
		}
	}

	public override void OnPause()
	{
		base.OnPause();
		isItemSlotSelected = false;
	}

	public override void OnResume()
	{
		base.OnResume();
		DolocAPI.DelayFrame(delegate
		{
			currentPanel.Select(currentIndex);
		});
	}

	private bool DisableDestroyButton()
	{
		if (buffer.CurrentItem == null && selectedItem == null)
		{
			return false;
		}
		return !DolocAPI.IsItemDisposable(buffer.CurrentItem ?? selectedItem);
	}

	protected virtual void DestroyItem()
	{
		Item bufferItem = buffer.CurrentItem;
		Item item = bufferItem ?? selectedItem;
		if (item == null)
		{
			return;
		}
		if (!DolocAPI.IsItemDisposable(item))
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrNotDisposeItem);
			return;
		}
		buffer.Take();
		DolocAPI.ShowQuestionBox(string.Format(base.staticTexts.UiQuesDisposeItem, item.count, item.title), delegate
		{
			if (bufferItem == null)
			{
				currentInventory.Take(currentIndex);
			}
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_ITEM_DELETE);
		}, delegate
		{
			buffer.CurrentItem = bufferItem;
		});
	}

	protected override void RefreshTip()
	{
		tipTextInBackpack = GetTipInBackpack();
		tipTextInContainer = GetTipInContainer();
		if (!disableUpdateTip)
		{
			base.panel.operationTip.SetTextKey(inBackpack ? tipTextInBackpack : tipTextInContainer);
		}
		else
		{
			base.panel.operationTip.SetTextKey(tipTextInBackpack);
		}
	}
}
