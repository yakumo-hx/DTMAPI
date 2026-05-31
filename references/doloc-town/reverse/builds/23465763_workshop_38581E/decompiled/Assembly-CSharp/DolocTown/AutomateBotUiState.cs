using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Recipe;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class AutomateBotUiState : DolocUiState<AutomateBotPanel>
{
	private AutomateBotStation station;

	private int botIndex;

	private List<string> currentRecipeList = new List<string>();

	private string recipeName;

	private AutomateBotWidget AutomateBotWidget => base.panel.automateBotWidget;

	private BackpackSideBarWidget backpackPanel => base.panel.backpackPanel;

	protected override UnityEvent OnCloseButtonClick => backpackPanel.OnCloseButtonClick;

	private int Capacity => station.Bots.Length;

	private InventorySystem inventorySystem => DolocAPI.archiveHandle.InventorySystem;

	public bool HandleStartUpArgs(AutomateBotStation station)
	{
		this.station = station;
		botIndex = 0;
		return true;
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsLastPressed)
		{
			MoveLast();
		}
		else if (userInput.BaseIsNextPressed)
		{
			MoveNext();
		}
		else if (userInput.BaseDestroyItem)
		{
			SwitchWorkState();
		}
		else if (userInput.BaseSortItem)
		{
			Unload();
		}
		else if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		RenderAllBots();
		AutomateBotWidget.AutomateBotSubMenu.FireClick(botIndex, fireSelect: true);
		backpackPanel.displayAnimType = UiPanelDisplayAnimType.FromRight;
		base.panel.operationTip.SetTextKey(new string[4]
		{
			base.staticTexts.UiTipSwitchAutomateBot,
			base.staticTexts.UiTipAutomateBotSwitch,
			base.staticTexts.UiTipAutomateBotUnload,
			base.staticTexts.UiTipAutomateBotLoad
		});
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	protected override void Register()
	{
		AutomateBotWidget.AutomateBotSubMenu.SetClickCallbacks(ClickBotSlot);
		AutomateBotWidget.BotRecipeViewer.onDataSelect.AddListener(OnDataSelect);
		AutomateBotWidget.BotRecipeViewer.onDataClick.AddListener(OnDataClick);
		AutomateBotWidget.BotRecipeViewer.DataGetter = RecipeDataGetter;
		backpackPanel.BindInventory(inventorySystem.inventory, OnRenderItem);
		backpackPanel.SetClickCallbacks(TryLoadBot);
	}

	protected override void Unregister()
	{
		AutomateBotWidget.AutomateBotSubMenu.RemoveCallbacks();
		AutomateBotWidget.BotRecipeViewer.onDataSelect.RemoveListener(OnDataSelect);
		AutomateBotWidget.BotRecipeViewer.onDataClick.RemoveListener(OnDataClick);
		AutomateBotWidget.BotRecipeViewer.DataGetter = null;
		backpackPanel.Clear();
	}

	private BotRecipeData[] RecipeDataGetter(int start, int end)
	{
		List<BotRecipeData> list = new List<BotRecipeData>();
		int count = currentRecipeList.Count;
		string usedRecipeId = ((station.Bots[botIndex].Param is AutomateParamProcessing automateParamProcessing) ? automateParamProcessing.Recipes.First() : string.Empty);
		for (int i = start; i < Mathf.Min(end, count); i++)
		{
			string recipeId = currentRecipeList[i];
			list.Add(new BotRecipeData(recipeId, usedRecipeId, i - start, end - start));
		}
		return list.ToArray();
	}

	private void ClickBotSlot(int index)
	{
		botIndex = index;
		AutomateBot automateBot = station.Bots[index];
		AutomateBotWidget.Viewer.RenderView(new AutomateBotData(automateBot));
		ResetViewerEvent(automateBot);
		DolocAPI.UIRaiseRoll();
		if (automateBot == null)
		{
			backpackPanel.Select(backpackPanel.selectedIndex);
		}
	}

	private void OnDataSelect(int index)
	{
		if (index >= 0 && index < currentRecipeList.Count)
		{
			recipeName = currentRecipeList[index];
			DolocAPI.UIRaiseRoll();
		}
	}

	private void OnDataClick(int index)
	{
		if (DolocButtonComponent.latestClickType != ClickType.Mouse)
		{
			SwitchRecipe();
			AutomateBotWidget.BotRecipeViewer.RefreshView();
		}
	}

	private void MoveNext()
	{
		int index = (botIndex + 1) % Capacity;
		AutomateBotWidget.AutomateBotSubMenu.FireClick(index, fireSelect: true);
	}

	private void MoveLast()
	{
		int index = (botIndex + Capacity - 1) % Capacity;
		AutomateBotWidget.AutomateBotSubMenu.FireClick(index, fireSelect: true);
	}

	private void SwitchRecipe()
	{
		if (station.Bots[botIndex].Param is AutomateParamProcessing automateParamProcessing && automateParamProcessing.Recipes.First() != recipeName)
		{
			station.Bots[botIndex].DecisionMaker.ResetTask();
		}
	}

	private void SwitchWorkState()
	{
		bool isPause = station.Bots[botIndex].IsPause;
		station.Bots[botIndex].IsPause = !isPause;
		RefreshBotByIndex(botIndex);
		AutomateBotWidget.AutomateBotSubMenu.FireClick(botIndex, fireSelect: true);
	}

	private void Unload()
	{
		station.Unload(botIndex);
		RefreshBotByIndex(botIndex);
		AutomateBotWidget.AutomateBotSubMenu.FireClick(botIndex, fireSelect: true);
	}

	private void TryLoadBot(int index)
	{
		if (backpackPanel.GetSlot(index).grayed)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.AutomateBotPanelItemError);
			return;
		}
		if (station.Load(botIndex, inventorySystem.inventory.Read(index)))
		{
			inventorySystem.inventory.Take(index);
			RefreshBotByIndex(botIndex);
		}
		AutomateBotWidget.AutomateBotSubMenu.FireClick(botIndex, fireSelect: true);
	}

	private void RenderAllBots()
	{
		AutomateBotWidget.AutomateBotSubMenu.SetCapacity(Capacity);
		for (int i = 0; i < station.Bots.Length; i++)
		{
			RefreshBotByIndex(i);
		}
	}

	private void RefreshBotByIndex(int index)
	{
		AutomateBot automateBot = station.Bots[index];
		if (automateBot != null)
		{
			AutomateBotWidget.AutomateBotSubMenu.Render(index, automateBot.proto.sprite, automateBot.IsLowPower, automateBot.IsPause);
		}
		else
		{
			AutomateBotWidget.AutomateBotSubMenu.RenderEmpty(index);
		}
	}

	private void ResetViewerEvent(AutomateBot bot)
	{
	}

	private void OnRenderItem(int index, Item item)
	{
		backpackPanel.slots[index].grayed = item != null && !(item is ItemAutomateBot);
	}

	private void SetRecipeSubMenu(RecipeMainTypeInfo recipeType, RecipeSubTypeInfo subType)
	{
	}
}
