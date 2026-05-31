using DolocTown.GameData;

namespace DolocTown;

public class AgentStateJump : AgentStateBase
{
	public int jumpTimes { get; set; }

	public override bool SupportJump => jumpTimes > 0;

	public AgentStateJump(AgentStateManager parent, BodyController body)
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
		jumpTimes--;
		status.VelocityY = body.MotionAbility.JumpForce;
		body.PlayAnimation("jump");
		body.HatRenderer.OnJump();
		DolocAPI.RaiseInstantAnimEffects(body.transform.position, InstAnimEffectType.PLAYER_JUMP_SMOKE);
		body.SortingOrder = 1;
		if (DolocAPI.archiveHandle.DoubleJumpTimes() == 2 && jumpTimes == 0)
		{
			DolocAPI.Broadcast(OperationEventType.DOUBLE_JUMP);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_DOUBLE_JUMP);
		}
		else
		{
			DolocAPI.Broadcast(OperationEventType.JUMP);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_JUMP);
		}
	}

	public override void OnExit()
	{
	}

	public override void OnPlay()
	{
		status.Move(body.MoveSpeed);
	}
}
