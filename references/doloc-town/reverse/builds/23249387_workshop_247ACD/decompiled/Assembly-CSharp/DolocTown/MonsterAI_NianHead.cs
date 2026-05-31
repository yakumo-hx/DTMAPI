using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[MonsterAI("nian_head")]
public class MonsterAI_NianHead : MonsterAI
{
	public class WanderState : MonsterStateManager.MoveState<MonsterAI_NianHead>
	{
		private readonly Vector2Int[] DragonPath01 = new Vector2Int[5]
		{
			Vector2Int.zero,
			new Vector2Int(3, 3),
			new Vector2Int(6, 0),
			new Vector2Int(9, 3),
			new Vector2Int(12, 0)
		};

		private readonly Vector2Int[] DragonPath02 = new Vector2Int[5]
		{
			Vector2Int.zero,
			new Vector2Int(-3, -3),
			new Vector2Int(-6, 0),
			new Vector2Int(-9, -3),
			new Vector2Int(-12, 0)
		};

		private readonly Vector2Int[] DragonPath03 = new Vector2Int[6]
		{
			Vector2Int.zero,
			new Vector2Int(0, 4),
			new Vector2Int(-4, 0),
			new Vector2Int(0, -4),
			new Vector2Int(4, 0),
			new Vector2Int(0, 4)
		};

		private readonly Vector2Int[] DragonPath04 = new Vector2Int[9]
		{
			Vector2Int.zero,
			new Vector2Int(-3, 2),
			new Vector2Int(-5, 5),
			new Vector2Int(-3, 8),
			new Vector2Int(0, 10),
			new Vector2Int(3, 8),
			new Vector2Int(5, 5),
			new Vector2Int(3, 2),
			Vector2Int.zero
		};

		private readonly Vector2Int[] DragonPath05 = new Vector2Int[7]
		{
			Vector2Int.zero,
			new Vector2Int(4, 4),
			new Vector2Int(8, 0),
			new Vector2Int(12, 4),
			new Vector2Int(16, 0),
			new Vector2Int(20, 4),
			new Vector2Int(24, 0)
		};

		private readonly Vector2Int[] DragonPath06 = new Vector2Int[7]
		{
			Vector2Int.zero,
			new Vector2Int(-4, 4),
			new Vector2Int(-8, 0),
			new Vector2Int(-12, 4),
			new Vector2Int(-16, 0),
			new Vector2Int(-20, 4),
			new Vector2Int(-24, 0)
		};

		private readonly Vector2Int[][] AllDragonPaths;

		private int interval;

		public WanderState()
		{
			AllDragonPaths = new Vector2Int[6][] { DragonPath01, DragonPath02, DragonPath03, DragonPath04, DragonPath05, DragonPath06 };
			ResetInterval();
		}

		private void ResetInterval()
		{
			interval = Random.Range(3, 5);
		}

		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (isMoving)
			{
				if (base.Module.ShouldPursuitTarget)
				{
					return GetState<PursuitState>();
				}
				return null;
			}
			if (base.Module._sight.LockedTarget != null && RandomUtils.Dice(0.5f))
			{
				return GetState<AttackState>();
			}
			return GetState<WanderState>();
		}

		public override void OnEnter()
		{
			base.OnEnter();
			if (base.Module._sight != null)
			{
				base.Module._sight.ValidateSight();
			}
			if (--interval <= 0)
			{
				ResetInterval();
				MoveToAround();
				return;
			}
			Vector2Int[] array = TryGetDragonPath();
			if (array == null)
			{
				isMoving = _controller.MoveTo(MoveTargetType.Around);
				return;
			}
			((MonsterMoverContextSteering)_controller.mover).SetOverridenPath(array);
			isMoving = _controller.MoveTo(MoveTargetType.Around);
		}

