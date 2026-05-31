using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[MonsterAI("amoeba")]
[MonsterAI("amoeba_wet_land")]
public class MonsterAI_Amoeba : MonsterAI
{
	private class AnyState : MonsterStateManager.MonsterState<MonsterAI_Amoeba>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.Module.lockedTarget != null)
			{
				return GetState<PursuitState>();
			}
			return GetState<PatrolState>();
		}
	}

	private class PursuitState : MonsterStateManager.MoveState<MonsterAI_Amoeba>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.Module.lockedTarget != null && _controller.TryGetFirstAvailableWorkableAttackBehaviour(base.Module.lockedTarget, out var _))
			{
				return GetState<AttackState>();
			}
			if (isMoving)
			{
				return null;
			}
			if (base.Module.angryCounter-- > 0)
			{
				return GetState<PursuitState>();
			}
			return GetState<PatrolState>();
		}

		public override void OnEnter()
		{
			isMoving = _controller.MoveTo(base.Module.lockedTarget);
		}
	}

	private class AttackState : MonsterStateManager.AttackState<MonsterAI_Amoeba>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.IsAttacking)
			{
				return null;
			}
			return GetState<PursuitState>();
		}

		public override void OnEnter()
		{
			if (_controller.attackBehaviourManager.TryGetFirstAvailableAttackBehaviour(out var type))
			{
				_controller.Attack(type, base.Module.lockedTarget);
			}
		}
	}

	private class PatrolState : MonsterStateManager.MoveState<MonsterAI_Amoeba>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.Module.angryCounter > 0 && Vector2.Distance(DolocAPI.AgentPosition, _controller.transform.position) < 5f)
			{
				return GetState<PursuitState>();
			}
			if (!isMoving)
			{
				if (!RandomUtils.Dice(0.3f))
				{
					return this;
				}
				return GetState<IdleState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			isMoving = _controller.MoveTo(MoveTargetType.Random);
		}
	}

	private class IdleState : MonsterStateManager.MonsterState<MonsterAI_Amoeba>
	{
		private float waitTime;

		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.Module.angryCounter > 0)
			{
				return GetState<PursuitState>();
			}
			if (waitTime > 0f)
			{
				waitTime -= dt;
				return null;
			}
			return GetState<PatrolState>();
		}

		public override void OnEnter()
		{
			waitTime = Random.Range(1f, 3f);
			_controller.Renderer.PlayAnimation("idle");
		}

		public void SetWaitTime(float duration)
		{
			waitTime = Mathf.Max(duration, 0f);
		}
	}

	private int angryCounter;

	private Transform lockedTarget;

	private bool isAmoebaHatCooling;

	private RSTimer amoebaHatCoolTimer = new RSTimer(5f);

	private readonly RSTimer timer = new RSTimer(3f);

	public override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (isAmoebaHatCooling && amoebaHatCoolTimer.Tick(dt))
		{
			isAmoebaHatCooling = false;
		}
		if (angryCounter > 0 && timer.Tick(dt))
		{
			angryCounter--;
			if (angryCounter <= 0)
			{
				angryCounter = 0;
				lockedTarget = null;
			}
		}
	}

	public override void OnHurt(Transform attacker)
	{
		if (angryCounter <= 0)
		{
			DolocAPI.RaiseEmotion(_controller.transform, EmotionName.ANGRY);
		}
		angryCounter += 20;
		lockedTarget = attacker;
	}

	public override void OnTouchByAgent()
	{
		if (!(lockedTarget != null) && DolocAPI.AgentEquipmentManager.TryGetAgentEquipmentFunction<AgentEquipmentFunctionAmoeba>(out var _) && !isAmoebaHatCooling)
		{
			isAmoebaHatCooling = true;
			amoebaHatCoolTimer.Reset();
			DolocAPI.RaiseEmotion(_controller.transform, EmotionName.LOVE);
		}
	}
}
