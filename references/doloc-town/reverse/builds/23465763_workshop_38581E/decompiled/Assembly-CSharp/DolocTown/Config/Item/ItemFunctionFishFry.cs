using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionFishFry : ItemFunctionBase
{
	public const int __ID__ = 685963727;

	public ItemFunctionFishFry(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionFishFry()
	{
	}

	public static ItemFunctionFishFry DeserializeItemFunctionFishFry(JSONNode _json)
	{
		return new ItemFunctionFishFry(_json);
	}

	public override int GetTypeId()
	{
		return 685963727;
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
