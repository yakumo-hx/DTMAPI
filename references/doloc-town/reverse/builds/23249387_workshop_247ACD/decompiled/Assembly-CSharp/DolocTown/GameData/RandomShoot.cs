using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class RandomShoot : MonsterAttackBehaviourSOBullet, IRandomShoot, IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	[SerializeField]
	[Range(0f, 5f)]
	protected float shootInterval = 0.3f;

	[SerializeField]
	protected Vector2Int bulletCountRange = new Vector2Int(3, 6);

	public float ShootInterval => shootInterval;

	public Vector2Int BulletCountRange => bulletCountRange;
}
