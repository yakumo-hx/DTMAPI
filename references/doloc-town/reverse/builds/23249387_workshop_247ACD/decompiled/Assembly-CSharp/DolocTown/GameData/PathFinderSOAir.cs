using RedSaw.AI;
using UnityEngine;

namespace DolocTown.GameData;

public class PathFinderSOAir : PathFinderSO
{
	[SerializeField]
	private bool constrainDiagonal;

	public override bool CreateProto(out PathFinderProto proto)
	{
		proto = new PathFinderProtoAir(constrainDiagonal);
		return true;
	}

	public override IPathFinder CreatePathFinder()
	{
		return new AStar(constrainDiagonal);
	}
}
