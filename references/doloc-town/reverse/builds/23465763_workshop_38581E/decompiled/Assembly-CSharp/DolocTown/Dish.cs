using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Recipe;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class Dish : IRecipe
{
	public readonly DishInfo proto;

	private readonly RangedItem outputItem;

	private readonly RangedItem maxQualityItem;

	[JsonProperty]
	private readonly CountItem[] inputItems;

	[JsonProperty]
	private string id => proto.Id;

	public string RecipeId => proto.Id;

	public string RecipeTitle
	{
		get
		{
			if (!proto.Title.IsNullOrEmpty())
			{
				return proto.Title;
			}
			return DolocAPI.GetItemTitle(OutputItemName);
		}
	}

	public CountItem[] InputItems => inputItems;

	public int MoneyCost => 0;

	public RangedItem OutputItem => outputItem;

	public int TechPoint => proto.TechPoint;

	public int CostTime => proto.CostTime;

	public int Storage => -1;

	public bool isValid { get; }

	public string OutputItemName => outputItem.itemName;

	public RangedItem MaxQualityItem => maxQualityItem;

	[JsonConstructor]
	public Dish(string id, CountItem[] inputItems, int priceThreshold)
		: this(DolocConfig.Tables.TbDish.GetOrDefault(id ?? ""), inputItems)
	{
	}

	public Dish(DishInfo proto, CountItem[] inputItems)
	{
		this.proto = proto;
		this.inputItems = inputItems.Where((CountItem x) => x.isValid).ToArray();
		isValid = proto != null && !this.inputItems.IsNullOrEmpty();
		if (!isValid)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < inputItems.Length; i++)
		{
			if (DolocAPI.QueryItemProto(inputItems[i].itemName, out var itemInfo))
			{
				num += itemInfo.SellingPrice;
			}
		}
		outputItem = proto?.GetItemByPriceThreshold(num) ?? default(RangedItem);
		maxQualityItem = proto?.GetMaxQualityItem() ?? default(RangedItem);
	}
}
