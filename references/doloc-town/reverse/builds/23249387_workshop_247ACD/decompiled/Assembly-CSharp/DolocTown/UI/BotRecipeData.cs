using System.Linq;
using UnityEngine;

namespace DolocTown.UI;

public struct BotRecipeData : IUIData
{
	public bool notEmpty { get; }

	public string costInfo { get; }

	public Sprite icon { get; }

	public string title { get; }

	public bool used { get; }

	public bool isLastLine { get; }

	public BotRecipeData(string recipeId, string usedRecipeId, int index, int count)
	{
		this = default(BotRecipeData);
		if (!string.IsNullOrEmpty(recipeId))
		{
			notEmpty = true;
			DolocAPI.QueryRecipe(recipeId, out var recipe);
			used = recipeId.Equals(usedRecipeId);
			isLastLine = index >= (count - 1) / 2 * 2;
			title = recipe.RecipeTitle;
			icon = DolocAPI.GetItemSprite(recipe.OutputItem.itemName);
			CountItem[] inputItems = recipe.InputItems;
			int[] counts = inputItems.Select((CountItem x) => x.itemCount).ToArray();
			string[] titles = inputItems.Select((CountItem x) => DolocAPI.GetItemTitle(x.itemName)).ToArray();
			costInfo = DolocUtils.GenerateCountItemInfo(titles, counts);
		}
	}
}
