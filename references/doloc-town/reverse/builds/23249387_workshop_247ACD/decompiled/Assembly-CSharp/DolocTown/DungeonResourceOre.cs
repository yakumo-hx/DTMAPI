using DolocTown.Config;
using DolocTown.Config.Resource;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class DungeonResourceOre : DungeonResource
{
	public DungeonResourceOre(IDungeonResourceHost host, ResourceInfo proto, Vector3 wp, Vector2Int anchor)
		: base(host, proto, wp, anchor)
	{
	}

	[JsonConstructor]
	protected DungeonResourceOre(int id, Vector2Int anchor, Vector3 position, string ResourceName, int GrowthLevel, int currentGrowth, int currentHealth, int skinIdx, int randomSeed)
		: base(id, anchor, position, ResourceName, GrowthLevel, currentGrowth, currentHealth, skinIdx, randomSeed)
	{
	}

	protected override void _OnFell(ResourceFellData fellData)
	{
		int num = fellData.Damage + DolocAPI.AgentEquipmentParams.fellCoundAdditionOre;
		RaiseRandomImpact(fellData.hitPoint);
		if (Random.value > 0.5f)
		{
			DolocAPI.effectProvider.RaiseInstPS(fellData.hitPoint, InstantParticleEffectsType.SPARKS);
		}
		DolocAPI.RaiseInstantAnimEffects(fellData.hitPoint, InstAnimEffectType.HIT_SPARK, LocMaterials.GAME_MAT_EFFECTS_SHINE);
		DolocAPI.Sound.PostSoundEvent(fellData.levelMatch ? SoundEvents.PLAY_RESOURCE_GATHER : SoundEvents.PLAY_RESOURCE_GATHER_ERROR);
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
		else if (num < currentHealth)
		{
			currentHealth -= num;
			Renderer.Shake(DolocAPI.eftConfig.dungeonResourceShakeTime);
			Renderer.Shine(DolocAPI.eftConfig.dungeonResourceShineTime);
			DolocAPI.effectProvider.RaiseInstPS(fellData.hitPoint, InstantParticleEffectsType.STONE_SHARDS);
		}
		else
		{
			DolocAPI.effectProvider.RaiseInstPS(fellData.hitPoint, InstantParticleEffectsType.STONE_SHARDS_LARGE);
			DolocAPI.RaiseInstantPSEffects(Renderer.position2d, InstantParticleEffectsType.SMOKE_BRUST_02);
			GenerateDropItems(isRender: true, fellData.overrideSpawnLut, fellData.extraItems);
			OnCompleteFell();
		}
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
			Renderer.Shine(0.1f);
			DolocAPI.RaiseInstantAnimEffects(pos, InstAnimEffectType.HIT_SPARK, LocMaterials.GAME_MAT_EFFECTS_SHINE);
		}
	}

	protected override void _ClearEffects(Vector2 hitPosition)
	{
		DolocAPI.effectProvider.RaiseInstPS(hitPosition, InstantParticleEffectsType.STONE_SHARDS_LARGE);
		DolocAPI.RaiseInstantPSEffects(Renderer.position2d, InstantParticleEffectsType.SMOKE_BRUST_02);
	}
}
