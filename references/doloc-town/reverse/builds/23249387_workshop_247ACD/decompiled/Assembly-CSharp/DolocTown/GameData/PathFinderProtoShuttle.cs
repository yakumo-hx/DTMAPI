using RedSaw.AI;

namespace DolocTown.GameData;

public class PathFinderProtoShuttle : PathFinderProto
{
	private readonly int shuttleRange;

	public PathFinderProtoShuttle(int shuttleRange)
	{
		this.shuttleRange = shuttleRange;
	}

	public override IPathFinder CreatePathFinder()
	{
		return new AStarShuttle(shuttleRange);
	}
}
