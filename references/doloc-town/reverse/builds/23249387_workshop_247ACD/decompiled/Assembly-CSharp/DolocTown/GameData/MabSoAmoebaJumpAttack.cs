using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoAmoebaJumpAttack : MonsterAttackBehaviourSOPhysical, IAmoebaJumpAttack, IMonsterAttackBehaviourPhysical, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction
{
	[SerializeField]
	[Range(3f, 8f)]
	private float _attackRange = 5f;

	[SerializeField]
	[Min(0f)]
	private float _jumpSpeed = 8f;

	[SerializeField]
	[Min(0f)]
	private float _maxJumpHeight = 4.5f;

	public float JumpSpeed => _jumpSpeed;

	public float AttackRange => _attackRange;

	public float MaxJumpHeight => _maxJumpHeight;

	private void LoadDefaultValues_AmoebaJumpAttack()
	{
		_attackRange = 5f;
		_jumpSpeed = 8f;
		_maxJumpHeight = 4.5f;
	}
}
