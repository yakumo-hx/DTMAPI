using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class WDP_ScorchSun_Custom : WeatherDecoratorProto
{
	public const int __ID__ = -2103028456;

	public WDP_ScorchSun_Custom(JSONNode _json)
		: base(_json)
	{
	}

	public WDP_ScorchSun_Custom()
	{
	}

	public static WDP_ScorchSun_Custom DeserializeWDP_ScorchSun_Custom(JSONNode _json)
	{
		return new WDP_ScorchSun_Custom(_json);
	}

	public override int GetTypeId()
	{
		return -2103028456;
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
