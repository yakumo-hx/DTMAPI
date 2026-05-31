using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Recipe;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionRecipeGroup : ItemFunctionBase
{
	public const int __ID__ = -1973684565;

	public string RecipeGroupId { get; private set; }

	public RecipeGroupInfo RecipeGroupId_Ref { get; private set; }

	public string DialogueNode { get; private set; }

	public ItemFunctionRecipeGroup(JSONNode _json)
		: base(_json)
	{
		if (!_json["recipe_group_id"].IsString)
		{
			throw new SerializationException();
		}
		RecipeGroupId = _json["recipe_group_id"];
		if (!_json["dialogue_node"].IsString)
		{
			throw new SerializationException();
		}
		DialogueNode = _json["dialogue_node"];
	}

	public ItemFunctionRecipeGroup(string recipe_group_id, string dialogue_node)
	{
		RecipeGroupId = recipe_group_id;
		DialogueNode = dialogue_node;
	}

	public static ItemFunctionRecipeGroup DeserializeItemFunctionRecipeGroup(JSONNode _json)
	{
		return new ItemFunctionRecipeGroup(_json);
	}

	public override int GetTypeId()
	{
		return -1973684565;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		RecipeGroupId_Ref = (_tables["Recipe.TbRecipeGroup"] as TbRecipeGroup).GetOrDefault(RecipeGroupId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ RecipeGroupId:" + RecipeGroupId + ",DialogueNode:" + DialogueNode + ",}";
	}
}
