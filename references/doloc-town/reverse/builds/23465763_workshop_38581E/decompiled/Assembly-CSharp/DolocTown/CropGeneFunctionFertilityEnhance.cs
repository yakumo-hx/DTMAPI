using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class CropGeneFunctionFertilityEnhance : CropGeneFunction
{
	private readonly CropGeneFuncProtoFertilityEnhance _func;

	public CropGeneFunctionFertilityEnhance(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoFertilityEnhance)geneProto.Function;
	}

	[JsonConstructor]
	public CropGeneFunctionFertilityEnhance(string geneId)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoFertilityEnhance)geneProto.Function;
		}
	}

	public override void AfterHarvest(bool shouldRender)
	{
		base.AfterHarvest(shouldRender);
		Item item = DolocAPI.GenerateItem(_func.FertilizerItem);
		if (item == null)
		{
			Debug.LogWarning("[CropGeneFunctionFertilityEnhance] Invalid FertilizerItem: " + _func.FertilizerItem);
			return;
		}
		ItemFertilizer itemFertilizer = (ItemFertilizer)item;
		ItemFunctionFertilizer itemFunctionFertilizer = (ItemFunctionFertilizer)itemFertilizer.proto.Function;
		base.crop.plantBasin.Fertilizer(itemFunctionFertilizer.Duration, itemFunctionFertilizer.Addition, itemFertilizer.proto, shouldRender);
	}
}
