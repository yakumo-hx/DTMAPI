using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Weather;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;

namespace DolocTown;

public class WD_AcidRain_PowerGenerator : WeatherDecorator
{
	private WDP_AcidRain_PowerGenerator proto;

	[JsonProperty]
	[DebugInfo("恢复计数器", Color = "#ff4f4f")]
	protected readonly Counter counter;

	public override WeatherType WeatherType => WeatherType.ACID_RAIN;

	public sealed override bool EndOnWeatherStop => false;

	public WD_AcidRain_PowerGenerator(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
		: base(parent, proto)
	{
		this.proto = (WDP_AcidRain_PowerGenerator)proto;
		counter = new Counter(this.proto.DurationTu);
	}

	[JsonConstructor]
	public WD_AcidRain_PowerGenerator(Counter counter)
	{
		this.counter = counter;
	}

	public override void RetrieveProto(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
	{
		base.RetrieveProto(parent, proto);
		if (proto != null)
		{
			this.proto = (WDP_AcidRain_PowerGenerator)proto;
			counter.ValidateInterval(this.proto.DurationTu);
		}
	}

	public override void OnInteract()
	{
		if (proto.CouldInteract)
		{
			DolocAPI.ShowMessageBoxSmall(DolocConfig.StaticTexts.UiOperationErrEquipmentCorroded);
		}
	}

	protected void SetStateAsCorroded()
	{
		EquipmentStateRenderer stateRenderer = base.EquipmentRenderer.GetStateRenderer();
		stateRenderer.SetAsCorroded();
		stateRenderer.position2d = base.Equipment.PositionCenter;
	}

	public override void AfterResumeDecorator()
	{
		((PowerGenerator)base.Equipment).ShouldStop = true;
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

	public override void OnRender()
	{
		SetStateAsCorroded();
	}

	public override void OnUnRender()
	{
		base.EquipmentRenderer.RemoveStateRenderer();
	}

	public override void BeginNoRender()
	{
		((PowerGenerator)base.Equipment).ShouldStop = true;
		counter.Reset();
	}

	public override void Begin()
	{
		BeginNoRender();
		SetStateAsCorroded();
	}

	public override void EndNoRender()
	{
		((PowerGenerator)base.Equipment).ShouldStop = false;
	}

	public override void End()
	{
		EndNoRender();
		base.EquipmentRenderer.RemoveStateRenderer();
	}
}
