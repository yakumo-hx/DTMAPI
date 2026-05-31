using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionDroneEngine : ItemFunctionWeaponBase
{
	public const int __ID__ = 1305534612;

	public ItemFunctionDroneEngine(JSONNode _json)
		: base(_json)
	{
	}

	public ItemFunctionDroneEngine()
	{
	}

	public static ItemFunctionDroneEngine DeserializeItemFunctionDroneEngine(JSONNode _json)
	{
		return new ItemFunctionDroneEngine(_json);
	}

	public override int GetTypeId()
	{
		return 1305534612;
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
