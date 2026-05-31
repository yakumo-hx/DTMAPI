using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncAnimalStation : EquipmentFuncEquipment
{
	public const int __ID__ = -1269542938;

	public EquipmentFuncAnimalStation(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncAnimalStation()
	{
	}

	public static EquipmentFuncAnimalStation DeserializeEquipmentFuncAnimalStation(JSONNode _json)
	{
		return new EquipmentFuncAnimalStation(_json);
	}

	public override int GetTypeId()
	{
		return -1269542938;
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
