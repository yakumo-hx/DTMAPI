using RedSaw.AI;

namespace DolocTown.GameData;

public class PathFinderSOEmpty : PathFinderSO
{
	public override bool CreateProto(out PathFinderProto proto)
	{
		proto = new PathFinderProtoEmpty();
		return true;
	}

	public override IPathFinder CreatePathFinder()
	{
		return IPathFinder.Empty;
	}
}
