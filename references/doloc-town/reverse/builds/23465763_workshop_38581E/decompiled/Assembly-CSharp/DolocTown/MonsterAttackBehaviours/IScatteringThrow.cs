using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public interface IScatteringThrow : IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	Vector2Int BulletCountRange { get; }

	float ShootDuration { get; }

	float SectorAngle { get; }
}
