using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public interface IMultiImpact : IImpact, IMonsterAttackBehaviourPhysical, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction, IHasDash
{
	Vector2Int TimesRange { get; }
}
