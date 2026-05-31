using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoBulletTrain : MonsterAttackBehaviourSOBullet, IBulletTrain, IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	[SerializeField]
	[Range(0.1f, 2f)]
	protected float shootInterval = 0.5f;

	[SerializeField]
	[Min(1f)]
	protected int bulletCount = 3;

	[SerializeField]
	[Min(1f)]
	protected int bulletShootTimes = 2;

	[SerializeField]
	[Range(0f, 360f)]
	protected int sectorAngle = 180;

	[SerializeField]
	protected Vector2 initialDirection = Vector2.up;

	[SerializeField]
	[Min(1f)]
	protected float flySpeed = 5f;

	public float ShootInterval => shootInterval;

	public int BulletCount => bulletCount;

	public int BulletShootTimes => bulletShootTimes;

	public int SectorAngle => sectorAngle;

	public Vector2 InitialDirection => initialDirection;

	public float FlySpeed => flySpeed;
}
