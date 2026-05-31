namespace DolocTown.GameData;

public interface IMonsterMoverProtoCtxSteering : IMonsterMoverProto
{
	float MaxSpeed { get; }

	float MaxAcceleration { get; }

	float TargetRadius { get; }

	float SlowRadius { get; }

	float TimeToTarget { get; }

	float DangerRadius { get; }

	float Sensitive { get; }

	float RotationOffset { get; }

	int DirCount { get; }
}
