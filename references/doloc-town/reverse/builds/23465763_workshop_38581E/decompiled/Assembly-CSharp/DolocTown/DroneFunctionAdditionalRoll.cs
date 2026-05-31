using DolocTown.Config.Drone;
using DolocTown.Config.Item;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DroneFunctionAdditionalRoll : DroneFunction
{
	private readonly DroneFunctionProtoAdditionalRoll _protoAdditionalRoll;

	public DroneFunctionAdditionalRoll(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
		_protoAdditionalRoll = (DroneFunctionProtoAdditionalRoll)proto;
	}

	public override bool OnReceiveMessage(DroneEventType type, GameEventArgs args)
	{
		if (type != DroneEventType.EnemyDead)
		{
			return false;
		}
		if (!RandomUtils.Dice(_protoAdditionalRoll.Probability))
		{
			return false;
		}
		GameEventArgs<AttackHitInfo> gameEventArgs = (GameEventArgs<AttackHitInfo>)args;
		if (!gameEventArgs.value.monsterProto.Name.IsNullOrEmpty())
		{
			ItemSpawnEntry dropSpawnEntry = gameEventArgs.value.monsterProto.DropSpawnEntry;
			Vector2 position = gameEventArgs.value.position;
			if (dropSpawnEntry.SpawnLut_Ref == null)
			{
				return false;
			}
			DolocAPI.RaiseInstantPSEffects(position, InstantParticleEffectsType.SPARKS);
			DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, dropSpawnEntry, 1, position);
		}
		return false;
	}
}
