using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoGrowUnchecked : CropGeneFuncProto
{
	public const int __ID__ = -679481025;

	public float GrowthIncrease { get; private set; }

	public CropGeneFuncProtoGrowUnchecked(JSONNode _json)
		: base(_json)
	{
		if (!_json["growth_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowthIncrease = _json["growth_increase"];
	}

	public CropGeneFuncProtoGrowUnchecked(float growth_increase)
	{
		GrowthIncrease = growth_increase;
	}

	public static CropGeneFuncProtoGrowUnchecked DeserializeCropGeneFuncProtoGrowUnchecked(JSONNode _json)
	{
		return new CropGeneFuncProtoGrowUnchecked(_json);
	}

	public override int GetTypeId()
	{
		return -679481025;
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
		return "{ GrowthIncrease:" + GrowthIncrease + ",}";
	}
}
