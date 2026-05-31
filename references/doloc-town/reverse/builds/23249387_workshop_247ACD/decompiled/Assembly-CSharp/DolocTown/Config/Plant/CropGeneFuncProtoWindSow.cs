using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoWindSow : CropGeneFuncProtoRange
{
	public const int __ID__ = 867272855;

	public int SowInterval { get; private set; }

	public CropGeneFuncProtoWindSow(JSONNode _json)
		: base(_json)
	{
		if (!_json["sow_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		SowInterval = _json["sow_interval"];
	}

	public CropGeneFuncProtoWindSow(int horizontal_range, int vertical_range_top, int vertical_range_bottom, int sow_interval)
		: base(horizontal_range, vertical_range_top, vertical_range_bottom)
	{
		SowInterval = sow_interval;
	}

	public static CropGeneFuncProtoWindSow DeserializeCropGeneFuncProtoWindSow(JSONNode _json)
	{
		return new CropGeneFuncProtoWindSow(_json);
	}

	public override int GetTypeId()
	{
		return 867272855;
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
		return "{ HorizontalRange:" + base.HorizontalRange + ",VerticalRangeTop:" + base.VerticalRangeTop + ",VerticalRangeBottom:" + base.VerticalRangeBottom + ",SowInterval:" + SowInterval + ",}";
	}
}
