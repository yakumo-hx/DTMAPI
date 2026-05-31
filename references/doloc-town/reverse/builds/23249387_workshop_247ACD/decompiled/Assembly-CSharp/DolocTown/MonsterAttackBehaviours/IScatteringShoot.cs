using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public interface IScatteringShoot : IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	Vector2Int BulletCountRange { get; }

	float ShootDuration { get; }

	float SectorAngle { get; }

	int Times { get; }

	float TimeInterval { get; }
}
