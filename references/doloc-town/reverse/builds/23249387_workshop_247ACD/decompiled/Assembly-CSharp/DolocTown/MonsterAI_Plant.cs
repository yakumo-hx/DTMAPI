using UnityEngine;

namespace DolocTown;

[MonsterAI("fungus")]
public class MonsterAI_Plant : MonsterAI
{
	private class IdleState : MonsterStateManager.MonsterState<MonsterAI_Plant>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.Module.Enemy == null)
			{
				return this;
			}
			if (!_controller.TryGetFirstAvailableWorkableAttackBehaviour(base.Module.Enemy, out var _))
			{
				return this;
			}
			return GetState<AttackState>();
		}
	}

	private class AttackState : MonsterStateManager.AttackState<MonsterAI_Plant>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (!base.IsAttacking)
			{
				return GetState<IdleState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			if (_controller.TryGetFirstAvailableWorkableAttackBehaviour(base.Module.Enemy, out var id))
			{
				_controller.Attack(id, base.Module.Enemy);
			}
		}
	}

	private MonsterSight _sight;

	private Transform Enemy => _sight.LockedTarget;

	public override void OnStart()
	{
		base.OnStart();
		_sight = _controller.GetComponentInChildren<MonsterSight>(includeInactive: true);
	}
}
