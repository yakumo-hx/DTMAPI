using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncDecorator : EquipmentFuncEquipment
{
	public const int __ID__ = 453199753;

	public EquipmentFuncDecorator(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncDecorator()
	{
	}

	public static EquipmentFuncDecorator DeserializeEquipmentFuncDecorator(JSONNode _json)
	{
		return new EquipmentFuncDecorator(_json);
	}

	public override int GetTypeId()
	{
		return 453199753;
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
