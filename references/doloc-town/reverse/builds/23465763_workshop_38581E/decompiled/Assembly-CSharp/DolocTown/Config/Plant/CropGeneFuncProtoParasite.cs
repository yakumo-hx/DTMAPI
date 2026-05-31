using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoParasite : CropGeneFuncProtoRange
{
	public const int __ID__ = -891484285;

	public float GrowthStolenValue { get; private set; }

	public int StealInterval { get; private set; }

	public CropGeneFuncProtoParasite(JSONNode _json)
		: base(_json)
	{
		if (!_json["growth_stolen_value"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowthStolenValue = _json["growth_stolen_value"];
		if (!_json["steal_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		StealInterval = _json["steal_interval"];
	}

	public CropGeneFuncProtoParasite(int horizontal_range, int vertical_range_top, int vertical_range_bottom, float growth_stolen_value, int steal_interval)
		: base(horizontal_range, vertical_range_top, vertical_range_bottom)
	{
		GrowthStolenValue = growth_stolen_value;
		StealInterval = steal_interval;
	}

	public static CropGeneFuncProtoParasite DeserializeCropGeneFuncProtoParasite(JSONNode _json)
	{
		return new CropGeneFuncProtoParasite(_json);
	}

	public override int GetTypeId()
	{
		return -891484285;
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
		return "{ HorizontalRange:" + base.HorizontalRange + ",VerticalRangeTop:" + base.VerticalRangeTop + ",VerticalRangeBottom:" + base.VerticalRangeBottom + ",GrowthStolenValue:" + GrowthStolenValue + ",StealInterval:" + StealInterval + ",}";
	}
}
