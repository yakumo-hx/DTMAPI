namespace DolocTown;

public interface IMonsterMoverGroundRenderer
{
	public class FallbackRenderer : IMonsterMoverGroundRenderer
	{
		public bool IsTouchGroundDone()
		{
			return true;
		}

		public bool IsJumpReadyDone()
		{
			return true;
		}

		public void OnMove()
		{
		}

		public void OnReadyJump()
		{
		}

		public void OnJump()
		{
		}

		public void OnDrop()
		{
		}

		public void OnTouchGround()
		{
		}
	}

	static IMonsterMoverGroundRenderer Fallback;

	bool IsTouchGroundDone();

	bool IsJumpReadyDone();

	void OnMove();

	void OnReadyJump();

	void OnJump();

	void OnDrop();

	void OnTouchGround();

	static IMonsterMoverGroundRenderer()
	{
		Fallback = new FallbackRenderer();
	}
}
