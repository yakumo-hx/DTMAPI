using Cysharp.Threading.Tasks;
using DolocTown.Config.Plant;
using DolocTown.Config.Weather;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class CropGeneFunctionCharge : CropGeneFunction
{
	private readonly Counter _counter;

	public CropGeneFunctionCharge(CropGeneInfo geneProto)
		: base(geneProto)
	{
	}

	[JsonConstructor]
	protected CropGeneFunctionCharge(string geneId)
		: base(geneId)
	{
	}

	public override void UpdateEx(bool shouldRender)
	{
		if (!shouldRender || base.CurrentRoom.CurrentWeatherInfo.Id != WeatherType.THUNDERSTORM || !RandomUtils.Dice(0.3f) || base.crop.CurrentLevel == 0)
		{
			return;
		}
		Vector3 centerPosition = base.crop.Renderer.PositionCenter;
		UniTask.Delay(Mathf.RoundToInt(Random.Range(0.5f, 1.5f) * 1000f)).ContinueWith(delegate
		{
			if (base.crop.Renderer != null)
			{
				DolocAPI.RaiseInstantAnimEffects(centerPosition, InstAnimEffectType.ELECTRIC_ARC, LocMaterials.GAME_MAT_EFFECTS_SHINE);
			}
		}).Forget();
	}

	public override void Thunder(bool shouldRender)
	{
		if (!base.crop._ThunderEvent_MatureEndYam(shouldRender) && !base.Data.isDead && base.crop._GrowForward(shouldClearGrowth: true) && shouldRender)
		{
			DolocAPI.RaiseInstantAnimEffects(base.Renderer.position, InstAnimEffectType.ELECTRIC_CURRENT, LocMaterials.GAME_MAT_THUNDER);
			base.crop.UpdateRenderer(animateGrow: true);
		}
	}
}
