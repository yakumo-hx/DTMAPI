using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Recipe;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncEquipmentWorkbench : EquipmentFuncEquipment
{
	public const int __ID__ = 49508419;

	public string RecipeGroupName { get; private set; }

	public RecipeGroupInfo RecipeGroupName_Ref { get; private set; }

	public EquipmentFuncEquipmentWorkbench(JSONNode _json)
		: base(_json)
	{
		if (!_json["recipe_group_name"].IsString)
		{
			throw new SerializationException();
		}
		RecipeGroupName = _json["recipe_group_name"];
	}

	public EquipmentFuncEquipmentWorkbench(string recipe_group_name)
	{
		RecipeGroupName = recipe_group_name;
	}

	public static EquipmentFuncEquipmentWorkbench DeserializeEquipmentFuncEquipmentWorkbench(JSONNode _json)
	{
		return new EquipmentFuncEquipmentWorkbench(_json);
	}

	public override int GetTypeId()
	{
		return 49508419;
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
