using UnityEngine;

namespace DolocTown.GameData;

public class MonsterMoverSOShuttle : MonsterMoverSO, IMonsterMoverProtoShuttle, IMonsterMoverProto
{
	[SerializeField]
	[Range(0f, 3f)]
	private float waitDuration = 0.5f;

	[SerializeField]
	[Range(0f, 3f)]
	private float moveDuration = 0.5f;

	float IMonsterMoverProtoShuttle.WaitDuration => waitDuration;

	float IMonsterMoverProtoShuttle.MoveDuration => moveDuration;

	public override bool CreateProto(out MonsterMoverProto proto)
	{
		if (!_pathFinder.CreateProto(out var proto2))
		{
			proto = null;
			return false;
		}
		proto = new MonsterMoverProtoShuttle(proto2, isDirectional, _aroundRange, waitDuration, moveDuration);
		return true;
	}
}
