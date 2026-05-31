using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncCaseLocator : EquipmentFuncEquipment
{
	public const int __ID__ = 1937864468;

	public string Lamp { get; private set; }

	public LampInfo Lamp_Ref { get; private set; }

	public EquipmentFuncCaseLocator(JSONNode _json)
		: base(_json)
	{
		if (!_json["lamp"].IsString)
		{
			throw new SerializationException();
		}
		Lamp = _json["lamp"];
	}

	public EquipmentFuncCaseLocator(string lamp)
	{
		Lamp = lamp;
	}

	public static EquipmentFuncCaseLocator DeserializeEquipmentFuncCaseLocator(JSONNode _json)
	{
		return new EquipmentFuncCaseLocator(_json);
	}

	public override int GetTypeId()
	{
		return 1937864468;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		Lamp_Ref = (_tables["Equipment.TbLamp"] as TbLamp).GetOrDefault(Lamp);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Lamp:" + Lamp + ",}";
	}
}
