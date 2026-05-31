using UnityEngine;

namespace DolocTown.GameData;

public class MonsterMoverSOCtxSteering : MonsterMoverSO, IMonsterMoverProtoCtxSteering, IMonsterMoverProto
{
	[SerializeField]
	private float maxSpeed = 1f;

	[SerializeField]
	private float maxAcceleration = 1f;

	[SerializeField]
	private float targetRadius;

	[SerializeField]
	private float slowRadius;

	[SerializeField]
	[Range(0.1f, 5f)]
	[Tooltip("该值越小则无人机响应的速度越快,行动越机械")]
	private float timeToTarget;

	[SerializeField]
	[Range(3f, 24f)]
	[Tooltip("该值越大则无人机的障碍规避越细节,但是整体的计算性能会下降")]
	private int dirCount;

	[SerializeField]
	[Range(0.1f, 10f)]
	private float dangerRadius;

	[SerializeField]
	[Range(0f, 5f)]
	private float sensitive;

	[SerializeField]
	[Range(0f, 10f)]
	private float rotationOffset;

	float IMonsterMoverProtoCtxSteering.MaxSpeed => maxSpeed;

	float IMonsterMoverProtoCtxSteering.MaxAcceleration => maxAcceleration;

	float IMonsterMoverProtoCtxSteering.TargetRadius => targetRadius;

	float IMonsterMoverProtoCtxSteering.SlowRadius => slowRadius;

	float IMonsterMoverProtoCtxSteering.TimeToTarget => timeToTarget;

	int IMonsterMoverProtoCtxSteering.DirCount => dirCount;

	float IMonsterMoverProtoCtxSteering.DangerRadius => dangerRadius;

	float IMonsterMoverProtoCtxSteering.Sensitive => sensitive;

	float IMonsterMoverProtoCtxSteering.RotationOffset => rotationOffset;

	public override bool CreateProto(out MonsterMoverProto proto)
	{
		proto = null;
		if (_pathFinder == null || !_pathFinder.CreateProto(out var proto2))
		{
			return false;
		}
		proto = new MonsterMoverProtoCtxSteering(proto2, isDirectional, _aroundRange, maxSpeed, maxAcceleration, targetRadius, slowRadius, timeToTarget, dirCount, dangerRadius, sensitive, rotationOffset, this);
		return true;
	}
}
