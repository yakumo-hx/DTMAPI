using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncFlowerPot : EquipmentFuncEquipment
{
	public const int __ID__ = -1569329848;

	public EquipmentFuncFlowerPot(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncFlowerPot()
	{
	}

	public static EquipmentFuncFlowerPot DeserializeEquipmentFuncFlowerPot(JSONNode _json)
	{
		return new EquipmentFuncFlowerPot(_json);
	}

	public override int GetTypeId()
	{
		return -1569329848;
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
