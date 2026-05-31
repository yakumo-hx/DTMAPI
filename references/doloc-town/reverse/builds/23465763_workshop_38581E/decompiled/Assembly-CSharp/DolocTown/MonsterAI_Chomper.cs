using RedSaw;
using UnityEngine;

namespace DolocTown;

[MonsterAI("chomper")]
public class MonsterAI_Chomper : MonsterAI
{
	private class WanderState : MonsterStateManager.MoveState<MonsterAI_Chomper>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (isMoving)
			{
				return null;
			}
			return GetState<WaitState>();
		}

		public override void OnEnter()
		{
			if (DolocAPI.AgentEquipmentManager.IsChomperMimicryForbidden)
			{
				isMoving = false;
				return;
			}
			isMoving = _controller.MoveTo(MoveTargetType.Around);
			if (RandomUtils.Dice(0.26f))
			{
				_controller.Attack(MonsterAttackId.AtkSkill01, null);
			}
		}
	}

	private class WaitState : MonsterStateManager.MonsterState<MonsterAI_Chomper>
	{
		private float waitTime = 1f;

		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.Module.attacker != null && _controller.IsAttackBehaviourWork(MonsterAttackId.AtkNormal, base.Module.attacker))
			{
				return GetState<AttackState>();
			}
			if (!(waitTime > 0f))
			{
				return GetState<WanderState>();
			}
			return null;
		}

		public override void OnFixedUpdate(float dt)
		{
			if (waitTime > 0f)
			{
				waitTime -= dt;
			}
		}

		public override void OnEnter()
		{
			bool flag = RandomUtils.Dice(0.3f);
			_controller.Renderer.PlayAnimation(flag ? "breath" : "idle");
			waitTime = Random.Range(6, 12);
		}
	}

	private class AttackState : MonsterStateManager.AttackState<MonsterAI_Chomper>
	{
		public bool ShouldFleeAtNextState { get; set; }

		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.IsAttacking)
			{
				return null;
			}
			if (ShouldFleeAtNextState)
			{
				ShouldFleeAtNextState = false;
				return GetState<WanderState>();
			}
			if (!RandomUtils.Dice(0.2f))
			{
				return GetState<WanderState>();
			}
			return GetState<ProvocationState>();
		}

		public override void OnEnter()
		{
			if (base.Module.attacker != null && _controller.IsAttackBehaviourWorkAndAvailable(MonsterAttackId.AtkNormal, base.Module.attacker))
			{
				_controller.Attack(MonsterAttackId.AtkNormal, base.Module.attacker);
			}
		}
	}

	private class ProvocationState : MonsterStateManager.MonsterState<MonsterAI_Chomper>
	{
		private float waitTime;

		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (!(waitTime > 0f))
			{
				return GetState<WaitState>();
			}
			return null;
		}

		public override void OnFixedUpdate(float dt)
		{
			if (waitTime > 0f)
			{
				waitTime -= dt;
			}
		}

		public override void OnEnter()
		{
			waitTime = Random.Range(1.5f, 3.5f);
			_controller.Renderer.PlayAnimation("provocation");
		}
	}

	private Transform attacker;

	private readonly RSTimerLock counterBackLocker = new RSTimerLock(15f);

	public override void OnFixedUpdate(float dt)
	{
		base.OnFixedUpdate(dt);
		counterBackLocker.Tick(dt);
	}

	public override void OnHurt(Transform attacker)
	{
		this.attacker = attacker;
		if (counterBackLocker.IsLocked)
		{
			counterBackLocker.Lock();
			base.StateManager.EnterState<AttackState>().ShouldFleeAtNextState = true;
		}
	}
}
