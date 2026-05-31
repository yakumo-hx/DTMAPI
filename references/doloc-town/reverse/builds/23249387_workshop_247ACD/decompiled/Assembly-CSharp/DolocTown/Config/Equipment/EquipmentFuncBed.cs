using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncBed : EquipmentFuncEquipment
{
	public const int __ID__ = -1293640113;

	public EquipmentFuncBed(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncBed()
	{
	}

	public static EquipmentFuncBed DeserializeEquipmentFuncBed(JSONNode _json)
	{
		return new EquipmentFuncBed(_json);
	}

	public override int GetTypeId()
	{
		return -1293640113;
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
