using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourScatteringThrow : MonsterAttackBehaviourBullet
{
	private readonly IScatteringThrow _scatteringThrow;

	private readonly MonsterAttackHelperTween _handle;

	public MonsterAttackBehaviourScatteringThrow(Transform host, IScatteringThrow proto, BulletManager bm)
		: base(host, proto, bm)
	{
		_scatteringThrow = proto;
		_handle = new MonsterAttackHelperTween(base.End);
	}

	public override bool OnUpdate(float dt)
	{
		return _handle.Update(dt);
	}

	public override void Invoke()
	{
		Begin();
		if (_scatteringThrow.HasReadyAction)
		{
			_handle.SetTween(DoReadyAction(_scatteringThrow.ReadyDuration, StartScatteringThrow));
		}
		else
		{
			StartScatteringThrow();
		}
	}

	private void StartScatteringThrow()
	{
		Tween finalTween = Recoil(delegate
		{
			int splitCount = _scatteringThrow.BulletCountRange.DiceCount();
			_controller.PostSoundEvent(_scatteringThrow.FireSound);
			foreach (Vector2 item in Vector2.up.SplitIntoSector(splitCount, _scatteringThrow.SectorAngle))
			{
				GenBullet(_controller, item, shouldRaiseSound: false);
			}
		}, _scatteringThrow.ShootDuration);
		_handle.SetFinalTween(finalTween);
	}
}
