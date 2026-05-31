using DolocTown.Config.Plant;
using Newtonsoft.Json;
using RedSaw;

namespace DolocTown;

public class CropGeneFunctionParasite : CropGeneFunction
{
	private readonly CropGeneFuncProtoParasite _func;

	private CropEnv _env;

	[JsonProperty]
	private Counter counter;

	public CropGeneFunctionParasite(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoParasite)geneProto.Function;
		counter = new Counter(_func.StealInterval);
	}

	[JsonConstructor]
	public CropGeneFunctionParasite(string geneId)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoParasite)geneProto.Function;
			counter = new Counter(_func.StealInterval);
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
		if (!counter.Tick() || base.crop.isDead)
		{
			return;
		}
		foreach (Crop aroundCrop in _env.AroundCrops)
		{
			float num = aroundCrop.TakeGrowthValue(_func.GrowthStolenValue);
			if (num > 0f)
			{
				base.crop._ApplyGrowth(num, shouldRender);
			}
		}
	}
}
