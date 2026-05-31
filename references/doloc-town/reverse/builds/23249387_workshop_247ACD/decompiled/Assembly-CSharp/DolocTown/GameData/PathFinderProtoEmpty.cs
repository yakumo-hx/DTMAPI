using RedSaw.AI;

namespace DolocTown.GameData;

public class PathFinderProtoEmpty : PathFinderProto
{
	public override IPathFinder CreatePathFinder()
	{
		return IPathFinder.Empty;
	}
}
