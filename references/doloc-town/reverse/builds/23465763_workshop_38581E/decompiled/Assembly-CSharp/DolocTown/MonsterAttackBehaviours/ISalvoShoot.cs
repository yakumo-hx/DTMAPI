namespace DolocTown.MonsterAttackBehaviours;

public interface ISalvoShoot : IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	int BulletCount { get; }

	float SectorAngle { get; }

	float ShootInterval { get; }
}
