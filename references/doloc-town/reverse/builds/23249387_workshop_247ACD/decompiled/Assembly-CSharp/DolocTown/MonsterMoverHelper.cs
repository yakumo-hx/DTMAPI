using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public static class MonsterMoverHelper
{
	public static MonsterMover CreateMover(MonsterEnv env, Transform transform, MonsterMoverProto proto, bool isAir)
	{
		if (!(proto is MonsterMoverProtoCtxSteering monsterMoverProtoCtxSteering))
		{
			if (!(proto is MonsterMoverProtoShuttle proto2))
			{
				if (!(proto is MonsterMoverProtoGround proto3))
				{
					if (!(proto is MonsterMoverProtoNone proto4))
					{
						if (proto is MonsterMoverProtoDoTween proto5)
						{
							return new MonsterMoverDoTween(env, transform, proto5, isAir);
						}
						return null;
					}
					return new MonsterMoverNone(env, transform, proto4, isAir);
				}
				return new MonsterMoverGround(env, transform, proto3, isAir: false);
			}
			return new MonsterMoverShuttle(env, transform, proto2, isAir);
		}
		return new MonsterMoverContextSteering(env, transform, monsterMoverProtoCtxSteering.Runtime, monsterMoverProtoCtxSteering, proto.isDirectional, isAir);
	}
}
