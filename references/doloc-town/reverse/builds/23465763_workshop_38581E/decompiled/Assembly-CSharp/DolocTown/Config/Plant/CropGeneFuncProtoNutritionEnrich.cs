using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoNutritionEnrich : CropGeneFuncProto
{
	public const int __ID__ = -378979615;

	public float CropOutputAddition { get; private set; }

	public CropGeneFuncProtoNutritionEnrich(JSONNode _json)
		: base(_json)
	{
		if (!_json["crop_output_addition"].IsNumber)
		{
			throw new SerializationException();
		}
		CropOutputAddition = _json["crop_output_addition"];
	}

	public CropGeneFuncProtoNutritionEnrich(float crop_output_addition)
	{
		CropOutputAddition = crop_output_addition;
	}

	public static CropGeneFuncProtoNutritionEnrich DeserializeCropGeneFuncProtoNutritionEnrich(JSONNode _json)
	{
		return new CropGeneFuncProtoNutritionEnrich(_json);
	}

	public override int GetTypeId()
	{
		return -378979615;
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
		return "{ CropOutputAddition:" + CropOutputAddition + ",}";
	}
}
