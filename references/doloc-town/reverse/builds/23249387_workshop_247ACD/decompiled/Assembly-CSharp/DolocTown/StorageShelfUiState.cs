using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Item;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown;

public class StorageShelfUiState : DolocUiState<StorageShelfPanel>
{
	private LinearInventory shelfInventory;

	private int boxCapacity;

	private int currentItemIndex;

	private bool inBackpack;

	private int currentBoxIndex;

	private ItemBox boxItemTemplate;

	private Timer timer = new Timer(DolocAPI.GlobalParameter.QuantitySelectTimer_Ref);

	private StorageShelfWidget ShelfWidget => base.panel.shelfWidget;

	private BackpackBottomPanel backpackPanel => base.panel.backpackPanel;

	private LinearInventory currentInventory
	{
		get
		{
			if (!inBackpack)
			{
				return currentBox?.inventory;
			}
			return backpackInventory;
		}
	}

	private Item selectedItem
	{
		get
		{
			if (currentItemIndex >= 0)
			{
				return currentInventory?.Read(currentItemIndex);
			}
			return shelfInventory.Read(currentBoxIndex);
		}
	}

	private bool noneItemSelected => (buffer.CurrentItem ?? selectedItem) == null;

	private ItemBox currentBox => GetBox(currentBoxIndex);

	private SingleInventory buffer => inventorySystem.buffer;

	private LinearInventory backpackInventory => DolocAPI.archiveHandle.InventorySystem.inventory;

	private InventorySystem inventorySystem => DolocAPI.archiveHandle.InventorySystem;

	protected override UnityEvent OnCloseButtonClick => ShelfWidget.OnCloseButtonClick;

	protected override void OnInit()
	{
		base.OnInit();
		boxItemTemplate = DolocAPI.GenerateItem(DolocAPI.GlobalParameter.ItemRefBox) as ItemBox;
	}

	public bool HandleStartUpArgs(StorageShelf storageShelf)
	{
		shelfInventory = storageShelf.inventory;
		if (shelfInventory == null)
		{
			return false;
		}
		if (!DolocAPI.QueryItemProto(DolocAPI.GlobalParameter.ItemRefBox, out var proto))
		{
			return false;
		}
		boxCapacity = ((ItemFunctionBox)proto.Function).TotalCapacity;
		ShelfWidget.SetCapacity(shelfInventory.capacity, boxCapacity);
		ShelfWidget.SetTitle(storageShelf.Title);
		return true;
	}

	protected override void Register()
	{
		buffer.onValueChanged.AddListener(OnBufferChanged);
		backpackPanel.BindInventory(inventorySystem.inventory, OnBackpackItemRender);
		backpackPanel.SetSelectCallbacks(OnBackpackItemSelect);
		backpackPanel.SetClickCallbacks(OnBackpackItemClick, HandleSwapOneItem, HandlePlaceToOtherSide, HandleSwapHalfItems, HandlePlaceToOtherSide, null, null, HandleSwapOneItemInterrupted);
		shelfInventory.AddReceiver(OnShelfRender);
		ShelfWidget.ForEach(delegate(int boxIdx, BoxInventoryWidget boxPanel)
		{
			boxPanel.onTitleIconSelect.AddListener(delegate
			{
				OnShelfBoxSelect(boxIdx);
			});
			boxPanel.titleIcon.SetClickCallbacks(delegate
			{
				OnShelfBoxClick(boxIdx);
			}, delegate
			{
				OnShelfBoxClick(boxIdx);
			}, delegate
			{
				PlaceBoxToOtherSide(boxIdx);
			}, delegate
			{
				OnShelfBoxClick(boxIdx);
			}, delegate
			{
				PlaceBoxToOtherSide(boxIdx);
			});
			boxPanel.SetSelectCallbacks(delegate(int idx)
			{
				OnBoxItemSelect(boxIdx, idx);
			});
			boxPanel.SetClickCallbacks(OnContainerItemClick, HandleSwapOneItem, HandlePlaceToOtherSide, HandleSwapHalfItems, HandlePlaceToOtherSide, null, null, HandleSwapOneItemInterrupted);
		});
		base.panel.RefreshLayout();
	}

