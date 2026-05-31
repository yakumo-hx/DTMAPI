using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Recipe;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncWorkbench : EquipmentFuncEquipment
{
	public const int __ID__ = 1018010893;

	public string RecipeGroupName { get; private set; }

	public RecipeGroupInfo RecipeGroupName_Ref { get; private set; }

	public EquipmentFuncWorkbench(JSONNode _json)
		: base(_json)
	{
		if (!_json["recipe_group_name"].IsString)
		{
			throw new SerializationException();
		}
		RecipeGroupName = _json["recipe_group_name"];
	}

	public EquipmentFuncWorkbench(string recipe_group_name)
	{
		RecipeGroupName = recipe_group_name;
	}

	public static EquipmentFuncWorkbench DeserializeEquipmentFuncWorkbench(JSONNode _json)
	{
		return new EquipmentFuncWorkbench(_json);
	}

	public override int GetTypeId()
	{
		return 1018010893;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		RecipeGroupName_Ref = (_tables["Recipe.TbRecipeGroup"] as TbRecipeGroup).GetOrDefault(RecipeGroupName);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ RecipeGroupName:" + RecipeGroupName + ",}";
	}
}
