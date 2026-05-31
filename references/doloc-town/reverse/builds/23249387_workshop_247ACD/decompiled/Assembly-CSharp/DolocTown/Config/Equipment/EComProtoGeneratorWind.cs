using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EComProtoGeneratorWind : EComGeneratorBase
{
	public const int __ID__ = 117768463;

	public EComProtoGeneratorWind(JSONNode _json)
		: base(_json)
	{
	}

	public EComProtoGeneratorWind(float efficiency)
		: base(efficiency)
	{
	}

	public static EComProtoGeneratorWind DeserializeEComProtoGeneratorWind(JSONNode _json)
	{
		return new EComProtoGeneratorWind(_json);
	}

	public override int GetTypeId()
	{
		return 117768463;
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
		return "{ Efficiency:" + base.Efficiency + ",}";
	}
}
