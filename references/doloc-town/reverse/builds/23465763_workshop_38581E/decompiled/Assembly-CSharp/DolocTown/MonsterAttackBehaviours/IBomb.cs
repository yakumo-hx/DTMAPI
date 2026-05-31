namespace DolocTown.MonsterAttackBehaviours;

public interface IBomb : IMonsterAttackBehaviourSkill, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	float ValidDistance { get; }

	float HeightDstThreshold { get; }
}
