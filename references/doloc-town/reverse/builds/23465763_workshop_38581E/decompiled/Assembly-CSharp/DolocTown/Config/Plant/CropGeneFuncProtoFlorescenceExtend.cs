using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoFlorescenceExtend : CropGeneFuncProto
{
	public const int __ID__ = 967905685;

	public int LifespanAddition { get; private set; }

	public float GrowthDecrease { get; private set; }

	public CropGeneFuncProtoFlorescenceExtend(JSONNode _json)
		: base(_json)
	{
		if (!_json["lifespan_addition"].IsNumber)
		{
			throw new SerializationException();
		}
		LifespanAddition = _json["lifespan_addition"];
		if (!_json["growth_decrease"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowthDecrease = _json["growth_decrease"];
	}

	public CropGeneFuncProtoFlorescenceExtend(int lifespan_addition, float growth_decrease)
	{
		LifespanAddition = lifespan_addition;
		GrowthDecrease = growth_decrease;
	}

	public static CropGeneFuncProtoFlorescenceExtend DeserializeCropGeneFuncProtoFlorescenceExtend(JSONNode _json)
	{
		return new CropGeneFuncProtoFlorescenceExtend(_json);
	}

	public override int GetTypeId()
	{
		return 967905685;
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
		return "{ LifespanAddition:" + LifespanAddition + ",GrowthDecrease:" + GrowthDecrease + ",}";
	}
}
