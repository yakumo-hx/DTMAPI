using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoHope : CropGeneFuncProto
{
	public const int __ID__ = 303642520;

	public int SeedDropCount { get; private set; }

	public CropGeneFuncProtoHope(JSONNode _json)
		: base(_json)
	{
		if (!_json["seed_drop_count"].IsNumber)
		{
			throw new SerializationException();
		}
		SeedDropCount = _json["seed_drop_count"];
	}

	public CropGeneFuncProtoHope(int seed_drop_count)
	{
		SeedDropCount = seed_drop_count;
	}

	public static CropGeneFuncProtoHope DeserializeCropGeneFuncProtoHope(JSONNode _json)
	{
		return new CropGeneFuncProtoHope(_json);
	}

	public override int GetTypeId()
	{
		return 303642520;
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
		return "{ SeedDropCount:" + SeedDropCount + ",}";
	}
}
