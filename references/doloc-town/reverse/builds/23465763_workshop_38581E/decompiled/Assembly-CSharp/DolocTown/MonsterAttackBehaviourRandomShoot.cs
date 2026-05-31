using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourRandomShoot : MonsterAttackBehaviourBullet
{
	private new readonly IRandomShoot _proto;

	private readonly RSTimer _shootTimer;

	private int _leftBulletCount;

	public MonsterAttackBehaviourRandomShoot(Transform host, IMonsterAttackBehaviourBullet proto, BulletManager bulletManager)
		: base(host, proto, bulletManager)
	{
		_proto = (IRandomShoot)proto;
		_shootTimer = new RSTimer(_proto.ShootInterval);
	}

	public override void Invoke()
	{
		_shootTimer.SetInterval(_proto.ShootInterval);
		_leftBulletCount = _proto.BulletCountRange.DiceCount();
	}

	public override bool OnUpdate(float dt)
	{
		if (!_shootTimer.Tick(dt))
		{
			return false;
		}
		ShootRandom();
		if (--_leftBulletCount > 0)
		{
			return false;
		}
		End();
		return true;
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		return true;
	}

	private void ShootRandom()
	{
		Vector2 normalized = Random.insideUnitCircle.normalized;
		GenBullet(_controller, normalized);
	}
}
