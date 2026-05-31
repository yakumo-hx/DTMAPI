namespace DolocTown.MonsterAttackBehaviours;

public interface IMonsterAttackBehaviour
{
	MonsterAttackId AttackId { get; }

	float CDDuration { get; }

	bool CDOnStart { get; }
}
