using UnityEngine;

namespace DolocTown;

[MonsterAI("seed_carrier")]
public class MonsterAI_SeedCarrier : MonsterAI
{
	private class WanderState : MonsterStateManager.MoveState<MonsterAI_SeedCarrier>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.Module.HasEnemy && _controller.TryGetFirstAvailableWorkableAttackBehaviour(base.Module.Enemy, out var _))
			{
				return GetState<AttackState>();
			}
			if (!isMoving)
			{
				return GetState<WanderState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			isMoving = _controller.MoveTo(MoveTargetType.Random);
		}
	}

	private class AttackState : MonsterStateManager.AttackState<MonsterAI_SeedCarrier>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (!base.IsAttacking)
			{
				return GetState<WanderState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			if (_controller.attackBehaviourManager.TryGetFirstAvailableAttackBehaviour(out var type))
			{
				_controller.Attack(type, base.Module.Enemy);
			}
		}
	}

	private MonsterSight _sight;

	private bool HasEnemy => _sight.LockedTarget != null;

	private Transform Enemy => _sight.LockedTarget;

	public override void OnStart()
	{
		base.OnStart();
		_sight = _controller.GetComponentInChildren<MonsterSight>(includeInactive: true);
	}
}
