using DolocTown.Config;
using DolocTown.Config.Resource;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class DungeonResourceBuildingsGarbage : DungeonResource
{
	public DungeonResourceBuildingsGarbage(IDungeonResourceHost host, ResourceInfo proto, Vector3 wp, Vector2Int anchor)
		: base(host, proto, wp, anchor)
	{
	}

	[JsonConstructor]
	protected DungeonResourceBuildingsGarbage(int id, Vector2Int anchor, Vector3 position, string ResourceName, int GrowthLevel, int currentGrowth, int currentHealth, int skinIdx, int randomSeed)
		: base(id, anchor, position, ResourceName, GrowthLevel, currentGrowth, currentHealth, skinIdx, randomSeed)
	{
	}

	protected override void _OnFell(ResourceFellData fellData)
	{
		Vector2 vector = GeometryUtils.CalcIndicatePos(Renderer.Sr, DolocAPI.eftConfig.dungeonResourceInstPsYRate);
		RaiseRandomImpact(fellData.hitPoint);
		if (Random.value > 0.5f)
		{
			DolocAPI.effectProvider.RaiseInstPS(vector, InstantParticleEffectsType.SPARKS);
		}
		DolocAPI.RaiseInstantAnimEffects(vector, InstAnimEffectType.HIT_SPARK, LocMaterials.GAME_MAT_EFFECTS_SHINE);
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
		}
		else
		{
			DolocAPI.effectProvider.RaiseInstPS(vector, InstantParticleEffectsType.MACHINE_LARGE);
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
		DolocAPI.effectProvider.RaiseInstPS(hitPosition, InstantParticleEffectsType.MACHINE_LARGE);
	}
}
