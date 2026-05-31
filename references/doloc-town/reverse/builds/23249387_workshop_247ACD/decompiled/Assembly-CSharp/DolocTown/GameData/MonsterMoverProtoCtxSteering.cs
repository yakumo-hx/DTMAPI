namespace DolocTown.GameData;

public class MonsterMoverProtoCtxSteering : MonsterMoverProto
{
	public readonly float maxSpeed;

	public readonly float maxAcceleration;

	public readonly float targetRadius;

	public readonly float slowRadius;

	public readonly float timeToTarget;

	public readonly float dangerRadius;

	public readonly float sensitive;

	public readonly float rotationOffset;

	public readonly int dirCount;

	public IMonsterMoverProtoCtxSteering Runtime;

	public MonsterMoverProtoCtxSteering(PathFinderProto pathFinderProto, bool isDirectional, int aroundRange, float maxSpeed, float maxAcceleration, float targetRadius, float slowRadius, float timeToTarget, int dirCount, float dangerRadius, float sensitive, float rotationOffset, IMonsterMoverProtoCtxSteering runtime)
		: base(pathFinderProto, isDirectional, aroundRange)
	{
		this.maxSpeed = maxSpeed;
		this.maxAcceleration = maxAcceleration;
		this.targetRadius = targetRadius;
		this.slowRadius = slowRadius;
		this.timeToTarget = timeToTarget;
		this.dirCount = dirCount;
		this.dangerRadius = dangerRadius;
		this.sensitive = sensitive;
		this.rotationOffset = rotationOffset;
		Runtime = runtime;
	}
}
