using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourShoot : MonsterAttackBehaviourBullet
{
	private new readonly IShoot _proto;

	private readonly MonsterAttackHelperTween _handle;

	public MonsterAttackBehaviourShoot(Transform host, IShoot proto, BulletManager bulletManager)
		: base(host, proto, bulletManager)
	{
		_proto = proto;
		_handle = new MonsterAttackHelperTween(base.End);
	}

	public override bool OnUpdate(float dt)
	{
		return _handle.Update(dt);
	}

	public override void Invoke()
	{
		Begin();
		if (_proto.HasReadyAction)
		{
			_controller.RaiseDangerWarning01();
			_handle.SetTween(DoReadyAction(_proto.ReadyDuration, Shoot));
		}
		else
		{
			Shoot();
		}
	}

	private void Shoot()
	{
		_handle.SetFinalTween(Shoot(_proto.ShootDuration));
	}
}
