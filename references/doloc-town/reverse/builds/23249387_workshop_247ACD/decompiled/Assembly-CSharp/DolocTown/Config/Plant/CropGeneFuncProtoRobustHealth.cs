using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoRobustHealth : CropGeneFuncProto
{
	public const int __ID__ = -35961399;

	public float GrowthDecrease { get; private set; }

	public CropGeneFuncProtoRobustHealth(JSONNode _json)
		: base(_json)
	{
		if (!_json["growth_decrease"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowthDecrease = _json["growth_decrease"];
	}

	public CropGeneFuncProtoRobustHealth(float growth_decrease)
	{
		GrowthDecrease = growth_decrease;
	}

	public static CropGeneFuncProtoRobustHealth DeserializeCropGeneFuncProtoRobustHealth(JSONNode _json)
	{
		return new CropGeneFuncProtoRobustHealth(_json);
	}

	public override int GetTypeId()
	{
		return -35961399;
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
		return "{ GrowthDecrease:" + GrowthDecrease + ",}";
	}
}
