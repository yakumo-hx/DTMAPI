using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public abstract class EComGeneratorBase : ElectronicComponentProto
{
	public float Efficiency { get; private set; }

	public EComGeneratorBase(JSONNode _json)
		: base(_json)
	{
		if (!_json["efficiency"].IsNumber)
		{
			throw new SerializationException();
		}
		Efficiency = _json["efficiency"];
	}

	public EComGeneratorBase(float efficiency)
	{
		Efficiency = efficiency;
	}

	public static EComGeneratorBase DeserializeEComGeneratorBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"EComProtoGeneratorCustom" => new EComProtoGeneratorCustom(_json), 
			"EComProtoGeneratorFuel" => new EComProtoGeneratorFuel(_json), 
			"EComProtoGeneratorWind" => new EComProtoGeneratorWind(_json), 
			"EComProtoGeneratorSolar" => new EComProtoGeneratorSolar(_json), 
			_ => throw new SerializationException(), 
		};
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
		return "{ Efficiency:" + Efficiency + ",}";
	}
}
