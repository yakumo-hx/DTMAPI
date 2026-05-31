using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EComProtoGeneratorFuel : EComGeneratorBase
{
	public const int __ID__ = 117273277;

	public float FuelCapacity { get; private set; }

	public float Rate { get; private set; }

	public float ExceedFuelCapacity { get; private set; }

	public EComProtoGeneratorFuel(JSONNode _json)
		: base(_json)
	{
		if (!_json["fuel_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		FuelCapacity = _json["fuel_capacity"];
		if (!_json["rate"].IsNumber)
		{
			throw new SerializationException();
		}
		Rate = _json["rate"];
		if (!_json["exceed_fuel_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		ExceedFuelCapacity = _json["exceed_fuel_capacity"];
	}

	public EComProtoGeneratorFuel(float efficiency, float fuel_capacity, float rate, float exceed_fuel_capacity)
		: base(efficiency)
	{
		FuelCapacity = fuel_capacity;
		Rate = rate;
		ExceedFuelCapacity = exceed_fuel_capacity;
	}

	public static EComProtoGeneratorFuel DeserializeEComProtoGeneratorFuel(JSONNode _json)
	{
		return new EComProtoGeneratorFuel(_json);
	}

	public override int GetTypeId()
	{
		return 117273277;
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
		return "{ Efficiency:" + base.Efficiency + ",FuelCapacity:" + FuelCapacity + ",Rate:" + Rate + ",ExceedFuelCapacity:" + ExceedFuelCapacity + ",}";
	}
}
