namespace DolocTown.GameData;

public interface IMonsterMoverProto
{
	IPathFinderProto PathFinderProto { get; }

	bool IsDirectional { get; }

	int AroundRange { get; }
}
