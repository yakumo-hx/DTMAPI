using System;
using System.Collections.Generic;
using DolocTown.GameData;
using RedSaw.AI;
using RedSaw.GameMap;
using UnityEngine;

namespace DolocTown;

public abstract class MonsterMover
{
	protected readonly MonsterMoverProto _proto;

	protected readonly MonsterEnv _env;

	protected readonly IGameMap _map;

	protected readonly Transform _transform;

	protected readonly bool _isAir;

	protected readonly MonsterController _controller;

	private readonly IPathFinder _pathFinder;

	public Vector2[] originPath;

	private readonly Queue<Vector2> path = new Queue<Vector2>();

	protected Action callback;

	protected Vector2 target { get; private set; }

	protected float DistanceToTarget => ((Vector2)_transform.position - target).magnitude;

	protected bool IsInObstacleOrInvalid => _map.IsObstacleOrInvalid(_map.WorldToCell(_transform.position));

	protected float DurationToTarget(float moveSpeed)
	{
		return DistanceToTarget / moveSpeed;
	}

	protected MonsterMover(MonsterEnv env, Transform transform, MonsterMoverProto proto, bool isAir)
	{
		_env = env;
		_proto = proto;
		_transform = transform;
		_isAir = isAir;
		_map = _env.GetMap(isAir);
		_controller = _transform.GetComponent<MonsterController>();
		_pathFinder = proto.pathFinderProto.CreatePathFinder();
	}

	protected bool MoveNext()
	{
		if (path.Count == 0)
		{
			return true;
		}
		target = path.Dequeue();
		return false;
	}

	public virtual bool TryMoveTo(Vector2 target, Action callback = null)
	{
		if (!TryFindPath(target, out var array, out var isStuck))
		{
			if (isStuck)
			{
				Debug.LogWarning("检测到怪物被墙体卡住");
			}
			originPath = null;
			return false;
		}
		this.callback = callback;
		originPath = PostProcess(array);
		path.Clear();
		Vector2[] array2 = originPath;
		foreach (Vector2 item in array2)
		{
			path.Enqueue(item);
		}
		StartMove();
		return true;
	}

	private bool FindPathOfAir(Vector2 to, out Vector2Int[] path, out bool isStuck)
	{
		Vector2Int vector2Int = _map.WorldToCell(_transform.position);
		isStuck = _map.IsValidPosition(vector2Int) && _map.IsObstacle(vector2Int);
		if (_map.IsObstacleOrInvalid(vector2Int))
		{
			vector2Int = _map.GetNearestEmptyPos(vector2Int);
		}
		Vector2Int vector2Int2 = _map.WorldToCell(to);
		if (_map.IsObstacleOrInvalid(vector2Int2))
		{
			vector2Int2 = _map.GetNearestEmptyPos(vector2Int2);
		}
		path = _pathFinder.FindPath(_map, vector2Int, vector2Int2);
		return path != null;
	}

	private bool TryFindPath(Vector2 to, out Vector2Int[] path, out bool isStuck)
	{
		if (!_isAir)
		{
			return TryFindPathOfGround(to, out path, out isStuck);
		}
		return FindPathOfAir(to, out path, out isStuck);
	}

	private bool TryFindPathOfGround(Vector2 to, out Vector2Int[] path, out bool isStuck)
	{
		Vector2Int vector2Int = _map.WorldToCell(_transform.position);
		isStuck = _map.IsValidPosition(vector2Int) && _map.IsObstacle(vector2Int);
		if (!_map.IsGround(vector2Int))
		{
			if (!_map.SearchNearestGround(vector2Int, out var result))
			{
				path = null;
				return false;
			}
			vector2Int = result;
		}
		Vector2Int pos = _map.WorldToCell(to);
		if (!_map.SearchNearestGround(pos, out var result2))
		{
			path = null;
			return false;
		}
		pos = result2;
		path = _pathFinder.FindPath(_map, vector2Int, pos);
		return path != null;
	}

	public void StopMove()
	{
		callback?.Invoke();
		callback = null;
	}

	public bool GetMoveTargetAround(Vector2 position, out Vector2 target)
	{
		if (!_isAir)
		{
			return _map.GetAroundGroundPositionWS(position, _proto.aroundRange, out target);
		}
		return _map.GetAroundPositionWS(position, _proto.aroundRange, out target);
	}

	public bool GetMoveTarget(MoveTargetType type, bool isAir, out Vector2 position)
	{
		switch (type)
		{
		case MoveTargetType.Random:
			position = (isAir ? _map.GetRandomEmptyPositionWS() : _map.GetRandomGroundPositionWS());
			return true;
		case MoveTargetType.Around:
			if (!isAir)
			{
				return _map.GetAroundGroundPositionWS(_transform.position, _proto.aroundRange, out position);
			}
			return _map.GetAroundPositionWS(_transform.position, _proto.aroundRange, out position);
		case MoveTargetType.Player:
			position = DolocAPI.AgentPosition;
			return true;
		default:
			position = default(Vector2);
			return false;
		}
	}

	protected virtual Vector2[] PostProcess(Vector2Int[] path)
	{
		return _env.CellToWorldCenter(path);
	}

	protected virtual void StartMove()
	{
	}

	public bool Move(float dt)
	{
		if (!OnUpdate(dt))
		{
			return false;
		}
		callback?.Invoke();
		callback = null;
		return true;
	}

	protected abstract bool OnUpdate(float dt);
}
