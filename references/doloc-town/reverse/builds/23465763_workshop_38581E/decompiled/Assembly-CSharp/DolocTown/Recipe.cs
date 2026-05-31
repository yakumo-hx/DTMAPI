using DolocTown.Config;
using DolocTown.Config.Recipe;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class Recipe : IRecipe
{
	[JsonProperty]
	private string id;

	public readonly RecipeInfo proto;

	public string RecipeId => proto?.Id ?? id;

	public string RecipeTitle => proto.RecipeTitle;

	public CountItem[] InputItems => proto.InputItems;

	public int MoneyCost => 0;

	public RangedItem OutputItem => proto.OutputItem;

	public int TechPoint => proto.TechPoint;

	public int CostTime => proto.CostTime;

	public int Storage => -1;

	public bool isValid { get; }

	[JsonConstructor]
	public Recipe(string id)
		: this(DolocConfig.Tables.TbRecipe.GetOrDefault(id ?? ""))
	{
		this.id = id;
	}

	public Recipe(RecipeInfo proto)
	{
		this.proto = proto;
		id = proto?.Id;
		isValid = proto != null;
	}
}
