using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoCapacitance : DroneFunctionProto
{
	public const int __ID__ = 2105371904;

	public int ExtraDamage { get; private set; }

	public float ExtraDamagePercent { get; private set; }

	public DroneFunctionProtoCapacitance(JSONNode _json)
		: base(_json)
	{
		if (!_json["extra_damage"].IsNumber)
		{
			throw new SerializationException();
		}
		ExtraDamage = _json["extra_damage"];
		if (!_json["extra_damage_percent"].IsNumber)
		{
			throw new SerializationException();
		}
		ExtraDamagePercent = _json["extra_damage_percent"];
	}

	public DroneFunctionProtoCapacitance(int extra_damage, float extra_damage_percent)
	{
		ExtraDamage = extra_damage;
		ExtraDamagePercent = extra_damage_percent;
	}

	public static DroneFunctionProtoCapacitance DeserializeDroneFunctionProtoCapacitance(JSONNode _json)
	{
		return new DroneFunctionProtoCapacitance(_json);
	}

	public override int GetTypeId()
	{
		return 2105371904;
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
		return "{ ExtraDamage:" + ExtraDamage + ",ExtraDamagePercent:" + ExtraDamagePercent + ",}";
	}
}
