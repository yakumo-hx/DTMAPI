using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public interface IBombingDive : IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction, IHasDash
{
	float ValidDistance { get; }

	Vector2Int BulletCountRange { get; }
}
