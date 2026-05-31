using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourAngryThrow : MonsterAttackBehaviourBullet
{
	private new readonly IAngryThrow _proto;

	private readonly MonsterAttackHelperTween _handle;

	public MonsterAttackBehaviourAngryThrow(Transform host, IAngryThrow proto, BulletManager bulletManager)
		: base(host, proto, bulletManager)
	{
		_proto = proto;
		_handle = new MonsterAttackHelperTween(base.End);
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		if (target.position.y > host.position.y)
		{
			return false;
		}
		if (_controller.DistanceToTarget(target) > _proto.ValidDistance)
		{
			return false;
		}
		return _controller.RayTest(target, _proto.BulletDistance);
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
			_handle.SetTween(DoReadyAction(_proto.ReadyDuration, StartAngryThrow));
		}
		else
		{
			StartAngryThrow();
		}
	}

	private void StartAngryThrow()
	{
		int num = _proto.BulletCountRange.DiceCount();
		Sequence sequence = DOTween.Sequence();
		for (int i = 0; i < num; i++)
		{
			sequence.AppendCallback(delegate
			{
				Vector2 insideUnitCircle = Random.insideUnitCircle;
				insideUnitCircle.y = Mathf.Abs(insideUnitCircle.y);
				GenBullet(_controller, insideUnitCircle);
			});
			sequence.AppendInterval(_proto.ShootInterval);
		}
		_handle.SetFinalTween(sequence);
	}
}
