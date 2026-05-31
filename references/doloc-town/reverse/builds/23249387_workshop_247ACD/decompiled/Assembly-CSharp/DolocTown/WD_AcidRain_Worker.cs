using DolocTown.Config.Equipment;
using DolocTown.Config.Weather;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;

namespace DolocTown;

public class WD_AcidRain_Worker : WeatherDecorator
{
	private WDP_AcidRain_Worker protoWorker;

	[JsonProperty]
	[DebugInfo("恢复计数器", Color = "#ff4f4f")]
	protected readonly Counter counter;

	public override WeatherType WeatherType => WeatherType.ACID_RAIN;

	public sealed override bool EndOnWeatherStop => false;

	public override bool CallOriginWhileNoOverride => true;

	public WD_AcidRain_Worker(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
		: base(parent, proto)
	{
		protoWorker = (WDP_AcidRain_Worker)proto;
		counter = new Counter(protoWorker.DurationTu);
	}

	[JsonConstructor]
	public WD_AcidRain_Worker(Counter counter)
	{
		this.counter = counter;
	}

	public override void RetrieveProto(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
	{
		base.RetrieveProto(parent, proto);
		if (proto != null)
		{
			protoWorker = (WDP_AcidRain_Worker)proto;
			counter.ValidateInterval(protoWorker.DurationTu);
		}
	}

	protected void SetStateAsCorroded()
	{
		EquipmentStateRenderer stateRenderer = base.EquipmentRenderer.GetStateRenderer();
		stateRenderer.SetAsCorroded();
		stateRenderer.position2d = base.Equipment.PositionCenter;
	}

	public override void Update(bool inProgress)
	{
		if (!inProgress && counter.Tick())
		{
			base.Equipment.decorator.RemoveDecorator();
		}
	}

	public override void UpdateNoRender(bool inProgress)
	{
		if (!inProgress && counter.Tick())
		{
			base.Equipment.decorator.RemoveDecoratorNoRender();
		}
	}

	public override void Begin()
	{
		counter.Reset();
		base.EquipmentRenderer.HideAllComponents();
		base.Equipment.HideSceneOperationTip();
		SetStateAsCorroded();
	}

	public override void BeginNoRender()
	{
		counter.Reset();
	}

	public override void OnRender()
	{
		SetStateAsCorroded();
	}

	public override void OnUnRender()
	{
		base.EquipmentRenderer.HideStateRenderer();
	}

	public override void End()
	{
		base.EquipmentRenderer.HideStateRenderer();
	}
}
