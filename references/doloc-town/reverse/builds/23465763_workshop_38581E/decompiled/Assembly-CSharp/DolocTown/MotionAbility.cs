namespace DolocTown;

public class MotionAbility
{
	private readonly IMotionParam _base;

	private MotionParamModifier _modifier;

	private float _moveSpeed;

	private float _jumpForce;

	public float MoveScaler => _modifier.moveSpeedModifier.x;

	public float MoveAdder => _modifier.moveSpeedModifier.y;

	public float JumpScaler => _modifier.jumpForceModifier.x;

	public float JumpAdder => _modifier.jumpForceModifier.y;

	public float MoveSpeed => _moveSpeed;

	public float JumpForce => _jumpForce;

	public float EnvModerateFactor => _base.EnvModerateFactor;

	public float JumpForceSuper => _base.JumpForceSuper;

	public float FallSpeed => _base.FallSpeed;

	public float DashDuration => _base.DashDuration;

	public float DashSpeed => _base.DashSpeed;

	public float HurtDuration => _base.HurtDuration;

	public float GravityScale => _base.GravityScale;

	public bool ShouldWalk => _moveSpeed <= _base.WalkThreshold;

	public bool ShouldRun => _moveSpeed > _base.WalkThreshold;

	public MotionAbility(IMotionParam baseMotionParam)
	{
		_base = baseMotionParam;
		_modifier = default(MotionParamModifier);
		UpdateMoveSpeed();
		UpdateJumpForce();
	}

	public void Clear()
	{
		_modifier = default(MotionParamModifier);
		UpdateMoveSpeed();
		UpdateJumpForce();
	}

	public void SetMoveScaler(float scaler)
	{
		_modifier.moveSpeedModifier.x = scaler;
		UpdateMoveSpeed();
	}

	public void SetMoveAdder(float adder)
	{
		_modifier.moveSpeedModifier.y = adder;
		UpdateMoveSpeed();
	}

	public void ComposeEnvModerate(int value)
	{
		_modifier.envModerateCount += value;
		UpdateMoveSpeed();
	}

	public void ClearEnvModerate()
	{
		_modifier.envModerateCount = 0;
		UpdateMoveSpeed();
	}

	private void UpdateMoveSpeed()
	{
		float num = _base.MoveSpeed * (1f + _modifier.moveSpeedModifier.x) + _modifier.moveSpeedModifier.y;
		_moveSpeed = (1f - _base.EnvModerateFactor * (float)_modifier.EnvModerateEnabled) * num;
	}

	public void SetJumpScaler(float scaler)
	{
		_modifier.jumpForceModifier.x = scaler;
		UpdateJumpForce();
	}

	public void SetJumpAdder(float adder)
	{
		_modifier.jumpForceModifier.y = adder;
		UpdateJumpForce();
	}

	private void UpdateJumpForce()
	{
		_jumpForce = _base.JumpForce * (1f + _modifier.jumpForceModifier.x) + _modifier.jumpForceModifier.y;
	}
}
