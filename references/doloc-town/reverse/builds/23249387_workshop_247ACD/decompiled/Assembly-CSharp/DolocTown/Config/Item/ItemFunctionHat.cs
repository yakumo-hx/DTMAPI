using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionHat : ItemFunctionHatBase
{
	public const int __ID__ = -197375563;

	public ItemFunctionHat(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionHat(string hat_id)
		: base(hat_id)
	{
	}

	public static ItemFunctionHat DeserializeItemFunctionHat(JSONNode _json)
	{
		return new ItemFunctionHat(_json);
	}

	public override int GetTypeId()
	{
		return -197375563;
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
		return "{ HatId:" + base.HatId + ",}";
	}
}
