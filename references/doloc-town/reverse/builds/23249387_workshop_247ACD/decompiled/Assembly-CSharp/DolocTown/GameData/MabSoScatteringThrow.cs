using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoScatteringThrow : MonsterAttackBehaviourSOBullet, IScatteringThrow, IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	[SerializeField]
	[Range(0f, 100f)]
	private float validDistance;

	[SerializeField]
	[Range(0f, 3f)]
	private float shootDuration = 0.75f;

	[SerializeField]
	private Vector2Int bulletCountRange = new Vector2Int(3, 5);

	[SerializeField]
	private float sectorAngle = 60f;

	public float ValidDistance => validDistance;

	public float ShootDuration => shootDuration;

	public Vector2Int BulletCountRange => bulletCountRange;

	public float SectorAngle => sectorAngle;
}
