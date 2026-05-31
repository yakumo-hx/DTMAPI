using DolocTown.Config.Plant;
using Newtonsoft.Json;

namespace DolocTown;

public class CropGeneFunctionProtoInfertility : CropGeneFunction
{
	private CropGeneFuncProtoInfertility _func => geneProto.Function as CropGeneFuncProtoInfertility;

	public CropGeneFunctionProtoInfertility(CropGeneInfo geneProto)
		: base(geneProto)
	{
	}

	[JsonConstructor]
	public CropGeneFunctionProtoInfertility(string geneId)
		: base(geneId)
	{
	}
}
