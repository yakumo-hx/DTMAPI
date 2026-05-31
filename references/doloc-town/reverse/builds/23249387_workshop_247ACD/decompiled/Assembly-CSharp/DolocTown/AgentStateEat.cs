using UnityEngine;

namespace DolocTown;

public class AgentStateEat : AgentStateBase
{
	public AgentStateEat(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (!IsAnimationDone("eat"))
		{
			return this;
		}
		return parent.GetState<AgentStateIdle>();
	}

	public override void OnEnter()
	{
		body.PlayAnimation("eat");
		status.Velocity = Vector2.zero;
	}
}
