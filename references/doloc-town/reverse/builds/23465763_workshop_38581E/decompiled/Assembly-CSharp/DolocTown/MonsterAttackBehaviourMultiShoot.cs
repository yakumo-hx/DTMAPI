using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourMultiShoot : MonsterAttackBehaviourBullet
{
	private new readonly IMultiShoot _proto;

	private Tween _tween;

	public MonsterAttackBehaviourMultiShoot(Transform host, IMultiShoot proto, BulletManager bm)
		: base(host, proto, bm)
	{
		_proto = proto;
	}

	public override bool OnUpdate(float dt)
	{
		if (_tween == null)
		{
			return true;
		}
		_tween.ManualUpdate(dt, dt);
		return false;
	}

	public override void Invoke()
	{
		Begin();
		if (_proto.HasReadyAction)
		{
			_controller.RaiseDangerWarning01();
			_tween = DoReadyAction(_proto.ReadyDuration, delegate
			{
				MultiShoot(_proto.BulletCountRange.DiceCount());
			});
		}
		else
		{
			MultiShoot(_proto.BulletCountRange.DiceCount());
		}
	}

	private void MultiShoot(int times)
	{
		if (times <= 0)
		{
			Stop();
			return;
		}
		_controller.Recoil(base.target, delegate
		{
			GenBullet(_controller, base.target);
		}, _proto.ShootDuration).OnComplete(delegate
		{
			MultiShoot(times - 1);
		});
	}

	private void Stop()
	{
		End();
		_tween?.Kill();
		_tween = null;
	}
}
