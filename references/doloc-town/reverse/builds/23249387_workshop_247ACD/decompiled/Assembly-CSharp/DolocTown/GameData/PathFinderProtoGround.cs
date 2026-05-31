using RedSaw.AI;

namespace DolocTown.GameData;

public class PathFinderProtoGround : PathFinderProto
{
	private readonly int horizontalRange;

	private readonly int verticalRange;

	private readonly int jumpTolerance;

	private readonly int touchTolerance;

	public PathFinderProtoGround(int horizontalRange, int verticalRange, int jumpTolerance, int touchTolerance)
	{
		this.horizontalRange = horizontalRange;
		this.verticalRange = verticalRange;
		this.jumpTolerance = jumpTolerance;
		this.touchTolerance = touchTolerance;
	}

	public override IPathFinder CreatePathFinder()
	{
		return new AStarGroundColumn(verticalRange, horizontalRange, jumpTolerance, touchTolerance);
	}
}
