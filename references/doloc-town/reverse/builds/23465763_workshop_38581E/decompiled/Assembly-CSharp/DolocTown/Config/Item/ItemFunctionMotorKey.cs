using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionMotorKey : ItemFunctionBase
{
	public const int __ID__ = 171230928;

	public ItemFunctionMotorKey(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionMotorKey()
	{
	}

	public static ItemFunctionMotorKey DeserializeItemFunctionMotorKey(JSONNode _json)
	{
		return new ItemFunctionMotorKey(_json);
	}

	public override int GetTypeId()
	{
		return 171230928;
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
