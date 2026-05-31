using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoSymbioticSupply : CropGeneFuncProtoRange
{
	public const int __ID__ = 773166976;

	public float GrowthValue { get; private set; }

	public CropGeneFuncProtoSymbioticSupply(JSONNode _json)
		: base(_json)
	{
		if (!_json["growth_value"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowthValue = _json["growth_value"];
	}

	public CropGeneFuncProtoSymbioticSupply(int horizontal_range, int vertical_range_top, int vertical_range_bottom, float growth_value)
		: base(horizontal_range, vertical_range_top, vertical_range_bottom)
	{
		GrowthValue = growth_value;
	}

	public static CropGeneFuncProtoSymbioticSupply DeserializeCropGeneFuncProtoSymbioticSupply(JSONNode _json)
	{
		return new CropGeneFuncProtoSymbioticSupply(_json);
	}

	public override int GetTypeId()
	{
		return 773166976;
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
		return "{ HorizontalRange:" + base.HorizontalRange + ",VerticalRangeTop:" + base.VerticalRangeTop + ",VerticalRangeBottom:" + base.VerticalRangeBottom + ",GrowthValue:" + GrowthValue + ",}";
	}
}
