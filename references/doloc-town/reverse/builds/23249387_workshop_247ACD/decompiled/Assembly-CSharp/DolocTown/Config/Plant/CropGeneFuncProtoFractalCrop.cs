using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoFractalCrop : CropGeneFuncProto
{
	public const int __ID__ = -178870459;

	public float CropOutputAddition { get; private set; }

	public CropGeneFuncProtoFractalCrop(JSONNode _json)
		: base(_json)
	{
		if (!_json["crop_output_addition"].IsNumber)
		{
			throw new SerializationException();
		}
		CropOutputAddition = _json["crop_output_addition"];
	}

	public CropGeneFuncProtoFractalCrop(float crop_output_addition)
	{
		CropOutputAddition = crop_output_addition;
	}

	public static CropGeneFuncProtoFractalCrop DeserializeCropGeneFuncProtoFractalCrop(JSONNode _json)
	{
		return new CropGeneFuncProtoFractalCrop(_json);
	}

	public override int GetTypeId()
	{
		return -178870459;
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
