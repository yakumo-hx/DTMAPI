using RedSaw.AI;

namespace DolocTown.GameData;

public class PathFinderProtoAir : PathFinderProto
{
	private readonly bool constrainDiagonal;

	public PathFinderProtoAir(bool constrainDiagonal)
	{
		this.constrainDiagonal = constrainDiagonal;
	}

	public override IPathFinder CreatePathFinder()
	{
		return new AStar(constrainDiagonal);
	}
}
