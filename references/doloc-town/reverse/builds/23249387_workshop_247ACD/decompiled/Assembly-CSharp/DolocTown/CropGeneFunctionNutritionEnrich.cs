using DolocTown.Config.Plant;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class CropGeneFunctionNutritionEnrich : CropGeneFunction
{
	private readonly CropGeneFuncProtoNutritionEnrich _func;

	[JsonProperty]
	private int startTU;

	public CropGeneFunctionNutritionEnrich(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoNutritionEnrich)geneProto.Function;
		startTU = DolocAPI.archiveHandle.DateNow.TotalTUs;
	}

	[JsonConstructor]
	public CropGeneFunctionNutritionEnrich(string geneId, int outputAddition, int startTU)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoNutritionEnrich)geneProto.Function;
			this.startTU = Mathf.Max(0, startTU);
		}
	}

	public override void AfterLoadData()
	{
		base.AfterLoadData();
		startTU = Mathf.Clamp(startTU, 0, DolocAPI.archiveHandle.DateNow.TotalTUs);
	}

	public override CropOutputData HandleOutputData(CropOutputData outputData, RangedItem item)
	{
		float num = DolocAPI.archiveHandle.DateNow.TotalTUs - startTU;
		float totalGrowthValue = base.crop.TotalGrowthValue;
		float num2 = num / totalGrowthValue * _func.CropOutputAddition;
		int minInclusive = Mathf.FloorToInt((float)item.minCount * num2);
		int num3 = Mathf.FloorToInt((float)item.maxCount * num2);
		outputData.finalCountAddition += Random.Range(minInclusive, num3 + 1);
		return outputData;
	}
}
