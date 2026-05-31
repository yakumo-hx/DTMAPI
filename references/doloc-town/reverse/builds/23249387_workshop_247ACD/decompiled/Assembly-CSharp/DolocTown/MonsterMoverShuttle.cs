using System;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterMoverShuttle : MonsterMover
{
	private readonly IMonsterMoverShuttleRenderer _renderer;

	private new readonly MonsterMoverProtoShuttle _proto;

	private readonly RSTimer _timer;

	private bool _isWaiting;

	public MonsterMoverShuttle(MonsterEnv env, Transform transform, MonsterMoverProtoShuttle proto, bool isAir)
		: base(env, transform, proto, isAir)
	{
		_proto = proto;
		_timer = new RSTimer(proto.waitDuration);
		_renderer = transform.GetComponent<IMonsterMoverShuttleRenderer>();
		if (_renderer == null)
		{
			_renderer = IMonsterMoverShuttleRenderer.Fallback;
		}
	}

	private void ShuttleIn()
	{
		_renderer.OnShuttleIn();
		_timer.SetInterval(_proto.moveDuration);
		_isWaiting = false;
	}

	private void ShuttleOut()
	{
		_transform.position = base.target;
		_renderer.OnShuttleOut();
		_timer.SetInterval(_proto.waitDuration);
		_isWaiting = true;
	}

	protected override Vector2[] PostProcess(Vector2Int[] path)
	{
		if (path.Length <= 1)
		{
			return _map.CellToWorldPivot(path, new Vector2(0.5f, 0f));
		}
		if (path.Length <= 2)
		{
			Vector2Int[] array = new Vector2Int[path.Length - 1];
			Array.Copy(path, 1, array, 0, array.Length);
			return _map.CellToWorldPivot(array, new Vector2(0.5f, 0f));
		}
		Vector2Int[] array2 = new Vector2Int[path.Length - 2];
		Array.Copy(path, 1, array2, 0, array2.Length);
		array2[^1] = path[^1];
		return _map.CellToWorldPivot(array2, new Vector2(0.5f, 0f));
	}

	protected override void StartMove()
	{
		MoveNext();
		ShuttleIn();
	}

	protected override bool OnUpdate(float dt)
	{
		if (_isWaiting)
		{
			if (_timer.Tick(dt))
			{
				if (MoveNext())
				{
					return true;
				}
				ShuttleIn();
			}
			return false;
		}
		if (_timer.Tick(dt))
		{
			ShuttleOut();
		}
		return false;
	}
}
