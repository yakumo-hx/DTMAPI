using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.Config.Recipe;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class RewardRecipe : Reward
{
	[JsonProperty]
	public readonly string recipeName;

	public override Sprite RewardIcon => LocSprites.UI_ICON_RECIPE;

	public override int Count => 1;

	public override string RewardBriefInfo
	{
		get
		{
			Recipe recipe;
			string arg = (DolocAPI.QueryRecipe(recipeName, out recipe) ? DolocAPI.GetItemTitle(recipe.OutputItem.itemName) : recipeName);
			return DolocUtils.Format(DolocConfig.StaticTexts.RewardInfoRecipeUnlock, arg, GetFirstRecipeGroupTitle(recipeName));
		}
	}

	public override string RewardInfo => RewardBriefInfo;

	[JsonConstructor]
	public RewardRecipe(string recipeName)
		: base(RewardType.RECIPE_UNLOCK)
	{
		this.recipeName = recipeName;
	}

	public override bool CheckValid()
	{
		Recipe recipe;
		if (!string.IsNullOrEmpty(recipeName))
		{
			return DolocAPI.QueryRecipe(recipeName, out recipe);
		}
		return false;
	}

	public override bool CashReward()
	{
		if (!DolocAPI.archiveHandle.UnlockRecipe(recipeName))
		{
			return false;
		}
		Recipe recipe;
		string arg = (DolocAPI.QueryRecipe(recipeName, out recipe) ? DolocAPI.GetItemTitle(recipe.OutputItem.itemName) : recipeName);
		DolocAPI.ShowMessageBoxNodeComplete(DolocUtils.Format(DolocConfig.StaticTexts.RewardInfoRecipeUnlockHint, arg, GetFirstRecipeGroupTitle(recipeName)));
		return true;
	}

	public override bool TryCombine(Reward other, out Reward combinedReward)
	{
		if (other is RewardRecipe rewardRecipe && rewardRecipe.recipeName == recipeName)
		{
			combinedReward = new RewardRecipe(recipeName);
			return true;
		}
		combinedReward = null;
		return false;
	}

	private string GetFirstRecipeGroupTitle(string recipe)
	{
		foreach (RecipeGroupInfo data in DolocConfig.Tables.TbRecipeGroup.DataList)
		{
			if (data.RecipeIds.Contains(recipe))
			{
				return data.Title;
			}
		}
		return string.Empty;
	}
}
