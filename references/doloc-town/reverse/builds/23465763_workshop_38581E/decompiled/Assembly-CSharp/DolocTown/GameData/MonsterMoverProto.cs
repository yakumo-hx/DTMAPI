namespace DolocTown.GameData;

public abstract class MonsterMoverProto
{
	public readonly PathFinderProto pathFinderProto;

	public readonly bool isDirectional;

	public readonly int aroundRange;

	protected MonsterMoverProto(PathFinderProto pathFinderProto, bool isDirectional, int aroundRange)
	{
		this.pathFinderProto = pathFinderProto;
		this.isDirectional = isDirectional;
		this.aroundRange = aroundRange;
	}
}
