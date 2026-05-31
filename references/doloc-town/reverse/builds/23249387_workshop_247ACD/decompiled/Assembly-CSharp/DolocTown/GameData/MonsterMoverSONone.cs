namespace DolocTown.GameData;

public class MonsterMoverSONone : MonsterMoverSO, IMonsterMoverProtoNone, IMonsterMoverProto
{
	public override bool CreateProto(out MonsterMoverProto proto)
	{
		proto = null;
		if (!_pathFinder.CreateProto(out var proto2))
		{
			return false;
		}
		proto = new MonsterMoverProtoNone(proto2, isDirectional, _aroundRange);
		return true;
	}
}
