using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EComProtoAppliance : ElectronicComponentProto
{
	public const int __ID__ = 737555265;

	public float Threshold { get; private set; }

	public EComProtoAppliance(JSONNode _json)
		: base(_json)
	{
		if (!_json["threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		Threshold = _json["threshold"];
	}

	public EComProtoAppliance(float threshold)
	{
		Threshold = threshold;
	}

	public static EComProtoAppliance DeserializeEComProtoAppliance(JSONNode _json)
	{
		return new EComProtoAppliance(_json);
	}

	public override int GetTypeId()
	{
		return 737555265;
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
		return "{ Threshold:" + Threshold + ",}";
	}
}
