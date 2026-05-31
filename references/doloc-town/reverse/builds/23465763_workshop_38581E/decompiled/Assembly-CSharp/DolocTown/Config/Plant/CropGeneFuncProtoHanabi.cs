using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoHanabi : CropGeneFuncProto
{
	public const int __ID__ = -270303985;

	public int StartTime { get; private set; }

	public int EndTime { get; private set; }

	public float Probability { get; private set; }

	public int CropOutputAddition { get; private set; }

	public CropGeneFuncProtoHanabi(JSONNode _json)
		: base(_json)
	{
		if (!_json["start_time"].IsNumber)
		{
			throw new SerializationException();
		}
		StartTime = _json["start_time"];
		if (!_json["end_time"].IsNumber)
		{
			throw new SerializationException();
		}
		EndTime = _json["end_time"];
		if (!_json["probability"].IsNumber)
		{
			throw new SerializationException();
		}
		Probability = _json["probability"];
		if (!_json["crop_output_addition"].IsNumber)
		{
			throw new SerializationException();
		}
		CropOutputAddition = _json["crop_output_addition"];
	}

	public CropGeneFuncProtoHanabi(int start_time, int end_time, float probability, int crop_output_addition)
	{
		StartTime = start_time;
		EndTime = end_time;
		Probability = probability;
		CropOutputAddition = crop_output_addition;
	}

	public static CropGeneFuncProtoHanabi DeserializeCropGeneFuncProtoHanabi(JSONNode _json)
	{
		return new CropGeneFuncProtoHanabi(_json);
	}

	public override int GetTypeId()
	{
		return -270303985;
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
		return "{ StartTime:" + StartTime + ",EndTime:" + EndTime + ",Probability:" + Probability + ",CropOutputAddition:" + CropOutputAddition + ",}";
	}
}
