using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoSalvoShoot : MonsterAttackBehaviourSOBullet, ISalvoShoot, IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	[SerializeField]
	[Range(1f, 30f)]
	private int bulletCount = 6;

	[SerializeField]
	private float sectorAngle = 360f;

	[SerializeField]
	[Range(0.1f, 0.5f)]
	private float shootInterval = 0.1f;

	public int BulletCount => bulletCount;

	public float SectorAngle => sectorAngle;

	public float ShootInterval => shootInterval;
}
