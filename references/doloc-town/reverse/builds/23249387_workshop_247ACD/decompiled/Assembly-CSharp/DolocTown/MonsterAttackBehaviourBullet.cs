using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public abstract class MonsterAttackBehaviourBullet : MonsterAttackBehaviour
{
	private new readonly IMonsterAttackBehaviourBullet _proto;

	private BulletManager _bulletManager;

	protected MonsterAttackBehaviourBullet(Transform host, IMonsterAttackBehaviourBullet proto, BulletManager bulletManager)
		: base(host, proto)
	{
		_proto = proto;
		_bulletManager = bulletManager;
	}

	private void TryRemakeBulletManager()
	{
	}

	public override void BeforeInvoke()
	{
		TryRemakeBulletManager();
	}

	private void OnBulletHit(Bullet b, Collider2D collider)
	{
		BattleUtils.OnBulletHitPlayer(_bulletManager, _proto.Damage, _proto.CriticalRate, b, collider);
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		return _controller.RayTest(target, _proto.BulletDistance);
	}

	protected Tween Shoot(float duration)
	{
		return Recoil(delegate(Vector2 dir)
		{
			GenBullet(_controller, dir);
		}, duration);
	}

	protected void GenBullet(MonsterController host, Transform target, bool shouldRaiseSound = true)
	{
		Vector2 firePosition = host.FirePosition;
		Vector2 dir = host.DirToTarget(target);
		if (shouldRaiseSound)
		{
			_controller.PostSoundEvent(_proto.FireSound);
		}
		_bulletManager?.InvokeBullet(firePosition, dir, _proto.BulletSpeed, _proto.BulletDuration, OnBulletHit, null, _proto.DisableShootEffects);
	}

	protected void GenBullet(MonsterController host, Vector2 dir, bool shouldRaiseSound = true)
	{
		if (shouldRaiseSound && !_proto.FireSound.IsNullOrEmpty())
		{
			_controller.PostSoundEvent(_proto.FireSound);
		}
		_bulletManager?.InvokeBullet(host.FirePosition, dir, _proto.BulletSpeed, _proto.BulletDuration, OnBulletHit, null, _proto.DisableShootEffects);
	}

	public sealed override void Dispose(BattleSystem bs)
	{
		if (_bulletManager != null)
		{
			bs.RecycleBulletManager(_bulletManager);
		}
	}
}
