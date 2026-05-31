using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[MonsterAI("nian_tail")]
public class MonsterAI_NianTail : MonsterAI
{
	public class WanderState : MonsterStateManager.MoveState<MonsterAI_NianTail>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (isMoving)
			{
				if (_controller.IsAnyAttackBehaviourAvailable(out var _))
				{
					return GetState<AttackState>();
				}
				return null;
			}
			return GetState<WanderState>();
		}

		public override void OnFixedUpdate(float dt)
		{
			base.OnFixedUpdate(dt);
			base.Module.TickEffects(dt);
		}

		public override void OnEnter()
		{
			base.OnEnter();
			if (base.Module._sight != null)
			{
				base.Module._sight.ValidateSight();
			}
			isMoving = _controller.MoveTo(MoveTargetType.Around);
		}
	}

	public class AttackState : MonsterStateManager.AttackState<MonsterAI_NianTail>
	{
		private bool _isMoving;

		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (base.IsAttacking)
			{
				return null;
			}
			return GetState<WanderState>();
		}

		public override void OnFixedUpdate(float dt)
		{
			base.Module.TickEffects(dt);
			base.OnFixedUpdate(dt);
			if (_isMoving && _controller.mover.Move(dt))
			{
				_isMoving = _controller.MoveTo(MoveTargetType.Around);
			}
		}

		public override void OnEnter()
		{
			_isMoving = _controller.MoveTo(MoveTargetType.Around);
			if (_controller.TryGetFirstAvailableWorkableAttackBehaviour(base.Module._sight.LockedTarget, out var id))
			{
				_controller.Attack(id, base.Module._sight.LockedTarget);
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

	private MonsterSight _sight;

	private readonly RSTimer _timer = new RSTimer(3f);

	private Transform LockedTarget => _sight.LockedTarget;

	public override void OnStart()
	{
		base.OnStart();
		_sight = _controller.GetComponentInChildren<MonsterSight>(includeInactive: true);
	}

	public void TickEffects(float dt)
	{
		if (_timer.Tick(dt))
		{
			_controller.transform.RaiseEmotion((!RandomUtils.Dice(0.3f)) ? EmotionName.CONFUSE : EmotionName.NOCOMMENT);
			DolocAPI.RaiseInstantPSEffects(_controller.position, InstantParticleEffectsType.ELECTRIC_SPARKS);
		}
	}
}
