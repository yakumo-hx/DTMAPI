using System;
using DG.Tweening;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.GameData;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class DestructibleObject : InteractableObject
{
	[SerializeField]
	private int totalHealth = 100;

	[SerializeField]
	private AttackableType _attackableType;

	[SerializeField]
	public ToolType toolType;

	[SerializeField]
	public int toolLevel;

	[SerializeField]
	private RangedItemListConfig itemListConfig;

	[SerializeField]
	private DestructibleEffectType effectType;

	[SerializeField]
	private bool linkToResource;

	[SerializeField]
	private string resourceId;

	[SerializeField]
	private bool useTechPoint;

	[SerializeField]
	private TechPointConfig[] techPoints = Array.Empty<TechPointConfig>();

	private int currentHealth;

	[HideInInspector]
	public UnityEvent onBroken = new UnityEvent();

	private bool isShine;

	private Tween anim;

	protected override bool notSupportCustomEvent => false;

	public override AttackableType attackableType => _attackableType;

	protected override void __Init()
	{
		base.__Init();
		currentHealth = totalHealth;
	}

	public override bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		base.OnFell(tool, hitPoint);
		ItemFunctionTool functionTool = tool.functionTool;
		if (toolType != functionTool.ToolType)
		{
			return false;
		}
		PlayFellEffectOnStart(hitPoint, effectType);
		if (functionTool.Level < toolLevel)
		{
			DolocAPI.agent.StateManager.Overwrite<AgentStateHit>();
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrLowToolLevel);
			Shine(DolocAPI.eftConfig.dungeonResourceShineTime);
			return true;
		}
		if (functionTool.ChopNumber < currentHealth)
		{
			currentHealth -= functionTool.ChopNumber;
			Shake(DolocAPI.eftConfig.dungeonResourceShakeTime);
			Shine(DolocAPI.eftConfig.dungeonResourceShineTime);
			PlayFellEffectInProgress(hitPoint, effectType);
			return true;
		}
		PlayFellEffectOnEnd(hitPoint, effectType);
		IDropItemHost currentRoom = DolocAPI.archiveHandle.currentRoom;
		if (currentRoom != null)
		{
			CountItem[] array = itemListConfig.GenerateCountItems();
			for (int i = 0; i < array.Length; i++)
			{
				CountItem countItem = array[i];
				DolocAPI.GenerateDropItems(currentRoom, countItem.itemName, base.transform.position, countItem.itemCount);
			}
		}
		TrySendGameEvent();
		base.archiveData.SaveObjectVisibleState(base.guid, visible: false);
		onBroken.Invoke();
		base.gameObject.SetActive(value: false);
		return true;
	}

	protected virtual void OnFellDown()
	{
		if (useTechPoint && techPoints.Length != 0)
		{
			TechPointConfig[] array = techPoints;
			for (int i = 0; i < array.Length; i++)
			{
				TechPointConfig techPointConfig = array[i];
				DolocAPI.AddTechExp(techPointConfig.type, techPointConfig.count);
			}
		}
		if (!resourceId.IsNullOrEmpty() && DolocAPI.QueryResourceProto(resourceId, out var proto))
		{
			DolocAPI.BroadcastString(GameEventType.FELL_DUNGEON_RESOURCE, resourceId);
			DolocAPI.BroadcastString(GameEventType.FELL_DUNGEON_RESOURCE_CLASS, proto.ResourceClass.ToString().ToLower());
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Resource, resourceId);
		}
	}

	public override bool OnAttacked(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		isDead = false;
		PlayEffectOnAttack(pos, effectType);
		DolocAPI.RaiseInstantAnimEffects(pos, InstAnimEffectType.HIT_SPARK, LocMaterials.GAME_MAT_EFFECTS_SHINE);
		return true;
	}

	private void PlayFellEffectOnStart(Vector2 hitPoint, DestructibleEffectType type)
	{
		switch (type)
		{
		case DestructibleEffectType.Tree:
			RaiseRandomImpact(hitPoint);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_RESOURCE_FELL);
			break;
		case DestructibleEffectType.Stone:
			DolocAPI.effectProvider.RaiseInstPS(hitPoint, InstantParticleEffectsType.SPARKS);
			DolocAPI.RaiseInstantAnimEffects(hitPoint, InstAnimEffectType.HIT_SPARK, LocMaterials.GAME_MAT_EFFECTS_SHINE);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_RESOURCE_GATHER);
			break;
		case DestructibleEffectType.Weeds:
			DolocAPI.effectProvider.RaiseInstAnim(hitPoint, InstAnimEffectType.DIFFUSION_BUBBLE);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_RESOURCE_HARVEST);
			break;
		default:
			throw new ArgumentOutOfRangeException("type", type, null);
		}
	}

	private void PlayFellEffectInProgress(Vector2 hitPoint, DestructibleEffectType type)
	{
		switch (type)
		{
		case DestructibleEffectType.Tree:
			DolocAPI.effectProvider.RaiseInstPS(hitPoint, InstantParticleEffectsType.SAWDUST);
			DolocAPI.effectProvider.RaiseInstPS(position2d + new Vector2(0f, _renderer.bounds.size.y * 0.75f), InstantParticleEffectsType.LEAVES);
			break;
		case DestructibleEffectType.Stone:
			DolocAPI.effectProvider.RaiseInstPS(hitPoint, InstantParticleEffectsType.STONE_SHARDS);
			break;
		case DestructibleEffectType.Weeds:
			DolocAPI.effectProvider.RaiseInstPS(hitPoint, InstantParticleEffectsType.WEEDS);
			break;
		default:
			throw new ArgumentOutOfRangeException("type", type, null);
		}
	}

	private void PlayFellEffectOnEnd(Vector2 hitPoint, DestructibleEffectType type)
	{
		switch (type)
		{
		case DestructibleEffectType.Tree:
			DolocAPI.effectProvider.RaiseInstPS(hitPoint, InstantParticleEffectsType.SPARKS);
			break;
		case DestructibleEffectType.Stone:
			DolocAPI.effectProvider.RaiseInstPS(hitPoint, InstantParticleEffectsType.HUGE_STONE_CRACK);
			break;
		case DestructibleEffectType.Weeds:
			DolocAPI.effectProvider.RaiseInstPS(position2d + new Vector2(0f, _renderer.bounds.size.y * 0.75f), InstantParticleEffectsType.WEEDS_LARGE);
			DolocAPI.effectProvider.RaiseInstPS(position2d + new Vector2(0f, _renderer.bounds.size.y * 0.75f), InstantParticleEffectsType.BUNCH_OF_LEAVES);
			break;
		default:
			throw new ArgumentOutOfRangeException("type", type, null);
		}
	}

	private void PlayEffectOnAttack(Vector2 pos, DestructibleEffectType type)
	{
		switch (type)
		{
		case DestructibleEffectType.Tree:
			DolocAPI.effectProvider.RaiseInstPS(pos, InstantParticleEffectsType.SAWDUST);
			break;
		case DestructibleEffectType.Stone:
			DolocAPI.effectProvider.RaiseInstPS(pos, InstantParticleEffectsType.STONE_SHARDS);
			break;
		default:
			throw new ArgumentOutOfRangeException("type", type, null);
		}
	}

	public void Shine(float duration = 0.3f)
	{
		if (!isShine)
		{
			isShine = true;
			Material originMat = _renderer.material;
			_renderer.material = LocMaterials.GAME_MAT_HIT;
			DolocAPI.Delay(duration, delegate
			{
				isShine = false;
				_renderer.sharedMaterial = originMat;
			});
		}
	}

	public void Shake(float duration = 0.3f, float shakeStrength = 0.1f)
	{
		if (anim != null)
		{
			anim.Kill();
		}
		anim = base.transform.DOShakePosition(duration, shakeStrength);
	}

	protected void RaiseRandomImpact(Vector2 pos)
	{
		InstAnimEffectType[] array = new InstAnimEffectType[3]
		{
			InstAnimEffectType.IMPACT_01,
			InstAnimEffectType.IMPACT_02,
			InstAnimEffectType.IMPACT_03
		};
		int num = UnityEngine.Random.Range(0, array.Length);
		DolocAPI.RaiseInstantAnimEffects(pos, array[num]);
	}
}
