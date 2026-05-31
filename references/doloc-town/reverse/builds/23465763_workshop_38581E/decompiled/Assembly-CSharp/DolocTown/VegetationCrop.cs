using DolocTown.Config;
using DolocTown.Config.Resource;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class VegetationCrop : VegetationGrow
{
	public override bool OnlyTouch => false;

	public VegetationCrop(VegetationInfo proto, Vector2Int anchor, Vector3 position, Vector2Int[] cvPositions)
		: base(proto, anchor, position, cvPositions)
	{
	}

	[JsonConstructor]
	public VegetationCrop(int index, Vector2Int anchor, Vector3 position, string vegetationName, int currentLevel, int currentGrowth, int randomSeed)
		: base(index, anchor, position, vegetationName, currentLevel, currentGrowth, randomSeed)
	{
	}

	public override void OnTouch()
	{
		base.OnTouch();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_ENTER_WEEDS);
		if (base.currentLevel == base.maxLevel)
		{
			ShowTip(DolocConfig.StaticTexts.UiOperationHarvest, DolocAPI.UserInput.GlobalInteractActionName);
		}
	}

	public override void OnDisTouch()
	{
		base.OnDisTouch();
		HideTip();
	}

	public override void OnInteract()
	{
		if (base.currentLevel >= base.maxLevel)
		{
			DolocAPI.agent._Interact(delegate
			{
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_HARVEST);
				DolocAPI.RaiseInstantPSEffects(base.Position + new Vector3(0f, 1.5f), InstantParticleEffectsType.BUNCH_OF_LEAVES);
				GenerateDropItems();
				SendGatherMessage();
				PushTipToDisappear();
				base.Host.RemoveVegetation(this);
			});
		}
	}

	public override void OnMonsterTouch(Vector2 pos)
	{
		base.OnMonsterTouch(pos);
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.WEEDS);
	}

	public override void OnMonsterDisTouch(Vector2 pos)
	{
		base.OnMonsterDisTouch(pos);
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.WEEDS);
	}

	public override void OnBomb(float damage, bool ctr, Vector2 pos)
	{
		if (base.currentLevel == base.maxLevel)
		{
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_RESOURCE_HARVEST);
			DolocAPI.RaiseInstantPSEffects(base.Position + new Vector3(0f, 1.5f), InstantParticleEffectsType.BUNCH_OF_LEAVES);
			GenerateDropItems();
		}
		base.Host.RemoveVegetation(this);
	}
}
