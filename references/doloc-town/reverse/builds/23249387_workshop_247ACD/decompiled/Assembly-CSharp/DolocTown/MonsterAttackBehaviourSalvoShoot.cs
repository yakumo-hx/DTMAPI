using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourSalvoShoot : MonsterAttackBehaviourBullet
{
	private new readonly ISalvoShoot _proto;

	private readonly MonsterAttackHelperTween _handle;

	public MonsterAttackBehaviourSalvoShoot(Transform host, ISalvoShoot proto, BulletManager _bulletManager)
		: base(host, proto, _bulletManager)
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
			_handle.SetTween(DoReadyAction(_proto.ReadyDuration, Salvo));
		}
		else
		{
			Salvo();
		}
	}

	private void Salvo()
	{
		Vector2 dirToTarget = base.DirToTarget;
		Sequence sequence = DOTween.Sequence();
		foreach (Vector2 dir in dirToTarget.SplitIntoSector(_proto.BulletCount, _proto.SectorAngle))
		{
			sequence.AppendCallback(delegate
			{
				GenBullet(_controller, dir);
			});
			sequence.AppendInterval(_proto.ShootInterval);
		}
		sequence.OnComplete(_handle.Stop);
		_handle.SetTween(sequence);
	}
}
