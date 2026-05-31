namespace DolocTown;

public class AgentStateSit : AgentStateBase
{
	public AgentStateSit(AgentStateManager stateManager, BodyController controller)
		: base(stateManager, controller)
	{
	}

	protected override AgentStateBase NextState()
	{
		return this;
	}

	public override void OnEnter()
	{
		body.SetAnimatorUpdateUnscaled(value: true);
		body.PlayAnimation("sit");
	}

	public override void OnExit()
	{
		base.OnExit();
		body.SetAnimatorUpdateUnscaled(value: false);
	}
}
