using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public interface IMultiShoot : IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	Vector2Int BulletCountRange { get; }

	float ShootDuration { get; }
}
