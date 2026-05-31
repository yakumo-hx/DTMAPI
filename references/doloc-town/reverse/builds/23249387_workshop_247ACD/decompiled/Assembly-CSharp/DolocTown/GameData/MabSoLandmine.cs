using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoLandmine : MonsterAttackBehaviourSOSkill, ILandmine, IMonsterAttackBehaviourSkill, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	[SerializeField]
	[Min(0f)]
	private float validDistance = 10f;

	[SerializeField]
	[Min(0f)]
	private float liveDuration = 10f;

	[SerializeField]
	private float moveSpeed = 8f;

	[SerializeField]
	[Min(0f)]
	private float reflectionTime = 1f;

	public float ValidDistance => validDistance;

	public float LiveDuration => liveDuration;

	public float MoveSpeed => moveSpeed;

	public float ReflectionTime => reflectionTime;

	private void LoadDefaultValues_Landmine()
	{
		validDistance = 10f;
		liveDuration = 10f;
		moveSpeed = 8f;
		reflectionTime = 1f;
	}
}
