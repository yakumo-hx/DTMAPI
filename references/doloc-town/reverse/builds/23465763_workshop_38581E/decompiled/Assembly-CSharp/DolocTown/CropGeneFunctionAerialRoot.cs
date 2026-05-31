using Cysharp.Threading.Tasks;
using DolocTown.Config.Plant;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class CropGeneFunctionAerialRoot : CropGeneFunction
{
	public override bool IsNaturalMoist => !base.crop.plantBasin.CurrentRoom.IsInHouse;

	public CropGeneFunctionAerialRoot(CropGeneInfo geneProto)
		: base(geneProto)
	{
	}

	[JsonConstructor]
	public CropGeneFunctionAerialRoot(string geneId)
		: base(geneId)
	{
	}

	public override void UpdateEx(bool shouldRender)
	{
		base.UpdateEx(shouldRender);
		if (base.crop.plantBasin.CurrentRoom.IsInHouse)
		{
			return;
		}
		if (!base.crop.hasGrowed)
		{
			float num = base.crop.CropDecorator.functions.CalcGrowthValue() + base.crop.lastAddition + base.crop.lastFertilizerAddition;
			if (num > 0f)
			{
				base.crop._ApplyGrowth(num, shouldRender);
			}
		}
		if (shouldRender)
		{
			RaiseAirFlowEffect();
		}
	}

	private void RaiseAirFlowEffect()
	{
		UniTask.Delay(Mathf.RoundToInt(Random.Range(0.5f, 2f) * 1000f)).ContinueWith(delegate
		{
			if (base.crop.plantBasin.IsRender)
			{
				DolocAPI.RaiseInstantAnimEffects(base.crop.plantBasin.PositionCrop, InstAnimEffectType.AIRFLOW);
			}
		}).Forget();
	}
}
