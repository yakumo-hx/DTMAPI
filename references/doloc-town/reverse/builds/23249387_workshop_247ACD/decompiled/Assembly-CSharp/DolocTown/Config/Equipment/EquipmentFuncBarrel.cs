using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncBarrel : EquipmentFuncEquipment
{
	public const int __ID__ = -94223592;

	public int Capacity { get; private set; }

	public EquipmentFuncBarrel(JSONNode _json)
		: base(_json)
	{
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
	}

	public EquipmentFuncBarrel(int capacity)
	{
		Capacity = capacity;
	}

	public static EquipmentFuncBarrel DeserializeEquipmentFuncBarrel(JSONNode _json)
	{
		return new EquipmentFuncBarrel(_json);
	}

	public override int GetTypeId()
	{
		return -94223592;
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
		return "{ Capacity:" + Capacity + ",}";
	}
}
