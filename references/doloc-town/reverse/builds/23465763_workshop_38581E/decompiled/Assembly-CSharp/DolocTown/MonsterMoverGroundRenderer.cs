using UnityEngine;

namespace DolocTown;

public abstract class MonsterMoverGroundRenderer : MonoBehaviour, IMonsterMoverGroundRenderer
{
	public abstract bool IsTouchGroundDone();

	public abstract bool IsJumpReadyDone();

	public abstract void OnReadyJump();

	public abstract void OnMove();

	public abstract void OnDrop();

	public abstract void OnTouchGround();

	public abstract void OnJump();
}
