using RedSaw.AI;
using UnityEngine;

namespace DolocTown.GameData;

public class PathFinderSOGround : PathFinderSO
{
	[SerializeField]
	private int horizontalRange = 3;

	[SerializeField]
	private int verticalRange = 4;

	[SerializeField]
	private int jumpTolerance = 3;

	[SerializeField]
	private int touchTolerance = 2;

	public override bool CreateProto(out PathFinderProto proto)
	{
		proto = new PathFinderProtoGround(horizontalRange, verticalRange, jumpTolerance, touchTolerance);
		return true;
	}

	public override IPathFinder CreatePathFinder()
	{
		return new AStarGroundColumn(verticalRange, horizontalRange, jumpTolerance, touchTolerance);
	}
}
