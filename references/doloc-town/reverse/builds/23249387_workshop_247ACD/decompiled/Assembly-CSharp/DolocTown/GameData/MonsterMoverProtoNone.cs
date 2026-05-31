namespace DolocTown.GameData;

public class MonsterMoverProtoNone : MonsterMoverProto
{
	public MonsterMoverProtoNone(PathFinderProto pathFinder, bool isDirectional = false, int aroundRange = 3)
		: base(pathFinder, isDirectional, aroundRange)
	{
	}
}
