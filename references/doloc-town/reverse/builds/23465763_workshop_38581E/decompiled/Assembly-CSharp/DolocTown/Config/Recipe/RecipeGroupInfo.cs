using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Recipe;

public sealed class RecipeGroupInfo : BeanBase
{
	public const int __ID__ = 1175805311;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string SwitchMainInfo { get; private set; }

	public string SwitchMainInfo_l10n_key { get; }

	public string SwitchSubInfo { get; private set; }

	public string SwitchSubInfo_l10n_key { get; }

	public float TimeRatio { get; private set; }

	public bool IsFixedDuration { get; private set; }

	public int MaxCraftCount { get; private set; }

	public List<string> RecipeIds { get; private set; }

	public List<RecipeInfo> RecipeIds_Ref { get; private set; }

	public RecipeGroupInfo(JSONNode _json)
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
		if (!_json["switch_main_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SwitchMainInfo_l10n_key = _json["switch_main_info"]["key"];
		if (!_json["switch_main_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SwitchMainInfo = _json["switch_main_info"]["text"];
		if (!_json["switch_sub_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SwitchSubInfo_l10n_key = _json["switch_sub_info"]["key"];
		if (!_json["switch_sub_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SwitchSubInfo = _json["switch_sub_info"]["text"];
		if (!_json["time_ratio"].IsNumber)
		{
			throw new SerializationException();
		}
		TimeRatio = _json["time_ratio"];
		if (!_json["is_fixed_duration"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsFixedDuration = _json["is_fixed_duration"];
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

	public RecipeGroupInfo(string id, string title, string switch_main_info, string switch_sub_info, float time_ratio, bool is_fixed_duration, int max_craft_count, List<string> recipe_ids)
	{
		Id = id;
		Title = title;
		SwitchMainInfo = switch_main_info;
		SwitchSubInfo = switch_sub_info;
		TimeRatio = time_ratio;
		IsFixedDuration = is_fixed_duration;
		MaxCraftCount = max_craft_count;
		RecipeIds = recipe_ids;
	}

	public static RecipeGroupInfo DeserializeRecipeGroupInfo(JSONNode _json)
	{
		return new RecipeGroupInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1175805311;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		TbRecipe tbRecipe = (TbRecipe)_tables["Recipe.TbRecipe"];
		RecipeIds_Ref = new List<RecipeInfo>();
		foreach (string recipeId in RecipeIds)
		{
			RecipeIds_Ref.Add(tbRecipe.GetOrDefault(recipeId));
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		SwitchMainInfo = translator(SwitchMainInfo_l10n_key, SwitchMainInfo);
		SwitchSubInfo = translator(SwitchSubInfo_l10n_key, SwitchSubInfo);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",SwitchMainInfo:" + SwitchMainInfo + ",SwitchSubInfo:" + SwitchSubInfo + ",TimeRatio:" + TimeRatio + ",IsFixedDuration:" + IsFixedDuration + ",MaxCraftCount:" + MaxCraftCount + ",RecipeIds:" + StringUtil.CollectionToString(RecipeIds) + ",}";
	}
}
