using DolocTown.Config.Plant;
using Newtonsoft.Json;

namespace DolocTown;

public class CropGeneFunctionRobustHealth : CropGeneFunction
{
	private readonly CropGeneFuncProtoRobustHealth _func;

	public CropGeneFunctionRobustHealth(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoRobustHealth)geneProto.Function;
	}

	[JsonConstructor]
	public CropGeneFunctionRobustHealth(string geneId)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoRobustHealth)geneProto.Function;
		}
	}

	public override bool CheckGrowthMonth(bool shouldRender)
	{
		return true;
	}

	public override float HandleBuffData(float originValue)
	{
		if (base.crop.plantBasin.currentSeed.CheckCurrentSeasonValid(ignoreGene: true))
		{
			return originValue;
		}
		return originValue - _func.GrowthDecrease;
	}
}
