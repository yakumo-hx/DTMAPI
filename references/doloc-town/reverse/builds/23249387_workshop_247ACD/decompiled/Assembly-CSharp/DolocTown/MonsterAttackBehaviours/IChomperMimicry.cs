using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public interface IChomperMimicry : IMonsterAttackBehaviourSkill, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	Vector2 DurationRange { get; }
}
