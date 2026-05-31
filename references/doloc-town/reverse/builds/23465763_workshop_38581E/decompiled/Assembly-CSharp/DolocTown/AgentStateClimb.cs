using UnityEngine;

namespace DolocTown;

public class AgentStateClimb : AgentStateBase
{
	public AgentStateClimb(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (body.IsClimb && body.Status.IsTouchWall)
		{
			return this;
		}
		if (!status.IsGrounded)
		{
			return parent.GetState<AgentStateDrop>();
		}
		return base.NextStateOnGround;
	}

	public override void OnExit()
	{
		status.Dynamic = true;
		status.Velocity = Vector2.zero;
	}

	public override void OnEnter()
	{
		status.Dynamic = false;
		body.PlayAnimation("climb");
		DolocAPI.Broadcast(OperationEventType.CLIMB);
	}
}
