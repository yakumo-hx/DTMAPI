using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class WDP_ScorchSun_IWaterContainer : WeatherDecoratorProto
{
	public const int __ID__ = -1829768148;

	public int Evaporation { get; private set; }

	public WDP_ScorchSun_IWaterContainer(JSONNode _json)
		: base(_json)
	{
		if (!_json["evaporation"].IsNumber)
		{
			throw new SerializationException();
		}
		Evaporation = _json["evaporation"];
	}

	public WDP_ScorchSun_IWaterContainer(int evaporation)
	{
		Evaporation = evaporation;
	}

	public static WDP_ScorchSun_IWaterContainer DeserializeWDP_ScorchSun_IWaterContainer(JSONNode _json)
	{
		return new WDP_ScorchSun_IWaterContainer(_json);
	}

	public override int GetTypeId()
	{
		return -1829768148;
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
		return "{ Evaporation:" + Evaporation + ",}";
	}
}
