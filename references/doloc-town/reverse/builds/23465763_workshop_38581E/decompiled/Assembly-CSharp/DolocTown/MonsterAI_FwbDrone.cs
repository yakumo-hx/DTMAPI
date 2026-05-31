using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

[MonsterAI("fwb_drone")]
public class MonsterAI_FwbDrone : MonsterAI_Group
{
	private class WanderState : MonsterStateManager.MoveState<MonsterAI_FwbDrone>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.Module.isAngry)
			{
				return GetState<PursuitState>();
			}
			if (!isMoving)
			{
				return GetState<WanderState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			isMoving = _controller.MoveTo(MoveTargetType.Around);
		}
	}

	private class PursuitState : MonsterStateManager.MoveState<MonsterAI_FwbDrone>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (_controller.TryGetFirstAvailableWorkableAttackBehaviour(base.Module.lockedTarget, out var _))
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
			if (base.Module.lockedTarget != null)
			{
				isMoving = _controller.MoveTo(base.Module.lockedTarget);
			}
		}
	}

	private class AttackState : MonsterStateManager.AttackState<MonsterAI_FwbDrone>
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
			if (_controller.TryGetFirstAvailableWorkableAttackBehaviour(base.Module.lockedTarget, out var id))
			{
				_controller.Attack(id, base.Module.lockedTarget);
			}
		}
	}

	private bool isAngry;

	private Transform lockedTarget;

	public override void OnStart()
	{
		base.OnStart();
		_controller.Renderer.PlayAnimation(isAngry ? "idle_angry" : "idle");
	}

	private void _OnHurt(Transform attacker)
	{
		if (!isAngry)
		{
			isAngry = true;
			lockedTarget = attacker;
			DolocAPI.RaiseEmotion(_controller.transform, EmotionName.ANGRY);
			_controller.Renderer.PlayAnimation("idle_angry");
		}
	}

	public override void OnHurt(Transform attacker)
	{
		_OnHurt(attacker);
		((FwbDroneGroup)base.group).BroadcastHurt(attacker);
	}

	public void OnBroadcastHurt(Transform transform)
	{
		_OnHurt(transform);
	}
}
