using DolocTown.Config.Plant;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;

namespace DolocTown;

public class CropGeneFunctionHanabi : CropGeneFunction
{
	private readonly CropGeneFuncProtoHanabi _func;

	[JsonProperty]
	private bool isHanabiActived;

	[JsonProperty]
	private int hanabiActivedCount;

	public CropGeneFunctionHanabi(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoHanabi)geneProto.Function;
	}

	[JsonConstructor]
	public CropGeneFunctionHanabi(string geneId, bool isHanabiActived)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoHanabi)geneProto.Function;
			this.isHanabiActived = isHanabiActived;
		}
	}

	public bool IsHanabiTime()
	{
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		if (dateNow.Hour >= _func.StartTime)
		{
			return dateNow.Hour < _func.EndTime;
		}
		return false;
	}

	public override void UpdateEx(bool shouldRender)
	{
		base.UpdateEx(shouldRender);
		if (!base.crop.isMature)
		{
			return;
		}
		if (isHanabiActived)
		{
			if (!IsHanabiTime())
			{
				isHanabiActived = false;
			}
		}
		else if (IsHanabiTime())
		{
			isHanabiActived = true;
			hanabiActivedCount++;
		}
	}

	public override CropOutputData HandleOutputData(CropOutputData outputData, RangedItem item)
	{
		if (IsHanabiTime() && hanabiActivedCount <= 1 && RandomUtils.Dice(_func.Probability))
		{
			outputData.finalCountAddition += _func.CropOutputAddition;
			return outputData;
		}
		return outputData;
	}
}
