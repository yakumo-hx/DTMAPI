using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class WDP_AcidRain_PlantBasin : WeatherDecoratorProto
{
	public const int __ID__ = -607526912;

	public WDP_AcidRain_PlantBasin(JSONNode _json)
		: base(_json)
	{
	}

	public WDP_AcidRain_PlantBasin()
	{
	}

	public static WDP_AcidRain_PlantBasin DeserializeWDP_AcidRain_PlantBasin(JSONNode _json)
	{
		return new WDP_AcidRain_PlantBasin(_json);
	}

	public override int GetTypeId()
	{
		return -607526912;
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
