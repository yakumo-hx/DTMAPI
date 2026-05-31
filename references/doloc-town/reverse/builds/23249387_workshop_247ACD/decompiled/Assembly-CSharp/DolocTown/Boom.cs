using System;
using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Animator), typeof(Collider2D))]
[GameEntityManager("/global/boom", DolocGameAssets.GAME_ENTITY_BOOM, Frequency = 5)]
public class Boom : GameEntity
{
	private readonly HashSet<IBombInteractive> _receivers = new HashSet<IBombInteractive>();

	private Animator _animator;

	private float _damage;

	private float _criticalRate;

	private bool _shouldWorkOnMonster;

	private bool _shouldWorkOnPlayer;

	private bool _shouldWorkOnEnvironment;

	private Action<Collider2D> _onHitCallback;

	private Action<Vector2> _customEffects;

	protected override void __Init()
	{
		base.__Init();
		_animator = GetComponent<Animator>();
	}

	public void Invoke(Vector2 position, float damage, float criticalRate, bool shouldRaiseScreenTwist = true, bool shouldWorkOnMonster = false, bool shouldWorkOnPlayer = true, bool shouldWorkOnEnvironment = true, Action<Collider2D> onHitCallback = null, Action<Vector2> customEffects = null)
	{
		base.transform.position = position;
		_onHitCallback = onHitCallback;
		_customEffects = customEffects;
		_damage = damage;
		_criticalRate = Mathf.Clamp01(criticalRate);
		_shouldWorkOnMonster = shouldWorkOnMonster;
		_shouldWorkOnPlayer = shouldWorkOnPlayer;
		_shouldWorkOnEnvironment = shouldWorkOnEnvironment;
		_receivers.Clear();
		_animator.Play("boom", 0, 0f);
		BoomEffectsWithoutScreenTwist();
		if (shouldRaiseScreenTwist)
		{
			DolocAPI.RaiseScreenTwist(base.transform.position, 2.5f);
		}
	}

	private void InstEffects(Vector2 position)
	{
		if (_customEffects != null)
		{
			_customEffects(position);
			_customEffects = null;
		}
		else
		{
			DolocAPI.RaiseInstantPSEffects(position, InstantParticleEffectsType.DRONE_PARTS);
			DolocAPI.RaiseInstantPSEffects(position, InstantParticleEffectsType.SPARKS);
		}
	}

	private void BoomEffectsWithoutScreenTwist()
	{
		Vector2 vector = base.transform.position;
		InstEffects(vector);
		for (int i = 0; i < 3; i++)
		{
			int num = UnityEngine.Random.Range(0, 3);
			InstAnimEffectType type = (InstAnimEffectType)(InstAnimEffectType.IMPACT_01.GetHashCode() + num);
			Vector2 vector2 = UnityEngine.Random.insideUnitCircle * 3f;
			DolocAPI.RaiseInstantAnimEffects((Vector2)base.transform.position + vector2, type);
		}
		DolocAPI.RaiseWind(vector, Vector2.right * 20f, 3f);
		DolocAPI.RaiseWind(vector, Vector2.left * 20f, 3f);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_DRONE_ATTACK_BOMB_EXPLOSIVE, base.gameObject);
		float num2 = Vector2.Distance(DolocAPI.AgentPosition, vector);
		float num3 = Mathf.Min(1f, num2 * 0.1f);
		DolocAPI.cameraController.ShakeScreen(0.2f, 0.3f * num3);
	}

	private void BoomEffects()
	{
		BoomEffectsWithoutScreenTwist();
		DolocAPI.RaiseScreenTwist(base.transform.position, 2.5f);
	}

	private void OnAnimationDone()
	{
		DolocAPI.EntitySystem.Recycle(this);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		IBombInteractive component = other.GetComponent<IBombInteractive>();
		if (component != null && _receivers.Add(component) && component.attackableType != AttackableType.Unattackable && (component.attackableType != AttackableType.Enemy || _shouldWorkOnMonster) && (component.attackableType != AttackableType.Player || _shouldWorkOnPlayer) && (component.attackableType != 0 || _shouldWorkOnEnvironment))
		{
			component.OnBomb(_damage, RandomUtils.Dice(_criticalRate), base.transform.position);
			_onHitCallback?.Invoke(other);
		}
	}

	public override void OnReuse()
	{
		base.OnReuse();
		DolocAPI.Sound.RegisterGameObject(base.gameObject);
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		DolocAPI.Sound.UnregisterGameObject(base.gameObject);
	}
}
