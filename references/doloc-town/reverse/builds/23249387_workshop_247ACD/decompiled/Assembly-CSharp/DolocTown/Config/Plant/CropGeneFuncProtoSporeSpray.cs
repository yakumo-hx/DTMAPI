using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoSporeSpray : CropGeneFuncProto
{
	public const int __ID__ = 1193475492;

	public CropGeneFuncProtoSporeSpray(JSONNode _json)
		: base(_json)
	{
	}

	public CropGeneFuncProtoSporeSpray()
	{
	}

	public static CropGeneFuncProtoSporeSpray DeserializeCropGeneFuncProtoSporeSpray(JSONNode _json)
	{
		return new CropGeneFuncProtoSporeSpray(_json);
	}

	public override int GetTypeId()
	{
		return 1193475492;
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
