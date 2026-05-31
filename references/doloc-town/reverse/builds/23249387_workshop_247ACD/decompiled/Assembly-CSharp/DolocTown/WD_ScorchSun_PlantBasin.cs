using DolocTown.Config.Equipment;
using Newtonsoft.Json;

namespace DolocTown;

public class WD_ScorchSun_PlantBasin : WD_ScorchSun
{
	private PlantBasin _plantBasin;

	private bool shouldWork;

	public override bool CallOriginWhileNoOverride => true;

	public WD_ScorchSun_PlantBasin(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
		: base(parent, proto)
	{
		_plantBasin = parent.Equipment as PlantBasin;
		shouldWork = _plantBasin != null;
	}

	[JsonConstructor]
	protected WD_ScorchSun_PlantBasin()
	{
	}

	public override void RetrieveProto(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
	{
		base.RetrieveProto(parent, proto);
		_plantBasin = parent.Equipment as PlantBasin;
		shouldWork = _plantBasin != null;
	}

	public override void Begin()
	{
		if (_plantBasin != null && !_plantBasin.Supply.IsProtected)
		{
			_plantBasin.ClearSupplyWater(shouldRender: true);
		}
	}

	public override void BeginNoRender()
	{
		if (_plantBasin != null && !_plantBasin.Supply.IsProtected)
		{
			_plantBasin.ClearSupplyWater();
		}
	}

	public override void Update(bool inProgress)
	{
		if (shouldWork && inProgress)
		{
			_plantBasin.UpdateScorchSun();
		}
	}

	public override void UpdateNoRender(bool inProgress)
	{
		if (shouldWork && inProgress)
		{
			_plantBasin.UpdateScorchSunNoRender();
		}
	}
}
