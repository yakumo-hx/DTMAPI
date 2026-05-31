using DolocTown.Config.Plant;
using Newtonsoft.Json;
using RedSaw;

namespace DolocTown;

public class CropGeneFunctionImmortalJellyfish : CropGeneFunction
{
	private readonly CropGeneFuncProtoImmortalJellyfish _func;

	public CropGeneFunctionImmortalJellyfish(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoImmortalJellyfish)geneProto.Function;
	}

	[JsonConstructor]
	protected CropGeneFunctionImmortalJellyfish(string geneId, int immortalCount)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoImmortalJellyfish)geneProto.Function;
		}
	}

	private void Rebirth(bool shouldRender)
	{
		base.crop.data.currentLevel = 0;
		base.crop.data.currentGrowthValue = 0f;
		base.crop.data.isMature = false;
		OnCropLevelDown(shouldRender);
		if (shouldRender)
		{
			base.crop.UpdateRenderer();
		}
	}

	public override bool Regrow(bool shouldRender)
	{
		if (RandomUtils.Dice(_func.RebirthProbability))
		{
			Rebirth(shouldRender);
			return true;
		}
		if (!base.crop.OriginRegrow(shouldRender))
		{
			return false;
		}
		base.crop.CropDecorator.OnCropLevelDown(shouldRender);
		return true;
	}
}
