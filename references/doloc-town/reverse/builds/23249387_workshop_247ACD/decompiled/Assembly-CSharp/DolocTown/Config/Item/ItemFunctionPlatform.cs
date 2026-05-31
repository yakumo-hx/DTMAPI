using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionPlatform : ItemFunctionBase
{
	public const int __ID__ = -2097475175;

	public ItemFunctionPlatform(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionPlatform()
	{
	}

	public static ItemFunctionPlatform DeserializeItemFunctionPlatform(JSONNode _json)
	{
		return new ItemFunctionPlatform(_json);
	}

	public override int GetTypeId()
	{
		return -2097475175;
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
