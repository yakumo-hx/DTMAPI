using DolocTown.Config.Store;
using Newtonsoft.Json;

namespace DolocTown;

public class ExchangeStoreRecipe : IRecipe
{
	public readonly ExchangeStoreItemData data;

	[JsonProperty]
	private string id => data.ItemId;

	public string RecipeId => data.ItemId;

	public string RecipeTitle => DolocAPI.GetItemTitle(data.ItemId);

	public CountItem[] InputItems => data.ItemCosts;

	public int MoneyCost
	{
		get
		{
			if (data.GoldCost >= 0)
			{
				return data.GoldCost;
			}
			return DolocAPI.GetItemBuyingPrice(data.ItemId);
		}
	}

	public RangedItem OutputItem => data.RangedItem;

	public int TechPoint => data.TechPoint;

	public int CostTime => 0;

	public int Storage { get; }

	public bool isValid { get; }

	public ExchangeStoreRecipe(ExchangeStoreItemData data, int storage)
	{
		this.data = data;
		Storage = storage;
		isValid = data != null;
	}
}
