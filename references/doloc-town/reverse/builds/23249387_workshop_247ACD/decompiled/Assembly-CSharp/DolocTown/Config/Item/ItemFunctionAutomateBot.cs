using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionAutomateBot : ItemFunctionBase
{
	public const int __ID__ = -1870458611;

	public ItemFunctionAutomateBot(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionAutomateBot()
	{
	}

	public static ItemFunctionAutomateBot DeserializeItemFunctionAutomateBot(JSONNode _json)
	{
		return new ItemFunctionAutomateBot(_json);
	}

	public override int GetTypeId()
	{
		return -1870458611;
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
