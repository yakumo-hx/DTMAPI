using System;
using UnityEngine;

namespace DolocTown;

public class AttackBehaviourRendererAircraft : AttackBehaviourRenderer
{
	public override void OnAttackBegin(MonsterAttackId id, Type type, Transform target)
	{
		if (id == MonsterAttackId.AtkSkill01)
		{
			DolocAPI.RaiseInstantGoEffects(base.transform.position, InstantGoEffectsType.WARNING_LIGHT);
		}
	}
}
