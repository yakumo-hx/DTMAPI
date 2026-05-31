using UnityEngine;

namespace DolocTown;

public class AgentTouchCheckerBase : IAgentTouchCheckStrategy, ITouchCheckStrategy
{
	private Rigidbody2D _playerRigidbody;

	public BodyController player => DolocAPI.agent;

	public Rigidbody2D playerRigidbody
	{
		get
		{
			if (_playerRigidbody == null)
			{
				_playerRigidbody = player.GetComponent<Rigidbody2D>();
			}
			return _playerRigidbody;
		}
	}

	public Vector2 playerVelocity => playerRigidbody.velocity;

	public virtual bool Check(GameObject other)
	{
		return other.CompareTag("Player");
	}

	public bool CheckState<T>() where T : AgentStateBase
	{
		return player.StateManager.current is T;
	}
}
