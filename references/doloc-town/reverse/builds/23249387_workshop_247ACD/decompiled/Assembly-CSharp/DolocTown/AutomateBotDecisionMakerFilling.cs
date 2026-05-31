using System;
using System.Linq;
using DolocTown.GameData;
using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateBotDecisionMakerFilling : AutomateBotDecisionMaker
{
	private readonly AutomateParamFilling _param;

	private PowerGenerator _generatorCache;

	private Feeder _feederCache;

	private IFishTank _fishFeederCache;

	public AutomateBotDecisionMakerFilling(AutomateBot bot, AutomateSystemLocker locker)
		: base(bot, locker)
	{
		_param = (AutomateParamFilling)base.Bot.Param;
	}

	protected override LinearTask AutomateBotMakeDecision()
	{
		if (TryBuildTaskOfFillFuel(out var task))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return task;
		}
		if (TryBuildTaskOfFillAnimalFeeder(out var task2))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return task2;
		}
		if (TryBuildTaskOfFillFishFeeder(out var task3))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return task3;
		}
		CountIdle();
		return base.FixedTaskWanderAroundStation;
	}

	private bool TryBuildTaskOfFillFuel(out LinearTask task)
	{
		task = null;
		PowerGenerator generator = TryGetLowFuelGenerator();
		if (generator == null)
		{
			return false;
		}
		if (!base.BotInventory.ReadAll().Any(IsFuel))
		{
			if (!TryFindFuelContainer(IsFuel, out var container))
			{
				return false;
			}
			if (base.BotInventory.ReadAll().Any((Item x) => !IsFuel(x)))
			{
				return TryReleaseAnyItems(out task);
			}
			task = SmartJourney(container).AutomateTakeItems(container, base.Bot.LeftInventorySpace, IsFuel);
			return true;
		}
		base.locker.LockEquipment(base.Bot, generator);
		task = SmartJourney(generator).AutomateAddFuel(generator, LowFuelGeneratorCondition);
		return true;
		bool IsFuel(Item item)
		{
			if (_param.IsFuel(item))
			{
				return generator.IsSuitableFuel(item);
			}
			return false;
		}
	}

	private bool LowFuelGeneratorCondition(PowerGenerator pg)
	{
		if (pg.IsFuelGenerator)
		{
			return pg.FuelPercent < _param.LowFuelThreshold;
		}
		return false;
	}

	private PowerGenerator TryGetLowFuelGenerator()
	{
		if (_generatorCache != null && LowFuelGeneratorCondition(_generatorCache) && base.locker.IsUnlocked(_generatorCache))
		{
			return _generatorCache;
		}
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		foreach (AutomateStationEnv.RoomEnv roomEnv in roomEnvs)
		{
			_generatorCache = roomEnv.GetEquipments<PowerGenerator>().FirstOrDefault((PowerGenerator x) => base.locker.IsUnlocked(x) && LowFuelGeneratorCondition(x));
			if (_generatorCache != null)
			{
				return _generatorCache;
			}
		}
		return null;
	}

	private bool TryFindFuelContainer(Func<Item, bool> isFuel, out Case container)
	{
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			foreach (Case item in roomEnvs[i].GetEquipments<Case>().Where(_param.IsFuelContainer))
			{
				if (item.inventory.ReadAll().Any(isFuel))
				{
					container = item;
					return true;
				}
			}
		}
		container = null;
		return false;
	}

	private bool TryBuildTaskOfFillAnimalFeeder(out LinearTask task)
	{
		task = null;
		Feeder feeder = TryGetEmptyFeeder();
		if (feeder == null)
		{
			return false;
		}
		if (!base.BotInventory.ReadAll().Any(_param.IsAnimalFeeds))
		{
			if (!TryGetAnimalFeedsContainer(out var container))
			{
				return false;
			}
			if (base.BotInventory.ReadAll().Any((Item x) => !_param.IsAnimalFeeds(x)))
			{
				return TryReleaseAnyItems(out task);
			}
			task = SmartJourney(container).AutomateTakeItems(container, base.Bot.LeftInventorySpace, _param.IsAnimalFeeds);
			return true;
		}
		base.locker.LockEquipment(base.Bot, feeder);
		task = SmartJourney(feeder).AutomateFillAnimalFeeds(feeder, LowAnimalFeederCondition);
		return true;
	}

	private bool LowAnimalFeederCondition(Feeder feeder)
	{
		return feeder.progress < _param.LowAnimalFeederThreshold;
	}

	private Feeder TryGetEmptyFeeder()
	{
		if (_feederCache != null && LowAnimalFeederCondition(_feederCache) && base.locker.IsUnlocked(_feederCache))
		{
			return _feederCache;
		}
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		foreach (AutomateStationEnv.RoomEnv roomEnv in roomEnvs)
		{
			_feederCache = roomEnv.GetEquipments<Feeder>().FirstOrDefault((Feeder x) => base.locker.IsUnlocked(x) && LowAnimalFeederCondition(x));
			if (_feederCache != null)
			{
				return _feederCache;
			}
		}
		return null;
	}

	private bool TryGetAnimalFeedsContainer(out Case container)
	{
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			foreach (Case item in roomEnvs[i].GetEquipments<Case>().Where(_param.IsAnimalFeedsContainer))
			{
				if (item.inventory.ReadAll().Any(_param.IsAnimalFeeds))
				{
					container = item;
					return true;
				}
			}
		}
		container = null;
		return false;
	}

	private bool TryBuildTaskOfFillFishFeeder(out LinearTask task)
	{
		task = null;
		IFishTank fishTank = TryGetEmptyFishFeeder();
		if (fishTank == null)
		{
			return false;
		}
		if (!base.BotInventory.ReadAll().Any(_param.IsFishFeeds))
		{
			if (!TryGetFishFeedsContainer(out var container))
			{
				return false;
			}
			if (base.BotInventory.ReadAll().Any((Item x) => !_param.IsFishFeeds(x)))
			{
				return TryReleaseAnyItems(out task);
			}
			task = SmartJourney(container).AutomateTakeItems(container, base.Bot.LeftInventorySpace, _param.IsFishFeeds);
			return true;
		}
		Equipment equipment = (Equipment)fishTank;
		base.locker.LockEquipment(base.Bot, equipment);
		task = SmartJourney(equipment).AutomateFillFishFeeds(fishTank, LowFishFeederCondition);
		return true;
	}

	private bool LowFishFeederCondition(IFishTank feeder)
	{
		return feeder.EnergyPercent < _param.LowFishFeederThreshold;
	}

	private IFishTank TryGetEmptyFishFeeder()
	{
		if (_fishFeederCache != null && LowFishFeederCondition(_fishFeederCache) && base.locker.IsUnlocked((Equipment)_fishFeederCache))
		{
			return _fishFeederCache;
		}
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		foreach (AutomateStationEnv.RoomEnv roomEnv in roomEnvs)
		{
			_fishFeederCache = roomEnv.GetEquipments<IFishTank>().FirstOrDefault((IFishTank x) => base.locker.IsUnlocked(x.FishTankEquipmentEntity) && LowFishFeederCondition(x));
			if (_fishFeederCache != null)
			{
				return _fishFeederCache;
			}
		}
		return null;
	}

	private bool TryGetFishFeedsContainer(out Case container)
	{
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			foreach (Case item in roomEnvs[i].GetEquipments<Case>().Where(_param.IsFishFeedsContainer))
			{
				if (item.inventory.ReadAll().Any(_param.IsFishFeeds))
				{
					container = item;
					return true;
				}
			}
		}
		container = null;
		return false;
	}
}
