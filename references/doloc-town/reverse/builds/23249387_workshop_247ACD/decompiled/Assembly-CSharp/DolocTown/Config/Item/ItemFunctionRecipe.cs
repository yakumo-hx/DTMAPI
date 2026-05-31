using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Recipe;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionRecipe : ItemFunctionBase
{
	public const int __ID__ = 84414516;

	public string RecipeId { get; private set; }

	public RecipeInfo RecipeId_Ref { get; private set; }

	public ItemFunctionRecipe(JSONNode _json)
		: base(_json)
	{
		if (!_json["recipe_id"].IsString)
		{
			throw new SerializationException();
		}
		RecipeId = _json["recipe_id"];
	}

	public ItemFunctionRecipe(string recipe_id)
	{
		RecipeId = recipe_id;
	}

	public static ItemFunctionRecipe DeserializeItemFunctionRecipe(JSONNode _json)
	{
		return new ItemFunctionRecipe(_json);
	}

	public override int GetTypeId()
	{
		return 84414516;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		RecipeId_Ref = (_tables["Recipe.TbRecipe"] as TbRecipe).GetOrDefault(RecipeId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ RecipeId:" + RecipeId + ",}";
	}
}
