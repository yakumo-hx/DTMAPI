using System;

namespace DolocTown;

public class AgentStateThrow : AgentStateBase
{
	public override bool SupportInteract => false;

	public override bool SupportJump => false;

	public override bool SupportUseItem => false;

	public override bool SupportDash => false;

	public AgentStateThrow(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		throw new NotImplementedException();
	}
}
