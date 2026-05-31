using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoDragonShoot : MonsterAttackBehaviourSOBullet, IDragonShoot, IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	[SerializeField]
	private Vector2Int bulletCountRange = new Vector2Int(10, 15);

	[SerializeField]
	private float sectorAngle = 60f;

	[SerializeField]
	[Min(1f)]
	private int sectorCount = 4;

	[SerializeField]
	[Range(0.1f, 0.5f)]
	private float shootInterval = 0.1f;

	public Vector2Int BulletCountRange => bulletCountRange;

	public float SectorAngle => sectorAngle;

	public int SectorCount => sectorCount;

	public float ShootDuration => shootInterval;
}
