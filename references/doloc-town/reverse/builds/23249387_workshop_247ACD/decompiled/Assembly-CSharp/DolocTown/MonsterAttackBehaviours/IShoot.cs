namespace DolocTown.MonsterAttackBehaviours;

public interface IShoot : IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	float ShootDuration { get; }
}
