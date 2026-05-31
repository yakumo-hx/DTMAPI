namespace DolocTown;

public class AgentStateMove : AgentStateBase
{
	private bool isRunning;

	public override bool SupportJump => true;

	public override bool SupportUseItem => status.IsGrounded;

	public override bool SupportInteract => true;

	public AgentStateMove(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (isRunning)
		{
			if (body.MotionAbility.ShouldWalk)
			{
				isRunning = false;
				body.PlayAnimation("walk");
			}
		}
		else if (body.MotionAbility.ShouldRun)
		{
			isRunning = true;
			body.PlayAnimation("run");
		}
		return base.NextStateUniversal;
	}

	public override void OnEnter()
	{
		DolocAPI.RaiseInstantAnimEffects(body.transform.position, InstAnimEffectType.PLAYER_WALK_SMOKE, body.transform.localScale.x < 0f);
		body.PlayAnimation(body.MotionAbility.ShouldWalk ? "walk" : "run");
		DolocAPI.Broadcast(OperationEventType.MOVE);
		status.VelocityY = 0f;
	}

	public override void OnExit()
	{
		base.OnExit();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_CHARACTER_FOOTSTEP);
	}

	public override void OnPlay()
	{
		status.Move(body.MoveSpeed);
	}
}
