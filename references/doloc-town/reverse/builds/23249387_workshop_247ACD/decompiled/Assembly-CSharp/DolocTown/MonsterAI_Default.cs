namespace DolocTown;

public class MonsterAI_Default : MonsterAI
{
	private class AttackState : MonsterStateManager.AttackState<MonsterAI_Default>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (!base.IsAttacking)
			{
				return GetState<PatrolState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			if (_controller.attackBehaviourManager.TryGetFirstAvailableAttackBehaviour(out var type))
			{
				_controller.Attack(type, DolocAPI.AgentTransform);
			}
		}
	}

	private class PatrolState : MonsterStateManager.MoveState<MonsterAI_Default>
	{
		public override MonsterStateManager.MonsterState NextState(float dt)
		{
			if (isMoving)
			{
				return null;
			}
			if (!_controller.IsAnyAttackBehaviourAvailable(out var _))
			{
				return this;
			}
			return GetState<AttackState>();
		}

		public override void OnEnter()
		{
			isMoving = true;
			_controller.MoveTo(MoveTargetType.Random);
		}
	}
}
