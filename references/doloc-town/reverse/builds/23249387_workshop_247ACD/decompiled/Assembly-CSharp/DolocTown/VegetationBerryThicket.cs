using DolocTown.Config;
using DolocTown.Config.Resource;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class VegetationBerryThicket : VegetationGrow
{
	public override bool OnlyTouch => base.currentLevel < base.maxLevel;

	public VegetationBerryThicket(VegetationInfo proto, Vector2Int anchor, Vector3 position, Vector2Int[] cvPositions)
		: base(proto, anchor, position, cvPositions)
	{
	}

	[JsonConstructor]
	public VegetationBerryThicket(int index, Vector2Int anchor, Vector3 position, string vegetationName, int currentLevel, int currentGrowth, int randomSeed)
		: base(index, anchor, position, vegetationName, currentLevel, currentGrowth, randomSeed)
	{
	}

	public override void OnTouch()
	{
		if (base.currentLevel >= base.maxLevel)
		{
			base.Renderer.ShowOutline = true;
			ShowTip(DolocConfig.StaticTexts.UiOperationHarvest, DolocAPI.UserInput.GlobalInteractActionName);
		}
	}

	public override void OnDisTouch()
	{
		base.Renderer.ShowOutline = false;
		HideTip();
	}

	public override void OnInteract()
	{
		if (base.currentLevel >= base.maxLevel)
		{
			DolocAPI.agent._Interact(delegate
			{
				ReGrow();
				DolocAPI.RaiseInstantPSEffects(base.Renderer.position2d, InstantParticleEffectsType.WEEDS_LARGE);
				GenerateDropItems();
				SendGatherMessage();
				PushTipToDisappear();
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_COLLECT_BERRY);
			});
		}
	}

	public override void OnBomb(float damage, bool ctr, Vector2 pos)
	{
		PushTipToDisappear();
		DolocAPI.RaiseInstantPSEffects(base.Renderer.position2d, InstantParticleEffectsType.WEEDS_LARGE);
		base.OnBomb(damage, ctr, pos);
	}

	public override void OnMonsterTouch(Vector2 pos)
	{
		if (RandomUtils.Dice(0.3f))
		{
			DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.WEEDS);
		}
	}
}
