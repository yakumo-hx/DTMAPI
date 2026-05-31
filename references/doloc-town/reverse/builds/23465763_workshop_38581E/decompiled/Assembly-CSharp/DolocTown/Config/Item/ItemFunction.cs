using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunction : ItemFunctionBase
{
	public const int __ID__ = 849586950;

	public ItemFunction(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunction()
	{
	}

	public static ItemFunction DeserializeItemFunction(JSONNode _json)
	{
		return new ItemFunction(_json);
	}

	public override int GetTypeId()
	{
		return 849586950;
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
