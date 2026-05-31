using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Recipe;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

public class AutomateParamProcessing : AutomateParam
{
	[JsonProperty]
	private HashSet<string> recipes = new HashSet<string>();

	[JsonProperty]
	private bool filterContainerSkin;

	[JsonProperty]
	private HashSet<int> containerSkinIdxSet;

	[DebugInfo("配方子类", Color = "#ff7f4f", AllowEdit = true)]
	public RecipeSubTypeInfo SubRecipe;

	[JsonProperty]
	private string subRecipe => SubRecipe.Id;

	[DebugInfo("配方大类", Color = "#ff7f4f", AllowEdit = true)]
	public RecipeMainTypeInfo MainRecipe => SubRecipe.MainType_Ref;

	[DebugInfo("目标配方列表", Color = "#ff7f4f", AllowEdit = true)]
	public HashSet<string> Recipes => recipes;

	[DebugInfo("是否过滤材料容器色彩", Color = "#ff7f4f", AllowEdit = true)]
	public bool FilterContainerSkin
	{
		get
		{
			return filterContainerSkin;
		}
		set
		{
			filterContainerSkin = value;
		}
	}

	[DebugInfo("材料容器色彩集合", Color = "#ff7f4f", AllowEdit = true)]
	public HashSet<int> ContainerSkinIdxSet => containerSkinIdxSet;

	public AutomateParamProcessing(AutomateBot bot)
		: base(bot)
	{
	}

	[JsonConstructor]
	public AutomateParamProcessing(string subRecipe, HashSet<string> recipes, bool filterContainerSkin, HashSet<int> containerSkinIdxSet)
	{
		if (subRecipe.IsNullOrEmpty() || !DolocConfig.Tables.TbRecipeSubType.DataMap.TryGetValue(subRecipe, out var value))
		{
			SubRecipe = DolocConfig.Tables.TbRecipeSubType.DataList.First();
		}
		else
		{
			SubRecipe = value;
		}
		this.recipes = recipes ?? new HashSet<string>();
		this.filterContainerSkin = filterContainerSkin;
		this.containerSkinIdxSet = containerSkinIdxSet ?? new HashSet<int>();
	}

	public override void LoadDefault()
	{
		SubRecipe = DolocConfig.Tables.TbRecipeSubType.DataList.First();
		if (recipes == null)
		{
			recipes = new HashSet<string>();
		}
		string firstRecipe = GetFirstRecipe(SubRecipe);
		if (firstRecipe != null)
		{
			recipes.Add(firstRecipe);
		}
		filterContainerSkin = false;
		if (containerSkinIdxSet == null)
		{
			containerSkinIdxSet = new HashSet<int>();
		}
		containerSkinIdxSet.Add(0);
	}

	public bool FilterContainer(Case container)
	{
		if (container != null)
		{
			if (filterContainerSkin)
			{
				return containerSkinIdxSet.Contains(container.skinIndex);
			}
			return true;
		}
		return false;
	}

	private string GetFirstRecipe(RecipeSubTypeInfo subType)
	{
		return DolocConfig.Tables.TbRecipe.DataMap.Values.FirstOrDefault((RecipeInfo r) => r.RecipeSubType == subType.Id)?.Id;
	}

	private IEnumerable<string> GetAllRecipes(RecipeSubTypeInfo subType)
	{
		return from r in DolocConfig.Tables.TbRecipe.DataMap.Values
			where r.RecipeSubType == subType.Id
			select r.Id;
	}

	[DebugButton("重置配方列表")]
	private void DebugResetRecipes()
	{
		recipes.Clear();
		foreach (string allRecipe in GetAllRecipes(SubRecipe))
		{
			recipes.Add(allRecipe);
		}
	}
}
