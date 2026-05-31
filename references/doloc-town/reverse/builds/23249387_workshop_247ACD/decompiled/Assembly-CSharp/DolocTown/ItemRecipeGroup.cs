using System.Collections.Generic;
using DolocTown.Config.Item;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemRecipeGroup : Item
{
	private RecipeGroup _recipeGroup;

	private ItemFunctionRecipeGroup func => base.proto.Function as ItemFunctionRecipeGroup;

	public RecipeGroup recipeGroup => _recipeGroup ?? (_recipeGroup = new RecipeGroup(func.RecipeGroupId_Ref));

	public ItemRecipeGroup(ItemInfo item, int count)
		: base(item, count)
	{
	}

	[JsonConstructor]
	protected ItemRecipeGroup(string itemName, int itemCount)
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
		if (recipeGroup == null)
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (string recipeId in recipeGroup.RecipeIds)
		{
			Recipe recipe = new Recipe(recipeId);
			if (recipe.isValid)
			{
				list.Add(recipe.RecipeTitle);
				DolocAPI.CashReward(new RewardProto(RewardType.RECIPE_UNLOCK, recipeId));
			}
		}
		DolocAPI.Broadcast(OperationEventType.USE_ITEM);
		CostSelf();
		CommandDefines.BackupString(list.HandleJoinString());
		DolocAPI.StartDialogueNode(func.DialogueNode);
	}
}
