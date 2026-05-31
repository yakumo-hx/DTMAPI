using RedSaw.AI;
using UnityEngine;

namespace DolocTown.GameData;

public class PathFinderSOShuttle : PathFinderSO
{
	[SerializeField]
	private int shuttleRange = 10;

	public override bool CreateProto(out PathFinderProto proto)
	{
		proto = new PathFinderProtoShuttle(shuttleRange);
		return true;
	}

	public override IPathFinder CreatePathFinder()
	{
		return new AStarShuttle(shuttleRange);
	}
}
