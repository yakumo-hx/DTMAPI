using DolocTown.Config;
using DolocTown.Config.Resource;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class DungeonResourceTreeTrunk : DungeonResource
{
	public DungeonResourceTreeTrunk(IDungeonResourceHost host, ResourceInfo proto, Vector3 wp, Vector2Int anchor)
		: base(host, proto, wp, anchor)
	{
	}

	[JsonConstructor]
	protected DungeonResourceTreeTrunk(int id, Vector2Int anchor, Vector3 position, string ResourceName, int GrowthLevel, int currentGrowth, int currentHealth, int skinIdx, int randomSeed)
		: base(id, anchor, position, ResourceName, GrowthLevel, currentGrowth, currentHealth, skinIdx, randomSeed)
	{
	}

	protected override void _OnFell(ResourceFellData fellData)
	{
		RaiseRandomImpact(fellData.hitPoint);
		DolocAPI.Sound.PostSoundEvent(fellData.levelMatch ? SoundEvents.PLAY_RESOURCE_FELL : SoundEvents.PLAY_RESOURCE_FELL_ERROR);
		if (!fellData.levelMatch)
		{
			if (fellData.shouldCounterBack)
			{
				DolocAPI.agent.StateManager.Overwrite<AgentStateHit>();
			}
			if (fellData.shouldRaiseToolTip)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrLowToolLevel);
			}
			Renderer.Shine(DolocAPI.eftConfig.dungeonResourceShineTime);
		}
		else if (fellData.Damage < currentHealth)
		{
			currentHealth -= fellData.Damage;
			Renderer.Shake(DolocAPI.eftConfig.dungeonResourceShakeTime);
			Renderer.Shine(DolocAPI.eftConfig.dungeonResourceShineTime);
			DolocAPI.effectProvider.RaiseInstPS(fellData.hitPoint, InstantParticleEffectsType.SAWDUST);
		}
		else
		{
			DolocAPI.effectProvider.RaiseInstPS(base.PositionCenter, InstantParticleEffectsType.SPARKS);
			GenerateDropItems(isRender: true, fellData.overrideSpawnLut, fellData.extraItems);
			OnCompleteFell();
		}
	}

	protected override void _ClearEffects(Vector2 hitPosition)
	{
		DolocAPI.effectProvider.RaiseInstPS(hitPosition, InstantParticleEffectsType.SPARKS);
	}

	public override void OnBomb(float damage, bool criticalRate, Vector2 pos)
	{
		if (criticalRate)
		{
			damage *= 2f;
		}
		damage = Mathf.Max(1f, damage);
		currentHealth -= (int)damage;
		if (currentHealth <= 0)
		{
			GenerateDropItems(isRender: true, null, null);
			OnCompleteFell();
		}
		else
		{
			Renderer.Shine(DolocAPI.eftConfig.dungeonResourceShineTime);
			DolocAPI.RaiseInstantAnimEffects(pos, InstAnimEffectType.HIT_SPARK, LocMaterials.GAME_MAT_EFFECTS_SHINE);
		}
	}
}
