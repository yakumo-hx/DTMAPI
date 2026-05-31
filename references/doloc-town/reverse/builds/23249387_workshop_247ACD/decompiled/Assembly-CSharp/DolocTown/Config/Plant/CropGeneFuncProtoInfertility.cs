using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoInfertility : CropGeneFuncProto
{
	public const int __ID__ = 1316968617;

	public CropGeneFuncProtoInfertility(JSONNode _json)
		: base(_json)
	{
	}

	public CropGeneFuncProtoInfertility()
	{
	}

	public static CropGeneFuncProtoInfertility DeserializeCropGeneFuncProtoInfertility(JSONNode _json)
	{
		return new CropGeneFuncProtoInfertility(_json);
	}

	public override int GetTypeId()
	{
		return 1316968617;
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
