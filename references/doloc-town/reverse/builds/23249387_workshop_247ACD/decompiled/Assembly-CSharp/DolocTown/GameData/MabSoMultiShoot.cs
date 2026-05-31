using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoMultiShoot : MonsterAttackBehaviourSOBullet, IMultiShoot, IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	[SerializeField]
	private Vector2Int bulletCountRange = new Vector2Int(3, 5);

	[SerializeField]
	[Range(0f, 3f)]
	private float shootDuration = 0.75f;

	public Vector2Int BulletCountRange => bulletCountRange;

	public float ShootDuration => shootDuration;
}
