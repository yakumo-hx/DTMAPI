using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public abstract class MonsterAttackBehaviourSOBullet : MonsterAttackBehaviourSO, IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction
{
	[SerializeField]
	private string bulletId;

	[SerializeField]
	private string bulletMoverId = "linear";

	[SerializeField]
	[Range(5f, 30f)]
	private float moveSpeed = 18f;

	[SerializeField]
	[Range(0f, 6f)]
	private float bulletDuration = 2.5f;

	[SerializeField]
	private float damage = 10f;

	[SerializeField]
	[Range(0f, 1f)]
	private float criticalRate;

	[SerializeField]
	private bool hasReadyAction;

	[SerializeField]
	[Range(0f, 3f)]
	private float readyDuration = 0.5f;

	[SerializeField]
	private bool disableLaunchEffect;

	[SerializeField]
	private string fireSound;

	public string BulletId => bulletId;

	public string BulletMoverId => bulletMoverId;

	public bool DisableRecoil => false;

	public float BulletSpeed => moveSpeed;

	public float BulletDuration => bulletDuration;

	public float Damage => damage;

	public float CriticalRate => criticalRate;

	public bool HasReadyAction => hasReadyAction;

	public float ReadyDuration => readyDuration;

	public bool DisableShootEffects => disableLaunchEffect;

	public string FireSound => fireSound;

	private void LoadDefaultValues_Bullet()
	{
		bulletId = null;
		moveSpeed = 18f;
		bulletDuration = 2.5f;
		damage = 10f;
		criticalRate = 0f;
		hasReadyAction = false;
		readyDuration = 0.5f;
		fireSound = "PLAY_DRONE_ATTACK";
	}

	public override void OnValidate()
	{
		base.OnValidate();
		if (string.IsNullOrEmpty(bulletId))
		{
			bulletId = "monster_01";
		}
		if (string.IsNullOrEmpty(bulletMoverId))
		{
			bulletMoverId = "linear";
		}
	}
}