		private void MoveToAround()
		{
			if (_controller.Env.AirMap.RaycastToGround(_controller.position, out var result) && _controller.Env.AirMap.GetAroundPositionWS(result, _controller.MonsterProto.MoverProto.aroundRange, out var result2))
			{
				isMoving = _controller.MoveTo(result2);
			}
			else
			{
				isMoving = _controller.MoveTo(MoveTargetType.Around);
			}
		}

		private Vector2Int[] TryGetDragonPath()
		{
			Vector2Int from = _controller.Env.AirMap.WorldToCell(_controller.PositionCenter);
			List<Vector2Int[]> list = new List<Vector2Int[]>();
			Vector2Int[][] allDragonPaths = AllDragonPaths;
			foreach (Vector2Int[] pathPat in allDragonPaths)
			{
				if (TryBuildPath(pathPat, from, out var result))
				{
					list.Add(result);
				}
			}
			if (list.Count == 0)
			{
				return null;
			}
			return list.Choice();
		}

		private bool TryBuildPath(Vector2Int[] pathPat, Vector2Int from, out Vector2Int[] result)
		{
			Vector2Int[] array = new Vector2Int[pathPat.Length];
			for (int i = 0; i < pathPat.Length; i++)
			{
				array[i] = from + pathPat[i];
				if (!_controller.Env.AirMap.IsEmpty(array[i]))
				{
					result = null;
					return false;
				}
			}
			result = array;
			return true;
		}
	}

	public class AttackState : MonsterStateManager.AttackState<MonsterAI_NianHead>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.IsAttacking)
			{
				return null;
			}
			if (base.Module.ShouldPursuitTarget)
			{
				return GetState<PursuitState>();
			}
			return GetState<WanderState>();
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

	public class DropState : MonsterStateManager.MonsterState<MonsterAI_Nian>
	{
		private bool countingDown;

		private float waitDuration;

		private bool _isWarned;

		private const float warnThreshold = 1f;

		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (countingDown && waitDuration <= 0f)
			{
				return GetState<WanderState>();
			}
			return null;
		}

		public override void OnFixedUpdate(float dt)
		{
			base.OnFixedUpdate(dt);
			if (countingDown)
			{
				waitDuration -= dt;
				if (!_isWarned && waitDuration <= 1f && _controller.isVisible)
				{
					_isWarned = true;
					_controller.RaiseDangerWarning02();
					_controller.PostSoundEvent(SoundEvents.PLAY_DRONE_BOMB_COUNT_DOWN);
				}
			}
		}

		public void StartCountdown(float duration)
		{
			waitDuration = duration;
			countingDown = true;
		}

		public override void OnEnter()
		{
			countingDown = false;
		}

		public override void OnExit()
		{
			base.OnExit();
			_controller.GetComponent<MonsterDecoratorNian>().OnExitDropState();
		}
	}

	public class PursuitState : MonsterStateManager.MoveState<MonsterAI_NianHead>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (isMoving)
			{
				if (!base.Module.ShouldPursuitTarget)
				{
					return GetState<WanderState>();
				}
				if (_controller.TryGetFirstAvailableWorkableAttackBehaviour(base.Module.LockedTarget, out var _))
				{
					return GetState<AttackState>();
				}
				return null;
			}
			if (base.Module.ShouldPursuitTarget)
			{
				return GetState<PursuitState>();
			}
			return GetState<WanderState>();
		}

		public override void OnEnter()
		{
			if (Vector2.Distance(_controller.position2d, DolocAPI.AgentPosition) < 3f)
			{
				isMoving = _controller.MoveAround(DolocAPI.AgentPosition);
				if (isMoving)
				{
					return;
				}
			}
			isMoving = _controller.MoveTo(MoveTargetType.Player);
		}
	}

	private MonsterSight _sight;

	private Transform LockedTarget => _sight.LockedTarget;

	private bool ShouldPursuitTarget => LockedTarget != null;

	public override void OnStart()
	{
		base.OnStart();
		_sight = _controller.GetComponentInChildren<MonsterSight>(includeInactive: true);
	}
}
