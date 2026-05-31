using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(menuName = "多洛可小镇/配置/主角运动参数")]
public class MotionParamSO : ScriptableObject, IMotionParam
{
	[SerializeField]
	private float _moveSpeed;

	[SerializeField]
	private float _envModerateFactor = 0.3f;

	[SerializeField]
	private float _walkThreshold;

	[SerializeField]
	private float _gravityScale;

	[SerializeField]
	private float _jumpForce;

	[SerializeField]
	private float _jumpForceSuper;

	[SerializeField]
	private float _fallSpeed;

	[SerializeField]
	private float _dashSpeed;

	[SerializeField]
	private float _dashDuration;

	[SerializeField]
	private float _hurtDuration;

	public float MoveSpeed => _moveSpeed;

	public float WalkThreshold => _walkThreshold;

	public float JumpForce => _jumpForce;

	public float JumpForceSuper => _jumpForceSuper;

	public float GravityScale => _gravityScale;

	public float FallSpeed => _fallSpeed;

	public float DashSpeed => _dashSpeed;

	public float DashDuration => _dashDuration;

	public float HurtDuration => _hurtDuration;

	public float EnvModerateFactor => _envModerateFactor;
}
