using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.ScorchSun;

public class WD_ScorchSun_IWaterContainer : WD_ScorchSun
{
	private WDP_ScorchSun_IWaterContainer protoIWaterContainer;

	private IWaterContainer _waterContainer;

	private bool shouldWork;

	public override bool CallOriginWhileNoOverride => true;

	public WD_ScorchSun_IWaterContainer(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
		: base(parent, proto)
	{
		protoIWaterContainer = (WDP_ScorchSun_IWaterContainer)proto;
		_waterContainer = base.Equipment as IWaterContainer;
		shouldWork = _waterContainer != null;
		if (!shouldWork)
		{
			Debug.LogWarning("WD_ScorchSun_IWaterContainer:设备\"" + base.Equipment.proto.Id + "\"类型不符");
		}
	}

	[JsonConstructor]
	public WD_ScorchSun_IWaterContainer()
	{
	}

	public override void RetrieveProto(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
	{
		base.RetrieveProto(parent, proto);
		if (proto is WDP_ScorchSun_IWaterContainer wDP_ScorchSun_IWaterContainer)
		{
			protoIWaterContainer = wDP_ScorchSun_IWaterContainer;
		}
		_waterContainer = base.Equipment as IWaterContainer;
		shouldWork = _waterContainer != null;
	}

	public override void Update(bool inProgress)
	{
		if (shouldWork && inProgress)
		{
			_waterContainer.Evaporation(protoIWaterContainer.Evaporation, shouldRender: true);
		}
	}

	public override void UpdateNoRender(bool inProgress)
	{
		if (shouldWork && inProgress)
		{
			_waterContainer.Evaporation(protoIWaterContainer.Evaporation);
		}
	}
}
