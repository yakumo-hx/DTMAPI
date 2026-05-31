namespace DolocTown.GameData;

public class MonsterMoverProtoGround : MonsterMoverProto
{
	public readonly float targetRadius;

	public readonly float moveSpeed;

	public readonly float jumpSpeed;

	public MonsterMoverProtoGround(PathFinderProto pathFinderProto, bool isDirectional, int aroundRange, float targetRadius, float moveSpeed, float jumpSpeed)
		: base(pathFinderProto, isDirectional, aroundRange)
	{
		this.targetRadius = targetRadius;
		this.moveSpeed = moveSpeed;
		this.jumpSpeed = jumpSpeed;
	}
}
