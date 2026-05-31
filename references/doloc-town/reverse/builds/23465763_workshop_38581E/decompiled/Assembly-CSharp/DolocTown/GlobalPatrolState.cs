using System;

namespace DolocTown;

public class GlobalPatrolState : MonsterStateManager.MonsterState
{
	private bool isMoving;

	private Type NextStateType;

	public override MonsterStateManager.MonsterState NextState(float dt)
	{
		if (isMoving)
		{
			return null;
		}
		if (NextStateType == null)
		{
			return GetState(_manager._defaultStateType) ?? this;
		}
		NextStateType = null;
		MonsterStateManager.MonsterState state = GetState(NextStateType);
		if (state != null)
		{
			return state;
		}
		return GetState(_manager._defaultStateType) ?? this;
	}

	public override void OnFixedUpdate(float dt)
	{
		if (isMoving && _controller.mover.Move(dt))
		{
			isMoving = false;
		}
	}

	public override void OnEnter()
	{
		isMoving = _controller.MoveTo(MoveTargetType.Random);
	}
}
