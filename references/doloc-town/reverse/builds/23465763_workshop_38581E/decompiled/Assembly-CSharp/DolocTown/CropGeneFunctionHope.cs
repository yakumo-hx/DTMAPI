using Cysharp.Threading.Tasks;
using DolocTown.Config.Plant;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class CropGeneFunctionHope : CropGeneFunction
{
	public CropGeneFunctionHope(CropGeneInfo geneProto)
		: base(geneProto)
	{
	}

	[JsonConstructor]
	public CropGeneFunctionHope(string geneId)
		: base(geneId)
	{
	}

	private void DropSeed(bool shouldRender)
	{
		if (base.crop.plantBasin.currentSeed == null)
		{
			Debug.LogError("基因「希望」无法生成种子，因为当前种子为空");
			return;
		}
		Item seedItem = base.crop.plantBasin.currentSeed.Clone();
		PlantBasin plantBasin = base.crop.plantBasin;
		Vector3 centerPosition = plantBasin.PositionCenter;
		UniTask.Delay(200).ContinueWith(delegate
		{
			if (plantBasin.IsRender)
			{
				DolocAPI.RaiseInstantPSEffects(centerPosition, InstantParticleEffectsType.BRUST_STARS);
			}
			plantBasin.CreateDropItem(seedItem, shouldRender, sendMessage: false);
		}).Forget();
	}

	public override void AfterHarvest(bool shouldRender)
	{
		base.AfterHarvest(shouldRender);
		if (base.crop.data.lifespan == 1)
		{
			DropSeed(shouldRender);
		}
	}

	public override void AfterClearWither(bool shouldRender)
	{
		base.AfterClearWither(shouldRender);
		DropSeed(shouldRender);
	}
}
