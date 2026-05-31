namespace DolocTown.GameData;

public interface IMonsterMoverProtoShuttle : IMonsterMoverProto
{
	float WaitDuration { get; }

	float MoveDuration { get; }
}
