using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Recipe;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct ItemRecipeData : IUIData
{
	public bool notEmpty { get; }

	public bool isUnlock { get; }

	public Sprite uiSprite { get; }

	public string outputCount { get; }

	public ItemSimpleData[] simpleData { get; }

	public bool hasSynthesizer { get; }

	public string synthesizerName { get; }

	public string time { get; }

	public string emptyHint { get; }

	public string targetItemName { get; }

	public string outputItemName { get; }

	public RecipeInfo recipeProto { get; }

	public ItemRecipeData(string itemName, string recipeName, string groupName)
	{
		this = default(ItemRecipeData);
		if (DolocAPI.QueryRecipeProto(recipeName, out var proto))
		{
			notEmpty = true;
			targetItemName = itemName;
			recipeProto = proto;
			isUnlock = DolocAPI.archiveHandle.IsRecipeUnlocked(recipeProto.Id);
			RangedItem outputItem = recipeProto.OutputItem;
			outputItemName = outputItem.itemName;
			DolocAPI.QueryItemProto(outputItemName, out var proto2);
			uiSprite = proto2.UiSpriteAsset.Asset;
			if (outputItem.minCount == outputItem.maxCount && outputItem.minCount == 1)
			{
				outputCount = string.Empty;
			}
			else
			{
				outputCount = ((outputItem.minCount == outputItem.maxCount) ? outputItem.minCount.ToString() : $"{outputItem.minCount}-{outputItem.maxCount}");
			}
			simpleData = recipeProto.InputItems.Select((CountItem x) => new ItemSimpleData(DolocAPI.GetItemSprite(x.itemName), x.itemCount)).ToArray();
			if (DolocConfig.Tables.TbRecipeGroup.DataMap.TryGetValue(groupName, out var value))
			{
				hasSynthesizer = true;
				synthesizerName = value.Title;
				time = ((recipeProto.CostTime == 0) ? DolocConfig.StaticTexts.CollectionPanelItemRecipeTime : DolocAPI.GetFormatTimeLengthByTU(Mathf.RoundToInt((float)recipeProto.CostTime * value.TimeRatio)));
			}
			emptyHint = DolocConfig.StaticTexts.UiOperationTalkUnknown;
		}
	}
}
