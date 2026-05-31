using DG.Tweening;

namespace DolocTown.GameData;

public class MonsterMoverProtoDoTween : MonsterMoverProto
{
	public readonly float moveSpeed;

	public readonly Ease moveEase;

	public MonsterMoverProtoDoTween(float moveSpeed, Ease ease, PathFinderProto pathFinderProto, bool isDirectional, int aroundRange)
		: base(pathFinderProto, isDirectional, aroundRange)
	{
		this.moveSpeed = moveSpeed;
		moveEase = ease;
	}
}
