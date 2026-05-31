using System;
using System.Linq;
using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class FishTankUiState : ContainerBaseUiState
{
	private bool inSocket;

	private string socketTitle;

	private LinearInventory socketInventory;

	private Func<Item, bool> socketItemFilter;

	private Func<string> infoGetter;

	private Func<bool, bool> lockBufferChecker;

	private bool refreshFilterOnInventoryChange;

	protected override bool disablePutMax => true;

	protected override InventoryPanel currentPanel
	{
		get
		{
			if (!inSocket)
			{
				return base.currentPanel;
			}
			return SocketUI;
		}
	}

	protected override LinearInventory currentInventory
	{
		get
		{
			if (!inSocket)
			{
				return base.currentInventory;
			}
			return socketInventory;
		}
	}

	protected override UnityEvent OnCloseButtonClick => base.backpackPanel.OnCloseButtonClick;

	protected override SoundEvents SoundEventShow => SoundEvents.PLAY_UI_WATER_BUBBLES;

	private ContainerSocketUI SocketUI => base.panel.containerWidget.containerSocketUI;

	public bool HandleContainerStartUpArgs(IContainer container, Action onExit, Func<string> infoGetter = null, Func<bool, bool> lockBufferChecker = null)
	{
		return HandleContainerStartUpArgs(container, string.Empty, null, null, onExit, infoGetter, lockBufferChecker);
	}

	public bool HandleContainerStartUpArgs(IContainer container, string socketTitle, LinearInventory socketInventory, Func<Item, bool> socketItemFilter, Action onExit, Func<string> infoGetter, Func<bool, bool> lockBufferChecker = null, bool refreshFilterOnInventoryChange = false)
	{
		if (socketInventory != null)
		{
			SocketUI.Show();
		}
		else
		{
			SocketUI.Hide();
		}
		this.socketTitle = socketTitle;
		this.socketInventory = socketInventory;
		this.socketItemFilter = socketItemFilter ?? ((Func<Item, bool>)((Item _) => false));
		this.infoGetter = infoGetter;
		this.lockBufferChecker = lockBufferChecker;
		this.refreshFilterOnInventoryChange = refreshFilterOnInventoryChange;
		return base.HandleStartUpArgs(container, onExit);
	}

	protected override string[] GetTipInContainer()
	{
		return new string[5]
		{
			base.staticTexts.UiTipTakeOutSelected,
			base.staticTexts.UiTipTakeOutAll,
			base.staticTexts.UiTipDispose,
			base.staticTexts.UiTipDestroyItem,
			base.staticTexts.UiTipTidy
		};
	}

	protected override string[] GetTipInBackpack()
	{
		return new string[6]
		{
			base.staticTexts.UiTipTakeOutAll,
			base.staticTexts.UiTipDispose,
			base.staticTexts.UiTipDestroyItem,
			base.staticTexts.UiTipTidy,
			base.staticTexts.UiTipPutOneSelected,
			base.staticTexts.UiTipPutAllSelected
		};
	}

	protected override void Register()
	{
		base.Register();
		base.backpackPanel.SetClickCallbacks(OnBackpackItemClick, OnBackpackItemClick, onLeftLongClick: OnBackpackItemLongClick, onAssistLeftClick: OnBackpackItemLongClick);
		base.containerWidget.SetClickCallbacks(OnContainerItemClick, OnContainerItemClick, onLeftLongClick: OnContainerItemClick, onAssistLeftClick: OnContainerItemClick);
		SocketUI.BindInventory(socketInventory, OnSocketItemRender);
		SocketUI.SetSelectCallbacks(OnSocketItemSelect);
		SocketUI.SetDeselectCallbacks(OnSocketItemDeselect);
		SocketUI.SetClickCallbacks(OnSocketItemClick, OnSocketItemClick, onLeftLongClick: OnSocketItemClick, onAssistLeftClick: OnSocketItemClick);
		if (infoGetter != null)
		{
			base.backpackInventory?.AddReceiver(OnAnyInventoryChange);
			base.containerInventory?.AddReceiver(OnAnyInventoryChange);
			socketInventory?.AddReceiver(OnAnyInventoryChange);
		}
	}

	protected override void Unregister()
	{
		base.Unregister();
		SocketUI.Clear();
		socketItemFilter = null;
		socketInventory = null;
		if (infoGetter != null)
		{
			base.backpackInventory?.RemoveReceiver(OnAnyInventoryChange);
			base.containerInventory?.RemoveReceiver(OnAnyInventoryChange);
			socketInventory?.RemoveReceiver(OnAnyInventoryChange);
		}
	}

	protected override void Show()
	{
		base.Show();
		base.backpackPanel.SetCloseButtonVisible(value: true);
		base.containerWidget.SetCloseButtonVisible(value: false);
		RefreshInfoText();
		inSocket = false;
		SocketUI.SetTitle(socketTitle);
		SocketUI.positionLocalX = (float)(DolocAPI.GlobalParameter.InventoryLineCapacity - DolocAPI.GlobalParameter.FishTankMaxCountOfSlots) * (base.containerWidget.CellSize.x + base.containerWidget.Spacing.x);
		base.panel.RebuildNavigation();
	}

	protected override void Hide()
	{
		base.Hide();
		SocketUI.Hide();
		base.containerWidget.SetInfo(string.Empty);
	}

	protected override bool HandleInput(float deltaTime)
	{
		if (base.HandleInput(deltaTime))
		{
			return true;
		}
		if (userInput.BaseIsConfirmHold)
		{
			if (CheckBufferLocked(showMessage: false))
			{
				return true;
			}
			PlaceToFishTank(base.currentIndex, putMax: true);
			return true;
		}
		return false;
	}

	protected override void OnBackpackItemRender(int index, Item item)
	{
		base.backpackPanel.GetSlot(index).grayed = CheckBufferLocked(showMessage: false) || (!base.container.ContentFilter(item) && !socketItemFilter(item));
	}

	protected override void OnContainerItemRender(int index, Item item)
	{
		base.containerWidget.GetSlot(index).grayed = CheckBufferLocked(showMessage: false);
	}

	protected void OnSocketItemRender(int index, Item item)
	{
		SocketUI.GetSlot(index).grayed = CheckBufferLocked(showMessage: false);
	}

	protected override void OnBackpackItemClick(int index)
	{
		if (!CheckBufferLocked(showMessage: true))
		{
			PlaceToFishTank(index, putMax: false);
		}
	}

	protected override void OnContainerItemClick(int index)
	{
		if (!CheckBufferLocked(showMessage: true))
		{
			if (!base.backpackInventory.CanPlaceIn(base.containerInventory.Read(index)))
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrBackpackIsFull);
				return;
			}
			Item item = base.containerInventory.Take(index);
			Item item2 = base.backpackInventory.PlaceItem(item);
			base.containerInventory.PlaceItemAt(index, item2);
		}
	}

	protected override void HandleSwapOneItem(int index)
	{
	}

	protected override void Sort()
	{
		if (inBackpack)
		{
			base.Sort();
		}
		else if (!inSocket && !CheckBufferLocked(showMessage: true))
		{
			int num = base.containerInventory.IsSorted();
			Item[] source = base.containerInventory.ReadAll();
			Item[] array = ((num <= 0) ? source.OrderBy((Item x) => x?.sortingOrder ?? 2.1474836E+09f).ToArray() : source.OrderByDescending((Item x) => x?.sortingOrder ?? (-1f)).ToArray());
			base.containerInventory.Clear();
			for (int i = 0; i < array.Length; i++)
			{
				base.containerInventory.PlaceItemAt(i, array[i]);
			}
		}
	}

	protected override void PutAll()
	{
		if (CheckBufferLocked(showMessage: true))
		{
			return;
		}
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_BACKPACK_TIDY_UP);
		for (int i = 0; i < base.containerInventory.capacity; i++)
		{
			Item target = base.containerInventory.Read(i);
			if (base.backpackInventory.CanPlaceIn(target))
			{
				base.backpackInventory.PlaceItem(base.containerInventory.Take(i));
			}
		}
	}

	private void OnBackpackItemLongClick(int index)
	{
		if (!CheckBufferLocked(showMessage: true))
		{
			PlaceToFishTank(index, putMax: true);
		}
	}

	private void OnSocketItemClick(int index)
	{
		if (!CheckBufferLocked(showMessage: true))
		{
			if (!base.backpackInventory.CanPlaceIn(socketInventory.Read(index)))
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrBackpackIsFull);
				return;
			}
			Item item = socketInventory.Take(index);
			Item item2 = base.backpackInventory.PlaceItem(item);
			socketInventory.PlaceItemAt(index, item2);
		}
	}

	private void OnSocketItemSelect(int index)
	{
		inSocket = true;
		OnContainerItemSelect(-1);
	}

	private void OnSocketItemDeselect(int index)
	{
		inSocket = false;
		isItemSlotSelected = false;
	}

	private void PlaceToFishTank(int index, bool putMax)
	{
		if (CheckBufferLocked(showMessage: false))
		{
			return;
		}
		Item item = base.backpackInventory.Read(index);
		if (item == null)
		{
			return;
		}
		if (socketItemFilter(item))
		{
			if (socketInventory.emptyCount == 0)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.InventoryPanelContainerFull, socketTitle));
				return;
			}
			int num = ((!putMax) ? 1 : Math.Min(socketInventory.emptyCount, item.count));
			for (int i = 0; i < num; i++)
			{
				socketInventory.PlaceItemAt(socketInventory.FirstEmptyIndex, item.Clone(1));
			}
			base.backpackInventory.TryCostAtIndex(index, num);
		}
		else if (!base.container.ContentFilter(item))
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.InventoryPanelCannotPutIn);
		}
		else if (base.containerInventory.emptyCount == 0)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.InventoryPanelContainerFull, base.container.title));
		}
		else
		{
			int num2 = ((!putMax) ? 1 : Math.Min(base.containerInventory.emptyCount, item.count));
			for (int j = 0; j < num2; j++)
			{
				base.containerInventory.PlaceItemAt(base.containerInventory.FirstEmptyIndex, item.Clone(1));
			}
			base.backpackInventory.TryCostAtIndex(index, num2);
		}
	}

	private void RefreshInfoText()
	{
		if (infoGetter != null)
		{
			base.containerWidget.SetInfo(infoGetter());
		}
	}

	private void OnAnyInventoryChange(int index, Item item, bool isSlotlocked)
	{
		RefreshInfoText();
		if (refreshFilterOnInventoryChange)
		{
			base.backpackPanel.RefreshView();
			base.containerWidget.RefreshView();
			SocketUI.RefreshView();
		}
	}

	private bool CheckBufferLocked(bool showMessage)
	{
		if (lockBufferChecker == null)
		{
			return false;
		}
		return lockBufferChecker(showMessage);
	}
}
