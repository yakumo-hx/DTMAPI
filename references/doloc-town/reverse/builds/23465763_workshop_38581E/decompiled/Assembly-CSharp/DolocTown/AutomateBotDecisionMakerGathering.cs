using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using RedSaw;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class AutomateBotDecisionMakerGathering : AutomateBotDecisionMaker
{
	private readonly AutomateParamGathering _param;

	private IdleMode _idleMode;

	public AutomateBotDecisionMakerGathering(AutomateBot bot, AutomateSystemLocker locker)
		: base(bot, locker)
	{
		_param = (AutomateParamGathering)bot.Param;
	}

	protected override LinearTask AutomateBotMakeDecision()
	{
		if (base.Bot.IsInventoryFull)
		{
			if (!TryReleaseAnyItems(out var task, _param.FilterContainer))
			{
				return base.FixedTaskDropAllItems;
			}
			return task;
		}
		if (_TryBuildGatherMission(out var task2))
		{
			return task2;
		}
		if (__TryBuildTaskOfHarvestCrop(out var task3))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return task3;
		}
		if (__TryBuildTaskOfGatherableEquipment(out var task4))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return task4;
		}
		if (TryReleaseAnyItems(out var task5, _param.FilterContainer, base.CurrentRoom))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return task5;
		}
		return BuildIdleTask();
	}

	private LinearTask BuildIdleTask()
	{
		CountIdle();
		switch (_idleMode)
		{
		case IdleMode.Wander:
			_idleMode = CvtIdleModeToView(5, 0.2f);
			return base.FixedTaskWanderAroundStation;
		case IdleMode.ViewEquipment:
		{
			if (TryGetTaskOfNormalFarming(out var task))
			{
				_idleCount = 0;
				_idleMode = (RandomUtils.Dice(0.5f) ? IdleMode.ViewEquipment : IdleMode.Wander);
				return task;
			}
			_idleMode = IdleMode.Wander;
			break;
		}
		}
		return base.FixedTaskWanderAroundStation;
	}

	private bool __TryBuildTaskOfHarvestCrop(out LinearTask task)
	{
		task = null;
		if (!_param.GatherCrop)
		{
			return false;
		}
		PlantBasin plantBasin = __FindNearestHarvestablePlantBasin();
		if (plantBasin == null)
		{
			return false;
		}
		base.locker.LockEquipment(base.Bot, plantBasin);
		task = SmartJourney(plantBasin.CurrentRoom, plantBasin.PositionTop, () => plantBasin.IsRemoved || !plantBasin.Crop.isMature).AutomateHarvest(plantBasin);
		return true;
	}

	private PlantBasin __FindNearestHarvestablePlantBasin()
	{
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		foreach (AutomateStationEnv.RoomEnv roomEnv in roomEnvs)
		{
			Vector2 futurePositionAfterJourney = GetFuturePositionAfterJourney(base.Bot.CurrentRoom, roomEnv.room);
			PlantBasin plantBasin = __FindNearestHarvestablePlantBasin(futurePositionAfterJourney, roomEnv.room);
			if (plantBasin != null)
			{
				return plantBasin;
			}
		}
		return null;
	}

	private PlantBasin __FindNearestHarvestablePlantBasin(Vector2 botPosition, Room room)
	{
		List<PlantBasin> list = (from x in room.DM_equipment.GetEquipments<PlantBasin>()
			where x.HasCrop && x.Crop.isMature && !base.locker.IsLocked(x)
			select x).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		return list.OrderBy((PlantBasin x) => Vector2.Distance(x.Position, botPosition)).FirstOrDefault();
	}

	private bool _TryBuildGatherMission(out LinearTask task)
	{
		task = null;
		if (!_param.GatherCrop)
		{
			return false;
		}
		Room targetRoom;
		DropItemBase dropItemBase = __FindNearestGatherableDropItem(out targetRoom);
		if (dropItemBase == null)
		{
			return false;
		}
		base.locker.LockDropItem(base.Bot, dropItemBase);
		task = SmartJourney(dropItemBase).AutomatePickUp(dropItemBase);
		return true;
	}

	private DropItemBase __FindNearestGatherableDropItem(out Room targetRoom)
	{
		targetRoom = null;
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		foreach (AutomateStationEnv.RoomEnv roomEnv in roomEnvs)
		{
			Vector2 futurePositionAfterJourney = GetFuturePositionAfterJourney(base.Bot.CurrentRoom, roomEnv.room);
			DropItemBase dropItemBase = __FindNearestGatherableDropItem(futurePositionAfterJourney, roomEnv.room);
			targetRoom = roomEnv.room;
			if (dropItemBase != null)
			{
				return dropItemBase;
			}
		}
		return null;
	}

	private DropItemBase __FindNearestGatherableDropItem(Vector2 botPosition, Room room)
	{
		List<DropItemBase> list = room.DM_dropitem.AllDatas.Where((DropItemBase x) => x.IsItem && !base.locker.IsLocked(x) && HasAnyMatchCase(x)).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		return list.OrderBy((DropItemBase x) => Vector2.Distance(x.PositionWS, botPosition)).FirstOrDefault();
	}

	private bool HasAnyMatchCase(DropItemBase dropItem)
	{
		return base.StationEnv.roomEnvs.Any((AutomateStationEnv.RoomEnv env) => HasAnyMatchCaseInRoom(env.room, dropItem));
	}

	private static bool HasAnyMatchCaseInRoom(Room room, DropItemBase dropItem)
	{
		return room.DM_equipment.GetEquipments<Case>().Any((Case container) => container.IsAutomateLabelMatch(dropItem));
	}

	private bool __TryBuildTaskOfGatherableEquipment(out LinearTask task)
	{
		task = null;
		if (!_param.GatherEquipmentItems)
		{
			return false;
		}
		Equipment equipment = __FindNearestGatherableEquipment();
		if (equipment == null)
		{
			return false;
		}
		base.locker.LockEquipment(base.Bot, equipment);
		task = SmartJourney(equipment).AutomateGatherEquipment(equipment);
		return true;
	}

	private Equipment __FindNearestGatherableEquipment()
	{
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			Equipment equipment = roomEnvs[i].AllEquipments.FirstOrDefault((Equipment x) => x is IGatherableEquipment { IsGatherable: not false } && !base.locker.IsLocked(x));
			if (equipment != null)
			{
				return equipment;
			}
		}
		return null;
	}

	private Equipment __FindNearestGatherableEquipment(Vector2 botPosition, Room room)
	{
		List<Equipment> list = room.DM_equipment.AllEquipments.Where((Equipment x) => x is IGatherableEquipment { IsGatherable: not false } && !base.locker.IsLocked(x)).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		return list.OrderBy((Equipment x) => Vector2.Distance(x.Position, botPosition)).FirstOrDefault();
	}
}
