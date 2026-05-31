using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncPowerGeneratorWind : EquipmentFuncEquipment
{
	public const int __ID__ = -1577607960;

	public EquipmentFuncPowerGeneratorWind(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncPowerGeneratorWind()
	{
	}

	public static EquipmentFuncPowerGeneratorWind DeserializeEquipmentFuncPowerGeneratorWind(JSONNode _json)
	{
		return new EquipmentFuncPowerGeneratorWind(_json);
	}

	public override int GetTypeId()
	{
		return -1577607960;
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
