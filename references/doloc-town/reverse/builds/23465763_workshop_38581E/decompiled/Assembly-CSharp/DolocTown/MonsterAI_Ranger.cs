using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

[MonsterAI("tardigrade")]
[MonsterAI("bomber")]
public class MonsterAI_Ranger : MonsterAI
{
	private class PatrolState : MonsterStateManager.MoveState<MonsterAI_Ranger>
	{
		private int transitionCounter = 8;

		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.Module.ShouldPursuit)
			{
				return GetState<PursuitState>();
			}
			if (!isMoving)
			{
				return GetState<PatrolState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			if (transitionCounter-- <= 0)
			{
				transitionCounter = 8;
				isMoving = _controller.MoveTo(MoveTargetType.Random);
			}
			else
			{
				isMoving = _controller.MoveTo(MoveTargetType.Around);
			}
		}
	}

	private class PursuitState : MonsterStateManager.MoveState<MonsterAI_Ranger>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			Transform lockedTarget = base.Module._sight.LockedTarget;
			if (lockedTarget == null)
			{
				base.Module._sight.ClearEnemy();
				return GetState<PatrolState>();
			}
			if (_controller.TryGetFirstAvailableWorkableAttackBehaviour(lockedTarget, out var _))
			{
				return GetState<AttackState>();
			}
			if (!isMoving)
			{
				return GetState<PursuitState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			Transform lockedTarget = base.Module._sight.LockedTarget;
			if (!(lockedTarget == null))
			{
				isMoving = _controller.MoveTo(lockedTarget);
			}
		}
	}

	private class AttackState : MonsterStateManager.AttackState<MonsterAI_Ranger>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (!base.IsAttacking)
			{
				return GetState<PursuitState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			Transform lockedTarget = base.Module._sight.LockedTarget;
			if (_controller.TryGetFirstAvailableWorkableAttackBehaviour(lockedTarget, out var id))
			{
				_controller.Attack(id, lockedTarget);
			}
		}
	}

	private MonsterSight _sight;

	public bool ShouldPursuit => _sight.LockedTarget != null;

	public override void OnStart()
	{
		base.OnStart();
		_sight = _controller.GetComponentInChildren<MonsterSight>(includeInactive: true);
	}

	public override void OnHurt(Transform attacker)
	{
		if (!(_sight.LockedTarget != null))
		{
			DolocAPI.RaiseEmotion(_controller.transform, EmotionName.ANGRY);
			_sight.SetTarget(attacker);
		}
	}
}
