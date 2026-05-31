using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class WeaponFunctionGunDefault : WeaponFunctionGun
{
	public const int __ID__ = -89159573;

	public WeaponFunctionGunDefault(JSONNode _json)
		: base(_json)
	{
	}

	public WeaponFunctionGunDefault()
	{
	}

	public static WeaponFunctionGunDefault DeserializeWeaponFunctionGunDefault(JSONNode _json)
	{
		return new WeaponFunctionGunDefault(_json);
	}

	public override int GetTypeId()
	{
		return -89159573;
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
