using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public abstract class WeaponFunction : BeanBase
{
	public WeaponFunction(JSONNode _json)
	{
	}

	public WeaponFunction()
	{
	}

	public static WeaponFunction DeserializeWeaponFunction(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"WeaponFunctionGunDefault" => new WeaponFunctionGunDefault(_json), 
			"WeaponFunctionGunDragonShooter" => new WeaponFunctionGunDragonShooter(_json), 
			"WeaponFunctionSword" => new WeaponFunctionSword(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ }";
	}
}
