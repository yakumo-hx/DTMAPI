using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EComProtoGeneratorSolar : EComGeneratorBase
{
	public const int __ID__ = -647662182;

	public EComProtoGeneratorSolar(JSONNode _json)
		: base(_json)
	{
	}

	public EComProtoGeneratorSolar(float efficiency)
		: base(efficiency)
	{
	}

	public static EComProtoGeneratorSolar DeserializeEComProtoGeneratorSolar(JSONNode _json)
	{
		return new EComProtoGeneratorSolar(_json);
	}

	public override int GetTypeId()
	{
		return -647662182;
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
