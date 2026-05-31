using DolocTown.Config;
using UnityEngine;

namespace DolocTown.UI;

public struct RecipeData : ICraftData, IUIData
{
	private int outputMinCount;

	private int outputMaxCount;

	public bool notEmpty { get; }

	public string outputItem { get; }

	public Sprite outputItemSprite { get; }

	public string outputCount { get; }

	public Sprite sceneSprite { get; }

	public string outputItemTitle { get; }

	public string recipeTitle { get; }

	public string typeInfo { get; }

	public string description { get; }

	public string comment { get; }

	public string detailInfo { get; }

	public int timeUnit { get; }

	public string costTimeInfo { get; }

	public string buttonText { get; }

	public CostViewerData itemCosts { get; }

	public bool isCostEnough { get; }

	public int existItemCount { get; }

	public bool showExistItemCount { get; }

	public string maxCraftCountInfo { get; }

	public EffectGroupData itemEffects { get; }

	public string storageInfo { get; }

	public bool soldOut { get; }

	public RecipeData(IRecipe recipe, LinearInventory[] inventories, IRecipeGroup recipeGroup = null, bool hideSingleCount = true, string confirmText = "")
	{
		this = default(RecipeData);
		if (recipe == null || inventories.IsNullOrEmpty())
		{
			return;
		}
		notEmpty = true;
		RangedItem rangedItem = recipe.OutputItem;
		Item item = DolocAPI.GenerateItem(rangedItem.itemName);
		if (item == null)
		{
			return;
		}
		outputItemSprite = item.uiSprite;
		outputMinCount = rangedItem.minCount;
		outputMaxCount = rangedItem.maxCount;
		if (hideSingleCount && rangedItem.minCount == rangedItem.maxCount && rangedItem.minCount == 1)
		{
			outputCount = string.Empty;
		}
		else
		{
			outputCount = ((rangedItem.minCount == rangedItem.maxCount) ? rangedItem.minCount.ToString() : $"{rangedItem.minCount}-{rangedItem.maxCount}");
		}
		outputItemTitle = item.title;
		recipeTitle = recipe.RecipeTitle;
		typeInfo = item.subType.Title;
		description = item.description;
		comment = ((recipe is Recipe) ? DolocConfig.StaticTexts.RecipePanelUnlockedRecipeComment : DolocConfig.StaticTexts.RecipePanelUnlockedDishComment);
		Item item2 = DolocAPI.GenerateItem(rangedItem.itemName);
		detailInfo = ((item2 != null) ? item2.GetDetailInfo() : "");
		showExistItemCount = !(recipe is ExchangeStoreRecipe);
		if (showExistItemCount)
		{
			existItemCount = DolocAPI.GetExistItemCount(rangedItem.itemName);
		}
		if (item is ItemEquipment itemEquipment)
		{
			sceneSprite = ((itemEquipment.EquipmentProto == null) ? item.uiSprite : itemEquipment.EquipmentProto.Sprite);
		}
		else if (item is ItemPlatform itemPlatform)
		{
			sceneSprite = ((itemPlatform.PlatformProto == null) ? item.uiSprite : itemPlatform.PlatformProto.SceneSpriteAsset.Asset);
		}
		else if (item is ItemBuilding itemBuilding)
		{
			sceneSprite = ((itemBuilding.BuildingProto == null) ? item.uiSprite : itemBuilding.BuildingProto.DefaultSceneSprite);
		}
		else
		{
			sceneSprite = item.uiSprite;
		}
		timeUnit = recipeGroup?.GetRecipeTime(recipe) ?? 0;
		costTimeInfo = GetFormatTimeString(1);
		itemCosts = new CostViewerData(recipe.InputItems, inventories, recipe.MoneyCost);
		isCostEnough = itemCosts.isEnough;
		buttonText = ((!itemCosts.isMoneyCostEnough) ? DolocConfig.StaticTexts.BuildingPanelMoneyNotEnough : ((!isCostEnough) ? DolocConfig.StaticTexts.BuildingPanelMaterialNotEnough : (confirmText.IsNullOrEmpty() ? DolocConfig.StaticTexts.RecipePanelStartBuild : confirmText)));
		itemEffects = new EffectGroupData(item);
		int num = recipeGroup?.MaxCraftCount ?? 0;
		maxCraftCountInfo = ((num <= 0) ? "" : DolocConfig.StaticTexts.RecipePanelMaxCraftCount.Format(num));
		int storage = recipe.Storage;
		if (storage < 0)
		{
			goto IL_02f8;
		}
		string text;
		if (storage != 0)
		{
			if (storage == int.MaxValue)
			{
				goto IL_02f8;
			}
			text = recipe.Storage.ToString();
		}
		else
		{
			text = DolocConfig.StaticTexts.StoreSoldOutIcon;
		}
		goto IL_0320;
		IL_0320:
		storageInfo = text;
		soldOut = recipe.Storage == 0;
		return;
		IL_02f8:
		text = "";
		goto IL_0320;
	}

	public string GetTargetCount(int multiple)
	{
		if (outputMinCount != outputMaxCount)
		{
			return $"{outputMinCount * multiple}-{outputMaxCount * multiple}";
		}
		return (outputMinCount * multiple).ToString();
	}

	public string GetFormatTimeString(int multiple)
	{
		int num = Mathf.Max(0, timeUnit * multiple);
		if (num != 0)
		{
			return DolocUtils.Format(DolocConfig.StaticTexts.RecipePanelTimeInfo, DolocAPI.GetFormatTimeLengthByTU(num));
		}
		return string.Empty;
	}
}
