using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionDroneStructure : ItemFunctionWeaponBase
{
	public const int __ID__ = 1796722145;

	public ItemFunctionDroneStructure(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionDroneStructure()
	{
	}

	public static ItemFunctionDroneStructure DeserializeItemFunctionDroneStructure(JSONNode _json)
	{
		return new ItemFunctionDroneStructure(_json);
	}

	public override int GetTypeId()
	{
		return 1796722145;
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
