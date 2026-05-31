using System;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class MonsterMoverNone : MonsterMover
{
	public MonsterMoverNone(MonsterEnv env, Transform transform, MonsterMoverProto proto, bool isAir)
		: base(env, transform, proto, isAir)
	{
	}

	protected override Vector2[] PostProcess(Vector2Int[] path)
	{
		return Array.Empty<Vector2>();
	}

	protected override bool OnUpdate(float dt)
	{
		return false;
	}
}
