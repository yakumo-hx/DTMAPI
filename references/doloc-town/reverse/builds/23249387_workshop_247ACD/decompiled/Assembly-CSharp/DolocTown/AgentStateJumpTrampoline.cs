using UnityEngine;

namespace DolocTown;

public class AgentStateJumpTrampoline : AgentStateBase
{
	public override bool SupportJump => true;

	public AgentStateJumpTrampoline(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (status.IsDrop)
		{
			return parent.GetState<AgentStateDrop>();
		}
		if (status.IsGrounded)
		{
			return base.NextStateOnGround;
		}
		return this;
	}

	public override void OnEnter()
	{
		parent.GetState<AgentStateJump>().jumpTimes = 1;
		status.VelocityY = body.MotionAbility.JumpForceSuper;
		body.PlayAnimation("jump");
		DolocAPI.RaiseInstantAnimEffects(body.transform.position, InstAnimEffectType.PLAYER_JUMP_SMOKE);
		body.gameObject.layer = LayerMask.NameToLayer("PlatformExclude");
		body.SortingOrder = 1;
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_SUPER_JUMP);
	}

	public override void OnExit()
	{
		body.gameObject.layer = LayerMask.NameToLayer("Player");
	}

	public override void OnPlay()
	{
		status.Move(body.MoveSpeed);
	}
}
