using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoOxygen : CropGeneFuncProtoRange
{
	public const int __ID__ = -48325380;

	public int Interval { get; private set; }

	public int LevelThreshold { get; private set; }

	public CropGeneFuncProtoOxygen(JSONNode _json)
		: base(_json)
	{
		if (!_json["interval"].IsNumber)
		{
			throw new SerializationException();
		}
		Interval = _json["interval"];
		if (!_json["level_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		LevelThreshold = _json["level_threshold"];
	}

	public CropGeneFuncProtoOxygen(int horizontal_range, int vertical_range_top, int vertical_range_bottom, int interval, int level_threshold)
		: base(horizontal_range, vertical_range_top, vertical_range_bottom)
	{
		Interval = interval;
		LevelThreshold = level_threshold;
	}

	public static CropGeneFuncProtoOxygen DeserializeCropGeneFuncProtoOxygen(JSONNode _json)
	{
		return new CropGeneFuncProtoOxygen(_json);
	}

	public override int GetTypeId()
	{
		return -48325380;
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
		return "{ HorizontalRange:" + base.HorizontalRange + ",VerticalRangeTop:" + base.VerticalRangeTop + ",VerticalRangeBottom:" + base.VerticalRangeBottom + ",Interval:" + Interval + ",LevelThreshold:" + LevelThreshold + ",}";
	}
}
