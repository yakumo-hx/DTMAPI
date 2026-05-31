using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionBuilding : ItemFunctionBase
{
	public const int __ID__ = -1107837990;

	public ItemFunctionBuilding(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionBuilding()
	{
	}

	public static ItemFunctionBuilding DeserializeItemFunctionBuilding(JSONNode _json)
	{
		return new ItemFunctionBuilding(_json);
	}

	public override int GetTypeId()
	{
		return -1107837990;
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
