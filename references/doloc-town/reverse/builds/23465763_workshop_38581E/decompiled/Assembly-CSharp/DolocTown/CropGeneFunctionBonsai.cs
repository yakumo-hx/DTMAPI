using DolocTown.Config.Plant;
using Newtonsoft.Json;

namespace DolocTown;

public class CropGeneFunctionBonsai : CropGeneFunction
{
	private readonly CropGeneFuncProtoBonsai _func;

	public override int MoodContribution
	{
		get
		{
			if (base.crop.isDead)
			{
				return 0;
			}
			if (base.crop.CurrentLevel < _func.LevelThreshold)
			{
				return 0;
			}
			return _func.MoodContribution;
		}
	}

	public CropGeneFunctionBonsai(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoBonsai)geneProto.Function;
	}

	[JsonConstructor]
	public CropGeneFunctionBonsai(string geneId)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoBonsai)geneProto.Function;
		}
	}
}
