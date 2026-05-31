using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionDroneChip : ItemFunctionWeaponBase
{
	public const int __ID__ = 850454302;

	public ItemFunctionDroneChip(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionDroneChip()
	{
	}

	public static ItemFunctionDroneChip DeserializeItemFunctionDroneChip(JSONNode _json)
	{
		return new ItemFunctionDroneChip(_json);
	}

	public override int GetTypeId()
	{
		return 850454302;
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
