using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoAngryThrow : MonsterAttackBehaviourSOBullet, IAngryThrow, IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	[SerializeField]
	[Range(0f, 30f)]
	private float validDistance = 15f;

	[SerializeField]
	private Vector2Int bulletCountRange = new Vector2Int(3, 5);

	[SerializeField]
	[Range(0.1f, 0.5f)]
	private float shootInterval = 0.12f;

	public float ValidDistance => validDistance;

	public Vector2Int BulletCountRange => bulletCountRange;

	public float ShootInterval => shootInterval;
}