	protected override void Unregister()
	{
		buffer.onValueChanged.RemoveListener(OnBufferChanged);
		backpackPanel.Clear();
		shelfInventory.RemoveReceiver(OnShelfRender);
		ShelfWidget.RemoveBoxContentReceivers();
		ShelfWidget.ForEach(delegate(int boxIdx, BoxInventoryWidget boxPanel)
		{
			boxPanel.onTitleIconSelect.RemoveAllListeners();
			boxPanel.titleIcon.ClearAllClickCallbacks();
			boxPanel.UnBindBox();
		});
	}

	private void OnShelfRender(int index, Item item, bool _)
	{
		ShelfWidget.RefreshView(shelfInventory);
	}

	private ItemBox GetBox(int index)
	{
		return (ItemBox)shelfInventory.Read(index);
	}

	private void OnShelfBoxSelect(int index)
	{
		currentItemIndex = -1;
		currentBoxIndex = index;
		backpackPanel.DisableDestroyButton(DisableDestroyButton(), noneItemSelected);
		if (inBackpack)
		{
			inBackpack = false;
			ShowTipInContainer();
		}
	}

	private void OnBoxItemSelect(int boxIndex, int slotIndex)
	{
		currentItemIndex = slotIndex;
		currentBoxIndex = boxIndex;
		backpackPanel.DisableDestroyButton(DisableDestroyButton(), noneItemSelected);
		if (inBackpack)
		{
			inBackpack = false;
			ShowTipInContainer();
		}
	}

	private void RebuildNavigation()
	{
		base.panel.RebuildNavigation();
	}

	private void OnBufferChanged(Item item)
	{
		backpackPanel.DisableDestroyButton(DisableDestroyButton(), noneItemSelected);
		ShelfWidget.ForEach(delegate(int idx, BoxInventoryWidget boxUi)
		{
			bool titleInteractive = item is ItemBox || (GetBox(idx) != null && item == null);
			boxUi.SetTitleInteractive(titleInteractive);
			bool contentInteractive = GetBox(idx) != null && boxItemTemplate.ContentFilter(item);
			boxUi.SetContentInteractive(contentInteractive);
		});
		DolocAPI.DelayFrame(RebuildNavigation);
	}

	protected virtual void OnBackpackItemRender(int index, Item item)
	{
	}

