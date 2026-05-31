using UnityEngine;

namespace DolocTown;

public class AgentStateDrop : AgentStateBase
{
	private float _drop_duration_threshold;

	public AgentStateDrop(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (status.IsTouchWall && body.IsClimb)
		{
			return parent.GetState<AgentStateClimb>();
		}
		if (status.IsTouchGround(body._isWaitingForJumpDownReleased))
		{
			return parent.GetState<AgentStateTouchGround>();
		}
		if (_drop_duration_threshold > 0f)
		{
			return this;
		}
		if (!status.IsDrop)
		{
			return parent.GetState<AgentStateTouchGround>();
		}
		return this;
	}

	public override void OnEnter()
	{
		body.PlayAnimation("drop");
		body.SortingLayerName = "PlatformExclude";
		body.SortingOrder = 1;
		_drop_duration_threshold = 0.5f;
		Vector2 position2d = body.position2d;
		body.position2d = new Vector2(position2d.x, position2d.y - 0.1f);
	}

	public override void OnExit()
	{
		status.HorizontalMoveFactor = 0f;
		body.SortingOrder = 0;
	}

	public override void OnPlay()
	{
		if (_drop_duration_threshold > 0f)
		{
			_drop_duration_threshold -= Time.fixedDeltaTime;
		}
		status.Drop(body.MoveSpeed);
	}
}
