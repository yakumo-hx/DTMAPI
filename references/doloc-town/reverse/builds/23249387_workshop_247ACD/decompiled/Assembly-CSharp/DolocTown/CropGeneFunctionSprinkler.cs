using DolocTown.Config.Plant;
using Newtonsoft.Json;
using RedSaw;

namespace DolocTown;

public class CropGeneFunctionSprinkler : CropGeneFunction
{
	private readonly CropGeneFuncProtoSprinkler _func;

	private CropEnv _env;

	[JsonProperty]
	private Counter counter;

	public CropGeneFunctionSprinkler(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoSprinkler)geneProto.Function;
		counter = new Counter(_func.Interval);
	}

	[JsonConstructor]
	public CropGeneFunctionSprinkler(string geneId, Counter counter)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoSprinkler)geneProto.Function;
			this.counter = counter ?? new Counter(_func.Interval);
			this.counter.ValidateInterval(_func.Interval);
		}
	}

	public override void OnCreate()
	{
		base.OnCreate();
		_env = new CropEnv(base.crop.plantBasin, _func.HorizontalRange, _func.VerticalRangeTop, _func.VerticalRangeBottom);
	}

	public override void OnWater(bool shouldRender)
	{
		base.OnWater(shouldRender);
		Invoke(shouldRender);
	}

	public override void AfterLoadData()
	{
		base.AfterLoadData();
		_env = new CropEnv(base.crop.plantBasin, _func.HorizontalRange, _func.VerticalRangeTop, _func.VerticalRangeBottom);
	}

	public override void UpdateEx(bool shouldRender)
	{
		if (counter.Tick() && !base.crop.isDead && base.crop.isMoist)
		{
			Invoke(shouldRender);
		}
	}

	private void Invoke(bool shouldRender)
	{
		foreach (PlantBasin item in _env.ChooseAroundBasins((PlantBasin x) => x.HasCrop && !x.Crop.isDead && x.Crop != base.crop))
		{
			if (!base.crop.plantBasin.TryCostSupplyWater(_func.WaterCost))
			{
				break;
			}
			item.Water(_func.WaterAddition, shouldRender, sendMessage: false, invokeGeneCallback: false);
		}
	}
}
