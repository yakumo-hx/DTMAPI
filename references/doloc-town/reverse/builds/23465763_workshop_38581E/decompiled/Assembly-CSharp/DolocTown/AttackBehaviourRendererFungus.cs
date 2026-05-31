using System;
using UnityEngine;

namespace DolocTown;

public class AttackBehaviourRendererFungus : AttackBehaviourRenderer
{
	public override void OnAttackEnd(MonsterAttackId id, Type type, Transform target)
	{
		base.MonsterRenderer.PlayAnimation("idle");
	}
}