	protected virtual void OnBackpackItemSelect(int index)
	{
		currentItemIndex = index;
		backpackPanel.DisableDestroyButton(DisableDestroyButton(), noneItemSelected);
		if (!inBackpack)
		{
			inBackpack = true;
			ShowTipInBackpack();
		}
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
				if (currentItemIndex < 0)
				{
					shelfInventory.Take(currentBoxIndex);
					Selectable selectOnDown = ShelfWidget.boxUis[currentBoxIndex].titleIcon.button.navigation.selectOnDown;
					EventSystem.current?.SetSelectedGameObject(selectOnDown.gameObject);
				}
				else
				{
					currentInventory.Take(currentItemIndex);
				}
			}
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_ITEM_DELETE);
			OnBufferChanged(null);
		}, delegate
		{
			buffer.CurrentItem = bufferItem;
		});
	}

	protected virtual void PutAll()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_BACKPACK_TIDY_UP);
		for (int i = 0; i < currentInventory.capacity; i++)
		{
			Item item = currentInventory.Read(i);
			if (item != null)
			{
				if (inBackpack)
				{
					backpackInventory.SwapItem(i, QuickPutInToShelf(currentInventory.Take(i), showMessageBox: false));
				}
				else if (backpackInventory.CanPlaceIn(item))
				{
					backpackInventory.PlaceItem(currentInventory.Take(i));
				}
			}
		}
	}

	protected virtual void Sort()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_BACKPACK_TIDY_UP);
		currentInventory?.Sort();
	}

	protected virtual void SwitchSlotLockStatus()
	{
		if (currentItemIndex >= 0)
		{
			currentInventory?.SwitchSlotLockStatus(currentItemIndex);
		}
	}

	protected virtual void PutMax()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_BACKPACK_TIDY_UP);
		for (int i = 0; i < currentInventory.capacity; i++)
		{
			Item item = currentInventory.Read(i);
			if (inBackpack)
			{
				if (CountItemInShelf(item) > 0)
				{
					backpackInventory.SwapItem(i, QuickPutInToShelf(currentInventory.Take(i), showMessageBox: false));
				}
			}
			else if (backpackInventory.Count(item, shouldEqualAsItem: false) > 0)
			{
				Item item2 = backpackInventory.PlaceItem(currentInventory.Take(i));
				if (item2 != null)
				{
					currentInventory.SwapItem(i, item2);
				}
			}
		}
	}

	private int CountItemInShelf(Item item)
	{
		return (from x in shelfInventory.ReadAll()
			select x as ItemBox into x
			where x != null
			select x).Sum((ItemBox x) => x.inventory.Count(item, shouldEqualAsItem: false));
	}

	protected override void Show()
	{
		base.Show();
		backpackPanel.SetDeleteCallback(DestroyItem);
		base.panel.Show(useTween: true, RebuildNavigation);
		backpackPanel.Select(DolocAPI.SelectedItemIndex);
		OnBufferChanged(buffer.CurrentItem);
		base.Show();
	}

	protected override void Hide()
	{
		base.Hide();
		currentBoxIndex = 0;
		base.panel.Hide();
		DolocAPI.PlaceInBackpackOrGenerateDropItem(buffer.Take(), DolocAPI.userSettings.autoUseBox);
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsSplitPressed)
		{
			timer.ReStart();
			if (currentItemIndex < 0)
			{
				HandleSwapShelfBox(currentBoxIndex, forceSwap: true);
			}
			else
			{
				HandleSwapOneItem(currentItemIndex);
			}
		}
		else if (userInput.BaseIsSplitInProgress && timer.Update(deltaTime))
		{
			if (currentItemIndex < 0)
			{
				HandleSwapShelfBox(currentBoxIndex, forceSwap: true);
			}
			else
			{
				HandleSwapOneItem(currentItemIndex);
			}
		}
		if (userInput.BaseIsConfirmPressed)
		{
			if (currentItemIndex < 0)
			{
				HandleSwapShelfBox(currentBoxIndex);
			}
			else
			{
				HandleSwapAllItems(currentItemIndex);
			}
		}
		if (userInput.BaseIsConfirmHold)
		{
			if (currentItemIndex < 0)
			{
				HandleSwapShelfBox(currentBoxIndex, forceSwap: true);
			}
			else
			{
				HandleSwapAllItems(currentItemIndex, forceSwap: true);
			}
		}
		else if (userInput.BaseSortItem)
		{
			Sort();
		}
		else if (userInput.BaseLockItem)
		{
			SwitchSlotLockStatus();
		}
		else if (userInput.BaseDisposeItem)
		{
			if (currentItemIndex < 0)
			{
				if (DolocAPI.DisposeItem(currentBox))
				{
					shelfInventory.Take(currentBoxIndex);
				}
			}
			else if (DolocAPI.DisposeItem(selectedItem))
			{
				currentInventory.Take(currentItemIndex);
			}
		}
		else if (userInput.BaseDestroyItem)
		{
			DestroyItem();
		}
		else if (userInput.BasePutMaxItem)
		{
			PutMax();
		}
		else if (userInput.BasePutAllItem)
		{
			PutAll();
		}
		else if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}

	private void ShowTipInBackpack()
	{
		string[] first = new string[6]
		{
			base.staticTexts.UiTipPutMax,
			base.staticTexts.UiTipPutAll,
			base.staticTexts.UiTipDispose,
			base.staticTexts.UiTipDestroyItem,
			base.staticTexts.UiTipTidy,
			base.staticTexts.UiTipLockSlot
		};
		base.panel.operationTip.SetTextKey(first.Concat(GetBackpackTipsByInput()).ToArray());
	}

	private IEnumerable<string> GetBackpackTipsByInput()
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

	private void ShowTipInContainer()
	{
		string[] first = new string[6]
		{
			base.staticTexts.UiTipTakeOutMax,
			base.staticTexts.UiTipTakeOutAll,
			base.staticTexts.UiTipDispose,
			base.staticTexts.UiTipDestroyItem,
			base.staticTexts.UiTipTidy,
			base.staticTexts.UiTipLockSlot
		};
		base.panel.operationTip.SetTextKey(first.Concat(GetContainerTipsByInput()).ToArray());
	}

	private IEnumerable<string> GetContainerTipsByInput()
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

	protected override void RefreshTip()
	{
		base.RefreshTip();
		if (inBackpack)
		{
			ShowTipInBackpack();
		}
		else
		{
			ShowTipInContainer();
		}
	}

	private void OnShelfBoxClick(int boxIndex)
	{
		if (!userInput.BaseIsConfirmPressed && !userInput.BaseIsConfirmInProgress)
		{
			HandleSwapShelfBox(boxIndex);
		}
	}

	private void HandleSwapShelfBox(int boxIndex, bool forceSwap = false)
	{
		Item currentItem = buffer.CurrentItem;
		if (currentItem == null || currentItem is ItemBox)
		{
			if (buffer.CurrentItem != null || forceSwap || DolocButtonComponent.latestClickType == ClickType.Mouse)
			{
				DolocAPI.SwapItemFromOutside(shelfInventory, boxIndex);
			}
			else
			{
				PlaceBoxToOtherSide(boxIndex);
			}
		}
	}

	private void PlaceBoxToOtherSide(int boxIndex)
	{
		if (shelfInventory == null)
		{
			return;
		}
		Item currentItem = buffer.CurrentItem;
		if (currentItem == null || currentItem is ItemBox)
		{
			if (backpackInventory.CanPlaceIn(shelfInventory.Read(boxIndex)))
			{
				Item item = shelfInventory.Take(boxIndex);
				backpackInventory.PlaceItem(item);
				int index = Mathf.Max(0, backpackInventory.IndexOf(item));
				backpackPanel.Select(index);
			}
			OnBufferChanged(null);
		}
	}

	protected virtual void OnBackpackItemClick(int index)
	{
		if (!userInput.BaseIsConfirmPressed && !userInput.BaseIsConfirmInProgress)
		{
			HandleSwapAllItems(index);
		}
	}

	protected virtual void OnContainerItemClick(int index)
	{
		if (!userInput.BaseIsConfirmPressed && !userInput.BaseIsConfirmInProgress)
		{
			HandleSwapAllItems(index);
		}
	}

	private void HandleSwapAllItems(int index, bool forceSwap = false)
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

	private void HandleSwapOneItem(int index)
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

	private void HandlePlaceToOtherSide(int index)
	{
		if (!TryPlaceBuffer(index))
		{
			PlaceToOtherSide();
		}
	}

	private void HandleSwapHalfItems(int index)
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
		if (!inBackpack && currentBox != null && !currentBox.ContentFilter(buffer.CurrentItem))
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
		if (selectedItem != null)
		{
			Item item = selectedItem.Clone();
			item = ((!inBackpack) ? backpackInventory.PlaceItem(item) : QuickPutInToShelf(item, showMessageBox: true));
			if (selectedItem.count == item?.count)
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrBackpackIsFull);
			}
			else
			{
				currentInventory.TryCostAtIndex(currentItemIndex, selectedItem.count - (item?.count ?? 0));
			}
		}
	}

	private Item QuickPutInToShelf(Item item, bool showMessageBox)
	{
		if (item == null)
		{
			return null;
		}
		ItemBox[] array = (from x in shelfInventory.ReadAll()
			select x as ItemBox into x
			where x != null
			select x).ToArray();
		if (item is ItemBox && shelfInventory.PlaceItem(item) == null)
		{
			OnBufferChanged(null);
			return null;
		}
		if (array.Length == 0)
		{
			if (showMessageBox)
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.InventoryPanelPutBoxFirst);
			}
			return item;
		}
		if (!boxItemTemplate.ContentFilter(item))
		{
			if (item != null && showMessageBox)
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.InventoryPanelCannotPutIn);
			}
			return item;
		}
		ItemBox[] array2 = array;
		foreach (ItemBox itemBox in array2)
		{
			if (itemBox.inventory.Count(item, shouldEqualAsItem: false) != 0)
			{
				item = itemBox.inventory.PlaceItem(item);
				if (item == null)
				{
					break;
				}
			}
		}
		array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			item = array2[i].inventory.PlaceItem(item);
			if (item == null)
			{
				break;
			}
		}
		return item;
	}
}
