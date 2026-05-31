using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

[MonsterAI("bee")]
[MonsterAI("drone")]
[MonsterAI("drone_ex")]
[MonsterAI("ball_drone")]
[MonsterAI("aircraft")]
public class MonsterAI_IndieGuarder : MonsterAI
{
	private class PatrolState : MonsterStateManager.MoveState<MonsterAI_IndieGuarder>
	{
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
			isMoving = _controller.Patrol(base.Module.strategicPoint, 8f);
		}
	}

	private class PursuitState : MonsterStateManager.MoveState<MonsterAI_IndieGuarder>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.Module.TooFarFromStrategicPoint)
			{
				base.Module._sight.ClearEnemy();
				return GetState<PatrolState>();
			}
			Transform lockedTarget = base.Module._sight.LockedTarget;
			if (_controller.TryGetFirstAvailableWorkableAttackBehaviour(lockedTarget, out var _))
			{
				return GetState<AttackState>();
			}
			if (!isMoving)
			{
				if (!(base.Module._sight.LockedTarget == null))
				{
					return GetState<PursuitState>();
				}
				return GetState<PatrolState>();
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

	private class AttackState : MonsterStateManager.AttackState<MonsterAI_IndieGuarder>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.IsAttacking)
			{
				return null;
			}
			if (base.Module.ShouldQuitFromPursuit)
			{
				return GetState<PatrolState>();
			}
			return GetState<PursuitState>();
		}

		public override void OnEnter()
		{
			Transform lockedTarget = base.Module._sight.LockedTarget;
			if (!(lockedTarget == null) && _controller.TryGetFirstAvailableWorkableAttackBehaviour(lockedTarget, out var id))
			{
				_controller.Attack(id, lockedTarget);
			}
		}
	}

	private Vector2 strategicPoint;

	private MonsterSight _sight;

	private bool HasEnemyNow => _sight.LockedTarget != null;

	private bool TooFarFromStrategicPoint => Vector2.Distance(_controller.transform.position, strategicPoint) > _sight.tooFarDistance;

	private bool ShouldPursuit
	{
		get
		{
			if (_sight.LockedTarget != null)
			{
				return !TooFarFromStrategicPoint;
			}
			return false;
		}
	}

	private bool ShouldQuitFromPursuit
	{
		get
		{
			if (!(_sight.LockedTarget == null))
			{
				return TooFarFromStrategicPoint;
			}
			return true;
		}
	}

	public override void OnStart()
	{
		base.OnStart();
		strategicPoint = _controller.Monster.position;
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
