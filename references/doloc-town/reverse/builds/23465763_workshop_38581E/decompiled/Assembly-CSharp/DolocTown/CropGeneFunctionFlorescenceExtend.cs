using DolocTown.Config.Plant;
using Newtonsoft.Json;

namespace DolocTown;

public class CropGeneFunctionFlorescenceExtend : CropGeneFunction
{
	private readonly CropGeneFuncProtoFlorescenceExtend _func;

	public override int MaxLifespan => base.crop.seedProto.Lifespan + _func.LifespanAddition;

	public CropGeneFunctionFlorescenceExtend(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoFlorescenceExtend)geneProto.Function;
	}

	[JsonConstructor]
	public CropGeneFunctionFlorescenceExtend(string geneId, int extraLifespan)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoFlorescenceExtend)geneProto.Function;
		}
	}

	public override void OnCreate()
	{
		base.OnCreate();
		base.crop.data.lifespan += _func.LifespanAddition;
	}

	public override float HandleBuffData(float originValue)
	{
		return originValue - _func.GrowthDecrease;
	}
}
