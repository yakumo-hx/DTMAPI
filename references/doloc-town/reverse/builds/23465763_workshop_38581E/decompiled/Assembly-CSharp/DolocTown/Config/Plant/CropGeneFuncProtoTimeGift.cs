using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoTimeGift : CropGeneFuncProto
{
	public const int __ID__ = 154121721;

	public int DayThreshold { get; private set; }

	public int CropOutputAddition { get; private set; }

	public CropGeneFuncProtoTimeGift(JSONNode _json)
		: base(_json)
	{
		if (!_json["day_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		DayThreshold = _json["day_threshold"];
		if (!_json["crop_output_addition"].IsNumber)
		{
			throw new SerializationException();
		}
		CropOutputAddition = _json["crop_output_addition"];
	}

	public CropGeneFuncProtoTimeGift(int day_threshold, int crop_output_addition)
	{
		DayThreshold = day_threshold;
		CropOutputAddition = crop_output_addition;
	}

	public static CropGeneFuncProtoTimeGift DeserializeCropGeneFuncProtoTimeGift(JSONNode _json)
	{
		return new CropGeneFuncProtoTimeGift(_json);
	}

	public override int GetTypeId()
	{
		return 154121721;
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
		return "{ DayThreshold:" + DayThreshold + ",CropOutputAddition:" + CropOutputAddition + ",}";
	}
}
