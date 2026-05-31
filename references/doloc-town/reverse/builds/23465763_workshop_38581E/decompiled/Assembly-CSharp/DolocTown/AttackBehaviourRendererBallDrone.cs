using System;
using UnityEngine;

namespace DolocTown;

public class AttackBehaviourRendererBallDrone : AttackBehaviourRenderer
{
	public override void OnAttackBegin(MonsterAttackId id, Type type, Transform target)
	{
		base.MonsterRenderer.PlayAnimation("attack");
	}

	public override void OnAttackEnd(MonsterAttackId id, Type type, Transform target)
	{
		base.MonsterRenderer.PlayAnimation("resume");
	}
}
