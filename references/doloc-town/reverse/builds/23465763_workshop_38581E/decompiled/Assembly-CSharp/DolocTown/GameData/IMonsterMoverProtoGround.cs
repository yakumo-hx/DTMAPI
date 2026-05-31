namespace DolocTown.GameData;

public interface IMonsterMoverProtoGround : IMonsterMoverProto
{
	float TargetRadius { get; }

	float MoveSpeed { get; }

	float JumpSpeed { get; }
}
