using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public abstract class MonsterAttackBehaviourSOPhysical : MonsterAttackBehaviourSO, IMonsterAttackBehaviourPhysical, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
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

	public bool HasReadyAction => hasReadyAction;

	public float ReadyDuration => readyDuration;

	public float Damage => damage;

	public float CriticalRate => criticalRate;

	private void LoadDefaultValues_Physical()
	{
		damage = 10f;
		criticalRate = 0f;
		hasReadyAction = false;
		readyDuration = 0.5f;
	}
}
