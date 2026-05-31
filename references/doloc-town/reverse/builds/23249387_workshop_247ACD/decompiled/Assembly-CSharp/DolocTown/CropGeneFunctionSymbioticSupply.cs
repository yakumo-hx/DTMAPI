using DolocTown.Config.Plant;
using Newtonsoft.Json;

namespace DolocTown;

public class CropGeneFunctionSymbioticSupply : CropGeneFunction
{
	private readonly CropGeneFuncProtoSymbioticSupply _func;

	private CropEnv _env;

	public CropGeneFunctionSymbioticSupply(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoSymbioticSupply)geneProto.Function;
	}

	[JsonConstructor]
	public CropGeneFunctionSymbioticSupply(string geneId)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoSymbioticSupply)geneProto.Function;
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

	public override void AfterHarvest(bool shouldRender)
	{
		foreach (Crop aroundCrop in _env.AroundCrops)
		{
			aroundCrop._ApplyGrowth(_func.GrowthValue, shouldRender);
		}
	}
}
