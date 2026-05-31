using DolocTown.Config.Plant;
using Newtonsoft.Json;

namespace DolocTown;

public class CropGeneFunctionGrowUnchecked : CropGeneFunction
{
	private readonly CropGeneFuncProtoGrowUnchecked _func;

	public CropGeneFunctionGrowUnchecked(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoGrowUnchecked)geneProto.Function;
	}

	[JsonConstructor]
	public CropGeneFunctionGrowUnchecked(string geneId)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoGrowUnchecked)geneProto.Function;
		}
	}

	public override float HandleBuffData(float originValue)
	{
		return originValue + _func.GrowthIncrease;
	}
}
