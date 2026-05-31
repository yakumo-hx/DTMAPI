using System;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public abstract class MonsterAttackBehaviourSO : IMonsterAttackBehaviour
{
	[SerializeField]
	protected MonsterAttackId attackId;

	[SerializeField]
	protected bool cdOnStart;

	[SerializeField]
	[Min(0f)]
	protected float cdDuration = 5f;

	public MonsterAttackId AttackId => attackId;

	public bool CDOnStart => cdOnStart;

	public float CDDuration => cdDuration;

	private void LoadDefaultValues()
	{
		cdOnStart = false;
		cdDuration = 5f;
		attackId = MonsterAttackId.Unset;
	}

	public virtual void OnValidate()
	{
	}
}
