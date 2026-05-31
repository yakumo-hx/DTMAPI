using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Platform;
using UnityEngine;

namespace DolocTown.UI;

public struct PlatformData : ICraftData, IUIData
{
	public bool notEmpty { get; }

	public Sprite outputItemSprite { get; }

	public Sprite sceneSprite { get; }

	public string recipeTitle { get; }

	public string outputItemTitle { get; }

	public string description { get; }

	public string buttonText { get; }

	public CostViewerData itemCosts { get; }

	public bool isCostEnough { get; }

	public string avgCostInfo { get; }

	public string itemTiles { get; }

	public PlatformData(PlatformInfo proto, LinearInventory currentInventory)
	{
		this = default(PlatformData);
		if (proto != null && currentInventory != null)
		{
			notEmpty = true;
			outputItemSprite = DolocAPI.GetItemSprite(proto.Id);
			sceneSprite = proto.SceneSpriteAsset.Asset;
			recipeTitle = proto.Title;
			outputItemTitle = recipeTitle;
			description = proto.Description;
			itemCosts = new CostViewerData(proto.CostItems, currentInventory);
			isCostEnough = itemCosts.isEnough;
			buttonText = ((!isCostEnough) ? DolocConfig.StaticTexts.BuildingPanelMaterialNotEnough : DolocConfig.StaticTexts.PlatformPanelStartBuild);
			avgCostInfo = string.Format(DolocConfig.StaticTexts.PlatformPanelCostPrefix, itemCosts.costInfo);
			itemTiles = string.Join(",", itemCosts.costTitles.ToList());
		}
	}
}
