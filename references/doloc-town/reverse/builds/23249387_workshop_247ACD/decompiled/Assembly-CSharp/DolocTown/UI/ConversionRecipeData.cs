using System;
using DolocTown.Config;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct ConversionRecipeData : IUIData
{
	private int outputMinCount;

	private int outputMaxCount;

	public bool notEmpty { get; }

	public ItemData outputItemData { get; }

	public string outputItemTitle { get; }

	public Sprite outputItemSprite { get; }

	public string outputCount { get; }

	public int timeUnit { get; }

	public string costTimeInfo { get; }

	public string existItemInfo { get; }

	public ConversionRecipeData(IRecipe recipe, IRecipeGroup recipeGroup, int scale, Func<IRecipeGroup, IRecipe, int, string> restTimeInfoGetter, Func<Item, Item> previewItemHandler = null)
	{
		this = default(ConversionRecipeData);
		if (recipe == null || scale == 0)
		{
			return;
		}
		RangedItem outputItem = recipe.OutputItem;
		Item item = DolocAPI.GenerateItem(outputItem.itemName);
		if (previewItemHandler != null)
		{
			item = previewItemHandler(item);
		}
		if (item != null)
		{
			notEmpty = true;
			bool flag = DolocAPI.archiveHandle.IsRecipeUnlocked(recipe.RecipeId);
			outputItemData = (flag ? new ItemData(item) : default(ItemData));
			outputItemTitle = (flag ? item.title : "???");
			outputItemSprite = (flag ? item.uiSprite : LocSprites.UI_ITEMICON_DEFAULT);
			outputMinCount = (flag ? outputItem.minCount : 0);
			outputMaxCount = (flag ? outputItem.maxCount : 0);
			outputCount = (flag ? GetTargetCount(scale) : "");
			int existItemCount = DolocAPI.GetExistItemCount(outputItem.itemName);
			existItemInfo = DolocUtils.Format(DolocConfig.StaticTexts.RecipePanelExistItems, flag ? existItemCount.ToString() : "???");
			timeUnit = recipeGroup?.GetRecipeTime(recipe) ?? 0;
			if (!flag)
			{
				costTimeInfo = DolocUtils.Format(DolocConfig.StaticTexts.RecipePanelTimeInfo, "???");
			}
			else
			{
				costTimeInfo = restTimeInfoGetter?.Invoke(recipeGroup, recipe, scale) ?? GetFormatTimeString(scale);
			}
		}
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
