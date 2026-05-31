using System.Linq;
using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourDragonShoot : MonsterAttackBehaviourBullet
{
	private new readonly IDragonShoot _proto;

	private readonly MonsterAttackHelperTween _handle;

	public MonsterAttackBehaviourDragonShoot(Transform host, IDragonShoot proto, BulletManager bm)
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
		Begin();
		if (_proto.HasReadyAction)
		{
			_controller.RaiseDangerWarning01();
			_handle.SetTween(DoReadyAction(_proto.ReadyDuration, StartDragonShoot));
		}
		else
		{
			StartDragonShoot();
		}
	}

	private void StartDragonShoot()
	{
		int num = _proto.BulletCountRange.DiceCount();
		Vector2[] array = base.DirToTarget.SplitIntoSector(_proto.SectorCount, _proto.SectorAngle).ToArray();
		int num2 = 0;
		int num3 = 1;
		Sequence sequence = DOTween.Sequence();
		for (int i = 0; i < num; i++)
		{
			Vector2 shootDir = array[num2];
			sequence.AppendCallback(delegate
			{
				GenBullet(_controller, shootDir);
			});
			sequence.AppendInterval(_proto.ShootDuration);
			if (num3 == 1)
			{
				num2++;
				if (num2 >= array.Length)
				{
					num2 = array.Length - 1;
					num3 = -1;
				}
			}
			else
			{
				num2--;
				if (num2 < 0)
				{
					num2 = 0;
					num3 = 1;
				}
			}
		}
		_handle.SetFinalTween(sequence);
	}
}
