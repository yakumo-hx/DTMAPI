using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoShoot : MonsterAttackBehaviourSOBullet, IShoot, IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	[SerializeField]
	[Range(0f, 1.5f)]
	protected float shootDuration = 0.75f;

	public float ShootDuration => shootDuration;
}
