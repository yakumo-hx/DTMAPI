namespace DolocTown.MonsterAttackBehaviours;

public interface IMonsterAttackBehaviourSkill : IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	string SkillId { get; }
}
