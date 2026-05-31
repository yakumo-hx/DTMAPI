namespace DolocTown.MonsterAttackBehaviours;

public interface ILandmine : IMonsterAttackBehaviourSkill, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	float ValidDistance { get; }

	float LiveDuration { get; }

	float ReflectionTime { get; }

	float MoveSpeed { get; }
}
