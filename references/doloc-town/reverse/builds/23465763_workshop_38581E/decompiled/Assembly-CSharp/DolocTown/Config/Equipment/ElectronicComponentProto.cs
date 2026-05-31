using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public abstract class ElectronicComponentProto : BeanBase
{
	public ElectronicComponentProto(JSONNode _json)
	{
	}

	public ElectronicComponentProto()
	{
	}

	public static ElectronicComponentProto DeserializeElectronicComponentProto(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"EComProtoAppliance" => new EComProtoAppliance(_json), 
			"EComProtoBattery" => new EComProtoBattery(_json), 
			"EComProtoGeneratorCustom" => new EComProtoGeneratorCustom(_json), 
			"EComProtoGeneratorFuel" => new EComProtoGeneratorFuel(_json), 
			"EComProtoGeneratorWind" => new EComProtoGeneratorWind(_json), 
			"EComProtoGeneratorSolar" => new EComProtoGeneratorSolar(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ }";
	}
}
