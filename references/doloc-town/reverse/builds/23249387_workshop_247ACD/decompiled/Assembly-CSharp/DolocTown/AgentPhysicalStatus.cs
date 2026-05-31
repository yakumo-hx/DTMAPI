using UnityEngine;

namespace DolocTown;

public class AgentPhysicalStatus
{
	public const float CONST_THRESHOLD_MOVESPEED_VERTICAL = 0.001f;

	public const float CONST_THRESHOLD_DROPSPEED = -0.001f;

	private readonly Transform transform;

	private readonly Rigidbody2D RB;

	private readonly GroundChecker groundChecker;

	private readonly EnvironmentChecker wallChecker;

	private readonly EnvironmentChecker wallTopChecker;

	private readonly float maxDropSpeed;

	public bool IsDrop => RB.velocity.y < -0.001f;

	public bool IsGrounded
	{
		get
		{
			if (groundChecker.isTouched)
			{
				return RB.velocity.y < 0.001f;
			}
			return false;
		}
	}

	public bool IsGroundedExcludePlatform
	{
		get
		{
			if (groundChecker.isTouchedExcludePlatforms)
			{
				return RB.velocity.y < 0.001f;
			}
			return false;
		}
	}

	public bool IsTouchWall => wallChecker.isTouched;

	public bool IsNearWallTop => !wallTopChecker.isTouched;

	public float HorizontalMoveFactor { get; set; }

	public bool ToolLatch { get; set; }

	public Vector2 Velocity
	{
		get
		{
			return RB.velocity;
		}
		set
		{
			RB.velocity = value;
		}
	}

	public float VelocityY
	{
		get
		{
			return RB.velocity.y;
		}
		set
		{
			Vector2 velocity = RB.velocity;
			velocity.y = value;
			RB.velocity = velocity;
		}
	}

	public float VelocityX
	{
		get
		{
			return RB.velocity.x;
		}
		set
		{
			Vector2 velocity = RB.velocity;
			velocity.x = value;
			RB.velocity = velocity;
		}
	}

	public bool RBEnabled
	{
		get
		{
			return RB.simulated;
		}
		set
		{
			RB.simulated = value;
		}
	}

	public bool Dynamic
	{
		set
		{
			RB.isKinematic = !value;
			if (!value)
			{
				RB.velocity = Vector2.zero;
			}
		}
	}

	public bool IsTouchGround(bool isWaitForJumpDownReleased)
	{
		if (!isWaitForJumpDownReleased)
		{
			return IsGrounded;
		}
		return IsGroundedExcludePlatform;
	}

	public AgentPhysicalStatus(Rigidbody2D rb, GroundChecker groundChecker, EnvironmentChecker wallChecker, EnvironmentChecker wallTopChecker, float maxDropSpeed)
	{
		RB = rb;
		transform = rb.transform;
		this.groundChecker = groundChecker;
		this.wallChecker = wallChecker;
		this.wallTopChecker = wallTopChecker;
		this.maxDropSpeed = maxDropSpeed;
	}

	public void Drop(float moveSpeed)
	{
		if (HorizontalMoveFactor != 0f)
		{
			transform.localScale = new Vector3(Mathf.Sign(HorizontalMoveFactor), 1f, 1f);
		}
		float y = Mathf.Min(Mathf.Max(maxDropSpeed, RB.velocity.y), -0.001f);
		RB.velocity = new Vector2(moveSpeed * HorizontalMoveFactor, y);
	}

	public void Move(float moveSpeed)
	{
		if (HorizontalMoveFactor != 0f)
		{
			transform.localScale = new Vector3(Mathf.Sign(HorizontalMoveFactor), 1f, 1f);
		}
		VelocityX = moveSpeed * HorizontalMoveFactor;
	}

	public void Dash(Vector2 dashDir)
	{
		transform.localScale = new Vector3(Mathf.Sign(dashDir.x), 1f, 1f);
		Velocity = dashDir;
	}

	public void Clear()
	{
		HorizontalMoveFactor = 0f;
		Velocity = Vector2.zero;
	}

	public void ClearHorizontalInput()
	{
		HorizontalMoveFactor = 0f;
	}

	public void OverwriteForce(Vector2 force)
	{
		Velocity = Vector2.zero;
		RB.AddForce(force, ForceMode2D.Impulse);
	}
}
