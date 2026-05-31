using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public abstract class ItemFunctionAgentEquipment : ItemFunctionBase
{
	public ItemFunctionAgentEquipment(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionAgentEquipment()
	{
	}

	public static ItemFunctionAgentEquipment DeserializeItemFunctionAgentEquipment(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"ItemFunctionHat" => new ItemFunctionHat(_json), 
			"ItemFunctionHatShield" => new ItemFunctionHatShield(_json), 
			"ItemFunctionPassive" => new ItemFunctionPassive(_json), 
			"ItemFunctionHerbPackage" => new ItemFunctionHerbPackage(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ }";
	}
}
