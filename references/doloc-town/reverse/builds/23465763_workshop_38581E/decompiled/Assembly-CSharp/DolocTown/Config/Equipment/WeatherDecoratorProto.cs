using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public abstract class WeatherDecoratorProto : BeanBase
{
	public WeatherDecoratorProto(JSONNode _json)
	{
	}

	public WeatherDecoratorProto()
	{
	}

	public static WeatherDecoratorProto DeserializeWeatherDecoratorProto(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"WDP_AcidRain_Worker" => new WDP_AcidRain_Worker(_json), 
			"WDP_AcidRain_PowerGenerator" => new WDP_AcidRain_PowerGenerator(_json), 
			"WDP_AcidRain_PlantBasin" => new WDP_AcidRain_PlantBasin(_json), 
			"WDP_ScorchSun_Custom" => new WDP_ScorchSun_Custom(_json), 
			"WDP_ScorchSun_IWaterContainer" => new WDP_ScorchSun_IWaterContainer(_json), 
			"WDP_ScorchSun_PlantBasin" => new WDP_ScorchSun_PlantBasin(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ }";
	}
}
