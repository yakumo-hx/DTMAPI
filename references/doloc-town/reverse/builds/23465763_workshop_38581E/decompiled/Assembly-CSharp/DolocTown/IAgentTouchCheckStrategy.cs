using UnityEngine;

namespace DolocTown;

public interface IAgentTouchCheckStrategy : ITouchCheckStrategy
{
	BodyController player { get; }

	Rigidbody2D playerRigidbody { get; }

	Vector2 playerVelocity { get; }

	bool CheckState<T>() where T : AgentStateBase;
}
