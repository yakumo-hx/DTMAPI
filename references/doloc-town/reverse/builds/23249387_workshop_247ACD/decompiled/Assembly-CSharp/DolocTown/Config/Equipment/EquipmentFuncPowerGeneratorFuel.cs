using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncPowerGeneratorFuel : EquipmentFuncEquipment
{
	public const int __ID__ = -1578103146;

	public EquipmentFuncPowerGeneratorFuel(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncPowerGeneratorFuel()
	{
	}

	public static EquipmentFuncPowerGeneratorFuel DeserializeEquipmentFuncPowerGeneratorFuel(JSONNode _json)
	{
		return new EquipmentFuncPowerGeneratorFuel(_json);
	}

	public override int GetTypeId()
	{
		return -1578103146;
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
