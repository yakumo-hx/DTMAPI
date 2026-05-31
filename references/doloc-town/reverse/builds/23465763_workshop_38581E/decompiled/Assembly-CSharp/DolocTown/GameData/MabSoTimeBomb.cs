using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoTimeBomb : MonsterAttackBehaviourSOSkill, ITimeBomb, IMonsterAttackBehaviourSkill, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	[SerializeField]
	[Min(0f)]
	private float validDistance = 10f;

	[SerializeField]
	[Min(0f)]
	private float moveSpeed = 8f;

	[SerializeField]
	private float maxMoveSpeed = 12f;

	[SerializeField]
	[Min(1f)]
	private int bombCount = 1;

	private bool MultiBomb => bombCount > 1;

	public float ValidDistance => validDistance;

	public float MoveSpeed => moveSpeed;

	public float MaxMoveSpeed => maxMoveSpeed;

	public int Count => bombCount;

	private void LoadDefaultValues_TimeBomb()
	{
		validDistance = 10f;
		moveSpeed = 8f;
		maxMoveSpeed = 12f;
		bombCount = 1;
	}
}
