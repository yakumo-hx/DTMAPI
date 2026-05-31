using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Recipe;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DishGroup : IRecipeGroup
{
	public readonly DishGroupInfo proto;

	[JsonProperty]
	public string id => proto.Id;

	public string GroupId => proto.Id;

	public List<string> RecipeIds => proto.DishIds;

	public string Title => proto.Title;

	public float BaseTimeRatio => proto.TimeRatio;

	public float TimeAddition { get; set; }

	public bool IsFixedDuration => false;

	public int MaxCraftCount => 1;

	public bool isValid { get; }

	public string SwitchMainInfo => proto.SwitchMainInfo;

	public string SwitchSubInfo => proto.SwitchSubInfo;

	private RecipeManager recipeManager => DolocAPI.archiveHandle.farmData.recipeManager;

	[JsonConstructor]
	public DishGroup(string id)
		: this(DolocConfig.Tables.TbDishGroup.GetOrDefault(id ?? ""))
	{
	}

	public DishGroup(DishGroupInfo proto)
	{
		this.proto = proto;
		isValid = proto != null;
	}

	public bool CheckRecipeUnlock(string recipeId)
	{
		return recipeManager.CheckDishUnlocked(recipeId);
	}

	public IRecipe GetRecipe(string recipeId)
	{
		return null;
	}

	public bool TryGetRecipe(CountItem[] items, out IRecipe dish)
	{
		dish = null;
		foreach (DishInfo item in proto.DishIds_Ref)
		{
			if (item != null && item.Match(items))
			{
				dish = new Dish(item, items);
				return true;
			}
		}
		return false;
	}

	public IRecipe GetDefaultRecipe(CountItem[] items)
	{
		return new Dish(proto.DefaultDish_Ref, items);
	}
}
