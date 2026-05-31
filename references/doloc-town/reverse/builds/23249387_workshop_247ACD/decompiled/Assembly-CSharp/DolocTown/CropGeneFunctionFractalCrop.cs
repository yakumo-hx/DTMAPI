using DolocTown.Config.Plant;
using Newtonsoft.Json;

namespace DolocTown;

public class CropGeneFunctionFractalCrop : CropGeneFunction
{
	private readonly CropGeneFuncProtoFractalCrop _func;

	public CropGeneFunctionFractalCrop(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoFractalCrop)geneProto.Function;
	}

	[JsonConstructor]
	protected CropGeneFunctionFractalCrop(string geneId)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoFractalCrop)geneProto.Function;
		}
	}

	public override CropOutputData HandleOutputData(CropOutputData outputData, RangedItem item)
	{
		outputData.finalCountMultiplication += _func.CropOutputAddition;
		return outputData;
	}
}
