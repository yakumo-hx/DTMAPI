using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncTrampolineEquipment : EquipmentFuncEquipment
{
	public const int __ID__ = -906344001;

	public EquipmentFuncTrampolineEquipment(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncTrampolineEquipment()
	{
	}

	public static EquipmentFuncTrampolineEquipment DeserializeEquipmentFuncTrampolineEquipment(JSONNode _json)
	{
		return new EquipmentFuncTrampolineEquipment(_json);
	}

	public override int GetTypeId()
	{
		return -906344001;
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
