using System;
using System.Linq;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class ConversionRecipeUiState : ContainerBaseUiState
{
	private IRecipeGroup recipeGroup;

	private IRecipe currentRecipe;

	private IRecipe[] allRecipes;

	private Action<IRecipe, IRecipeGroup, int> onCraft;

	private bool backupItemName;

	private bool canEditBuffer;

	private string taskTitle;

	private Func<Item, string> extraInfoGetter;

	private Func<IRecipeGroup, IRecipe, int, string> restTimeInfoGetter;

	private Func<Item, Item> previewItemHandler;

	private int maxCraftCount => recipeGroup.MaxCraftCount;

	protected override UnityEvent OnCloseButtonClick => base.backpackPanel.OnCloseButtonClick;

	public bool HandleStartUpArgs(RecipeGroup recipeGroup, IContainer container, bool canEditBuffer, string taskTitle, Func<IRecipeGroup, IRecipe, int, string> restTimeInfoGetter, Action<IRecipe, IRecipeGroup, int> onCraft = null, Func<Item, string> extraInfoGetter = null, Func<Item, Item> previewItemHandler = null)
	{
		if (recipeGroup == null || container == null)
		{
			return false;
		}
		this.recipeGroup = recipeGroup;
		allRecipes = ((IRecipeGroup)recipeGroup).GetAllRecipes(includeLocked: true) ?? Array.Empty<IRecipe>();
		base.container = container;
		this.canEditBuffer = canEditBuffer;
		this.taskTitle = taskTitle;
		this.onCraft = onCraft;
		this.restTimeInfoGetter = restTimeInfoGetter;
		this.extraInfoGetter = extraInfoGetter;
		this.previewItemHandler = previewItemHandler;
		if (maxCraftCount > 0)
		{
			base.containerWidget.SetInfo(base.staticTexts.RecipePanelMaxCraftCount.Format(maxCraftCount));
		}
		return container.inventory != null;
	}

	protected override void OnBackpackItemRender(int index, Item item)
	{
		base.backpackPanel.GetSlot(index).grayed = !canEditBuffer || !base.container.ContentFilter(item);
	}

	protected override void OnBackpackItemClick(int index)
	{
		HandlePlaceToOtherSide(index);
	}

	protected override void OnContainerItemRender(int index, Item item)
	{
		base.OnContainerItemRender(index, item);
		if (extraInfoGetter != null)
		{
			string str = extraInfoGetter(item);
			string text = "";
			if (maxCraftCount > 0)
			{
				text += base.staticTexts.RecipePanelMaxCraftCount.Format(maxCraftCount);
			}
			text = text + " " + str.Colored(DolocUiColor.EYECATCHCOLOR_CYAN);
			if (!text.IsNullOrEmpty())
			{
				base.containerWidget.SetInfo(text.Trim());
			}
		}
	}

	protected override void OnContainerItemClick(int index)
	{
		HandlePlaceToOtherSide(index);
	}

	private bool CheckCanEdit()
	{
		if (!canEditBuffer && base.selectedItem != null && !taskTitle.IsNullOrEmpty())
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.RecipePanelAlreadyWorking, taskTitle));
		}
		return canEditBuffer;
	}

	protected override void HandleSwapOneItem(int index)
	{
		if (!CheckCanEdit())
		{
			return;
		}
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
		if (!CheckCanEdit())
		{
			return;
		}
		if (inBackpack)
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
			}
			else if (!base.container.ContentFilter(item))
			{
				if (item != null)
				{
					DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.InventoryPanelCannotPutIn);
				}
			}
			else if (base.containerInventory.CanPlaceIn(item.Clone(1)))
			{
				if (num >= item.count)
				{
					base.containerInventory.PlaceItem(base.backpackInventory.Take(base.currentIndex));
				}
				else if (base.backpackInventory.TryCostAtIndex(base.currentIndex, num))
				{
					base.containerInventory.PlaceItem(item.Clone(num));
				}
			}
			else
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.InventoryPanelContainerFull, base.container.title));
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
		if (!CheckCanEdit())
		{
			return;
		}
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

	protected override void Show()
	{
		base.Show();
		base.backpackPanel.SetCloseButtonVisible(value: true);
		base.containerWidget.SetCloseButtonVisible(value: false);
		RefreshInfo();
	}

	protected override void Hide()
	{
		base.Hide();
		onCraft?.Invoke(currentRecipe, recipeGroup, base.containerInventory.FirstItem?.count ?? 0);
		currentRecipe = null;
	}

	private void OnInventoryChange(int index, Item item, bool _)
	{
		RefreshInfo();
	}

	private void RefreshInfo()
	{
		currentRecipe = null;
		Item firstItem = base.containerInventory.FirstItem;
		if (firstItem != null)
		{
			IRecipe[] array = allRecipes;
			foreach (IRecipe recipe in array)
			{
				if (recipe != null && !(recipe.InputItems.First().itemName != firstItem.name))
				{
					currentRecipe = recipe;
					break;
				}
			}
			if (currentRecipe == null)
			{
				base.containerWidget.RenderRecipeViewer(default(ConversionRecipeData));
				return;
			}
		}
		base.containerWidget.RenderRecipeViewer(new ConversionRecipeData(currentRecipe, recipeGroup, base.containerInventory.FirstItem?.count ?? 0, restTimeInfoGetter, previewItemHandler));
		DolocAPI.DelayFrame(base.panel.RebuildNavigation);
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
