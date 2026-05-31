using DolocTown.Config.Item;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemRecipe : Item
{
	private Recipe _recipe;

	private ItemFunctionRecipe func => base.proto.Function as ItemFunctionRecipe;

	public Recipe recipe => _recipe ?? (_recipe = new Recipe(func.RecipeId_Ref));

	public override string title
	{
		get
		{
			if (!base.title.IsNullOrEmpty() || recipe == null)
			{
				return base.title;
			}
			return DolocUtils.Format(recipe.proto.RecipeSubType_Ref.ItemTitleFormat, recipe.RecipeTitle);
		}
	}

	public override string description
	{
		get
		{
			if (!base.description.IsNullOrEmpty() || recipe == null)
			{
				return base.description;
			}
			return base.proto.DescriptionPrefix + DolocUtils.Format(recipe.proto.RecipeSubType_Ref.ItemDescFormat, recipe.RecipeTitle);
		}
	}

	public ItemRecipe(ItemInfo item, int count)
		: base(item, count)
	{
	}

	[JsonConstructor]
	protected ItemRecipe(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		Use();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Use();
	}

	private void Use()
	{
		if (recipe != null)
		{
			if (DolocAPI.CashReward(new RewardProto(RewardType.RECIPE_UNLOCK, func.RecipeId)))
			{
				DolocAPI.Broadcast(OperationEventType.USE_ITEM);
			}
			CostSelf();
		}
	}
}
