using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.TechTree;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class RecipePanelUiState : DolocUiState<CookPanel>
{
	private LinearInventory[] inventoriesAround;

	private bool inFixedTask;

	private bool inDynamicTask;

	private string taskTitle;

	private string confirmText;

	private IRecipeGroup recipeGroup;

	private DishGroup dishGroup;

	private LinearInventory dishItemBuffer;

	private IRecipe[] unlockedRecipes;

	private bool craftState;

	private Action<IRecipe, IRecipeGroup, int> onCraft;

	private Action<IRecipe, IRecipeGroup> onExit;

	private CookPanelMode currentPanelMode;

	protected int firstSelectedIndex;

	private IRecipe currentRecipe;

	private IRecipeGroup currentRecipeGroup;

	private bool closeAfterCraft;

	private Func<IRecipe, int> maxCraftCountGetter;

	private Func<IRecipe, RecipeData> recipeDataConverter;

	private int prevIndex;

	private bool inTask
	{
		get
		{
			if (!inFixedTask)
			{
				return inDynamicTask;
			}
			return true;
		}
	}

	private bool isFixedRecipeMode
	{
		get
		{
			CookPanelMode cookPanelMode = currentPanelMode;
			return cookPanelMode == CookPanelMode.Fixed || cookPanelMode == CookPanelMode.OnlyFixed;
		}
	}

	private RecipePanel recipePanel => base.panel.recipePanel;

	private DishPanel dishPanel => base.panel.dishPanel;

	private BackpackSideBarWidget backpackPanel => base.panel.backpackPanel;

	protected int totalCapacity => unlockedRecipes.Length;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	private int currentIndex => recipePanel.selectedIndex;

	private LinearInventory backpackInventory => DolocAPI.archiveHandle.InventorySystem.inventory;

	private RecipeManager recipeManager => DolocAPI.archiveHandle.farmData.recipeManager;

	private bool hasLimitation => recipeGroup.MaxCraftCount > 0;

	private int maxCraftCount => maxCraftCountGetter(currentRecipe);

	protected RecipeData[] DataGetter(int start, int end)
	{
		List<RecipeData> list = new List<RecipeData>();
		int b = unlockedRecipes.Length;
		for (int i = start; i < Mathf.Min(end, b); i++)
		{
			IRecipe arg = unlockedRecipes[i];
			list.Add(recipeDataConverter(arg));
		}
		return list.ToArray();
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		currentRecipe = null;
	}

	protected override void Register()
	{
		recipePanel.SetTotalCapacity(totalCapacity);
		recipePanel.DataGetter = DataGetter;
		recipePanel.onDataSelect.AddListener(OnDataSelect);
		recipePanel.onDataClick.AddListener(OnDataClick);
		recipePanel.BtnCraft.SetClickCallbacks(OnStartButtonClickLeft, OnStartButtonClickRight, null, null, OnStartButtonLongClickLeft, null, null, OnStartButtonContinuesClickRight);
		recipePanel.switchBar.OnSwitch.AddListener(TrySwitchMode);
		if (dishItemBuffer == null)
		{
			dishPanel.ClearSlots();
		}
		else
		{
			dishItemBuffer.AddReceiver(OnDishBufferChanged);
		}
		dishPanel.OnSlotClick.AddListener(OnDishSlotClick);
		dishPanel.confirmButton.onClick.AddListener(OnDishConfirmClick);
		dishPanel.switchBar.OnSwitch.AddListener(TrySwitchMode);
		backpackPanel.BindInventory(backpackInventory, OnRenderItem);
		backpackPanel.SetClickCallbacks(OnBackpackClick);
	}

	protected override void Unregister()
	{
		recipePanel.onDataSelect.RemoveListener(OnDataSelect);
		recipePanel.onDataClick.RemoveListener(OnDataClick);
		recipePanel.BtnCraft.ClearAllClickCallbacks();
		recipePanel.DataGetter = null;
		recipePanel.switchBar.OnSwitch.RemoveListener(TrySwitchMode);
		dishItemBuffer?.RemoveReceiver(OnDishBufferChanged);
		dishPanel.OnSlotClick.RemoveListener(OnDishSlotClick);
		dishPanel.confirmButton.onClick.RemoveListener(OnDishConfirmClick);
		dishPanel.switchBar.OnSwitch.RemoveListener(TrySwitchMode);
		backpackPanel.Clear();
	}

	private void OnRenderItem(int index, Item item)
	{
		backpackPanel.slots[index].grayed = item != null && !item.cookable;
	}

	private void OnDataSelect(int index)
	{
		if (index >= 0 && index < unlockedRecipes.Length)
		{
			currentRecipe = unlockedRecipes[index];
			DolocAPI.UIRaiseRoll();
		}
		DolocAPI.HideHoverBox();
	}

	private void OnDataClick(int index)
	{
		if (DolocButtonComponent.latestClickType != ClickType.Mouse)
		{
			recipePanel.BtnCraft.FireClick();
		}
	}

	private void OnStartButtonClickLeft(int _)
	{
		OnStartButtonClick(single: false, useMaxCount: false);
	}

	private void OnStartButtonLongClickLeft(int _)
	{
		if (CheckCondition())
		{
			if (!hasLimitation || !closeAfterCraft)
			{
				OnStartButtonClick(single: false, useMaxCount: true);
			}
			else
			{
				TryConfirmCraft(maxCraftCount);
			}
		}
	}

	private void OnStartButtonClickRight(int _)
	{
		OnStartButtonClick(single: true, useMaxCount: false);
	}

	private bool OnStartButtonContinuesClickRight(int _)
	{
		return OnStartButtonClick(single: true, useMaxCount: false);
	}

	private bool OnStartButtonClick(bool single, bool useMaxCount)
	{
		base.panel.GetFocus();
		if (!CheckCondition())
		{
			return false;
		}
		currentRecipeGroup = recipeGroup;
		if (single)
		{
			TryConfirmCraft(1);
		}
		else
		{
			int maxCnt = maxCraftCount;
			if (maxCnt == 0)
			{
				return false;
			}
			DolocAPI.EnterUI((CraftQuantitySubmitUiState state) => state.HandleStartUpArgs(new CraftQuantityData(maxCnt, currentRecipe, inventoriesAround, recipeGroup, (!useMaxCount) ? 1 : maxCnt), TryConfirmCraft));
		}
		return true;
	}

	private bool CheckCondition()
	{
		if (totalCapacity == 0)
		{
			return false;
		}
		if (currentRecipe.Storage == 0)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.StoreItemSoldOut);
			return false;
		}
		if (inTask)
		{
			ShowInTaskErrorMessage();
			return false;
		}
		if (recipePanel.BtnCraft.grayed)
		{
			DolocAPI.ShowMessageBoxSmallErr((DolocAPI.archiveHandle.CurrentMoney < currentRecipe.MoneyCost) ? base.staticTexts.UiErrMoneyNotEnough : base.staticTexts.UiErrMaterialNotEnough);
			recipePanel.Select(currentIndex);
			if (DolocAPI.gameManager.shouldBuilderCostAssets)
			{
				return false;
			}
		}
		if (currentRecipe == null)
		{
			return false;
		}
		return true;
	}

	private void TryConfirmCraft(int count)
	{
		if (count == 0)
		{
			return;
		}
		LinearInventory linearInventory = dishItemBuffer;
		if (linearInventory != null && !linearInventory.isEmpty)
		{
			DolocAPI.ShowQuestionBox(DolocUtils.Format(base.staticTexts.RecipePanelConfirmPopBuffer, currentRecipeGroup.Title), delegate
			{
				Item[] array = dishItemBuffer.ReadAll();
				foreach (Item item in array)
				{
					DolocAPI.GenerateDropItem(DolocAPI.CurrentRoom, item, DolocAPI.AgentPosition);
				}
				dishItemBuffer.Clear();
				TryCostInputItemsInInventory(currentRecipe, count);
				onCraft?.Invoke(currentRecipe, recipeGroup, count);
				OnConfirmCraftEnd();
			});
		}
		else
		{
			TryCostInputItemsInInventory(currentRecipe, count);
			onCraft?.Invoke(currentRecipe, recipeGroup, count);
			OnConfirmCraftEnd();
		}
	}

	private void TryCostInputItemsInInventory(IRecipe recipe, int count)
	{
		if (DolocAPI.gameManager.shouldBuilderCostAssets)
		{
			DolocAPI.archiveHandle.CurrentMoney -= currentRecipe.MoneyCost * count;
			recipe.TryCostInputItemsInInventory(inventoriesAround, count);
		}
	}

	private void OnConfirmCraftEnd()
	{
		if (!closeAfterCraft)
		{
			recipePanel.RaiseCostItemsFadeUp();
		}
		recipePanel.RefreshView();
		if (closeAfterCraft)
		{
			gameController.PopState();
		}
	}

	private int GetMaxCraftCountDefault(IRecipe recipe)
	{
		int num = recipe.MaxAffordScale(inventoriesAround, DolocAPI.archiveHandle.CurrentMoney);
		if (recipe.MoneyCost > 0)
		{
			return Math.Min(num, DolocAPI.archiveHandle.CurrentMoney / recipe.MoneyCost);
		}
		return num;
	}

	private RecipeData ConvertRecipeDataDefault(IRecipe recipe)
	{
		return new RecipeData(recipe, inventoriesAround, recipeGroup, hideSingleCount: true, confirmText);
	}

	private void OnCraftDefault(IRecipe recipe, IRecipeGroup group, int count)
	{
		recipe.GenerateOutputItemAsDropItem(count);
		for (int i = 0; i < count; i++)
		{
			DolocAPI.AddTechExp(TechPointType.SCIENCE, recipe.TechPoint);
			DolocAPI.BroadcastString(GameEventType.MAKE_ITEM, currentRecipe.OutputItem.itemName);
		}
	}

	private void OnBackpackClick(int index)
	{
		if (dishItemBuffer == null)
		{
			return;
		}
		if (inTask)
		{
			ShowInTaskErrorMessage();
			return;
		}
		if (dishItemBuffer.isFull)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.RecipePanelLimitUp);
			return;
		}
		Item item = backpackInventory.Read(index);
		if (item != null)
		{
			if (!item.cookable)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.RecipePanelNotCookable);
				return;
			}
			backpackInventory.TryCostAtIndex(index, 1);
			dishItemBuffer.PlaceItemAt(dishItemBuffer.FirstEmptyIndex, item.Clone(1));
		}
	}

	private void OnDishBufferChanged(int index, Item item, bool _)
	{
		dishPanel.OnDishSlotChange(index, item);
		if (!isFixedRecipeMode)
		{
			RefreshRecipe();
		}
	}

	private void OnDishSlotClick(int index)
	{
		if (inTask)
		{
			ShowInTaskErrorMessage();
			backpackPanel.GetFocus();
			return;
		}
		Item item = dishItemBuffer.Take(index);
		if (item == null)
		{
			backpackPanel.GetFocus();
		}
		else
		{
			DolocAPI.PlaceInBackpackOrGenerateDropItem(item);
		}
	}

	private void ShowInTaskErrorMessage()
	{
		if (!taskTitle.IsNullOrEmpty())
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.RecipePanelAlreadyWorking, taskTitle));
		}
	}

	private void RefreshRecipe()
	{
		if (dishGroup != null)
		{
			CountItem[] items = dishItemBuffer.ReadAll().ConvertToCountItems();
			bool flag = false;
			IRecipe dish;
			if (recipeGroup != null && recipeGroup.TryGetRecipe(items, out var recipe))
			{
				currentRecipe = recipe;
				flag = recipeManager.CheckRecipeUnlocked(recipe.RecipeId);
			}
			else if (dishGroup.TryGetRecipe(items, out dish))
			{
				currentRecipe = dish;
				flag = recipeManager.CheckDishUnlocked(dish.RecipeId);
			}
			else
			{
				currentRecipe = dishGroup.GetDefaultRecipe(items);
			}
			if (!flag)
			{
				dishPanel.RenderLockedViewer(dishGroup.proto);
			}
			else
			{
				dishPanel.RenderViewer(new RecipeData(currentRecipe, inventoriesAround, dishGroup, hideSingleCount: true, confirmText));
			}
			DolocAPI.DelayFrame(base.panel.RefreshDishPanelNavigation);
		}
	}

	private void OnDishConfirmClick(int _)
	{
		DolocAPI.HideItemBorder();
		RefreshRecipe();
		if (inTask)
		{
			if (DolocButtonComponent.latestClickType != ClickType.Mouse)
			{
				ShowInTaskErrorMessage();
			}
			return;
		}
		if (dishItemBuffer.isEmpty)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.RecipePanelNoMaterial);
			return;
		}
		currentRecipeGroup = dishGroup;
		onCraft?.Invoke(currentRecipe, dishGroup, 1);
		if (closeAfterCraft)
		{
			DolocAPI.DelayFrame(delegate
			{
				gameController.PopState();
			});
		}
	}

	protected override void Show()
	{
		base.Show();
		currentRecipe = null;
		if (dishGroup != null)
		{
			backpackPanel.Select(DolocAPI.SelectedItemIndex);
			RefreshRecipe();
		}
		base.panel.SetMode(currentPanelMode);
		RefreshTip();
		base.panel.Show();
		if (isFixedRecipeMode && totalCapacity > 0)
		{
			recipePanel.Select(firstSelectedIndex);
			currentRecipe = unlockedRecipes[currentIndex];
		}
		recipePanel.SetTitle(recipeGroup.Title);
		recipePanel.SetEmptyInfo(base.staticTexts.UiTipEmptyList);
	}

	private void TrySwitchMode()
	{
		CookPanelMode cookPanelMode = currentPanelMode;
		if (cookPanelMode == CookPanelMode.Fixed || cookPanelMode == CookPanelMode.Dynamic)
		{
			if (inFixedTask)
			{
				ShowInTaskErrorMessage();
				return;
			}
			CookPanelMode cookPanelMode2 = currentPanelMode switch
			{
				CookPanelMode.Fixed => CookPanelMode.Dynamic, 
				CookPanelMode.Dynamic => CookPanelMode.Fixed, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
			currentRecipeGroup = cookPanelMode2 switch
			{
				CookPanelMode.Fixed => recipeGroup, 
				CookPanelMode.Dynamic => dishGroup, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
			base.panel.SetMode(cookPanelMode2);
			currentPanelMode = cookPanelMode2;
			RefreshTip();
		}
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BasePageDownPressed)
		{
			TrySwitchMode();
		}
		else if (isFixedRecipeMode)
		{
			if (ContinuouslyPressLast(deltaTime, recipePanel.PrevPage) || ContinuouslyPressNext(deltaTime, recipePanel.NextPage))
			{
				return;
			}
		}
		else if (userInput.BaseSubmitItem)
		{
			if (dishPanel.confirmButton.IsSelected)
			{
				dishPanel.confirmButton.FireClick();
			}
			else
			{
				DolocAPI.HideItemBorder();
				dishPanel.confirmButton.Select();
			}
		}
		if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}

	protected override void Hide()
	{
		onExit?.Invoke(currentRecipe, currentRecipeGroup);
		base.panel.Hide();
		base.Hide();
		firstSelectedIndex = 0;
	}

	protected override void RefreshTip()
	{
		base.RefreshTip();
		CookPanelMode cookPanelMode = currentPanelMode;
		if (cookPanelMode == CookPanelMode.Dynamic || cookPanelMode == CookPanelMode.OnlyDynamic)
		{
			base.panel.operationTip.SetTextKey("");
			return;
		}
		List<string> list = new List<string>();
		if (DolocAPI.UserInput.DeviceType == DolocInputDeviceType.KeyboardMouse)
		{
			if (confirmText.IsNullOrEmpty())
			{
				list.Add(base.staticTexts.UiTipMakeAll);
				list.Add(base.staticTexts.UiTipMakeOne);
			}
			else
			{
				list.Add(base.staticTexts.UiTipBuyAllGamepad);
				list.Add(base.staticTexts.UiTipBuyOne);
			}
		}
		else
		{
			list.Add("");
		}
		base.panel.operationTip.SetTextKey(list.ToArray());
	}

	public bool HandleStartUpArgs(IRecipeGroup recipeGroup, DishGroup dishGroup, bool firstFocusOnRecipe, LinearInventory dishItemBuffer, LinearInventory[] inventories, string firstSelectRecipe = "", bool closeAfterCraft = false, Func<IRecipe, int> maxCraftCountGetter = null, Action<IRecipe, IRecipeGroup, int> onCraft = null, Action<IRecipe, IRecipeGroup> onExit = null, string taskInfo = "", string taskTitle = "", bool inFixedTask = false, bool inDynamicTask = false, string confirmText = "")
	{
		this.recipeGroup = ((recipeGroup != null && recipeGroup.isValid) ? recipeGroup : null);
		this.dishGroup = ((dishGroup != null && dishGroup.isValid) ? dishGroup : null);
		if (dishItemBuffer != null && (this.dishGroup == null || this.dishGroup.proto.SlotCount != dishItemBuffer.capacity))
		{
			Debug.LogError("原料缓存容量与食谱组不匹配");
			return false;
		}
		if (this.dishGroup != null)
		{
			dishPanel.SetCapacity(this.dishGroup.proto.SlotCount);
		}
		this.dishItemBuffer = dishItemBuffer;
		if (this.recipeGroup == null && this.dishGroup == null)
		{
			return false;
		}
		if (this.recipeGroup == null)
		{
			currentPanelMode = CookPanelMode.OnlyDynamic;
		}
		else if (this.dishGroup == null)
		{
			currentPanelMode = CookPanelMode.OnlyFixed;
		}
		else if (firstFocusOnRecipe)
		{
			currentPanelMode = CookPanelMode.Fixed;
		}
		else
		{
			currentPanelMode = CookPanelMode.Dynamic;
		}
		IRecipeGroup recipeGroup2;
		switch (currentPanelMode)
		{
		case CookPanelMode.OnlyFixed:
		case CookPanelMode.Fixed:
			recipeGroup2 = this.recipeGroup;
			break;
		case CookPanelMode.OnlyDynamic:
		case CookPanelMode.Dynamic:
			recipeGroup2 = dishGroup;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		currentRecipeGroup = recipeGroup2;
		inventoriesAround = inventories ?? Array.Empty<LinearInventory>();
		unlockedRecipes = this.recipeGroup?.GetAllUnlockRecipes() ?? Array.Empty<IRecipe>();
		firstSelectedIndex = 0;
		if (firstFocusOnRecipe && !firstSelectRecipe.IsNullOrEmpty())
		{
			for (int i = 0; i < unlockedRecipes.Length; i++)
			{
				if (unlockedRecipes[i].RecipeId == firstSelectRecipe)
				{
					firstSelectedIndex = i;
					break;
				}
			}
		}
		this.onCraft = onCraft ?? new Action<IRecipe, IRecipeGroup, int>(OnCraftDefault);
		this.onExit = onExit;
		this.closeAfterCraft = closeAfterCraft;
		this.maxCraftCountGetter = maxCraftCountGetter ?? new Func<IRecipe, int>(GetMaxCraftCountDefault);
		recipeDataConverter = ConvertRecipeDataDefault;
		base.panel.InitSwitchBar(recipeGroup, dishGroup);
		base.panel.SetTaskInfo(taskInfo);
		this.inFixedTask = inFixedTask;
		this.inDynamicTask = inDynamicTask;
		this.taskTitle = (inTask ? taskTitle : "");
		this.confirmText = confirmText;
		return true;
	}

	public bool HandleExchangeStoreStartUpArgs(ExchangeStore store, LinearInventory[] inventories)
	{
		return HandleStartUpArgs(store, null, firstFocusOnRecipe: true, null, inventories, "", closeAfterCraft: false, (IRecipe recipe) => Mathf.Min(GetMaxCraftCountDefault(recipe), store.GetItemCurrentStorage(recipe.RecipeId)), delegate(IRecipe recipe, IRecipeGroup group, int count)
		{
			store.BuyItem(recipe.RecipeId);
			OnCraftDefault(recipe, group, count);
			int selectedIndex = recipePanel.selectedIndex;
			unlockedRecipes = ((IRecipeGroup)store).GetAllUnlockRecipes();
			recipePanel.SetTotalCapacity(unlockedRecipes.Length);
			recipePanel.RefreshView();
			recipePanel.Select(selectedIndex);
		}, null, "", "", inFixedTask: false, inDynamicTask: false, store.proto.CraftText);
	}

	public bool HandleStartUpArgs(string recipeGroupId, LinearInventory[] inventories, string firstSelectRecipe = "", bool closeAfterCraft = false, Func<IRecipe, int> maxCraftCountGetter = null, Action<IRecipe, IRecipeGroup, int> onCraft = null, Action<IRecipe, IRecipeGroup> onExit = null, string taskInfo = "")
	{
		RecipeGroup recipeGroup = new RecipeGroup(recipeGroupId);
		return HandleStartUpArgs(recipeGroup, null, firstFocusOnRecipe: true, null, inventories, firstSelectRecipe, closeAfterCraft, maxCraftCountGetter, onCraft, onExit, taskInfo);
	}

	public bool HandleStartUpArgs(RecipeGroup recipeGroup, LinearInventory[] inventories, string firstSelectRecipe = "", bool closeAfterCraft = false, Func<IRecipe, int> maxCraftCountGetter = null, Action<IRecipe, IRecipeGroup, int> onCraft = null, Action<IRecipe, IRecipeGroup> onExit = null, string taskInfo = "")
	{
		return HandleStartUpArgs(recipeGroup, null, firstFocusOnRecipe: true, null, inventories, firstSelectRecipe, closeAfterCraft, maxCraftCountGetter, onCraft, onExit, taskInfo);
	}
}
