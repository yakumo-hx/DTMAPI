using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncSimpleWell : EquipmentFuncEquipment
{
	public const int __ID__ = 543488018;

	public int Capacity { get; private set; }

	public int Adder { get; private set; }

	public EquipmentFuncSimpleWell(JSONNode _json)
		: base(_json)
	{
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
		if (!_json["adder"].IsNumber)
		{
			throw new SerializationException();
		}
		Adder = _json["adder"];
	}

	public EquipmentFuncSimpleWell(int capacity, int adder)
	{
		Capacity = capacity;
		Adder = adder;
	}

	public static EquipmentFuncSimpleWell DeserializeEquipmentFuncSimpleWell(JSONNode _json)
	{
		return new EquipmentFuncSimpleWell(_json);
	}

	public override int GetTypeId()
	{
		return 543488018;
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
		return "{ Capacity:" + Capacity + ",Adder:" + Adder + ",}";
	}
}
