using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourScatteringShoot : MonsterAttackBehaviourBullet
{
	private new readonly IScatteringShoot _proto;

	private readonly MonsterAttackHelperTween _handle;

	public MonsterAttackBehaviourScatteringShoot(Transform host, IScatteringShoot proto, BulletManager bm)
		: base(host, proto, bm)
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
		if (_proto.HasReadyAction)
		{
			_handle.SetTween(DoReadyAction(_proto.ReadyDuration, StartScatteringShoot));
		}
		else
		{
			StartScatteringShoot();
		}
	}

	private void StartScatteringShoot()
	{
		Begin();
		int num = Mathf.Max(1, _proto.Times);
		Sequence sequence = DOTween.Sequence();
		for (int i = 0; i < num; i++)
		{
			sequence.Append(_ScatteringShoot());
			if (i != _proto.Times - 1)
			{
				sequence.AppendInterval(_proto.TimeInterval);
			}
		}
		_handle.SetFinalTween(sequence);
	}

	private Tween _ScatteringShoot()
	{
		return Recoil(delegate(Vector2 dir)
		{
			int splitCount = _proto.BulletCountRange.DiceCount();
			_controller.PostSoundEvent(_proto.FireSound);
			foreach (Vector2 item in dir.SplitIntoSector(splitCount, _proto.SectorAngle))
			{
				GenBullet(_controller, item, shouldRaiseSound: false);
			}
		}, _proto.ShootDuration);
	}
}
