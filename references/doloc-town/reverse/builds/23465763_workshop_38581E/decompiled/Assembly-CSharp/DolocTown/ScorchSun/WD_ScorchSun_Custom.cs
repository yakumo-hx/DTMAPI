using DolocTown.Config.Equipment;
using DolocTown.Config.Weather;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.ScorchSun;

public class WD_ScorchSun_Custom : WeatherDecorator
{
	private WDP_ScorchSun_Custom protoCustom;

	public override WeatherType WeatherType => WeatherType.SCORCH_SUN;

	public WD_ScorchSun_Custom(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
		: base(parent, proto)
	{
		protoCustom = (WDP_ScorchSun_Custom)proto;
	}

	[JsonConstructor]
	public WD_ScorchSun_Custom()
	{
	}

	public override void RetrieveProto(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
	{
		base.RetrieveProto(parent, proto);
		if (proto is WDP_ScorchSun_Custom wDP_ScorchSun_Custom)
		{
			protoCustom = wDP_ScorchSun_Custom;
		}
	}

	public override void Update(bool inProgress)
	{
		Debug.Log("ScorchSun Update");
	}
}
