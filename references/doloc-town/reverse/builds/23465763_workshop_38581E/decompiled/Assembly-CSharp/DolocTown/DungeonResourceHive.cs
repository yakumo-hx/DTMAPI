using DolocTown.Config;
using DolocTown.Config.Resource;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DungeonResourceHive : DungeonResource
{
	[JsonProperty]
	private int leftBees;

	public DungeonResourceHive(IDungeonResourceHost host, ResourceInfo proto, Vector3 wp, Vector2Int anchor)
		: base(host, proto, wp, anchor)
	{
		leftBees = Random.Range(3, 5);
	}

	[JsonConstructor]
	protected DungeonResourceHive(int id, Vector2Int anchor, Vector3 position, string ResourceName, int GrowthLevel, int currentGrowth, int currentHealth, int skinIdx, int randomSeed, int leftBees)
		: base(id, anchor, position, ResourceName, GrowthLevel, currentGrowth, currentHealth, skinIdx, randomSeed)
	{
		this.leftBees = leftBees;
	}

	private void InvokeCounterBack(int count = 1)
	{
		if (Renderer == null)
		{
			return;
		}
		if (leftBees <= 0)
		{
			if (Renderer != null && RandomUtils.Dice(0.2f))
			{
				DolocAPI.RaiseEmotion(Renderer.transform, EmotionName.CONFUSE);
			}
			return;
		}
		count = Mathf.Min(count, leftBees);
		leftBees -= count;
		IMonsterHost currentRoom = base.Host.CurrentRoom;
		if (currentRoom == null)
		{
			return;
		}
		Vector2 vector = Renderer.position2d + new Vector2(0f, 3f);
		if (DolocAPI.assets.monsters.QueryMonster("bee", out var proto))
		{
			for (int i = 0; i < count; i++)
			{
				currentRoom.GenerateMonster(proto, vector + Random.insideUnitCircle);
			}
		}
	}

	protected override void _OnFell(ResourceFellData fellData)
	{
		Vector2 vector = GeometryUtils.CalcIndicatePos(Renderer.Sr, DolocAPI.eftConfig.dungeonResourceInstPsYRate);
		RaiseRandomImpact(vector);
		if (Random.value > 0.5f)
		{
			DolocAPI.effectProvider.RaiseInstPS(vector, InstantParticleEffectsType.SPARKS);
		}
		DolocAPI.RaiseInstantAnimEffects(vector, InstAnimEffectType.HIT_SPARK, LocMaterials.GAME_MAT_EFFECTS_SHINE);
		DolocAPI.Sound.PostSoundEvent(fellData.levelMatch ? SoundEvents.PLAY_GARBAGE_COLLECT : SoundEvents.PLAY_RESOURCE_GATHER_ERROR);
		InvokeCounterBack(Random.Range(2, 3));
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
			DolocAPI.RaiseInstantPSEffects(Renderer.position2d, InstantParticleEffectsType.SMOKE_BRUST_02);
			GenerateDropItems(isRender: true, fellData.overrideSpawnLut, fellData.extraItems);
			OnCompleteFell();
		}
	}

	public override void OnBomb(float damage, bool criticalRate, Vector2 pos)
	{
		if (currentHealth > 0)
		{
			InvokeCounterBack(Random.Range(2, 3));
			currentHealth -= (int)damage;
			if (currentHealth <= 0)
			{
				DolocAPI.effectProvider.RaiseInstPS(pos, InstantParticleEffectsType.MACHINE_LARGE);
				DolocAPI.RaiseInstantPSEffects(Renderer.position2d, InstantParticleEffectsType.SMOKE_BRUST_02);
				GenerateDropItems(isRender: true, null, null);
				OnCompleteFell();
			}
		}
	}
}
