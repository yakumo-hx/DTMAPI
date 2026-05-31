using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoImmortalJellyfish : CropGeneFuncProto
{
	public const int __ID__ = 126167783;

	public float RebirthProbability { get; private set; }

	public CropGeneFuncProtoImmortalJellyfish(JSONNode _json)
		: base(_json)
	{
		if (!_json["rebirth_probability"].IsNumber)
		{
			throw new SerializationException();
		}
		RebirthProbability = _json["rebirth_probability"];
	}

	public CropGeneFuncProtoImmortalJellyfish(float rebirth_probability)
	{
		RebirthProbability = rebirth_probability;
	}

	public static CropGeneFuncProtoImmortalJellyfish DeserializeCropGeneFuncProtoImmortalJellyfish(JSONNode _json)
	{
		return new CropGeneFuncProtoImmortalJellyfish(_json);
	}

	public override int GetTypeId()
	{
		return 126167783;
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
		return "{ RebirthProbability:" + RebirthProbability + ",}";
	}
}
