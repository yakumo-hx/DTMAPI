namespace DolocTown;

public class AgentStateYawn : AgentStateBase
{
	public override bool SupportJump => true;

	public override bool SupportUseItem => true;

	public override bool SupportInteract => true;

	public AgentStateYawn(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (status.IsDrop)
		{
			return parent.GetState<AgentStateDrop>();
		}
		if (status.HorizontalMoveFactor != 0f)
		{
			return parent.GetState<AgentStateMove>();
		}
		if (IsAnimationDone("yawn"))
		{
			return parent.GetState<AgentStateIdle>();
		}
		return this;
	}

	public override void OnEnter()
	{
		status.VelocityX = 0f;
		body.PlayAnimation("yawn");
	}

	public override void OnExit()
	{
		base.OnExit();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_CHARACTER_YAWN);
	}
}
