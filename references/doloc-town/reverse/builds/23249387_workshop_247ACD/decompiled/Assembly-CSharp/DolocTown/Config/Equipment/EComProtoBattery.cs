using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EComProtoBattery : ElectronicComponentProto
{
	public const int __ID__ = -162934623;

	public float Voltage { get; private set; }

	public float Capacity { get; private set; }

	public EComProtoBattery(JSONNode _json)
		: base(_json)
	{
		if (!_json["voltage"].IsNumber)
		{
			throw new SerializationException();
		}
		Voltage = _json["voltage"];
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
	}

	public EComProtoBattery(float voltage, float capacity)
	{
		Voltage = voltage;
		Capacity = capacity;
	}

	public static EComProtoBattery DeserializeEComProtoBattery(JSONNode _json)
	{
		return new EComProtoBattery(_json);
	}

	public override int GetTypeId()
	{
		return -162934623;
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
		return "{ Voltage:" + Voltage + ",Capacity:" + Capacity + ",}";
	}
}
