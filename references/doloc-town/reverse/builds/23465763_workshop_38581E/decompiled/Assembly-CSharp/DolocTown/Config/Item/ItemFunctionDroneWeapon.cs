using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionDroneWeapon : ItemFunctionWeaponBase
{
	public const int __ID__ = 1812375662;

	public ItemFunctionDroneWeapon(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionDroneWeapon()
	{
	}

	public static ItemFunctionDroneWeapon DeserializeItemFunctionDroneWeapon(JSONNode _json)
	{
		return new ItemFunctionDroneWeapon(_json);
	}

	public override int GetTypeId()
	{
		return 1812375662;
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
