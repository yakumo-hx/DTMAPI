using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.TechTree;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class BuildingPanelUiState : PageUiStateBase<BuildingPanel, BuildingData>
{
	private List<string> unlockedBuildings;

	private string currentBuildingName;

	private LinearInventory[] inventoriesAround;

	private int prevIndex;

	protected override int totalCapacity => unlockedBuildings.Count;

	private int selectedIndex => base.panel.selectedIndex;

	protected override BuildingData[] DataGetter(int start, int end)
	{
		List<BuildingData> list = new List<BuildingData>();
		int count = unlockedBuildings.Count;
		for (int i = start; i < Mathf.Min(end, count); i++)
		{
			DolocAPI.QueryBuilding(unlockedBuildings[i], out var proto);
			list.Add(new BuildingData(proto, DolocAPI.archiveHandle.CurrentMoney, inventoriesAround));
		}
		return list.ToArray();
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		currentBuildingName = null;
		inventoriesAround = DolocAPI.GetBackpackWithInsideBoxes();
		unlockedBuildings = DolocAPI.archiveHandle.farmData.unlockedBuildings.OrderBy((string x) => DolocConfig.Tables.TbBuilding.GetOrDefault(x).Order).ToList();
	}

	protected override void Register()
	{
		base.Register();
		base.panel.onDataSelect.AddListener(OnDataSelect);
		base.panel.onDataClick.AddListener(OnDataClick);
		base.panel.BtnCraft.onClick.AddListener(OnStartButtonClick);
	}

	protected override void Unregister()
	{
		currentBuildingName = null;
		base.panel.onDataSelect.RemoveListener(OnDataSelect);
		base.panel.onDataClick.RemoveListener(OnDataClick);
		base.panel.BtnCraft.onClick.RemoveListener(OnStartButtonClick);
		base.Unregister();
	}

	private void OnDataSelect(int index)
	{
		if (index >= 0 && index < unlockedBuildings.Count)
		{
			currentBuildingName = unlockedBuildings[index];
			DolocAPI.UIRaiseRoll();
		}
		DolocAPI.HideHoverBox();
	}

	private void OnDataClick(int index)
	{
		if (DolocButtonComponent.latestClickType != ClickType.Mouse)
		{
			base.panel.BtnCraft.FireClick();
		}
	}

	private void OnStartButtonClick(int _)
	{
		if (currentBuildingName.IsNullOrEmpty() || !DolocAPI.QueryBuilding(currentBuildingName, out var proto))
		{
			return;
		}
		if (base.panel.BtnCraft.grayed)
		{
			DolocAPI.ShowMessageBoxSmallErr((DolocAPI.archiveHandle.CurrentMoney < proto.MoneyCost) ? base.staticTexts.UiErrMoneyNotEnough : base.staticTexts.UiErrMaterialNotEnough);
			base.panel.Select(selectedIndex);
			if (DolocAPI.gameManager.shouldBuilderCostAssets)
			{
				return;
			}
		}
		IRecipe recipe = new BuildingRecipe(proto);
		if (!recipe.isValid)
		{
			Debug.LogError("配方<" + proto.Id + ">不存在");
			return;
		}
		int maxCount = recipe.MaxAffordScale(inventoriesAround, DolocAPI.archiveHandle.CurrentMoney);
		if (maxCount == 0)
		{
			return;
		}
		DolocAPI.EnterUI((CraftQuantitySubmitUiState state) => state.HandleStartUpArgs(new CraftQuantityData(maxCount, recipe, inventoriesAround), delegate(int count)
		{
			MakeItem(recipe, count);
		}));
	}

	private void MakeItem(IRecipe recipe, int count)
	{
		recipe.GenerateOutputItemAsDropItem(count);
		if (DolocAPI.gameManager.shouldBuilderCostAssets)
		{
			DolocAPI.archiveHandle.CurrentMoney -= recipe.MoneyCost * count;
			recipe.TryCostInputItemsInInventory(inventoriesAround, count);
		}
		base.panel.RefreshView();
		base.panel.RaiseCostItemsFadeUp();
		string arg = DolocUtils.Format(base.staticTexts.UiItemTip, DolocAPI.GetItemTitle(currentBuildingName), count);
		DolocAPI.ShowMessageBoxSmall(DolocUtils.Format(base.staticTexts.EquipmentPanelMakeComplete, arg));
		DolocAPI.AddTechExp(TechPointType.SCIENCE, recipe.TechPoint * count);
		for (int i = 0; i < count; i++)
		{
			DolocAPI.BroadcastString(GameEventType.MAKE_ITEM, recipe.OutputItem.itemName);
		}
	}

	protected override void Show()
	{
		base.Show();
		if (totalCapacity > 0)
		{
			currentBuildingName = unlockedBuildings[0];
		}
		base.panel.SetTitle(base.staticTexts.BuildingPanelTitle);
		base.panel.SetEmptyInfo(base.staticTexts.UiTipEmptyList);
	}

	public override void OnPause()
	{
		base.OnPause();
		prevIndex = selectedIndex;
	}

	public override void OnResume()
	{
		base.OnResume();
		base.panel.Select(prevIndex);
	}
}
