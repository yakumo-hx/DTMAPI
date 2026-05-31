using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class WDP_ScorchSun_PlantBasin : WeatherDecoratorProto
{
	public const int __ID__ = 456553141;

	public WDP_ScorchSun_PlantBasin(JSONNode _json)
		: base(_json)
	{
	}

	public WDP_ScorchSun_PlantBasin()
	{
	}

	public static WDP_ScorchSun_PlantBasin DeserializeWDP_ScorchSun_PlantBasin(JSONNode _json)
	{
		return new WDP_ScorchSun_PlantBasin(_json);
	}

	public override int GetTypeId()
	{
		return 456553141;
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
