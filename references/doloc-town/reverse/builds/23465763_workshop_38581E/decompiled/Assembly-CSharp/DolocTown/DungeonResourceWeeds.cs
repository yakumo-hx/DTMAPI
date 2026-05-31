using DolocTown.Config;
using DolocTown.Config.Resource;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DungeonResourceWeeds : DungeonResource, IFeeder, IAnimalInteractable
{
	protected bool hasModerate;

	private int __animal_counter;

	public Room AnimalInteractableRoom => base.Host.CurrentRoom;

	public Vector2Int AnimalInteractablePosition => base.Anchor;

	public int AnimalInteractableWidth => base.Proto.Width;

	public Vector2 AnimalInteractablePositionWS => base.Position;

	public bool AnimalInteractableIsValid => base.index >= 0;

	public bool IsAnimalInteractableLocked { get; set; }

	public int FeederPriority => 1;

	public bool IsFeederEmpty
	{
		get
		{
			if (DolocConfig.Tables.TbHusbandryEnergy.IsHusbandryFeeds(base.Proto.Id, out var _))
			{
				return currentHealth <= 0;
			}
			return true;
		}
	}

	public int AnimalCounter
	{
		get
		{
			return __animal_counter;
		}
		set
		{
			__animal_counter = Mathf.Max(value, 0);
		}
	}

	public DungeonResourceWeeds(IDungeonResourceHost host, ResourceInfo proto, Vector3 wp, Vector2Int anchor)
		: base(host, proto, wp, anchor)
	{
	}

	[JsonConstructor]
	protected DungeonResourceWeeds(int id, Vector2Int anchor, Vector3 position, string ResourceName, int GrowthLevel, int currentGrowth, int currentHealth, int skinIdx, int randomSeed)
		: base(id, anchor, position, ResourceName, GrowthLevel, currentGrowth, currentHealth, skinIdx, randomSeed)
	{
	}

	protected override void OnRender()
	{
		base.OnRender();
		Renderer.Animator.runtimeAnimatorController = DolocAPI.GetAsset<RuntimeAnimatorController>(DolocGameAssets.GAME_ANIM_UNIVERSAL_PLANT);
	}

	protected override void _OnFell(ResourceFellData fellData)
	{
		Vector2 pos = GeometryUtils.CalcRandomIndicatePos(Renderer.Sr, Vector2.right, new Vector2(0.4f, 0.6f));
		RaiseRandomImpact(pos);
		DolocAPI.Sound.PostSoundEvent(fellData.levelMatch ? SoundEvents.PLAY_RESOURCE_HARVEST : SoundEvents.PLAY_RESOURCE_GATHER_ERROR);
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
			Renderer.Shake(DolocAPI.eftConfig.dungeonResourceShakeTime);
			return;
		}
		Vector2 positionWS = GeometryUtils.CalcIndicatePos(Renderer.Sr, DolocAPI.eftConfig.dungeonResourceInstPsYRate);
		if (fellData.Damage < currentHealth)
		{
			currentHealth -= fellData.Damage;
			Renderer.Shake(DolocAPI.eftConfig.dungeonResourceShakeTime);
			Renderer.Shine(DolocAPI.eftConfig.dungeonResourceShineTime);
			DolocAPI.effectProvider.RaiseInstPS(positionWS, InstantParticleEffectsType.WEEDS);
			return;
		}
		DolocAPI.RaiseInstantPSEffects(base.PositionCenter, (base.currentLevel >= 2) ? InstantParticleEffectsType.LEAVES : InstantParticleEffectsType.LEAVES_AND_SOILS);
		if (base.currentLevel == base.Proto.MaxLevel)
		{
			DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.BUNCH_OF_LEAVES);
		}
		GenerateDropItems(isRender: true, fellData.overrideSpawnLut, fellData.extraItems);
		OnCompleteFell();
	}

	public override void OnTouch()
	{
		base.OnTouch();
		if (base.currentLevel > 0 && !hasModerate)
		{
			hasModerate = true;
			DolocAPI.AbilitySystem.motionAbility.ComposeEnvModerate(1);
		}
		Renderer.SwingOnTouch();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_ENTER_WEEDS);
		if (base.currentLevel >= 1 && RandomUtils.Dice(0.3f))
		{
			Vector2 position2d = Renderer.position2d;
			position2d.y += Renderer.Sr.sprite.bounds.size.y * 0.5f;
			DolocAPI.RaiseInstantPSEffects(position2d, InstantParticleEffectsType.WEEDS);
		}
	}

	public override void OnDisTouch()
	{
		base.OnDisTouch();
		Renderer.SwingOnTouch();
		if (hasModerate)
		{
			hasModerate = false;
			DolocAPI.AbilitySystem.motionAbility.ComposeEnvModerate(-1);
		}
	}

	public override void OnWater()
	{
		DolocAPI.effectProvider.RaiseInstAnim(Renderer.position, InstAnimEffectType.WATER_LARGE);
	}

	public override void OnMonsterTouch(Vector2 pos)
	{
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.WEEDS);
		Renderer.SwingOnTouch();
	}

	public override void OnMonsterDisTouch(Vector2 pos)
	{
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.WEEDS);
		Renderer.SwingOnTouch();
	}

	public override void OnWindBlow(Vector2 pos)
	{
		Renderer.SwingOnBlow();
	}

	public override void OnBomb(float damage, bool criticalRate, Vector2 pos)
	{
		DolocAPI.RaiseInstantPSEffects(base.PositionCenter, (base.currentLevel >= 2) ? InstantParticleEffectsType.LEAVES : InstantParticleEffectsType.LEAVES_AND_SOILS);
		OnCompleteFell();
	}

	protected override void _ClearEffects(Vector2 hitPosition)
	{
		DolocAPI.RaiseInstantPSEffects(base.PositionCenter, (base.currentLevel >= 2) ? InstantParticleEffectsType.LEAVES : InstantParticleEffectsType.LEAVES_AND_SOILS);
		if (base.currentLevel == base.Proto.MaxLevel)
		{
			DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.BUNCH_OF_LEAVES);
		}
	}

	public override void OnAnimalTouch()
	{
		Renderer.SwingOnTouch();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_ENTER_WEEDS);
		if (base.currentLevel >= 1 && RandomUtils.Dice(0.05f))
		{
			Vector2 position2d = Renderer.position2d;
			position2d.y += Renderer.Sr.sprite.bounds.size.y * 0.5f;
			DolocAPI.RaiseInstantPSEffects(position2d, InstantParticleEffectsType.WEEDS);
		}
	}

	public int TakeFeeds(int require, out string name)
	{
		name = "";
		if (!DolocConfig.Tables.TbHusbandryEnergy.IsHusbandryFeeds(base.Proto.Id, out var energy))
		{
			return 0;
		}
		name = base.Proto.Id;
		require = Mathf.Min(require, energy);
		if (currentHealth > require)
		{
			currentHealth -= require;
			if (base.isRender)
			{
				DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.WEEDS);
			}
			return require;
		}
		if (base.isRender)
		{
			DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.WEEDS);
			base.Host.RemoveDungeonResource(this);
		}
		else
		{
			base.Host.RemoveDungeonResourceNoRender(this);
		}
		return currentHealth;
	}
}
