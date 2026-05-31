using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionDoorplate : ItemFunctionBase
{
	public const int __ID__ = 42880962;

	public ItemFunctionDoorplate(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionDoorplate()
	{
	}

	public static ItemFunctionDoorplate DeserializeItemFunctionDoorplate(JSONNode _json)
	{
		return new ItemFunctionDoorplate(_json);
	}

	public override int GetTypeId()
	{
		return 42880962;
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
