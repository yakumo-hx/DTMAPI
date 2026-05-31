using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Recipe;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class RecipeManager
{
	[JsonProperty]
	private HashSet<string> unlockedRecipes;

	[JsonProperty]
	private HashSet<string> unlockedDishes;

	public HashSet<string> UnlockedRecipes => unlockedRecipes;

	private TbRecipeGroup recipeGroupTable => DolocConfig.Tables.TbRecipeGroup;

	private TbRecipe recipeTable => DolocConfig.Tables.TbRecipe;

	private TbDishGroup dishGroupTable => DolocConfig.Tables.TbDishGroup;

	private TbDish dishTable => DolocConfig.Tables.TbDish;

	[JsonConstructor]
	public RecipeManager(HashSet<string> unlockedGlobal = null, HashSet<string> unlockedDishes = null)
	{
		unlockedRecipes = unlockedGlobal ?? new HashSet<string>();
		this.unlockedDishes = unlockedDishes ?? new HashSet<string>();
	}

	public bool UnlockRecipe(string recipeId, bool includeDish)
	{
		bool flag = false;
		if (includeDish)
		{
			DishInfo orDefault = dishTable.GetOrDefault(recipeId);
			if (orDefault != null && orDefault.Unlockable)
			{
				flag |= unlockedDishes.Add(recipeId);
			}
		}
		if (recipeTable.GetOrDefault(recipeId) != null)
		{
			flag |= unlockedRecipes.Add(recipeId);
		}
		return flag;
	}

	public bool LockRecipe(string recipeId, bool includeDish)
	{
		bool flag = false;
		if (includeDish)
		{
			flag |= unlockedDishes.Remove(recipeId);
		}
		return flag | unlockedRecipes.Remove(recipeId);
	}

	public bool CheckRecipeUnlocked(string recipeId, bool includeDish = false)
	{
		RecipeInfo orDefault = recipeTable.GetOrDefault(recipeId);
		bool flag = (orDefault != null && orDefault.DefaultUnlock) || unlockedRecipes.Contains(recipeId);
		if (includeDish)
		{
			flag |= unlockedDishes.Contains(recipeId);
		}
		return flag;
	}

	public bool UnlockDish(string dishId)
	{
		if (dishTable.GetOrDefault(dishId) == null)
		{
			return false;
		}
		return unlockedDishes.Add(dishId);
	}

	public bool CheckDishUnlocked(string dishId)
	{
		return unlockedDishes.Contains(dishId);
	}
}
