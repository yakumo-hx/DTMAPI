using System.Collections.Generic;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class MonsterMoverGround : MonsterMover
{
	private struct MoveNode
	{
		public readonly Vector2 position;

		public readonly Vector2Int cell;

		public readonly bool shouldJump;

		public MoveNode(Vector2 position, Vector2Int cell, bool shouldJump)
		{
			this.position = position;
			this.cell = cell;
			this.shouldJump = shouldJump;
		}
	}

	private readonly MonsterMoverProtoGround _protoGround;

	private readonly IMonsterMoverGroundRenderer _renderer;

	private readonly MonsterMoverJump _jumpHelper;

	private readonly Queue<MoveNode> _nodes;

	private Vector2 _moveDelta;

	private bool _isJump;

	public bool IsJumping => _isJump;

	public MonsterMoverGround(MonsterEnv env, Transform transform, MonsterMoverProtoGround proto, bool isAir)
		: base(env, transform, proto, isAir)
	{
		_protoGround = proto;
		_jumpHelper = new MonsterMoverJump(transform, _protoGround.jumpSpeed);
		_renderer = transform.GetComponent<IMonsterMoverGroundRenderer>();
		_nodes = new Queue<MoveNode>();
	}

	protected override Vector2[] PostProcess(Vector2Int[] path)
	{
		_nodes.Clear();
		Vector2Int from = _map.WorldToCell(_transform.position);
		foreach (Vector2Int vector2Int in path)
		{
			bool shouldJump = ShouldJump(from, vector2Int);
			Vector2 position = _map.CellToWorldPivot(vector2Int, new Vector2(0.5f, 0f));
			_nodes.Enqueue(new MoveNode(position, vector2Int, shouldJump));
			from = vector2Int;
		}
		return _map.CellToWorldPivot(path, new Vector2(0.5f, 0f));
	}

	private bool ShouldJump(Vector2Int from, Vector2Int to)
	{
		if (from.y != to.y)
		{
			return true;
		}
		int y = from.y - 1;
		for (int i = from.x; i <= to.x; i++)
		{
			Vector2Int pos = new Vector2Int(i, y);
			if (!_map.IsObstacle(pos))
			{
				return true;
			}
		}
		return false;
	}

	private void ConfigureMove()
	{
		MoveNode moveNode = _nodes.Dequeue();
		_isJump = moveNode.shouldJump;
		if (moveNode.shouldJump)
		{
			_jumpHelper.JumpTo(moveNode.position);
			_transform.localScale = new Vector3(Mathf.Sign(_transform.position.x - moveNode.position.x), 1f, 1f);
		}
		else
		{
			_renderer.OnMove();
			_moveDelta = (_moveDelta = (moveNode.position - (Vector2)_transform.position).normalized * _protoGround.moveSpeed);
			_transform.localScale = new Vector3(Mathf.Sign(0f - _moveDelta.x), 1f, 1f);
		}
	}

	protected override void StartMove()
	{
		MoveNext();
		ConfigureMove();
	}

	protected override bool OnUpdate(float dt)
	{
		if (!_isJump)
		{
			return OnUpdateMove(dt);
		}
		return OnUpdateJump(dt);
	}

	private bool OnUpdateMove(float dt)
	{
		if (Vector2.Distance(_transform.position, base.target) <= _protoGround.targetRadius)
		{
			if (MoveNext())
			{
				return true;
			}
			ConfigureMove();
			return false;
		}
		_transform.Translate(_moveDelta * dt);
		return false;
	}

	private bool OnUpdateJump(float dt)
	{
		if (!_jumpHelper.Update(dt))
		{
			return false;
		}
		if (MoveNext())
		{
			return true;
		}
		ConfigureMove();
		return false;
	}
}
