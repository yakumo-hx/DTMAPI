using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionFishRoe : ItemFunctionBase
{
	public const int __ID__ = 685975146;

	public ItemFunctionFishRoe(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionFishRoe()
	{
	}

	public static ItemFunctionFishRoe DeserializeItemFunctionFishRoe(JSONNode _json)
	{
		return new ItemFunctionFishRoe(_json);
	}

	public override int GetTypeId()
	{
		return 685975146;
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
