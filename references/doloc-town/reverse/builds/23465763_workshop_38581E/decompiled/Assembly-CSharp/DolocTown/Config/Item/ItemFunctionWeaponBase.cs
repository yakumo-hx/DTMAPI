using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public abstract class ItemFunctionWeaponBase : ItemFunctionBase
{
	public ItemFunctionWeaponBase(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionWeaponBase()
	{
	}

	public static ItemFunctionWeaponBase DeserializeItemFunctionWeaponBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"ItemFunctionDroneStructure" => new ItemFunctionDroneStructure(_json), 
			"ItemFunctionDroneWeapon" => new ItemFunctionDroneWeapon(_json), 
			"ItemFunctionDroneEngine" => new ItemFunctionDroneEngine(_json), 
			"ItemFunctionDroneChip" => new ItemFunctionDroneChip(_json), 
			"ItemFunctionDroneAssist" => new ItemFunctionDroneAssist(_json), 
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
