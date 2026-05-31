namespace DolocTown.MonsterAttackBehaviours;

public interface ITimeBomb : IMonsterAttackBehaviourSkill, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	float ValidDistance { get; }

	float MoveSpeed { get; }

	float MaxMoveSpeed { get; }

	int Count { get; }
}
