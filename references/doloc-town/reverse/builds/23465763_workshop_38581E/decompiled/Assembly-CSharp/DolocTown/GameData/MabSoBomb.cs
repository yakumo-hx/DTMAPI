using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoBomb : MonsterAttackBehaviourSOSkill, IBomb, IMonsterAttackBehaviourSkill, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	[SerializeField]
	[Min(0f)]
	private float validDistance = 10f;

	[SerializeField]
	[Min(3f)]
	private float heightDstThreshold = 5f;

	public float ValidDistance => validDistance;

	public float HeightDstThreshold => heightDstThreshold;

	private void LoadDefaultValues_Bomb()
	{
		validDistance = 10f;
		heightDstThreshold = 5f;
	}
}
