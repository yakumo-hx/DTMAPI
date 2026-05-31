using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public interface IRecipeGroup
{
	string GroupId { get; }

	List<string> RecipeIds { get; }

	string Title { get; }

	float BaseTimeRatio { get; }

	float TimeAddition { get; set; }

	float TimeRatio => Mathf.Max(0f, BaseTimeRatio * (1f + TimeAddition));

	bool IsFixedDuration { get; }

	int MaxCraftCount { get; }

	bool isValid { get; }

	string SwitchMainInfo { get; }

	string SwitchSubInfo { get; }

	int GetRecipeTime(IRecipe recipe, int scale = 1)
	{
		if (recipe == null)
		{
			return 0;
		}
		int num = Mathf.RoundToInt(TimeRatio * (float)recipe.CostTime);
		if (IsFixedDuration)
		{
			return num;
		}
		return num * scale;
	}

	bool CheckRecipeUnlock(string recipeId);

	IRecipe[] GetAllRecipes(bool includeLocked)
	{
		List<IRecipe> list = new List<IRecipe>();
		foreach (string recipeId in RecipeIds)
		{
			if (includeLocked || CheckRecipeUnlock(recipeId))
			{
				IRecipe recipe = GetRecipe(recipeId);
				if (recipe != null && recipe.isValid)
				{
					list.Add(recipe);
				}
			}
		}
		return list.ToArray();
	}

	IRecipe GetRecipe(string recipeId);

	IRecipe[] GetAllUnlockRecipes()
	{
		return GetAllRecipes(includeLocked: false);
	}

	bool TryGetRecipe(CountItem[] items, out IRecipe recipe);
}
