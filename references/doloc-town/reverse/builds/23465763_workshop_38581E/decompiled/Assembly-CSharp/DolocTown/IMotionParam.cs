namespace DolocTown;

public interface IMotionParam
{
	float MoveSpeed { get; }

	float WalkThreshold { get; }

	float EnvModerateFactor { get; }

	float JumpForce { get; }

	float JumpForceSuper { get; }

	float GravityScale { get; }

	float FallSpeed { get; }

	float DashDuration { get; }

	float DashSpeed { get; }

	float HurtDuration { get; }
}
