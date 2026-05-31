using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Recipe;

public sealed class DismantleRecipeGroupInfo : BeanBase
{
	public const int __ID__ = -1738302306;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public float TimeRatio { get; private set; }

	public int MaxCraftCount { get; private set; }

	public List<string> RecipeIds { get; private set; }

	public List<DismantleRecipeInfo> RecipeIds_Ref { get; private set; }

	public DismantleRecipeGroupInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["time_ratio"].IsNumber)
		{
			throw new SerializationException();
		}
		TimeRatio = _json["time_ratio"];
		if (!_json["max_craft_count"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxCraftCount = _json["max_craft_count"];
		JSONNode jSONNode = _json["recipe_ids"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		RecipeIds = new List<string>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string item = child;
			RecipeIds.Add(item);
		}
	}

	public DismantleRecipeGroupInfo(string id, string title, float time_ratio, int max_craft_count, List<string> recipe_ids)
	{
		Id = id;
		Title = title;
		TimeRatio = time_ratio;
		MaxCraftCount = max_craft_count;
		RecipeIds = recipe_ids;
	}

	public static DismantleRecipeGroupInfo DeserializeDismantleRecipeGroupInfo(JSONNode _json)
	{
		return new DismantleRecipeGroupInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1738302306;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		TbDismantleRecipe tbDismantleRecipe = (TbDismantleRecipe)_tables["Recipe.TbDismantleRecipe"];
		RecipeIds_Ref = new List<DismantleRecipeInfo>();
		foreach (string recipeId in RecipeIds)
		{
			RecipeIds_Ref.Add(tbDismantleRecipe.GetOrDefault(recipeId));
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",TimeRatio:" + TimeRatio + ",MaxCraftCount:" + MaxCraftCount + ",RecipeIds:" + StringUtil.CollectionToString(RecipeIds) + ",}";
	}
}
