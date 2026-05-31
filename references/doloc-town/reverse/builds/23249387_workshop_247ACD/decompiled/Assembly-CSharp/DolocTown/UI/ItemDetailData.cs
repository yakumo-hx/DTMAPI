using System.Collections.Generic;
using System.Linq;
using System.Text;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using DolocTown.Config.Recipe;
using DolocTown.Config.UI;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct ItemDetailData : IUIData
{
	public bool notEmpty { get; }

	public bool obtained { get; }

	public string itemName { get; }

	public Sprite icon { get; }

	public string name { get; }

	public string source { get; }

	public string type { get; }

	public string desc { get; }

	public string geneInfo { get; }

	public string price { get; }

	public ItemRecipeData[] recipesData { get; }

	public ItemDetailData(string itemName)
	{
		this = default(ItemDetailData);
		Item item = DolocAPI.GenerateItem(itemName);
		if (item == null)
		{
			return;
		}
		notEmpty = true;
		if (!DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Item, item.name, out var record))
		{
			return;
		}
		obtained = record.isUnlock;
		this.itemName = itemName;
		icon = item.uiSprite;
		name = item.title;
		string arg = item.proto.Source_Ref.Select((ItemSourceInfo info) => info.Title).HandleJoinString();
		source = string.Format(DolocConfig.StaticTexts.CollectionPanelItemSource, arg);
		type = item.subType.Title;
		desc = item.description;
		if (item is ItemGeneCapsule itemGeneCapsule)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (itemGeneCapsule.HasGene)
			{
				CropGeneInfo[] genes = itemGeneCapsule.GeneGroup.Genes;
				foreach (CropGeneInfo cropGeneInfo in genes)
				{
					stringBuilder.AppendLine(DolocUtils.Format(DolocConfig.StaticTexts.UiTextGeneDescription, cropGeneInfo.Description).Colored(DolocUiColor.EYECATCHCOLOR_CYAN));
				}
			}
			geneInfo = stringBuilder.ToString().TrimEnd();
		}
		int itemSellingPrice = DolocAPI.GetItemSellingPrice(itemName);
		price = (item.salable ? (DolocConfig.StaticTexts.ItemTipBasicsPrice.Colored(DolocUiColor.TEXTCOLOR_STD) + itemSellingPrice.ToString().Colored(DolocUiColor.EYECATCHCOLOR_CYAN) + DolocConfig.StaticTexts.ItemTipMoneyUnit.Colored(DolocUiColor.TEXTCOLOR_STD)) : DolocConfig.StaticTexts.StoreItemNotSaleableComment.Colored(DolocUiColor.SLIENTCOLOR_RED));
		List<ItemRecipeData> list = new List<ItemRecipeData>();
		foreach (string relatedRecipesDatum in GetRelatedRecipesData())
		{
			list.AddRange(GetAllRecipeGroupData(relatedRecipesDatum));
		}
		recipesData = list.OrderBy((ItemRecipeData x) => (!(x.outputItemName == itemName)) ? 1 : 0).ToArray();
	}

	private IEnumerable<string> GetRelatedRecipesData()
	{
		foreach (RecipeInfo recipeProto in DolocConfig.Tables.TbRecipe.DataList)
		{
			if (!recipeProto.ShowInHandbook)
			{
				continue;
			}
			if (recipeProto.OutputItem.itemName == itemName)
			{
				yield return recipeProto.Id;
			}
			CountItem[] inputItems = recipeProto.InputItems;
			for (int i = 0; i < inputItems.Length; i++)
			{
				if (inputItems[i].itemName == itemName)
				{
					yield return recipeProto.Id;
				}
			}
		}
	}

	private List<ItemRecipeData> GetAllRecipeGroupData(string recipe)
	{
		List<ItemRecipeData> list = new List<ItemRecipeData>();
		foreach (RecipeGroupInfo data in DolocConfig.Tables.TbRecipeGroup.DataList)
		{
			if (data.RecipeIds.Contains(recipe))
			{
				list.Add(new ItemRecipeData(itemName, recipe, data.Id));
			}
		}
		if (list.Count == 0)
		{
			list.Add(new ItemRecipeData(itemName, recipe, string.Empty));
		}
		return list;
	}
}
