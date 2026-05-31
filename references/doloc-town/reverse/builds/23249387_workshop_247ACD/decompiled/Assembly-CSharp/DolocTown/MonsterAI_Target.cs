using UnityEngine;

namespace DolocTown;

[MonsterAI("target_01")]
public class MonsterAI_Target : MonsterAI
{
	private class WanderState : MonsterStateManager.MoveState<MonsterAI_Target>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (!isMoving)
			{
				return GetState<WaitState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			isMoving = _controller.MoveTo(MoveTargetType.Random);
		}
	}

	private class WaitState : MonsterStateManager.MonsterState<MonsterAI_Target>
	{
		private float waitTime;

		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (waitTime <= 0f)
			{
				return GetState<WanderState>();
			}
			return null;
		}

		public override void OnFixedUpdate(float dt)
		{
			waitTime -= dt;
		}

		public override void OnEnter()
		{
			waitTime = Random.Range(2, 3);
		}
	}
}
