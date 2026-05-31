using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public abstract class WeaponFunctionGun : WeaponFunction
{
	public WeaponFunctionGun(JSONNode _json)
		: base(_json)
	{
	}

	public WeaponFunctionGun()
	{
	}

	public static WeaponFunctionGun DeserializeWeaponFunctionGun(JSONNode _json)
	{
		string text = _json["$type"];
		if (!(text == "WeaponFunctionGunDefault"))
		{
			if (text == "WeaponFunctionGunDragonShooter")
			{
				return new WeaponFunctionGunDragonShooter(_json);
			}
			throw new SerializationException();
		}
		return new WeaponFunctionGunDefault(_json);
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
