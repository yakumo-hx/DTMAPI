using DolocTown.Config;
using DolocTown.Config.Building;
using UnityEngine;

namespace DolocTown.UI;

public struct BuildingData : ICraftData, IUIData
{
	public bool notEmpty { get; }

	public Sprite outputItemSprite { get; }

	public Sprite sceneSprite { get; }

	public string recipeTitle { get; }

	public string outputItemTitle { get; }

	public string description { get; }

	public string coverDescription { get; }

	public string innerDescription { get; }

	public string sizeDescription { get; }

	public string animalSpaceDescription { get; }

	public string buttonText { get; }

	public int moneyCost { get; }

	public CostViewerData itemCosts { get; }

	public bool isItemCostEnough { get; }

	public bool isMoneyCostEnough { get; }

	public bool isCostEnough { get; }

	public BuildingData(BuildingInfo proto, int currentMoney, LinearInventory[] currentInventories)
	{
		this = default(BuildingData);
		if (proto != null)
		{
			notEmpty = true;
			outputItemSprite = proto.UiSpriteAsset.Asset;
			sceneSprite = proto.DefaultSceneSprite;
			recipeTitle = proto.Title;
			outputItemTitle = proto.Title;
			description = proto.Description;
			coverDescription = string.Format(DolocConfig.StaticTexts.BuildingPanelCoverSizeDescription, proto.CoverSize.x, proto.CoverSize.y);
			innerDescription = string.Format(DolocConfig.StaticTexts.BuildingPanelInnerSizeDescription, proto.DefaultInnerSize.x, proto.DefaultInnerSize.y);
			sizeDescription = string.Format(DolocConfig.StaticTexts.BuildingPanelSizeDescription, proto.CoverSize.x, proto.CoverSize.y, proto.DefaultInnerSize.x, proto.DefaultInnerSize.y);
			animalSpaceDescription = ((proto.AnimalSpace <= 0) ? "" : DolocConfig.StaticTexts.BuildingPanelAnimalCapacityDescription.Format(proto.AnimalSpace));
			moneyCost = proto.MoneyCost;
			itemCosts = new CostViewerData(proto.ItemCost, currentInventories, proto.MoneyCost);
			isMoneyCostEnough = currentMoney >= moneyCost;
			isItemCostEnough = itemCosts.isEnough;
			isCostEnough = isMoneyCostEnough & isItemCostEnough;
			buttonText = ((!isMoneyCostEnough) ? DolocConfig.StaticTexts.BuildingPanelMoneyNotEnough : ((!isItemCostEnough) ? DolocConfig.StaticTexts.BuildingPanelMaterialNotEnough : DolocConfig.StaticTexts.BuildingPanelStartBuild));
		}
	}
}
