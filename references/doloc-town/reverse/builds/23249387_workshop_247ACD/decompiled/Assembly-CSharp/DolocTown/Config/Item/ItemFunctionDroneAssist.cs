using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionDroneAssist : ItemFunctionWeaponBase
{
	public const int __ID__ = 1195993275;

	public ItemFunctionDroneAssist(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionDroneAssist()
	{
	}

	public static ItemFunctionDroneAssist DeserializeItemFunctionDroneAssist(JSONNode _json)
	{
		return new ItemFunctionDroneAssist(_json);
	}

	public override int GetTypeId()
	{
		return 1195993275;
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
