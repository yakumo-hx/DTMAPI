using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public interface IAngryThrow : IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	float ValidDistance { get; }

	Vector2Int BulletCountRange { get; }

	float ShootInterval { get; }
}
