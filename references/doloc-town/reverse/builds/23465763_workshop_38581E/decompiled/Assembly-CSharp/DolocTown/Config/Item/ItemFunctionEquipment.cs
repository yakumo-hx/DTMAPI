using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionEquipment : ItemFunctionBase
{
	public const int __ID__ = -1801494232;

	public ItemFunctionEquipment(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionEquipment()
	{
	}

	public static ItemFunctionEquipment DeserializeItemFunctionEquipment(JSONNode _json)
	{
		return new ItemFunctionEquipment(_json);
	}

	public override int GetTypeId()
	{
		return -1801494232;
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
