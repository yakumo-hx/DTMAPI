using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncPowerGeneratorBiogas : EquipmentFuncEquipment
{
	public const int __ID__ = -558970447;

	public float Efficiency { get; private set; }

	public EquipmentFuncPowerGeneratorBiogas(JSONNode _json)
		: base(_json)
	{
		if (!_json["efficiency"].IsNumber)
		{
			throw new SerializationException();
		}
		Efficiency = _json["efficiency"];
	}

	public EquipmentFuncPowerGeneratorBiogas(float efficiency)
	{
		Efficiency = efficiency;
	}

	public static EquipmentFuncPowerGeneratorBiogas DeserializeEquipmentFuncPowerGeneratorBiogas(JSONNode _json)
	{
		return new EquipmentFuncPowerGeneratorBiogas(_json);
	}

	public override int GetTypeId()
	{
		return -558970447;
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
		return "{ Efficiency:" + Efficiency + ",}";
	}
}
