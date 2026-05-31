using System.Collections.Generic;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoChomperBite : MonsterAttackBehaviourSOPhysical, IChomperBite, IMonsterAttackBehaviourPhysical, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	[SerializeField]
	private RuntimeAnimatorController _animatorController;

	[SerializeField]
	private string animName;

	[SerializeField]
	[Min(0f)]
	private float attackRange;

	private IEnumerable<string> AvailableAnims
	{
		get
		{
			if ((object)_animatorController != null)
			{
				AnimationClip[] animationClips = _animatorController.animationClips;
				foreach (AnimationClip animationClip in animationClips)
				{
					yield return animationClip.name;
				}
			}
		}
	}

	public string AnimName => animName;

	public float AttackRange => attackRange;
}
