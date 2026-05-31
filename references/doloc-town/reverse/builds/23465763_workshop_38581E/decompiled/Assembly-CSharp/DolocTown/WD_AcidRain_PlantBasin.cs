using DolocTown.Config.Equipment;
using DolocTown.Config.Weather;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class WD_AcidRain_PlantBasin : WeatherDecorator
{
	private WDP_AcidRain_PlantBasin proto;

	private PlantBasin plantBasin;

	public override WeatherType WeatherType => WeatherType.ACID_RAIN;

	public override bool CallOriginWhileNoOverride => true;

	public WD_AcidRain_PlantBasin(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
		: base(parent, proto)
	{
		this.proto = (WDP_AcidRain_PlantBasin)proto;
		if (!(base.Equipment is PlantBasin plantBasin))
		{
			Debug.LogError("设备类型<" + base.Equipment.GetType().Name + ">与天气装饰器<" + GetType().Name + ">类型不匹配");
		}
		else
		{
			this.plantBasin = plantBasin;
		}
	}

	[JsonConstructor]
	public WD_AcidRain_PlantBasin()
	{
	}

	public override void RetrieveProto(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
	{
		base.RetrieveProto(parent, proto);
		if (proto is WDP_AcidRain_PlantBasin wDP_AcidRain_PlantBasin)
		{
			this.proto = wDP_AcidRain_PlantBasin;
		}
	}

	public override void AfterLoadData()
	{
		plantBasin = (PlantBasin)base.Equipment;
	}

	public override void Update(bool inProgress)
	{
		plantBasin.UpdateAcidRain();
	}

	public override void UpdateNoRender(bool inProgress)
	{
		plantBasin.UpdateAcidRainNoRender();
	}
}
