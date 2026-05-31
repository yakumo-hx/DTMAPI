using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class StoreUiState : PageUiStateBase<StorePanel, StoreItemData>
{
	private enum FocusMode
	{
		Backpack,
		Store
	}

	private Store store;

	private FocusMode focusMode;

	private Timer timer = new Timer(DolocAPI.GlobalParameter.QuantitySelectTimer_Ref);

	private Dictionary<int, StoreItemRef> storeItemCaches = new Dictionary<int, StoreItemRef>();

	private BackpackSideBarWidget backpackPanel => base.panel.backpackPanel;

	private InventorySystem inventorySystem => DolocAPI.archiveHandle.InventorySystem;

	private LinearInventory backpack => inventorySystem.inventory;

	private StoreWidget storePage => base.panel.storeWidget;

	protected override int totalCapacity => store.ItemCategory;

	protected override UnityEvent OnCloseButtonClick => backpackPanel.OnCloseButtonClick;

	public static Dictionary<string, int> soldItems { get; private set; } = new Dictionary<string, int>();


	public static Dictionary<string, int> boughtItems { get; private set; } = new Dictionary<string, int>();


	public static int latestEarning { get; private set; }

	public static int latestSpending { get; private set; }

	public bool HandleStartUpArgs(string storeName)
	{
		return DolocAPI.archiveHandle.QueryStore(storeName, out store);
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
		else if (userInput.BaseIsSplitPressed)
		{
			timer.ReStart();
			if (focusMode == FocusMode.Backpack)
			{
				SellItem(backpackPanel.selectedIndex, submitQuantity: false, useMaxCount: false);
			}
			else
			{
				BuyItem(storePage.selectedIndex, submitQuantity: true, useMaxQuantity: false);
			}
		}
		else if (userInput.BaseIsSplitInProgress)
		{
			if (timer.Update(deltaTime))
			{
				if (focusMode == FocusMode.Backpack)
				{
					SellItem(backpackPanel.selectedIndex, submitQuantity: false, useMaxCount: false);
				}
				else
				{
					BuyItem(storePage.selectedIndex, submitQuantity: false, useMaxQuantity: false);
				}
			}
		}
		else if (userInput.BaseIsConfirmPressed)
		{
			if (focusMode == FocusMode.Backpack)
			{
				SellItem(backpackPanel.selectedIndex, submitQuantity: true, useMaxCount: false);
			}
			else
			{
				BuyItem(storePage.selectedIndex, submitQuantity: true, useMaxQuantity: false);
			}
		}
		else if (userInput.BaseIsConfirmHold)
		{
			if (focusMode == FocusMode.Backpack)
			{
				SellItem(backpackPanel.selectedIndex, submitQuantity: true, useMaxCount: true);
			}
			else
			{
				BuyItem(storePage.selectedIndex, submitQuantity: true, useMaxQuantity: true);
			}
		}
		else if (!ContinuouslyPressLast(deltaTime, storePage.PrevPage))
		{
			ContinuouslyPressNext(deltaTime, storePage.NextPage);
		}
	}

	private void SwitchFocusMode(FocusMode type, bool force = false)
	{
		if (force || focusMode != type)
		{
			focusMode = type;
			DolocAPI.HideItemBorder();
			switch (focusMode)
			{
			case FocusMode.Backpack:
				DolocAPI.SetItemBorderVisible(value: true);
				break;
			case FocusMode.Store:
				DolocAPI.SetItemBorderVisible(value: false);
				break;
			}
			RefreshTip();
		}
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		storePage.SetTitle(store.title);
		storePage.SetEmptyInfo(base.staticTexts.StoreSoldOutIcon);
		storePage.SetMoney(store.money, useAnimation: false);
		backpackPanel.RefreshMoney(useAnimation: false);
	}

	protected override void Show()
	{
		base.Show();
		ClearCommandCaches();
		SetItemSlotGrayed();
		base.panel.Show();
		storePage.Select(0);
		SwitchFocusMode(FocusMode.Store, force: true);
		DolocAPI.DelayFrame(base.panel.BuildNavigation);
	}

	private string[] GetTipPanelText()
	{
		switch (focusMode)
		{
		case FocusMode.Backpack:
			_ = DolocAPI.UserInput.DeviceType;
			return new string[2]
			{
				base.staticTexts.UiTipSellAll,
				base.staticTexts.UiTipSellOne
			};
		case FocusMode.Store:
			_ = DolocAPI.UserInput.DeviceType;
			return new string[2]
			{
				base.staticTexts.UiTipBuyAllGamepad,
				base.staticTexts.UiTipBuyOne
			};
		default:
			return null;
		}
	}

	protected override void Hide()
	{
		base.panel.Hide();
		base.Hide();
	}

	protected override StoreItemData[] DataGetter(int start, int end)
	{
		List<StoreItemData> list = new List<StoreItemData>();
		StoreItemRef[] sortedItems = store.GetSortedItems();
		storeItemCaches.Clear();
		for (int i = start; i < Mathf.Min(end, sortedItems.Length); i++)
		{
			StoreItemRef storeItemRef = sortedItems[i];
			StoreItemData item = new StoreItemData(storeItemRef, store);
			list.Add(item);
			storeItemCaches[i] = storeItemRef;
		}
		DolocAPI.DelayFrame(base.panel.BuildNavigation, 2);
		return list.ToArray();
	}

	protected override void Register()
	{
		base.Register();
		storePage.onDataSelect.AddListener(OnStoreItemSelect);
		storePage.onDataDeselect.AddListener(HideStoreItemHover);
		storePage.onDataClick.AddListener(OnStoreItemLeftClick);
		storePage.onDataAssistClick.AddListener(OnStoreItemLeftLongClick);
		storePage.onDataLongClick.AddListener(OnStoreItemLeftLongClick);
		storePage.onDataRightClick.AddListener(OnStoreItemRightClick);
		storePage.onDataRightContinuesClick = OnStoreItemRightClickContinues;
		storePage.onDataPointerEnter.AddListener(ShowStoreItemHover);
		storePage.onDataPointerExit.AddListener(HideStoreItemHover);
		backpackPanel.BindInventory(inventorySystem.inventory, ConvertItemDataInBackpack);
		backpackPanel.SetSelectCallbacks(OnBackpackItemSelect);
		backpackPanel.SetClickCallbacks(OnBackpackItemLeftClick, OnBackpackItemRightClick, OnBackpackItemLeftLongClick, null, OnBackpackItemLeftLongClick, null, null, OnBackpackItemRightClickInterrupted);
	}

	protected override void Unregister()
	{
		storePage.onDataSelect.RemoveListener(OnStoreItemSelect);
		storePage.onDataDeselect.RemoveListener(HideStoreItemHover);
		storePage.onDataClick.RemoveListener(OnStoreItemLeftClick);
		storePage.onDataAssistClick.RemoveListener(OnStoreItemLeftLongClick);
		storePage.onDataLongClick.RemoveListener(OnStoreItemLeftLongClick);
		storePage.onDataRightClick.RemoveListener(OnStoreItemRightClick);
		storePage.onDataRightContinuesClick = null;
		storePage.onDataPointerEnter.RemoveListener(ShowStoreItemHover);
		storePage.onDataPointerExit.RemoveListener(HideStoreItemHover);
		backpackPanel.Clear();
		base.Unregister();
	}

	private ItemData ConvertItemDataInBackpack(Item item)
	{
		float priceScale;
		int itemUnitSellingPrice = store.GetItemUnitSellingPrice(item, out priceScale);
		return ItemData.ShowWithPrice(item, itemUnitSellingPrice, priceScale);
	}

	private void OnStoreItemSelect(int index)
	{
		DolocAPI.UIRaiseRoll();
		SwitchFocusMode(FocusMode.Store);
		ShowStoreItemHover(index);
	}

	private void ShowStoreItemHover(int index)
	{
		if (storeItemCaches.TryGetValue(index, out var value))
		{
			storePage.GetSlot(index).HoverItemViewer(ItemData.ShowAsGood(value.itemName), UIAlignmentType.RightMiddle, UIAlignmentType.LeftMiddle);
		}
	}

	private void HideStoreItemHover(int index)
	{
		DolocAPI.HideHoverBox();
	}

	private void OnStoreItemLeftClick(int index)
	{
		if (!userInput.BaseIsConfirmPressed && !userInput.BaseIsConfirmInProgress)
		{
			BuyItem(index, submitQuantity: true, useMaxQuantity: false);
		}
	}

	private void OnStoreItemRightClick(int index)
	{
		BuyItem(index, submitQuantity: false, useMaxQuantity: false);
	}

	private bool OnStoreItemRightClickContinues(int index)
	{
		return BuyItem(index, submitQuantity: false, useMaxQuantity: false);
	}

	private void OnStoreItemLeftLongClick(int index)
	{
		if (!userInput.BaseIsConfirmPressed && !userInput.BaseIsConfirmInProgress)
		{
			BuyItem(index, submitQuantity: true, useMaxQuantity: true);
		}
	}

	private bool BuyItem(int index, bool submitQuantity, bool useMaxQuantity)
	{
		if (!storeItemCaches.TryGetValue(index, out var cache))
		{
			return false;
		}
		if (store.GetCurrentCount(cache) == 0)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.StoreItemSoldOut);
			return false;
		}
		int unitPrice = store.GetItemUnitBuyingPrice(cache.itemName, cache.isBuyback);
		if (!DolocAPI.CanAffordMoney(unitPrice))
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.StorePlayerMoneyNotEnough);
			if (!DolocAPI.gameManager.gameInitConfig.skipMoneyVerifyInShop)
			{
				return false;
			}
		}
		if (!DolocAPI.CanPlaceItem(cache.itemName, 1, out var inventoryIndex))
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrBackpackIsFull);
			return false;
		}
		if (submitQuantity || !cache.isBuyback)
		{
			int itemCount = DolocAPI.CountItem(cache.itemName);
			int num = DolocAPI.MaxItemPlaceCount(cache.itemName);
			int currentCount = store.GetCurrentCount(cache);
			int num2 = ((unitPrice == 0) ? currentCount : (DolocAPI.archiveHandle.CurrentMoney / unitPrice));
			int maxCount = Mathf.Min(num, num2, currentCount);
			DolocAPI.EnterUI((StoreQuantitySubmitUiState state) => state.HandleStartUpArgs(new StoreQuantitySubmitData(maxCount, StoreSubmitType.Buying, DolocAPI.GenerateItem(cache.itemName), unitPrice, itemCount, DolocAPI.archiveHandle.CurrentMoney, (!useMaxQuantity) ? 1 : maxCount), delegate(int count)
			{
				BuyItemInternal(cache, count, index, inventoryIndex);
			}));
		}
		else
		{
			BuyItemInternal(cache, 1, index, inventoryIndex);
		}
		return true;
	}

	private void BuyItemInternal(StoreItemRef cache, int count, int storeIndex, int inventoryIndex)
	{
		store.BuyItem(cache, count, out var price);
		DolocAPI.PlaceItemAllForce(cache.itemName, count, out var _);
		DolocAPI.archiveHandle.CurrentMoney -= price;
		if (cache.isBuyback)
		{
			DolocAPI.archiveHandle.TraceBackMoneyMade(price);
		}
		storePage.RaiseSpriteFadeUp(storeIndex);
		backpackPanel.RaiseSpriteFadeDown(inventoryIndex);
		boughtItems.TryAdd(cache.itemName, 0);
		if (!cache.isBuyback)
		{
			latestSpending += price;
			boughtItems[cache.itemName] += count;
		}
		else
		{
			latestEarning -= price;
			if (soldItems.ContainsKey(cache.itemName))
			{
				soldItems[cache.itemName]--;
			}
		}
		DolocAPI.DelayFrame(RefreshView);
	}

	private void OnBackpackItemSelect(int index)
	{
		SwitchFocusMode(FocusMode.Backpack);
	}

	private void OnBackpackItemLeftClick(int index)
	{
		if (!userInput.BaseIsConfirmPressed && !userInput.BaseIsConfirmInProgress)
		{
			SellItem(index, submitQuantity: true, useMaxCount: false);
		}
	}

	private void OnBackpackItemLeftLongClick(int index)
	{
		if (!userInput.BaseIsConfirmPressed && !userInput.BaseIsConfirmInProgress && backpackPanel.itemGetter?.Invoke(index) != null)
		{
			SellItem(index, submitQuantity: true, useMaxCount: true);
		}
	}

	private void OnBackpackItemRightClick(int index)
	{
		SellItem(index, submitQuantity: false, useMaxCount: false);
	}

	private bool OnBackpackItemRightClickInterrupted(int index)
	{
		return SellItem(index, submitQuantity: false, useMaxCount: false);
	}

	private bool SellItem(int index, bool submitQuantity, bool useMaxCount)
	{
		Item item = backpackPanel.itemGetter?.Invoke(index);
		if (item == null)
		{
			return false;
		}
		if (backpackPanel.GetSlot(index).grayed)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.StoreItemNotSaleable);
			return false;
		}
		float priceScale;
		int unitPrice = store.GetItemUnitSellingPrice(item, out priceScale);
		if (store.money < unitPrice)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.StoreStoreMoneyNotEnough);
			if (!DolocAPI.gameManager.gameInitConfig.skipMoneyVerifyInShop)
			{
				return false;
			}
		}
		if (submitQuantity)
		{
			int itemCount = DolocAPI.CountItem(item, checkBox: false, shouldEqualAsItem: true);
			int maxSellCount = ((unitPrice == 0) ? itemCount : (store.money / unitPrice));
			int maxCount = Mathf.Min(itemCount, maxSellCount);
			DolocAPI.EnterUI((StoreQuantitySubmitUiState state) => state.HandleStartUpArgs(new StoreQuantitySubmitData(Mathf.Min(itemCount, maxSellCount), StoreSubmitType.Selling, item, unitPrice, itemCount, store.money, (!useMaxCount) ? 1 : maxCount), delegate(int count)
			{
				SellItemInternal(item, count, index);
			}));
		}
		else
		{
			SellItemInternal(item, 1, index);
		}
		return true;
	}

	private void SellItemInternal(Item item, int count, int index)
	{
		int[] array = (from x in backpack.ReadAllWithNull()
			select x?.count ?? 0).ToArray();
		HashSet<int> itemIndexes = new HashSet<int>();
		backpack.TryCostAtIndex(index, count, shouldEqualAsItem: true, itemIndexes, out var leftover);
		store.SellItem(item, count - leftover, limitCountByItem: false, out var price);
		DolocAPI.archiveHandle.CurrentMoney += price;
		for (int i = 0; i < array.Length; i++)
		{
			if ((backpack.Read(i)?.count ?? 0) != array[i])
			{
				backpackPanel.RaiseSpriteFadeUp(i);
			}
		}
		soldItems.TryAdd(item.name, 0);
		soldItems[item.name] += count;
		latestEarning += price;
		if (item is ItemAnimalPackage itemAnimalPackage && item.name == DolocAPI.GlobalParameter.ItemRefSturdySack && itemAnimalPackage.isFull)
		{
			for (int j = 0; j < count; j++)
			{
				DolocAPI.GenerateDropItem(DolocAPI.CurrentRoom, DolocAPI.GenerateItem(DolocAPI.GlobalParameter.ItemRefSturdySack), DolocAPI.AgentPosition, shouldSendMsg: false);
			}
		}
		DolocAPI.DelayFrame(delegate
		{
			RefreshView();
			foreach (KeyValuePair<int, StoreItemRef> storeItemCache in storeItemCaches)
			{
				if (item.canBuyback && storeItemCache.Value.isBuyback && !(storeItemCache.Value.itemName != item.name))
				{
					storePage.RaiseSpriteFadeDown(storeItemCache.Key);
					break;
				}
			}
		});
	}

	protected override void RefreshTip()
	{
		base.RefreshTip();
		base.panel.operationTip.SetTextKey(GetTipPanelText());
	}

	private void RefreshView()
	{
		backpackPanel.RefreshMoney();
		backpack.ReEmit();
		storePage.SetMoney(store.money);
		storePage.SetTotalCapacity(totalCapacity);
		storePage.RefreshView();
		SetItemSlotGrayed();
		DolocAPI.DelayFrame(base.panel.BuildNavigation);
	}

	private void SetItemSlotGrayed()
	{
		foreach (ItemNavSlot slot in backpackPanel.slots)
		{
			Item item = backpackPanel.itemGetter?.Invoke(slot.index);
			slot.grayed = !DolocAPI.IsItemSalable(item);
		}
	}

	private void ClearCommandCaches()
	{
		boughtItems.Clear();
		soldItems.Clear();
		latestEarning = 0;
		latestSpending = 0;
	}

	public static int GetLatestSoldItemCount(string itemName = "")
	{
		if (itemName.IsNullOrEmpty())
		{
			return soldItems.Values.Sum();
		}
		return soldItems.GetValueOrDefault(itemName, 0);
	}

	public static int GetLatestBoughtItemCount(string itemName = "")
	{
		if (itemName.IsNullOrEmpty())
		{
			return boughtItems.Values.Sum();
		}
		return boughtItems.GetValueOrDefault(itemName, 0);
	}
}
