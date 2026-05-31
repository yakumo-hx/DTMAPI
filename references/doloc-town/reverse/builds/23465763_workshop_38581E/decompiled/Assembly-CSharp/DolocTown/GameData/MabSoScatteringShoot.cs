using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoScatteringShoot : MonsterAttackBehaviourSOBullet, IScatteringShoot, IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	[SerializeField]
	[Range(0f, 3f)]
	private float shootDuration = 0.75f;

	[SerializeField]
	private Vector2Int bulletCountRange = new Vector2Int(3, 5);

	[SerializeField]
	private float sectorAngle = 60f;

	[SerializeField]
	[Min(1f)]
	private int times = 1;

	[SerializeField]
	[Min(0f)]
	private float timeInterval = 0.6f;

	private bool MultiTimes => times > 1;

	public float ShootDuration => shootDuration;

	public Vector2Int BulletCountRange => bulletCountRange;

	public float SectorAngle => sectorAngle;

	public int Times => times;

	public float TimeInterval => timeInterval;

	private void LoadDefaultValues_ScatteringShoot()
	{
		shootDuration = 0.75f;
		bulletCountRange = new Vector2Int(3, 5);
		sectorAngle = 60f;
		times = 1;
		timeInterval = 0.6f;
	}
}
