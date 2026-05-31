using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Recipe;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class ModRecipeGroupExtensionInfo : BeanBase
{
	public const int __ID__ = 136335218;

	public string Id { get; private set; }

	public RecipeGroupInfo Id_Ref { get; private set; }

	public string[] ExtraRecipes { get; private set; }

	public RecipeInfo[] ExtraRecipes_Ref { get; private set; }

	public ModRecipeGroupExtensionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["extra_recipes"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ExtraRecipes = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			ExtraRecipes[num++] = text;
		}
	}

	public ModRecipeGroupExtensionInfo(string id, string[] extra_recipes)
	{
		Id = id;
		ExtraRecipes = extra_recipes;
	}

	public static ModRecipeGroupExtensionInfo DeserializeModRecipeGroupExtensionInfo(JSONNode _json)
	{
		return new ModRecipeGroupExtensionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 136335218;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Recipe.TbRecipeGroup"] as TbRecipeGroup).GetOrDefault(Id);
		int num = ExtraRecipes.Length;
		TbRecipe tbRecipe = (TbRecipe)_tables["Recipe.TbRecipe"];
		ExtraRecipes_Ref = new RecipeInfo[num];
		for (int i = 0; i < num; i++)
		{
			ExtraRecipes_Ref[i] = tbRecipe.GetOrDefault(ExtraRecipes[i]);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ExtraRecipes:" + StringUtil.CollectionToString(ExtraRecipes) + ",}";
	}
}
