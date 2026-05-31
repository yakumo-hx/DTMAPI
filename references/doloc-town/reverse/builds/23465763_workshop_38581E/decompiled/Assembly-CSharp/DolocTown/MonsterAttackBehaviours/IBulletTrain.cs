using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public interface IBulletTrain : IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	float ShootInterval { get; }

	int BulletCount { get; }

	int BulletShootTimes { get; }

	int SectorAngle { get; }

	Vector2 InitialDirection { get; }

	float FlySpeed { get; }
}
