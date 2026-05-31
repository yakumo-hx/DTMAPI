using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionDemolitionTool : ItemFunctionBase
{
	public const int __ID__ = 1510783506;

	public ItemFunctionDemolitionTool(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionDemolitionTool()
	{
	}

	public static ItemFunctionDemolitionTool DeserializeItemFunctionDemolitionTool(JSONNode _json)
	{
		return new ItemFunctionDemolitionTool(_json);
	}

	public override int GetTypeId()
	{
		return 1510783506;
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
