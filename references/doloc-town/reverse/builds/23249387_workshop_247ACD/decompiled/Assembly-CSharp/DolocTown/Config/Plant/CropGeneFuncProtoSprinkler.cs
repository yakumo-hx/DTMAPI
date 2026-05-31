using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoSprinkler : CropGeneFuncProtoRange
{
	public const int __ID__ = 1898181004;

	public int Interval { get; private set; }

	public int WaterCost { get; private set; }

	public int WaterAddition { get; private set; }

	public CropGeneFuncProtoSprinkler(JSONNode _json)
		: base(_json)
	{
		if (!_json["interval"].IsNumber)
		{
			throw new SerializationException();
		}
		Interval = _json["interval"];
		if (!_json["water_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		WaterCost = _json["water_cost"];
		if (!_json["water_addition"].IsNumber)
		{
			throw new SerializationException();
		}
		WaterAddition = _json["water_addition"];
	}

	public CropGeneFuncProtoSprinkler(int horizontal_range, int vertical_range_top, int vertical_range_bottom, int interval, int water_cost, int water_addition)
		: base(horizontal_range, vertical_range_top, vertical_range_bottom)
	{
		Interval = interval;
		WaterCost = water_cost;
		WaterAddition = water_addition;
	}

	public static CropGeneFuncProtoSprinkler DeserializeCropGeneFuncProtoSprinkler(JSONNode _json)
	{
		return new CropGeneFuncProtoSprinkler(_json);
	}

	public override int GetTypeId()
	{
		return 1898181004;
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
		return "{ HorizontalRange:" + base.HorizontalRange + ",VerticalRangeTop:" + base.VerticalRangeTop + ",VerticalRangeBottom:" + base.VerticalRangeBottom + ",Interval:" + Interval + ",WaterCost:" + WaterCost + ",WaterAddition:" + WaterAddition + ",}";
	}
}
