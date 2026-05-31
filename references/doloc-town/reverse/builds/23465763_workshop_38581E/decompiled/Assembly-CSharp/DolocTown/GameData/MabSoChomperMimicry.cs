using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoChomperMimicry : MonsterAttackBehaviourSOSkill, IChomperMimicry, IMonsterAttackBehaviourSkill, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	[SerializeField]
	private Vector2 durationRange;

	public Vector2 DurationRange => durationRange;
}
