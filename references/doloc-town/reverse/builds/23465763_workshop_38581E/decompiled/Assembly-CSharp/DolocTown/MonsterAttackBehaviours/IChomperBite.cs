namespace DolocTown.MonsterAttackBehaviours;

public interface IChomperBite : IMonsterAttackBehaviourPhysical, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	string AnimName { get; }

	float AttackRange { get; }
}
