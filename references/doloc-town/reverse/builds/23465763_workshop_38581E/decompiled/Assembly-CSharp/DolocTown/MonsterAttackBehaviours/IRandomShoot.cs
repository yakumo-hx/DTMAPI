using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public interface IRandomShoot : IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	float ShootInterval { get; }

	Vector2Int BulletCountRange { get; }
}
