using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Automate;
using DolocTown.Config.Plant;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using RedSaw;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class AutomateBotDecisionMakerFarming : AutomateBotDecisionMaker
{
	private readonly AutomateParamFarming _param;

	private readonly AutomateBotFunctionFarming _func;

	private IdleMode _idleMode;

	public AutomateBotDecisionMakerFarming(AutomateBot bot, AutomateSystemLocker locker)
		: base(bot, locker)
	{
		_param = (AutomateParamFarming)bot.Param;
		_func = (AutomateBotFunctionFarming)bot.proto.Function;
		_idleMode = IdleMode.Wander;
	}

	protected override LinearTask AutomateBotMakeDecision()
	{
		if (TryBuildTaskOfProtect(isHighPriority: true, out var task))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return task;
		}
		if (TryBuildTaskOfPlant(out var task2))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return task2;
		}
		if (TryBuildTaskOfWatering(out var task3))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return task3;
		}
		if (TryBuildTaskOfFertilizer(out var task4))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return task4;
		}
		if (TryBuildTaskOfProtect(isHighPriority: false, out var task5))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return task5;
		}
		if (TryReleaseAnyItems(out var task6))
		{
			return task6;
		}
		return BuildIdleTask();
	}

	private LinearTask BuildIdleTask()
	{
		CountIdle();
		switch (_idleMode)
		{
		case IdleMode.Wander:
			if (_idleCount >= 5)
			{
				_idleMode = IdleMode.ViewEquipment;
				_idleCount = 0;
			}
			return base.FixedTaskWanderAroundStation;
		case IdleMode.ViewEquipment:
		{
			if (TryGetTaskOfNormalFarming(out var task))
			{
				_idleMode = (RandomUtils.Dice(0.8f) ? IdleMode.ViewEquipment : IdleMode.Wander);
				return task;
			}
			_idleMode = IdleMode.Wander;
			break;
		}
		}
		return base.FixedTaskWanderAroundStation;
	}

	private Case GetFarmingContainer(Func<Case, bool> condition)
	{
		return base.StationEnv.GetEquipment((Case _case) => (!_param.FilterContainerSkin || _param.ContainerSkinSet.Contains(_case.skinIndex)) && condition(_case));
	}

	private bool TryBuildTaskOfPlant(out LinearTask task)
	{
		if (!_param.AutoPlant)
		{
			task = null;
			return false;
		}
		if (base.BotInventory.isEmpty)
		{
			return __TryBuildTaskOfPlantEmptyInventory(out task);
		}
		if (!__TryBuildTaskOfPlantWithExistedSeeds(out task))
		{
			return __TryReleaseUselessSeeds(out task);
		}
		return true;
	}

	private bool __TryBuildTaskOfPlantEmptyInventory(out LinearTask task)
	{
		task = null;
		var (seedCase, array) = TryGetPlantMissions(base.Bot.LeftInventorySpace);
		if (seedCase == null)
		{
			return false;
		}
		AutomatePlantMissionInfo[] missions1 = array;
		LinearTask linearTask = SmartJourney(seedCase).Do(delegate
		{
			PlantCondition[] conds = missions1.Select((AutomatePlantMissionInfo m) => m.condition).ToArray();
			ItemSeed[] array3 = seedCase.inventory.AutomatePatchTakeSeedsFromContainer(conds);
			List<Sprite> list = new List<Sprite>();
			ItemSeed[] array4 = array3;
			foreach (ItemSeed itemSeed in array4)
			{
				Item item = base.Bot.inventory.PlaceItem(itemSeed);
				if (item != null)
				{
					seedCase.inventory.PlaceItem(item);
				}
				else
				{
					list.Add(itemSeed.uiSprite);
				}
			}
			base.Bot.RaiseSpriteArrayFadeUp(list.ToArray());
		});
		task = linearTask;
		Room currentRoom = seedCase.CurrentRoom;
		Vector3 vector = seedCase.Position;
		AutomatePlantMissionInfo[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			AutomatePlantMissionInfo missionInfo = array2[i];
			PlantBasin plantBasin = missionInfo.plantBasin;
			base.locker.LockEquipment(base.Bot, plantBasin);
			task = task.Then(_JourneyToRoom(currentRoom, vector, plantBasin.CurrentRoom)).AutomateMove(plantBasin.PositionTop).Wait(2f)
				.AutomatePlant(missionInfo);
			currentRoom = plantBasin.CurrentRoom;
			vector = plantBasin.PositionTop;
		}
		return true;
	}

	private bool __TryBuildTaskOfPlantWithExistedSeeds(out LinearTask task)
	{
		task = null;
		if (!TryGetPlantMissionFromBotInventory(out var missionInfo))
		{
			return false;
		}
		PlantBasin basin = missionInfo.plantBasin;
		task = SmartJourney(basin, null, 2, () => basin.IsPlanted).AutomatePlant(missionInfo);
		return true;
	}

	protected (Case, AutomatePlantMissionInfo[]) TryGetPlantMissions(int maxCount)
	{
		if (maxCount <= 0)
		{
			return default((Case, AutomatePlantMissionInfo[]));
		}
		Dictionary<Case, List<AutomatePlantMissionInfo>> dictionary = new Dictionary<Case, List<AutomatePlantMissionInfo>>();
		int num = 0;
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			PlantBasin[] array = roomEnvs[i].GetEquipments((PlantBasin b) => b.IsNotPlanted && !base.locker.IsLocked(b)).ToArray();
			foreach (PlantBasin plantBasin in array)
			{
				PlantCondition cond = plantBasin.GetPlantCondition();
				Case farmingContainer = GetFarmingContainer((Case c) => c.inventory.ReadAll<ItemSeed>().Any((ItemSeed seed) => cond.IsMatchSeed(seed)));
				if (farmingContainer != null)
				{
					if (!dictionary.ContainsKey(farmingContainer))
					{
						dictionary[farmingContainer] = new List<AutomatePlantMissionInfo>();
					}
					dictionary[farmingContainer].Add(new AutomatePlantMissionInfo(cond, plantBasin, farmingContainer));
					if (++num >= maxCount)
					{
						break;
					}
				}
			}
			if (num >= maxCount)
			{
				break;
			}
		}
		if (dictionary.Count <= 0)
		{
			return default((Case, AutomatePlantMissionInfo[]));
		}
		return AutomateUtils.ClipMissions(dictionary);
	}

	protected bool TryGetPlantMissionFromBotInventory(out AutomatePlantMissionInfo missionInfo)
	{
		missionInfo = default(AutomatePlantMissionInfo);
		if (base.BotInventory.isEmpty)
		{
			return false;
		}
		foreach (ItemSeed item in base.BotInventory.ReadAll<ItemSeed>())
		{
			if (__TryBuildMissionFromGivenSeed(item, out missionInfo))
			{
				return true;
			}
		}
		return false;
	}

	private bool __TryBuildMissionFromGivenSeed(ItemSeed seed, out AutomatePlantMissionInfo missionInfo)
	{
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			PlantBasin plantBasin = roomEnvs[i].GetEquipments<PlantBasin>().FirstOrDefault((PlantBasin B) => B.IsNotPlanted && !base.locker.IsLocked(B) && B.GetPlantCondition().IsMatchSeed(seed));
			if (plantBasin != null)
			{
				PlantCondition plantCondition = plantBasin.GetPlantCondition();
				missionInfo = new AutomatePlantMissionInfo(plantCondition, plantBasin, null);
				return true;
			}
		}
		missionInfo = default(AutomatePlantMissionInfo);
		return false;
	}

	private bool __TryReleaseUselessSeeds(out LinearTask task)
	{
		task = null;
		Item firstItem = base.BotInventory.ReadAll().FirstOrDefault();
		if (firstItem == null)
		{
			return false;
		}
		Case farmingContainer = GetFarmingContainer((Case c) => c.IsAutomateLabelMatch(firstItem) && c.inventory.CanPlaceIn(firstItem));
		if (farmingContainer == null)
		{
			return false;
		}
		task = SmartJourney(farmingContainer).ReleaseItemsAsPossible(farmingContainer);
		return true;
	}

	private bool TryBuildTaskOfProtect(bool isHighPriority, out LinearTask task)
	{
		task = null;
		if (!_param.AutoProtect)
		{
			return false;
		}
		if (isHighPriority && !DolocAPI.archiveHandle.CurrentWeatherType.IsMalignantWeather())
		{
			return false;
		}
		if (!HasAnyProtectMissionFromBotInventory(isHighPriority, out var missionInfo))
		{
			return false;
		}
		if (__TryBuildTaskOfProtectFromBotInventory(missionInfo, out task))
		{
			return true;
		}
		if (base.Bot.LeftInventorySpace > 0)
		{
			return __TryBuildTaskOfProtectHighPriority(isHighPriority, out task);
		}
		if (isHighPriority)
		{
			return TryReleaseAnyItems(out task);
		}
		return false;
	}

	private bool __TryBuildTaskOfProtectHighPriority(bool isHighPriority, out LinearTask task)
	{
		task = null;
		var (@case, array) = TryGetProtectMissions(isHighPriority, base.Bot.LeftInventorySpace);
		if (@case == null)
		{
			return false;
		}
		LinearTask linearTask = SmartJourney(@case).AutomateTakeItems(@case, array.Length, (ItemFilm I) => true);
		task = linearTask;
		Room currentRoom = @case.CurrentRoom;
		Vector3 vector = @case.Position;
		AutomateFarmingMissionInfo[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			PlantBasin plantBasin = array2[i].plantBasin;
			base.locker.LockEquipment(base.Bot, plantBasin);
			task = task.Then(_JourneyToRoom(currentRoom, vector, plantBasin.CurrentRoom)).AutomateMove(plantBasin.PositionTop).Wait(2f)
				.AutomateProtectUseLocalItem(plantBasin);
			currentRoom = plantBasin.CurrentRoom;
			vector = plantBasin.PositionTop;
		}
		return true;
	}

	private bool __TryBuildTaskOfProtectFromBotInventory(AutomateFarmingMissionInfo missionInfo, out LinearTask task)
	{
		task = null;
		if (!base.BotInventory.ReadAll<ItemFilm>().Any())
		{
			return false;
		}
		PlantBasin basin = missionInfo.plantBasin;
		base.locker.LockEquipment(base.Bot, basin);
		task = SmartJourney(basin, null, 2, () => !basin.HasCrop || basin.IsProtected).AutomateProtectUseLocalItem(basin);
		return true;
	}

	private bool HasAnyProtectMissionFromBotInventory(bool isHighPriority, out AutomateFarmingMissionInfo missionInfo)
	{
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		foreach (AutomateStationEnv.RoomEnv roomEnv in roomEnvs)
		{
			if (!isHighPriority || !(roomEnv.room is TemplateRoomInHouse templateRoomInHouse) || templateRoomInHouse.Building.IsBroken)
			{
				PlantBasin plantBasin = (from pb in roomEnv.GetEquipments<PlantBasin>()
					where pb.HasCrop && !pb.IsProtected && !base.locker.IsLocked(pb)
					select pb).ToArray().FirstOrDefault((PlantBasin pb) => NeedProtect(isHighPriority, pb));
				if (plantBasin != null)
				{
					missionInfo = new AutomateFarmingMissionInfo(plantBasin, null);
					return true;
				}
			}
		}
		missionInfo = default(AutomateFarmingMissionInfo);
		return false;
	}

	private (Case, AutomateFarmingMissionInfo[]) TryGetProtectMissions(bool isHighPriority, int maxCount)
	{
		if (maxCount <= 0)
		{
			return default((Case, AutomateFarmingMissionInfo[]));
		}
		Dictionary<Case, List<AutomateFarmingMissionInfo>> dictionary = new Dictionary<Case, List<AutomateFarmingMissionInfo>>();
		bool flag = false;
		int num = 0;
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		foreach (AutomateStationEnv.RoomEnv roomEnv in roomEnvs)
		{
			if (isHighPriority && roomEnv.room is TemplateRoomInHouse templateRoomInHouse && !templateRoomInHouse.Building.IsBroken)
			{
				continue;
			}
			foreach (PlantBasin item2 in from pb in roomEnv.GetEquipments<PlantBasin>()
				where pb.HasCrop && !pb.IsProtected && !base.locker.IsLocked(pb)
				select pb)
			{
				if (NeedProtect(isHighPriority, item2))
				{
					Case farmingContainer = GetFarmingContainer((Case C) => C.inventory.ReadAll().OfType<ItemFilm>().Any());
					if (farmingContainer == null)
					{
						flag = true;
						break;
					}
					AutomateFarmingMissionInfo item = new AutomateFarmingMissionInfo(item2, farmingContainer);
					if (!dictionary.ContainsKey(farmingContainer))
					{
						dictionary[farmingContainer] = new List<AutomateFarmingMissionInfo>();
					}
					dictionary[farmingContainer].Add(item);
					if (++num >= maxCount)
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (dictionary.Count <= 0)
		{
			return default((Case, AutomateFarmingMissionInfo[]));
		}
		return AutomateUtils.ClipMissions(dictionary);
	}

	private static bool NeedProtect(bool isHighPriority, PlantBasin basin)
	{
		if (!basin.HasCrop)
		{
			return false;
		}
		Crop crop = basin.Crop;
		return basin.CurrentRoom.CurrentWeatherInfo.Id switch
		{
			WeatherType.ACID_RAIN => crop.geneProtos.All((CropGeneInfo x) => x.Id != "anti_acid_rain"), 
			WeatherType.SCORCH_SUN => crop.geneProtos.All((CropGeneInfo x) => x.Id != "conifer_leaf"), 
			_ => !isHighPriority, 
		};
	}

	private bool TryBuildTaskOfWatering(out LinearTask task)
	{
		task = null;
		if (!_param.AutoWatering)
		{
			return false;
		}
		if (base.Bot.Power < _func.SprinklerCost)
		{
			return false;
		}
		var (basin, array) = GetWateringMission();
		if (basin == null)
		{
			return false;
		}
		base.locker.LockEquipment(base.Bot, basin);
		PlantBasin[] array2 = array;
		foreach (PlantBasin equipment in array2)
		{
			base.locker.LockEquipment(base.Bot, equipment);
		}
		PlantBasin[] array3 = new PlantBasin[array.Length + 1];
		for (int j = 0; j < array.Length; j++)
		{
			array3[j] = array[j];
		}
		array3[^1] = basin;
		task = SmartJourney(basin, null, 2, () => !basin.HasCrop).AutomateWatering(array3, _func.SprinklerCost);
		return true;
	}

	private (PlantBasin, PlantBasin[]) GetWateringMission()
	{
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			HashSet<PlantBasin> set = new HashSet<PlantBasin>(roomEnvs[i].GetEquipments((PlantBasin pb) => pb.HasCrop && !base.locker.IsLocked(pb) && (float)pb.Supply.WaterValue < _param.WateringThreshold).ToArray());
			(PlantBasin, PlantBasin[]) result = __GetBestWateringBasin(set);
			if (result.Item1 != null)
			{
				return result;
			}
		}
		return (null, null);
	}

	private (PlantBasin, PlantBasin[]) __GetBestWateringBasin(HashSet<PlantBasin> set)
	{
		int num = -1;
		PlantBasin item = null;
		PlantBasin[] item2 = null;
		foreach (PlantBasin item3 in set)
		{
			PlantBasin[] array = __GetAroundBasins(set, item3);
			if (array.Length > num)
			{
				num = array.Length;
				item = item3;
				item2 = array;
			}
		}
		return (item, item2);
	}

	private PlantBasin[] __GetAroundBasins(HashSet<PlantBasin> set, PlantBasin basin)
	{
		HashSet<PlantBasin> hashSet = new HashSet<PlantBasin>();
		Vector2Int[] range = _func.GetRange(basin.CoveredSize, basin.Anchor);
		foreach (Vector2Int pos in range)
		{
			PlantBasin content = basin.CurrentRoom.DM_terrain.GetContent<PlantBasin>(pos);
			if (content != null && set.Contains(content))
			{
				hashSet.Add(content);
			}
		}
		return hashSet.ToArray();
	}

	private bool TryBuildTaskOfFertilizer(out LinearTask task)
	{
		task = null;
		if (!_param.AutoFertilizer)
		{
			return false;
		}
		if (!HasAnyFertilizerMissionFromBotInventory(out var missionInfo))
		{
			return false;
		}
		if (__TryBuildTaskOfFertilizerFromBotInventory(missionInfo, out task))
		{
			return true;
		}
		if (base.Bot.LeftInventorySpace > 0)
		{
			return __TryBuildTaskOfFertilizer(out task);
		}
		return false;
	}

	private bool HasAnyFertilizerMissionFromBotInventory(out AutomateFarmingMissionInfo missionInfo)
	{
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			PlantBasin plantBasin = (from pb in roomEnvs[i].GetEquipments<PlantBasin>()
				where pb.HasCrop && !pb.IsFertilizerd && !base.locker.IsLocked(pb)
				select pb).ToArray().FirstOrDefault();
			if (plantBasin != null)
			{
				missionInfo = new AutomateFarmingMissionInfo(plantBasin, null);
				return true;
			}
		}
		missionInfo = default(AutomateFarmingMissionInfo);
		return false;
	}

	private bool __TryBuildTaskOfFertilizerFromBotInventory(AutomateFarmingMissionInfo mission, out LinearTask task)
	{
		task = null;
		if (base.BotInventory.ReadAll<ItemFertilizer>().All((ItemFertilizer f) => f.func.IsTree))
		{
			return false;
		}
		PlantBasin basin = mission.plantBasin;
		base.locker.LockEquipment(base.Bot, basin);
		task = SmartJourney(basin, null, 2, () => !basin.HasCrop || basin.IsFertilizerd).AutomateFertilizerUseLocalItem(basin);
		return true;
	}

	private bool __TryBuildTaskOfFertilizer(out LinearTask task)
	{
		task = null;
		var (@case, array) = TryGetFertilizerMissions(base.Bot.LeftInventorySpace);
		if (@case == null)
		{
			return false;
		}
		LinearTask linearTask = SmartJourney(@case).AutomateTakeItems(@case, array.Length, (ItemFertilizer f) => !f.func.IsTree);
		task = linearTask;
		Room currentRoom = @case.CurrentRoom;
		Vector3 vector = @case.Position;
		AutomateFarmingMissionInfo[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			PlantBasin plantBasin = array2[i].plantBasin;
			base.locker.LockEquipment(base.Bot, plantBasin);
			task = task.Then(_JourneyToRoom(currentRoom, vector, plantBasin.CurrentRoom)).AutomateMove(plantBasin.PositionTop).Wait(2f)
				.AutomateFertilizerUseLocalItem(plantBasin);
			currentRoom = plantBasin.CurrentRoom;
			vector = plantBasin.PositionTop;
		}
		return true;
	}

	private (Case, AutomateFarmingMissionInfo[]) TryGetFertilizerMissions(int maxCount)
	{
		if (maxCount <= 0)
		{
			return default((Case, AutomateFarmingMissionInfo[]));
		}
		Dictionary<Case, List<AutomateFarmingMissionInfo>> dictionary = new Dictionary<Case, List<AutomateFarmingMissionInfo>>();
		bool flag = false;
		int num = 0;
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			foreach (PlantBasin item2 in from pb in roomEnvs[i].GetEquipments<PlantBasin>()
				where pb.HasCrop && !pb.IsFertilizerd && !base.locker.IsLocked(pb)
				select pb)
			{
				Case farmingContainer = GetFarmingContainer((Case C) => C.inventory.ReadAll<ItemFertilizer>().Any((ItemFertilizer f) => !f.func.IsTree));
				if (farmingContainer == null)
				{
					flag = true;
					break;
				}
				AutomateFarmingMissionInfo item = new AutomateFarmingMissionInfo(item2, farmingContainer);
				if (!dictionary.ContainsKey(farmingContainer))
				{
					dictionary[farmingContainer] = new List<AutomateFarmingMissionInfo>();
				}
				dictionary[farmingContainer].Add(item);
				if (++num >= maxCount)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (dictionary.Count <= 0)
		{
			return default((Case, AutomateFarmingMissionInfo[]));
		}
		return AutomateUtils.ClipMissions(dictionary);
	}
}
