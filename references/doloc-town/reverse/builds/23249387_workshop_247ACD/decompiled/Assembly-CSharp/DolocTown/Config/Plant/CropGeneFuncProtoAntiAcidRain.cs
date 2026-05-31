using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoAntiAcidRain : CropGeneFuncProto
{
	public const int __ID__ = -1961525809;

	public CropGeneFuncProtoAntiAcidRain(JSONNode _json)
		: base(_json)
	{
	}

	public CropGeneFuncProtoAntiAcidRain()
	{
	}

	public static CropGeneFuncProtoAntiAcidRain DeserializeCropGeneFuncProtoAntiAcidRain(JSONNode _json)
	{
		return new CropGeneFuncProtoAntiAcidRain(_json);
	}

	public override int GetTypeId()
	{
		return -1961525809;
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
		return "{ }";
	}
}
