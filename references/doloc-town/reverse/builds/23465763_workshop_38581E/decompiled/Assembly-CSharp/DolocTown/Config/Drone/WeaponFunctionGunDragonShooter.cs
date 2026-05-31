using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class WeaponFunctionGunDragonShooter : WeaponFunctionGun
{
	public const int __ID__ = -659726813;

	public float Angle { get; private set; }

	public int DirCount { get; private set; }

	public WeaponFunctionGunDragonShooter(JSONNode _json)
		: base(_json)
	{
		if (!_json["angle"].IsNumber)
		{
			throw new SerializationException();
		}
		Angle = _json["angle"];
		if (!_json["dir_count"].IsNumber)
		{
			throw new SerializationException();
		}
		DirCount = _json["dir_count"];
	}

	public WeaponFunctionGunDragonShooter(float angle, int dir_count)
	{
		Angle = angle;
		DirCount = dir_count;
	}

	public static WeaponFunctionGunDragonShooter DeserializeWeaponFunctionGunDragonShooter(JSONNode _json)
	{
		return new WeaponFunctionGunDragonShooter(_json);
	}

	public override int GetTypeId()
	{
		return -659726813;
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
		return "{ Angle:" + Angle + ",DirCount:" + DirCount + ",}";
	}
}
