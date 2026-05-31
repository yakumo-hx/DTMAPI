using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoFirefly : CropGeneFuncProto
{
	public const int __ID__ = -1331645791;

	public CropGeneFuncProtoFirefly(JSONNode _json)
		: base(_json)
	{
	}

	public CropGeneFuncProtoFirefly()
	{
	}

	public static CropGeneFuncProtoFirefly DeserializeCropGeneFuncProtoFirefly(JSONNode _json)
	{
		return new CropGeneFuncProtoFirefly(_json);
	}

	public override int GetTypeId()
	{
		return -1331645791;
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
