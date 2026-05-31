using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Recipe;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class RecipeGroup : IRecipeGroup
{
	public readonly RecipeGroupInfo proto;

	[JsonProperty]
	public string id => proto.Id;

	public string GroupId => proto?.Id;

	public List<string> RecipeIds => proto.RecipeIds;

	public string Title => proto?.Title;

	public float BaseTimeRatio => proto.TimeRatio;

	public float TimeAddition { get; set; }

	public bool IsFixedDuration => proto.IsFixedDuration;

	public int MaxCraftCount => proto.MaxCraftCount;

	public string SwitchMainInfo => proto.SwitchMainInfo;

	public string SwitchSubInfo => proto.SwitchSubInfo;

	private RecipeManager recipeManager => DolocAPI.archiveHandle.farmData.recipeManager;

	public bool isValid { get; }

	[JsonConstructor]
	public RecipeGroup(string id)
		: this(DolocConfig.Tables.TbRecipeGroup.GetOrDefault(id ?? ""))
	{
	}

	public RecipeGroup(RecipeGroupInfo proto)
	{
		this.proto = proto;
		isValid = proto != null;
	}

	public bool CheckRecipeUnlock(string recipeId)
	{
		return recipeManager.CheckRecipeUnlocked(recipeId);
	}

	public IRecipe GetRecipe(string recipeId)
	{
		return new Recipe(recipeId);
	}

	public List<Recipe> GetAllRecipes(bool includeLocked)
	{
		List<Recipe> list = new List<Recipe>();
		foreach (string recipeId in proto.RecipeIds)
		{
			if (includeLocked || CheckRecipeUnlock(recipeId))
			{
				list.Add(new Recipe(recipeId));
			}
		}
		return list;
	}

	public bool TryGetRecipe(CountItem[] items, out IRecipe recipe)
	{
		recipe = null;
		foreach (RecipeInfo item in proto.RecipeIds_Ref)
		{
			if (item != null && item.Match(items))
			{
				recipe = new Recipe(item);
				return true;
			}
		}
		return false;
	}
}
