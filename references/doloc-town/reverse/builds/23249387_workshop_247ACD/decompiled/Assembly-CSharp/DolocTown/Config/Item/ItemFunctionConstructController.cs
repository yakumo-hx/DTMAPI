using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionConstructController : ItemFunctionBase
{
	public const int __ID__ = -49527315;

	public ItemFunctionConstructController(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionConstructController()
	{
	}

	public static ItemFunctionConstructController DeserializeItemFunctionConstructController(JSONNode _json)
	{
		return new ItemFunctionConstructController(_json);
	}

	public override int GetTypeId()
	{
		return -49527315;
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
