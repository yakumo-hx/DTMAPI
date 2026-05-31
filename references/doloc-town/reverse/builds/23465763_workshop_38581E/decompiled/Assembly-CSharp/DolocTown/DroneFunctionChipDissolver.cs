using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Drone;
using DolocTown.Config.Monster;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DroneFunctionChipDissolver : DroneFunction
{
	private readonly DroneFunctionProtoChipDissolver _func;

	public DroneFunctionChipDissolver(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
		_func = (DroneFunctionProtoChipDissolver)proto;
	}

	private bool IsRequiredMonster(string monsterName)
	{
		if (DolocConfig.Tables.TbMonsterDocument.DataMap.TryGetValue(monsterName, out var doc))
		{
			return _func.MonsterType.Any((MonsterType x) => x == doc.MonsterType);
		}
		return false;
	}

	private void GenDropItem(Vector2 position)
	{
		DolocAPI.RaiseInstantPSEffects(position, InstantParticleEffectsType.SPARKS);
		Item item = DolocAPI.GenerateItem(_func.ItemName);
		DolocAPI.GenerateDropItem(DolocAPI.CurrentRoom, item, position);
	}

	public override bool OnReceiveMessage(DroneEventType type, GameEventArgs args)
	{
		if (type != DroneEventType.EnemyDead)
		{
			return false;
		}
		if (!(args is GameEventArgs<AttackHitInfo> { value: var value }))
		{
			return false;
		}
		if (!IsRequiredMonster(value.monsterProto.Name))
		{
			return false;
		}
		if (!RandomUtils.Dice(_func.Probability))
		{
			return false;
		}
		GenDropItem(value.position);
		return true;
	}
}
