using DolocTown.Config;

namespace DolocTown.UI;

public class CraftQuantityData : QuantitySubmitData
{
	public RecipeData recipeData;

	public CraftQuantityData(int maxCount, IRecipe recipe, LinearInventory[] currentInventory, IRecipeGroup recipeGroup = null, int initCount = 1)
		: base(maxCount, initCount)
	{
		title = DolocUtils.Format(DolocConfig.StaticTexts.RecipePanelTitle, DolocAPI.GetItemTitle(recipe.OutputItem.itemName));
		recipeData = new RecipeData(recipe, currentInventory, recipeGroup, hideSingleCount: false);
		base.notEmpty = recipeData.notEmpty;
	}
}
