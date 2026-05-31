using System;
using RedSaw.AI;

namespace DolocTown.GameData;

[Serializable]
public abstract class PathFinderSO : IPathFinderProto
{
	public abstract bool CreateProto(out PathFinderProto proto);

	public abstract IPathFinder CreatePathFinder();
}
