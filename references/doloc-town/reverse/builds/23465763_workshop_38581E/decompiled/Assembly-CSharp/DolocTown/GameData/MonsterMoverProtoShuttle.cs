namespace DolocTown.GameData;

public class MonsterMoverProtoShuttle : MonsterMoverProto
{
	public readonly float waitDuration;

	public readonly float moveDuration;

	public MonsterMoverProtoShuttle(PathFinderProto pathFinderProto, bool isDirectional, int aroundRange, float waitDuration, float moveDuration)
		: base(pathFinderProto, isDirectional, aroundRange)
	{
		this.waitDuration = waitDuration;
		this.moveDuration = moveDuration;
	}
}
