using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public class MonsterAttackBehaviourFungusShoot : MonsterAttackBehaviourBullet
{
	private readonly IFungusShoot fungusShoot;

	private readonly MonsterAttackHelperAnim waiter;

	public MonsterAttackBehaviourFungusShoot(Transform host, IFungusShoot proto, BulletManager bulletManager)
		: base(host, proto, bulletManager)
	{
		fungusShoot = proto;
		waiter = new MonsterAttackHelperAnim(host.GetComponent<Animator>());
	}

	private void DoAttack()
	{
		IEnumerable<Vector2> enumerable = new Vector2(Mathf.Sign(base.target.position.x - host.position.x), 1f).normalized.SplitIntoSector(3, 90f);
		_controller.PostSoundEvent(fungusShoot.FireSound);
		foreach (Vector2 item in enumerable)
		{
			GenBullet(_controller, item, shouldRaiseSound: false);
		}
	}

	public override void Invoke()
	{
		Begin();
		_controller.BindAnimationEvent(DoAttack);
		if (fungusShoot.HasReadyAction)
		{
			_controller.RaiseDangerWarning01();
			waiter.Invoke(fungusShoot.ReadyDuration, "attack");
		}
		else
		{
			waiter.Invoke("attack");
		}
	}

	public override bool OnUpdate(float dt)
	{
		if (!waiter.Update(dt))
		{
			return false;
		}
		End();
		return true;
	}
}
