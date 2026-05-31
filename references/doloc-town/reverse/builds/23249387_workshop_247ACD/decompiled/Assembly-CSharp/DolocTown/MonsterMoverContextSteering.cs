using System;
using DolocTown.GameData;
using RedSaw.AI;
using UnityEngine;

namespace DolocTown;

public class MonsterMoverContextSteering : MonsterMover
{
	private readonly bool isDirectional;

	private readonly IMonsterMoverProtoCtxSteering moverProtoRuntime;

	private readonly MonsterMoverProtoCtxSteering moverProto;

	private readonly ContextSteeringSystemRuntime steeringSystemRuntime;

	private readonly ContextSteeringSystemEx steeringSystem;

	private bool _shouldVoidMoving;

	private Vector2Int[] overridenPath;

	public Vector2 BestDir => steeringSystem.bestDir;

	public MonsterMoverContextSteering(MonsterEnv env, Transform transform, IMonsterMoverProtoCtxSteering moverProtoRuntime, MonsterMoverProtoCtxSteering moverProto, bool isDirectional, bool isAir)
		: base(env, transform, moverProto, isAir)
	{
		this.moverProtoRuntime = moverProtoRuntime;
		this.moverProto = moverProto;
		this.isDirectional = isDirectional;
		steeringSystem = new ContextSteeringSystemEx(transform, moverProto.maxSpeed, moverProto.maxAcceleration, moverProto.targetRadius, moverProto.slowRadius, moverProto.timeToTarget, moverProto.dirCount, moverProto.dangerRadius, moverProto.sensitive);
		steeringSystemRuntime = new ContextSteeringSystemRuntime(transform, moverProtoRuntime);
	}

	public void SetOverridenPath(Vector2Int[] path)
	{
		overridenPath = path;
	}

	protected override Vector2[] PostProcess(Vector2Int[] path)
	{
		if (overridenPath != null && overridenPath.Length > 1)
		{
			path = new Vector2Int[overridenPath.Length];
			Array.Copy(overridenPath, path, overridenPath.Length);
			Vector2[] result = _env.CellToWorldCenter(path);
			overridenPath = null;
			return result;
		}
		return PathFinderUtils.SmoothPath(_env.CellToWorldCenter(path), DolocAPI.gameConfig.groundMask);
	}

	private bool _MoveNext()
	{
		_shouldVoidMoving = base.IsInObstacleOrInvalid;
		return MoveNext();
	}

	protected override void StartMove()
	{
		_MoveNext();
	}

	protected override bool OnUpdate(float dt)
	{
		return UpdateBuild(dt);
	}

	private bool UpdateRuntime(float dt)
	{
		ContextSteeringObstacle[] obstacles = (_shouldVoidMoving ? Array.Empty<ContextSteeringObstacle>() : _env.GetObstacles(_transform.position, steeringSystemRuntime.DangerRadius));
		steeringSystemRuntime.Update(dt, base.target, obstacles);
		_transform.rotation = Quaternion.Euler(0f, 0f, (0f - moverProtoRuntime.RotationOffset) * steeringSystemRuntime.xSpeedProcess);
		if (isDirectional)
		{
			_transform.localScale = new Vector3(steeringSystemRuntime.FlipXInt, 1f, 1f);
		}
		if (base.DistanceToTarget <= moverProtoRuntime.TargetRadius)
		{
			return MoveNext();
		}
		return false;
	}

	private bool UpdateBuild(float dt)
	{
		ContextSteeringObstacle[] obstacles = (_shouldVoidMoving ? Array.Empty<ContextSteeringObstacle>() : _env.GetObstacles(_transform.position, steeringSystem.DangerRadius));
		steeringSystem.Update(dt, base.target, obstacles);
		_transform.rotation = Quaternion.Euler(0f, 0f, (0f - moverProto.rotationOffset) * steeringSystem.xSpeedProcess);
		if (isDirectional)
		{
			_transform.localScale = new Vector3(steeringSystem.FlipXInt, 1f, 1f);
		}
		if (base.DistanceToTarget <= moverProto.targetRadius)
		{
			return MoveNext();
		}
		return false;
	}
}
