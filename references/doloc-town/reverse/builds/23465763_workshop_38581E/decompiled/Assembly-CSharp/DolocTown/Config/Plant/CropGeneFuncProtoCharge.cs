using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoCharge : CropGeneFuncProto
{
	public const int __ID__ = -407355888;

	public CropGeneFuncProtoCharge(JSONNode _json)
		: base(_json)
	{
	}

	public CropGeneFuncProtoCharge()
	{
	}

	public static CropGeneFuncProtoCharge DeserializeCropGeneFuncProtoCharge(JSONNode _json)
	{
		return new CropGeneFuncProtoCharge(_json);
	}

	public override int GetTypeId()
	{
		return -407355888;
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
