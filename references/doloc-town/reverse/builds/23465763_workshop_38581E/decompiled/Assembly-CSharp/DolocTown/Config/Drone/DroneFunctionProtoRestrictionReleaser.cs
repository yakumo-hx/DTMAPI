using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoRestrictionReleaser : DroneFunctionProto
{
	public const int __ID__ = 1183565513;

	public float PowerCostIncrease { get; private set; }

	public float DamageIncrease { get; private set; }

	public DroneFunctionProtoRestrictionReleaser(JSONNode _json)
		: base(_json)
	{
		if (!_json["power_cost_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		PowerCostIncrease = _json["power_cost_increase"];
		if (!_json["damage_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		DamageIncrease = _json["damage_increase"];
	}

	public DroneFunctionProtoRestrictionReleaser(float power_cost_increase, float damage_increase)
	{
		PowerCostIncrease = power_cost_increase;
		DamageIncrease = damage_increase;
	}

	public static DroneFunctionProtoRestrictionReleaser DeserializeDroneFunctionProtoRestrictionReleaser(JSONNode _json)
	{
		return new DroneFunctionProtoRestrictionReleaser(_json);
	}

	public override int GetTypeId()
	{
		return 1183565513;
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
		return "{ PowerCostIncrease:" + PowerCostIncrease + ",DamageIncrease:" + DamageIncrease + ",}";
	}
}
