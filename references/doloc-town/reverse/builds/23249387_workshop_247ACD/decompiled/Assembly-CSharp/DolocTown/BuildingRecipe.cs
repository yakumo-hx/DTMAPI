using DolocTown.Config.Building;
using Newtonsoft.Json;

namespace DolocTown;

public class BuildingRecipe : IRecipe
{
	public readonly BuildingInfo data;

	[JsonProperty]
	private string id => data.Id;

	public string RecipeId => data.Id;

	public string RecipeTitle => DolocAPI.GetItemTitle(data.Id);

	public CountItem[] InputItems => data.ItemCost;

	public int MoneyCost => data.MoneyCost;

	public RangedItem OutputItem => new RangedItem(data.Id, 1, 1);

	public int TechPoint => 0;

	public int CostTime => 0;

	public int Storage => -1;

	public bool isValid { get; }

	public BuildingRecipe(BuildingInfo data)
	{
		this.data = data;
		isValid = data != null;
	}
}
