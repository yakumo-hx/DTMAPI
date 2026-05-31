using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoConiferLeaf : CropGeneFuncProto
{
	public const int __ID__ = 1931575982;

	public float GrowthDecrease { get; private set; }

	public CropGeneFuncProtoConiferLeaf(JSONNode _json)
		: base(_json)
	{
		if (!_json["growth_decrease"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowthDecrease = _json["growth_decrease"];
	}

	public CropGeneFuncProtoConiferLeaf(float growth_decrease)
	{
		GrowthDecrease = growth_decrease;
	}

	public static CropGeneFuncProtoConiferLeaf DeserializeCropGeneFuncProtoConiferLeaf(JSONNode _json)
	{
		return new CropGeneFuncProtoConiferLeaf(_json);
	}

	public override int GetTypeId()
	{
		return 1931575982;
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
