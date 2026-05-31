using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncLamp : EquipmentFuncEquipment
{
	public const int __ID__ = -1447843382;

	public string Lamp { get; private set; }

	public LampInfo Lamp_Ref { get; private set; }

	public EquipmentFuncLamp(JSONNode _json)
		: base(_json)
	{
		if (!_json["lamp"].IsString)
		{
			throw new SerializationException();
		}
		Lamp = _json["lamp"];
	}

	public EquipmentFuncLamp(string lamp)
	{
		Lamp = lamp;
	}

	public static EquipmentFuncLamp DeserializeEquipmentFuncLamp(JSONNode _json)
	{
		return new EquipmentFuncLamp(_json);
	}

	public override int GetTypeId()
	{
		return -1447843382;
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
