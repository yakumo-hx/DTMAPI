using UnityEngine;

namespace DolocTown;

public class AgentStateClimbJump : AgentStateBase
{
	private bool isNearTopWhileJump;

	public AgentStateClimbJump(AgentStateManager parent, BodyController body)
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

	public override void OnPlay()
	{
		if (isNearTopWhileJump)
		{
			status.Move(body.MoveSpeed);
		}
	}

	public override void OnEnter()
	{
		isNearTopWhileJump = body.Status.IsNearWallTop;
		if (isNearTopWhileJump)
		{
			status.VelocityY = body.MotionAbility.JumpForce;
		}
		else
		{
			float num = 0f - body.transform.localScale.x;
			body.transform.localScale = new Vector3(num, 1f, 1f);
			status.Velocity = new Vector2(num * body.MoveSpeed, body.MotionAbility.JumpForce);
		}
		body.PlayAnimation("climb_jump");
		DolocAPI.RaiseInstantAnimEffects(body.transform.position, InstAnimEffectType.PLAYER_JUMP_SMOKE);
		body.SortingOrder = 1;
	}
}
