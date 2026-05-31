using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public abstract class DroneFunctionProto : BeanBase
{
	public DroneFunctionProto(JSONNode _json)
	{
	}

	public DroneFunctionProto()
	{
	}

	public static DroneFunctionProto DeserializeDroneFunctionProto(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"DroneFunctionProtoDebugger" => new DroneFunctionProtoDebugger(_json), 
			"DroneFunctionProtoCrow" => new DroneFunctionProtoCrow(_json), 
			"DroneFunctionProtoShadowShooter" => new DroneFunctionProtoShadowShooter(_json), 
			"DroneFunctionProtoLight" => new DroneFunctionProtoLight(_json), 
			"DroneFunctionProtoCollectorHelper" => new DroneFunctionProtoCollectorHelper(_json), 
			"DroneFunctionProtoBrustCore" => new DroneFunctionProtoBrustCore(_json), 
			"DroneFunctionProtoCapacitance" => new DroneFunctionProtoCapacitance(_json), 
			"DroneFunctionProtoRubberBullet" => new DroneFunctionProtoRubberBullet(_json), 
			"DroneFunctionProtoAdditionalRoll" => new DroneFunctionProtoAdditionalRoll(_json), 
			"DroneFunctionProtoRestrictionReleaser" => new DroneFunctionProtoRestrictionReleaser(_json), 
			"DroneFunctionProtoFarmCollectorHelper" => new DroneFunctionProtoFarmCollectorHelper(_json), 
			"DroneFunctionProtoGarbageCombustionEngine" => new DroneFunctionProtoGarbageCombustionEngine(_json), 
			"DroneFunctionProtoChipDissolver" => new DroneFunctionProtoChipDissolver(_json), 
			"DroneFunctionProtoMineHelper" => new DroneFunctionProtoMineHelper(_json), 
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
