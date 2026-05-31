using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public abstract class MonsterAttackBehaviourSOSkill : MonsterAttackBehaviourSO, IMonsterAttackBehaviourSkill, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	[SerializeField]
	protected string skillId;

	[SerializeField]
	[Min(0f)]
	[Tooltip("如果目标技能没有伤害可以忽略该值")]
	protected float damage = 10f;

	[SerializeField]
	[Range(0f, 1f)]
	[Tooltip("如果目标技能没有暴击率可以忽略该值")]
	protected float criticalRate;

	[SerializeField]
	protected bool hasReadyAction;

	[SerializeField]
	[Range(0f, 3f)]
	protected float readyDuration = 0.5f;

	public string SkillId => skillId;

	public float Damage => damage;

	public float CriticalRate => criticalRate;

	public bool HasReadyAction => hasReadyAction;

	public float ReadyDuration => readyDuration;

	private void LoadDefaultValues_Skill()
	{
		skillId = null;
		damage = 10f;
		criticalRate = 0f;
		hasReadyAction = false;
		readyDuration = 0.5f;
	}
}
