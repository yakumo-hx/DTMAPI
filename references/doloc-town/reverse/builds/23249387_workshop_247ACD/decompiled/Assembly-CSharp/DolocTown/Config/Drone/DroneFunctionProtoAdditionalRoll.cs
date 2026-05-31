using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoAdditionalRoll : DroneFunctionProto
{
	public const int __ID__ = -2063381390;

	public float Probability { get; private set; }

	public int Count { get; private set; }

	public DroneFunctionProtoAdditionalRoll(JSONNode _json)
		: base(_json)
	{
		if (!_json["probability"].IsNumber)
		{
			throw new SerializationException();
		}
		Probability = _json["probability"];
		if (!_json["count"].IsNumber)
		{
			throw new SerializationException();
		}
		Count = _json["count"];
	}

	public DroneFunctionProtoAdditionalRoll(float probability, int count)
	{
		Probability = probability;
		Count = count;
	}

	public static DroneFunctionProtoAdditionalRoll DeserializeDroneFunctionProtoAdditionalRoll(JSONNode _json)
	{
		return new DroneFunctionProtoAdditionalRoll(_json);
	}

	public override int GetTypeId()
	{
		return -2063381390;
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
		return "{ Probability:" + Probability + ",Count:" + Count + ",}";
	}
}
