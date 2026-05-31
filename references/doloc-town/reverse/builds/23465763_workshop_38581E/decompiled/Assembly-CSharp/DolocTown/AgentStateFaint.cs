using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class AgentStateFaint : AgentStateBase
{
	public AgentStateFaint(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		return this;
	}

	public override void OnPlay()
	{
		base.OnPlay();
		body.Status.VelocityX = 0f;
	}

	public override void OnEnter()
	{
		parent.GetState<AgentStateJump>().jumpTimes = DolocAPI.archiveHandle.DoubleJumpTimes();
		body.PlayAnimation("faint");
		status.Velocity = Vector2.zero;
	}
}
