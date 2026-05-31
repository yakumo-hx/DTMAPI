using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AgentStateIdle : AgentStateBase
{
	private readonly RSTimer timer = new RSTimer();

	private bool shouldYawn;

	public override bool SupportJump => true;

	public override bool SupportUseItem => true;

	public override bool SupportInteract => true;

	public AgentStateIdle(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (status.IsGrounded)
		{
			if (status.HorizontalMoveFactor != 0f)
			{
				return parent.GetState<AgentStateMove>();
			}
			if (status.VelocityX != 0f)
			{
				status.VelocityX = 0f;
			}
			if (!shouldYawn)
			{
				return this;
			}
			return parent.GetState<AgentStateYawn>();
		}
		if (!status.IsDrop)
		{
			return this;
		}
		return parent.GetState<AgentStateDrop>();
	}

	public override void OnPlay()
	{
		if (timer.Tick(Time.fixedDeltaTime))
		{
			shouldYawn = true;
		}
		if (status.VelocityX != 0f)
		{
			if (Mathf.Abs(status.VelocityX) < 0.01f)
			{
				status.VelocityX = 0f;
			}
			else
			{
				status.VelocityX *= -0.5f;
			}
		}
	}

	public override void OnEnter()
	{
		status.VelocityX = 0f;
		body.PlayAnimation("idle");
		shouldYawn = false;
		timer.SetInterval(Random.Range(5, 10));
	}
}
