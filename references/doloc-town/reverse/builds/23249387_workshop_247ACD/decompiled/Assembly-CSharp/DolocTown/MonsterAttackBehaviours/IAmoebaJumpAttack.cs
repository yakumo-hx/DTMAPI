namespace DolocTown.MonsterAttackBehaviours;

public interface IAmoebaJumpAttack : IMonsterAttackBehaviourPhysical, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	float JumpSpeed { get; }

	float AttackRange { get; }

	float MaxJumpHeight { get; }
}
