using System.Linq;
using DolocTown.Config.Plant;
using DolocTown.Config.Weather;
using Newtonsoft.Json;
using RedSaw;

namespace DolocTown;

public class CropGeneFunctionWindSow : CropGeneFunction
{
	private readonly CropGeneFuncProtoWindSow _func;

	private CropEnv _env;

	[JsonProperty]
	private readonly Counter counter;

	public CropGeneFunctionWindSow(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoWindSow)geneProto.Function;
		counter = new Counter(_func.SowInterval);
	}

	[JsonConstructor]
	protected CropGeneFunctionWindSow(string geneId, Counter counter)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoWindSow)geneProto.Function;
			this.counter = counter ?? new Counter(_func.SowInterval);
			this.counter.ValidateInterval(_func.SowInterval);
		}
	}

	public override void OnCreate()
	{
		base.OnCreate();
		_env = new CropEnv(base.crop.plantBasin, _func.HorizontalRange, _func.VerticalRangeTop, _func.VerticalRangeBottom);
	}

	public override void AfterLoadData()
	{
		base.AfterLoadData();
		_env = new CropEnv(base.crop.plantBasin, _func.HorizontalRange, _func.VerticalRangeTop, _func.VerticalRangeBottom);
	}

	public override void UpdateEx(bool shouldRender)
	{
		if (base.crop.isMature && counter.Tick() && base.CurrentRoom.CurrentWeatherInfo.Id == WeatherType.WINDY)
		{
			PlantBasin[] array = _env.AroundEmptyBasins.Where((PlantBasin x) => x.SeedTypeInfo.Id == base.SeedProto.SeedType).ToArray();
			if (array.Length != 0)
			{
				array.Choice().Plant(base.crop.plantBasin.currentSeed, shouldRender, sendMessage: false, shouldRender);
			}
		}
	}
}
