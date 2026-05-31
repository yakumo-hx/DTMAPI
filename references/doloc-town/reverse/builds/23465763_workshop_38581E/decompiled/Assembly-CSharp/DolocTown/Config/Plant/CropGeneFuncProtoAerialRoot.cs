using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoAerialRoot : CropGeneFuncProto
{
	public const int __ID__ = 286454308;

	public CropGeneFuncProtoAerialRoot(JSONNode _json)
		: base(_json)
	{
	}

	public CropGeneFuncProtoAerialRoot()
	{
	}

	public static CropGeneFuncProtoAerialRoot DeserializeCropGeneFuncProtoAerialRoot(JSONNode _json)
	{
		return new CropGeneFuncProtoAerialRoot(_json);
	}

	public override int GetTypeId()
	{
		return 286454308;
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
