using UnityEngine;

namespace DolocTown.GameData;

public class MonsterMoverSOGround : MonsterMoverSO, IMonsterMoverProtoGround, IMonsterMoverProto
{
	[SerializeField]
	[Range(0.1f, 1f)]
	private float arriveRadius;

	[SerializeField]
	[Min(1f)]
	private float moveSpeed;

	[SerializeField]
	[Min(1f)]
	private float jumpSpeed;

	float IMonsterMoverProtoGround.TargetRadius => arriveRadius;

	float IMonsterMoverProtoGround.MoveSpeed => moveSpeed;

	float IMonsterMoverProtoGround.JumpSpeed => jumpSpeed;

	public override bool CreateProto(out MonsterMoverProto proto)
	{
		proto = null;
		if (!_pathFinder.CreateProto(out var proto2))
		{
			return false;
		}
		proto = new MonsterMoverProtoGround(proto2, isDirectional, _aroundRange, arriveRadius, moveSpeed, jumpSpeed);
		return true;
	}
}
