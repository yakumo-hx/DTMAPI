using DolocTown.Config.Plant;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class CropGeneFunctionTimeGift : CropGeneFunction
{
	private readonly CropGeneFuncProtoTimeGift _func;

	[JsonProperty]
	private int countStartDay;

	[JsonProperty]
	private int cropOutputAddition;

	public CropGeneFunctionTimeGift(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoTimeGift)geneProto.Function;
		countStartDay = DolocAPI.archiveHandle.DateNow.TotalDays;
		cropOutputAddition = 0;
	}

	[JsonConstructor]
	public CropGeneFunctionTimeGift(string geneId, int countStartDay, int cropOutputAddition)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoTimeGift)geneProto.Function;
			this.countStartDay = countStartDay;
			this.cropOutputAddition = Mathf.Max(0, cropOutputAddition);
		}
	}

	public override void UpdateEx(bool shouldRender)
	{
		base.UpdateEx(shouldRender);
		int totalDays = DolocAPI.archiveHandle.DateNow.TotalDays;
		if (totalDays - countStartDay >= _func.DayThreshold)
		{
			countStartDay = totalDays;
			cropOutputAddition += _func.CropOutputAddition;
		}
	}

	public override CropOutputData HandleOutputData(CropOutputData outputData, RangedItem item)
	{
		outputData.finalCountAddition += cropOutputAddition;
		return outputData;
	}
}
