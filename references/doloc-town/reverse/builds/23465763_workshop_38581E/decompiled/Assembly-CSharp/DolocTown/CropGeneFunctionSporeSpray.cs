using DolocTown.Config.Plant;
using Newtonsoft.Json;

namespace DolocTown;

public class CropGeneFunctionSporeSpray : CropGeneFunction
{
	public CropGeneFunctionSporeSpray(CropGeneInfo geneProto)
		: base(geneProto)
	{
	}

	[JsonConstructor]
	public CropGeneFunctionSporeSpray(string geneId)
		: base(geneId)
	{
	}

	public override void OnCropMature(bool shouldRender)
	{
		base.OnCropMature(shouldRender);
		base.crop.plantBasin.Harvest();
	}
}
