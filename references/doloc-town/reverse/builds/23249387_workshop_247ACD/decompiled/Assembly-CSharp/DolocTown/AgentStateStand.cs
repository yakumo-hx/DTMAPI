using UnityEngine;

namespace DolocTown;

public class AgentStateStand : AgentStateBase
{
	public AgentStateStand(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (IsAnimationDone("stand"))
		{
			body.OnStandDone();
			return parent.GetState<AgentStateIdle>();
		}
		return this;
	}

	public override void OnEnter()
	{
		body.PlayAnimation("stand");
		status.Velocity = Vector2.zero;
	}
}
