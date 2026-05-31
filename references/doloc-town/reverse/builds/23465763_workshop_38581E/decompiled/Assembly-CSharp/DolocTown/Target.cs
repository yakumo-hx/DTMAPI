using System;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Target : MonoBehaviour, IAttackable
{
	private Shaker _shaker;

	private Shiner _shiner;

	private Action _callbackOnBroken;

	private int _defend;

	private int _health;

	private Shaker Shaker
	{
		get
		{
			if (_shaker != null)
			{
				return _shaker;
			}
			_shaker = new Shaker(base.transform);
			return _shaker;
		}
	}

	private Shiner Shiner
	{
		get
		{
			if (_shiner != null)
			{
				return _shiner;
			}
			_shiner = new Shiner(GetComponent<SpriteRenderer>());
			return _shiner;
		}
	}

	public string ProtoName { get; set; }

	public AttackableType attackableType => AttackableType.Enemy;

	public void CallbackOnBroken(Action callback)
	{
		_callbackOnBroken = callback;
	}

	public void ResetTarget(int health, int defend)
	{
		_defend = defend;
		_health = health;
		base.gameObject.SetActive(value: true);
	}

	public bool OnAttacked(float attack, bool criticalRate, Vector2 position, out bool isDead)
	{
		int num = BattleUtils.CalcDamage(attack, _defend, criticalRate);
		Shiner.Raise(LocMaterials.GAME_MAT_HIT, 0.1f);
		Shaker.Shake(0.25f);
		DolocAPI.RaiseInstantPSEffects(position, InstantParticleEffectsType.TARGET_PARTS_SM);
		DolocAPI.RaiseDamageTip(num, position);
		isDead = false;
		_health -= num;
		if (_health > 0)
		{
			return true;
		}
		isDead = true;
		_callbackOnBroken?.Invoke();
		DolocAPI.RaiseInstantPSEffects(base.transform.position, InstantParticleEffectsType.TARGET_PARTS_LG);
		base.gameObject.SetActive(value: false);
		if (ProtoName.IsNullOrEmpty())
		{
			DolocAPI.Broadcast(GameEventType.SLAIN_MONSTER);
		}
		else
		{
			DolocAPI.BroadcastString(GameEventType.SLAIN_MONSTER, ProtoName);
		}
		return true;
	}

	public bool OnSwordAttack(float atk, bool isCritical, Vector2 position, out bool isDead)
	{
		return OnAttacked(atk, isCritical, position, out isDead);
	}
}
