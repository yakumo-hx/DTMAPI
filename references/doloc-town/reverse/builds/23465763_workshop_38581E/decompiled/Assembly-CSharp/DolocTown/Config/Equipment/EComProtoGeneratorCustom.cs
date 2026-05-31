using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EComProtoGeneratorCustom : EComGeneratorBase
{
	public const int __ID__ = 945010360;

	public EComProtoGeneratorCustom(JSONNode _json)
		: base(_json)
	{
	}

	public EComProtoGeneratorCustom(float efficiency)
		: base(efficiency)
	{
	}

	public static EComProtoGeneratorCustom DeserializeEComProtoGeneratorCustom(JSONNode _json)
	{
		return new EComProtoGeneratorCustom(_json);
	}

	public override int GetTypeId()
	{
		return 945010360;
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
