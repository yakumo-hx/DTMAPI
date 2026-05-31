using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Resource;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;
using UnityEngine.Rendering;

namespace DolocTown;

public class DungeonResourceTree : DungeonResource, IDecalHost, IHasIndex
{
	protected bool isFalling;

	protected bool isWaitingForRemoving;

	private RoomScanner roomScanner => DolocAPI.gameStateManager.agentController.RoomScanner;

	public string Name => base.ResourceName;

	public DecalSlot[] ContainedSlots => base.Proto.ContainedSlots;

	public Dictionary<int, IDecal> AttachedDecals { get; set; } = new SerializedDictionary<int, IDecal>();


	public Sprite HostSprite => CurrentSprite;

	public Vector3 WorldPosition => base.Position;

	public DungeonResourceTree(IDungeonResourceHost host, ResourceInfo proto, Vector3 wp, Vector2Int anchor)
		: base(host, proto, wp, anchor)
	{
	}

	[JsonConstructor]
	protected DungeonResourceTree(int id, Vector2Int anchor, Vector3 position, string ResourceName, int GrowthLevel, int currentGrowth, int currentHealth, int skinIdx, int randomSeed)
		: base(id, anchor, position, ResourceName, GrowthLevel, currentGrowth, currentHealth, skinIdx, randomSeed)
	{
	}

	public override void OnRemove()
	{
		base.OnRemove();
		this.TakeOffAllDecals();
	}

	public override bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		if (isFalling)
		{
			return false;
		}
		return base.OnFell(tool, hitPoint);
	}

	protected override void _OnFell(ResourceFellData fellData)
	{
		RaiseRandomImpact(fellData.hitPoint);
		DolocAPI.Sound.PostSoundEvent(fellData.levelMatch ? SoundEvents.PLAY_RESOURCE_FELL : SoundEvents.PLAY_RESOURCE_FELL_ERROR);
		if (this.TakeOffAllDecals())
		{
			return;
		}
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
			Renderer.Shine(DolocAPI.eftConfig.dungeonResourceShineTime);
			Renderer.Shake(DolocAPI.eftConfig.dungeonResourceShakeTime);
			DolocAPI.effectProvider.RaiseInstPS(fellData.hitPoint, InstantParticleEffectsType.SAWDUST);
			if (base.currentLevel >= 3)
			{
				DolocAPI.effectProvider.RaiseInstPS(Renderer.position2d + new Vector2(0f, CurrentSprite.bounds.size.y * 0.75f), InstantParticleEffectsType.LEAVES);
			}
		}
		else
		{
			if (base.currentLevel >= 3)
			{
				DolocAPI.effectProvider.RaiseInstPS(Renderer.position2d + new Vector2(0f, CurrentSprite.bounds.size.y * 0.75f), InstantParticleEffectsType.BUNCH_OF_LEAVES);
			}
			DolocAPI.effectProvider.RaiseInstPS(fellData.hitPoint, InstantParticleEffectsType.SPARKS);
			DolocAPI.RaiseInstantPSEffects(Renderer.position, InstantParticleEffectsType.SMOKE_BRUST_02);
			GenerateDropItems(isRender: true, fellData.overrideSpawnLut, fellData.extraItems);
			OnCompleteFell();
		}
	}

	public override void OnTouch()
	{
		roomScanner.currentResource = this;
	}

	public override void OnDisTouch()
	{
		if (roomScanner.currentResource == this)
		{
			roomScanner.currentResource = null;
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
			Renderer.Shine(DolocAPI.eftConfig.dungeonResourceShineTime);
			DolocAPI.RaiseInstantAnimEffects(pos, InstAnimEffectType.HIT_SPARK, LocMaterials.GAME_MAT_EFFECTS_SHINE);
		}
	}

	public new void SetMaxHealth()
	{
		currentHealth = base.Proto.GetLevelData(base.currentLevel).MaxHealth;
	}

	protected override void _ClearEffects(Vector2 hitPosition)
	{
		if (base.currentLevel >= 3)
		{
			DolocAPI.effectProvider.RaiseInstPS(Renderer.position2d + new Vector2(0f, CurrentSprite.bounds.size.y * 0.75f), InstantParticleEffectsType.BUNCH_OF_LEAVES);
		}
		DolocAPI.effectProvider.RaiseInstPS(hitPosition, InstantParticleEffectsType.SPARKS);
		DolocAPI.RaiseInstantPSEffects(Renderer.position, InstantParticleEffectsType.SMOKE_BRUST_02);
	}

	public override void ShiftTerrainContent(Vector2Int offset, Vector3 positionOffset)
	{
		base.ShiftTerrainContent(offset, positionOffset);
		foreach (IDecal value in AttachedDecals.Values)
		{
			value.RefreshPosition();
		}
	}
}
