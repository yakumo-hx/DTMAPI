using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class WeaponFunctionSword : WeaponFunction
{
	public const int __ID__ = 1163801651;

	public WeaponFunctionSword(JSONNode _json)
		: base(_json)
	{
	}

	public WeaponFunctionSword()
	{
	}

	public static WeaponFunctionSword DeserializeWeaponFunctionSword(JSONNode _json)
	{
		return new WeaponFunctionSword(_json);
	}

	public override int GetTypeId()
	{
		return 1163801651;
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
