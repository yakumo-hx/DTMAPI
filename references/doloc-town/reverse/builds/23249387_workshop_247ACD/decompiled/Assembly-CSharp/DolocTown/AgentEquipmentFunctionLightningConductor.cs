using System.Collections.Generic;
using DolocTown.Config.Player;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AgentEquipmentFunctionLightningConductor : AgentEquipmentFunction
{
	private readonly AgentEquipmentFuncProtoLightningConductor _func;

	private readonly Counter cdCounter;

	private bool isCooling;

	public AgentEquipmentFunctionLightningConductor(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		_func = (AgentEquipmentFuncProtoLightningConductor)proto;
		cdCounter = new Counter(_func.CdDuration / DolocAPI.GlobalParameter.TULength);
		isCooling = false;
	}

	public override void UpdatePerTu()
	{
		if (isCooling)
		{
			if (cdCounter.Tick())
			{
				isCooling = RaiseThunder();
			}
		}
		else
		{
			isCooling = RaiseThunder();
		}
	}

	private bool RaiseThunder()
	{
		IMonsterHost currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom.MonsterCount == 0)
		{
			return false;
		}
		List<MonsterController> list = new List<MonsterController>();
		foreach (Monster allMonster in currentRoom.MonsterEnv.AllMonsters)
		{
			if (!(allMonster.Controller == null))
			{
				Vector2 positionCenter = allMonster.Controller.PositionCenter;
				if (Vector2.Distance(DolocAPI.AgentPosition, positionCenter) <= _func.Range)
				{
					list.Add(allMonster.Controller);
				}
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		MonsterController monsterController = list.Choice();
		Vector2 positionCenter2 = monsterController.PositionCenter;
		DolocAPI.RaiseInstantAnimEffects(positionCenter2, InstAnimEffectType.THUNDER, LocMaterials.GAME_MAT_EFFECTS_SHINE);
		DolocAPI.cameraController.ShakeScreen();
		DolocAPI.RaiseInstantPSEffects(positionCenter2, InstantParticleEffectsType.SMOKE_BRUST_02);
		DolocAPI.RaiseInstantPSEffects(positionCenter2, InstantParticleEffectsType.ELECTRIC_SPARKS);
		monsterController.Monster.Damage(_func.Damage, critical: false, out var value);
		DolocAPI.RaiseDamageTip(value, positionCenter2);
		return true;
	}
}
