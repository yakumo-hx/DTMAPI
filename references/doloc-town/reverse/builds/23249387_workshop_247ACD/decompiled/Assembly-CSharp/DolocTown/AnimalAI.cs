using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Time;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using DolocTown.Utils;
using RedSaw;
using RedSaw.AI.LinearTask;
using RedSaw.AI.StateMachine;
using UnityEngine;

namespace DolocTown;

public class AnimalAI : RedSaw.AI.StateMachine.StateMachine
{
	[State("chicken", true)]
	public class Chicken_FreeTimeState : AnimalAIState
	{
		private readonly Counter metabolismCDCounter = new Counter(100);

		public Chicken_FreeTimeState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (base.ShouldSleepNow || animal.isSleep)
			{
				return GetState<Normal_SleepState>();
			}
			if (animal.NeedBreed && animal.CheckBreedInterval())
			{
				return GetState<Normal_BreedState>();
			}
			if (animal.NeedMetabolism && animal.Energy >= (float)DolocAPI.GlobalParameter.AnimalChickennestEnergyRequire && animal.CheckMetabolismInterval())
			{
				return GetState<Chicken_MetabolismState>();
			}
			if (animal.CheckExcreteInterval() && animal.ShouldExcrete)
			{
				return GetState<Normal_ExcreteState>();
			}
			if (animal.IsHungry)
			{
				return GetState<Normal_HungryState>();
			}
			return null;
		}

		public override LinearTask MakeDecision()
		{
			return MakeDecision_FreeTime();
		}
	}

	[State("chicken", false)]
	public class Chicken_MetabolismState : AnimalAIState
	{
		private int buildCount;

		public Chicken_MetabolismState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (base.ShouldSleepNow || animal.isSleep)
			{
				return GetState<Normal_SleepState>();
			}
			if (hasFailed || !animal.NeedMetabolism)
			{
				return GetState<Chicken_FreeTimeState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			base.OnEnter();
			buildCount = 0;
		}

		public override LinearTask MakeDecision()
		{
			if (TryEscapeRain(out var task))
			{
				return task;
			}
			if (!animal.IsInHouse && animal.GenTask_JourneyToHomeRoom(force: false, out var task2))
			{
				return task2;
			}
			if (FindChickenNestToProduce(animal, out var task3))
			{
				return task3;
			}
			if (buildCount > 0)
			{
				hasFailed = true;
				return WanderEx();
			}
			buildCount++;
			if (TryBuildChickenNest(animal, out var task4))
			{
				return task4;
			}
			hasFailed = true;
			return WanderEx();
		}
	}

	public abstract class AnimalAIState : State
	{
		protected bool hasFailed;

		protected readonly Queue<Room> availableRooms = new Queue<Room>();

		protected readonly Animal animal;

		protected readonly State defaultAnyState;

		private int wanderCount;

		protected bool ShouldSleepNow => DolocAPI.archiveHandle.CurrentDayPeriodType == DayPeriodType.Night;

		protected bool IsRainyWeather
		{
			get
			{
				WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
				if (!currentWeatherType.IsRainyWeather())
				{
					return currentWeatherType == WeatherType.ACID_RAIN;
				}
				return true;
			}
		}

		protected AnimalAIState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(machine)
		{
			this.animal = animal;
			defaultAnyState = machine.GetState(GetDefaultAnyState(animal.protoName));
		}

		public override void OnEnter()
		{
			hasFailed = false;
			availableRooms.Clear();
			availableRooms.Enqueue(animal.AnotherRoom);
		}

		protected bool FindFoodInCurrentEnv(Animal animal, out LinearTask task)
		{
			List<IFeeder> list = (from x in animal.CurrentEnv.GetFeeders()
				where !x.IsFeederEmpty && x.AnimalInteractableIsValid
				select x).ToList();
			task = null;
			if (list.Count == 0)
			{
				return false;
			}
			IOrderedEnumerable<IFeeder> orderedEnumerable = from x in list
				orderby x.FeederPriority descending, x.AnimalInteractablePosition.ManhattenDistance(animal.positionCell) + x.AnimalCounter
				select x;
			foreach (IFeeder feeder2 in orderedEnumerable)
			{
				Vector2Int feederTouchPosition = feeder2.GetFeederTouchPosition(animal.currentRoom, animal.width, animal.positionCell);
				if (feederTouchPosition.y == animal.positionCell.y && Mathf.Abs(feederTouchPosition.x - animal.positionCell.x) <= animal.width)
				{
					task = LinearTask.StartWith.BeginBreaker(() => !feeder2.AnimalInteractableIsValid || feeder2.IsFeederEmpty).AnimalEat(feeder2).EndBreaker()
						.Wait(1f);
					return true;
				}
				if (AnimalUtils.CanArrive(animal.currentRoom, animal.positionCell, feederTouchPosition, animal.width, out var path))
				{
					task = LinearTask.StartWith.BeginBreaker(() => !feeder2.AnimalInteractableIsValid || feeder2.IsFeederEmpty).AnimalMoveTaskSequence(path).AnimalEat(feeder2)
						.EndBreaker()
						.Wait(1f);
					return true;
				}
			}
			if (orderedEnumerable.Any())
			{
				IFeeder feeder = orderedEnumerable.First();
				task = LinearTask.StartWith.BeginBreaker(() => !feeder.AnimalInteractableIsValid || feeder.IsFeederEmpty).AnimalEat(feeder, playAnimation: false).EndBreaker()
					.Wait(1f);
				return true;
			}
			task = null;
			return false;
		}

		protected bool FindTolietInCurrentEnv(Animal animal, out LinearTask task)
		{
			List<IAnimalToilet> list = (from x in animal.CurrentEnv.GetToilets()
				where !x.IsToiletFull
				select x).ToList();
			task = null;
			if (list.Count == 0)
			{
				return false;
			}
			list.Sort((IAnimalToilet a, IAnimalToilet b) => a.Distance(animal.positionCell) - b.Distance(animal.positionCell) + a.AnimalCounter - b.AnimalCounter);
			foreach (IAnimalToilet toilet in list)
			{
				if (AnimalUtils.CanArrive(animal.currentRoom, animal.positionCell, toilet.AnimalInteractablePosition, animal.width, out var path))
				{
					task = LinearTask.WaitFrames(1).BeginBreaker(() => !toilet.AnimalInteractableIsValid || toilet.IsToiletFull).AnimalMoveTaskSequence(path)
						.AnimalExcrete(toilet)
						.EndBreaker()
						.Wait(1f);
					return true;
				}
			}
			task = null;
			return false;
		}

		protected bool FindLivestockNurseryToBreed(Animal animal, out LinearTask task)
		{
			List<IAnimalLivestockNursery> list = (from x in animal.CurrentEnv.GetLivestockNurseries()
				where x.IsLivestockNurseryFree
				select x).ToList();
			task = null;
			if (list.Count == 0)
			{
				return false;
			}
			list.Sort((IAnimalLivestockNursery a, IAnimalLivestockNursery b) => a.Distance(animal.positionCell) - b.Distance(animal.positionCell));
			foreach (IAnimalLivestockNursery nursery in list)
			{
				if (!AnimalUtils.CanArrive(animal.currentRoom, animal.positionCell, nursery.AnimalInteractablePosition, animal.width, out var path))
				{
					continue;
				}
				task = LinearTask.WaitFrames(1).BeginBreaker(() => !nursery.AnimalInteractableIsValid || !nursery.IsLivestockNurseryFree).AnimalMoveTaskSequence(path)
					.Do(delegate
					{
						if (nursery.IsLivestockNurseryFree && animal._Breed())
						{
							nursery.StartBreed(animal.proto.BreedDuration);
						}
					})
					.EndBreaker()
					.Wait(1f);
				return true;
			}
			task = null;
			return false;
		}

		protected bool FindMilkingMachineToProduce(Animal animal, out LinearTask task)
		{
			task = null;
			List<IAnimalMilkingMachine> list = (from x in animal.CurrentEnv.GetMilkingMachines()
				where !x.IsFull
				select x).ToList();
			list.Sort((IAnimalMilkingMachine a, IAnimalMilkingMachine b) => a.AnimalInteractablePosition.ManhattenDistance(animal.positionCell) - b.AnimalInteractablePosition.ManhattenDistance(animal.positionCell));
			foreach (IAnimalMilkingMachine machine in list)
			{
				if (!animal.GenTask_JourneyToPosition(machine.AnimalInteractablePosition, out var task2))
				{
					continue;
				}
				task = task2.Do(delegate
				{
					if (!machine.IsFull)
					{
						machine.Produce(animal.ProduceAsItems());
					}
				});
				return true;
			}
			return false;
		}

		protected bool FindLintRollerToProduce(Animal animal, out LinearTask task)
		{
			task = null;
			List<IAnimalLintRoller> list = (from x in animal.CurrentEnv.GetLintRollers()
				where !x.IsFull
				select x).ToList();
			list.Sort((IAnimalLintRoller a, IAnimalLintRoller b) => a.AnimalInteractablePosition.ManhattenDistance(animal.positionCell) - b.AnimalInteractablePosition.ManhattenDistance(animal.positionCell));
			foreach (IAnimalLintRoller roller in list)
			{
				if (!animal.GenTask_JourneyToPosition(roller.AnimalInteractablePosition, out var task2))
				{
					continue;
				}
				task = task2.Do(delegate
				{
					if (!roller.IsFull)
					{
						roller.Produce(animal.ProduceAsItems());
					}
				});
				return true;
			}
			return false;
		}

		protected bool FindChickenNestToProduce(Animal animal, out LinearTask task)
		{
			task = null;
			List<ChickenNest> list = (from x in animal.CurrentEnv.GetChickenNests()
				where !x.IsFull
				select x).ToList();
			list.Sort((ChickenNest a, ChickenNest b) => a.Anchor.ManhattenDistance(animal.positionCell) - b.Anchor.ManhattenDistance(animal.positionCell));
			foreach (ChickenNest nest in list)
			{
				if (!animal.GenTask_JourneyToPosition(nest.Anchor, out var task2))
				{
					continue;
				}
				task = task2.Do(delegate
				{
					if (!nest.IsFull)
					{
						nest.Produce(animal.ProduceAsItems());
					}
				});
				return true;
			}
			return false;
		}

		protected bool TryBuildChickenNest(Animal animal, out LinearTask task)
		{
			task = null;
			if (!animal.IsInHome)
			{
				return false;
			}
			if (animal.Energy < (float)DolocAPI.GlobalParameter.AnimalChickennestEnergyRequire)
			{
				return false;
			}
			EquipmentInfo equipmentInfo = DolocConfig.Tables.TbEquipment.Get("chicken_nest");
			if (equipmentInfo == null)
			{
				Debug.LogError("AnimalWork_BuildChickenNest.GenTask: 未找到鸡窝原型");
				return false;
			}
			AnimalRoomEnv homeEnv = animal.HomeEnv;
			if (homeEnv == null)
			{
				return false;
			}
			if (!homeEnv.GetRandomEmptyPositionForEquipment(equipmentInfo.CoverSize, out var anchor))
			{
				return false;
			}
			if (!animal.GenTask_JourneyToPosition(anchor, out var task2))
			{
				Debug.LogError($"AnimalWork_BuildChickenNest.GenTask: 无法生成前往位置{anchor}的任务");
				return false;
			}
			task = task2.Do(delegate
			{
				Debug.Log("小动物\"" + animal.Title + "\"正在建造鸡窝");
				if (AnimalUtils.BuildChickenNest(animal, anchor, out var equipment))
				{
					animal.TryCostEnergy(DolocAPI.GlobalParameter.AnimalChickennestEnergyRequire);
					if (animal.IsRender)
					{
						animal.Renderer.Eat();
						DolocAPI.RaiseInstantAnimEffects(equipment.PositionBottom, InstAnimEffectType.PLAYER_LAND_SMOKE);
						DolocAPI.RaiseInstantPSEffects(equipment.PositionCenter, InstantParticleEffectsType.BRUST_STARS);
					}
				}
			});
			return true;
		}

		protected bool FindHoneyCombToProduce(Animal animal, out LinearTask task)
		{
			task = null;
			List<IAnimalHoneyComb> list = (from x in animal.CurrentEnv.GetHoneyCombs()
				where !x.IsHoneyCombFull
				select x).ToList();
			if (list.Count == 0)
			{
				return false;
			}
			list.Sort((IAnimalHoneyComb a, IAnimalHoneyComb b) => a.Distance(animal.positionCell) - b.Distance(animal.positionCell) + a.AnimalCounter - b.AnimalCounter);
			foreach (IAnimalHoneyComb honeyComb2 in list)
			{
				if (AnimalUtils.CanArrive(animal.currentRoom, animal.positionCell, honeyComb2.AnimalInteractablePosition, animal.width, out var path))
				{
					task = LinearTask.StartWith.AnimalMoveTaskSequence(path).Do(delegate
					{
						Produce(honeyComb2, animal);
					}).Wait(1f);
					return true;
				}
			}
			return false;
			static void Produce(IAnimalHoneyComb honeyComb, Animal animal)
			{
				if (honeyComb.AnimalInteractableIsValid && !honeyComb.IsHoneyCombFull && animal.NeedMetabolism)
				{
					honeyComb.ProduceHoney(animal.ProduceAsItems());
				}
			}
		}

		protected LinearTask Idle(int min = 2, int max = 6)
		{
			return LinearTask.WaitFrames(UnityEngine.Random.Range(min, max));
		}

		protected LinearTask WanderEx()
		{
			if (wanderCount < 5)
			{
				wanderCount++;
				if (!RandomUtils.Dice(0.5f))
				{
					return Idle();
				}
				return WanderHorizontal(3);
			}
			if (RandomUtils.Dice((float)(wanderCount - 5) * 0.1f + 0.1f))
			{
				wanderCount = 0;
				if (RandomUtils.Dice(0.5f))
				{
					return Idle();
				}
				if (!RandomUtils.Dice(0.7f))
				{
					return WanderRandom();
				}
				return WanderAround();
			}
			wanderCount++;
			if (!RandomUtils.Dice(0.5f))
			{
				return Idle();
			}
			return WanderHorizontal(3);
		}

		private LinearTask WanderAround()
		{
			if (!animal.GenTask_JourneyToPosition(animal.AroundPosition, out var task))
			{
				return Idle();
			}
			return task;
		}

		private LinearTask WanderRandom()
		{
			if (!animal.GenTask_JourneyToPosition(animal.RandomPosition, out var task))
			{
				return Idle();
			}
			return task;
		}

		private LinearTask WanderHorizontal(int range = 5)
		{
			List<Vector2Int> list = new List<Vector2Int>();
			for (int i = -range; i <= range; i++)
			{
				list.Add(new Vector2Int(animal.positionCell.x + i, animal.positionCell.y));
			}
			list = list.Where((Vector2Int x) => AnimalUtils.IsPositionWalkable(animal.currentRoom, x, animal.width) && animal.CurrentEnv.ContainsPosition(x)).ToList();
			if (list.Count == 0)
			{
				return Idle();
			}
			Vector2Int targetPosition = list.Choice();
			if (!animal.GenTask_JourneyToPosition(targetPosition, out var task))
			{
				return Idle();
			}
			return task;
		}

		protected bool TryEnterToRoom(Room room, out LinearTask task, bool force = false)
		{
			return animal.GenTask_JourneyToRoom(room, force, out task);
		}

		protected bool TryEscapeRain(out LinearTask task)
		{
			task = null;
			if (IsRainyWeather && !animal.currentRoom.IsInHouse)
			{
				return animal.GenTask_JourneyToRoom(animal.homeRoom, force: true, out task);
			}
			return false;
		}

		protected bool TryEscapeRain(out LinearTask task, out bool isRainyWeather)
		{
			isRainyWeather = IsRainyWeather;
			task = null;
			if (isRainyWeather && !animal.currentRoom.IsInHouse)
			{
				return animal.GenTask_JourneyToRoom(animal.homeRoom, force: true, out task);
			}
			return false;
		}

		protected bool EscapeRainCheckForRoom(Room room)
		{
			if (IsRainyWeather)
			{
				return room.IsInHouse;
			}
			return true;
		}

		public abstract LinearTask MakeDecision();

		protected LinearTask MakeDecision_FreeTime()
		{
			if (TryEscapeRain(out var task, out var isRainyWeather))
			{
				return task;
			}
			if (!isRainyWeather && animal.IsInHouse && animal.GenTask_JourneyToRoom(animal.AnotherRoom, force: false, out var task2))
			{
				return task2;
			}
			return WanderEx();
		}

		protected LinearTask MakeDecision_Sleep()
		{
			if (animal.isSleep)
			{
				return LinearTask.WaitFrames(5);
			}
			if (animal.currentRoom.IsInHouse)
			{
				if (animal.GenTask_JourneyToPosition(animal.RandomPosition, out var task))
				{
					return task.Do(animal.Sleep);
				}
				return LinearTask.StartWith.Do(animal.Sleep);
			}
			if (animal.GenTask_JourneyToRoom(animal.homeRoom, force: true, out var task2))
			{
				return task2;
			}
			return LinearTask.WaitFrames(1);
		}

		protected LinearTask MakeDecision_Hungry()
		{
			if (TryEscapeRain(out var task))
			{
				return task;
			}
			if (FindFoodInCurrentEnv(animal, out var task2))
			{
				return task2;
			}
			if (availableRooms.Count > 0)
			{
				Room room = availableRooms.Dequeue();
				if (EscapeRainCheckForRoom(room) && animal.GenTask_JourneyToRoom(room, force: false, out var task3))
				{
					return task3;
				}
			}
			hasFailed = true;
			return WanderEx();
		}

		protected LinearTask MakeDecision_Excrete()
		{
			if (TryEscapeRain(out var task))
			{
				return task;
			}
			if (FindTolietInCurrentEnv(animal, out var task2))
			{
				return task2;
			}
			if (availableRooms.Count > 0)
			{
				Room room = availableRooms.Dequeue();
				if (EscapeRainCheckForRoom(room) && animal.GenTask_JourneyToRoom(room, force: false, out var task3))
				{
					return task3;
				}
			}
			hasFailed = true;
			return Idle();
		}

		protected LinearTask MakeDecision_Breed()
		{
			if (TryEscapeRain(out var task))
			{
				return task;
			}
			if (FindLivestockNurseryToBreed(animal, out var task2))
			{
				return task2;
			}
			if (availableRooms.Count > 0)
			{
				Room room = availableRooms.Dequeue();
				if (EscapeRainCheckForRoom(room) && animal.GenTask_JourneyToRoom(room, force: false, out var task3))
				{
					return task3;
				}
			}
			hasFailed = true;
			return Idle();
		}
	}

	[State("goat", false)]
	public class Goat_FreeTimeState : AnimalAIState
	{
		public Goat_FreeTimeState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (base.ShouldSleepNow || animal.isSleep)
			{
				return GetState<Normal_SleepState>();
			}
			if (animal.NeedBreed && animal.CheckBreedInterval())
			{
				return GetState<Normal_BreedState>();
			}
			if (animal.NeedMetabolism && animal.CheckMetabolismInterval())
			{
				return GetState<Goat_MetabolismState>();
			}
			if (animal.CheckExcreteInterval() && animal.ShouldExcrete)
			{
				return GetState<Normal_ExcreteState>();
			}
			if (animal.IsHungry)
			{
				return GetState<Normal_HungryState>();
			}
			return null;
		}

		public override LinearTask MakeDecision()
		{
			return MakeDecision_FreeTime();
		}
	}

	[State("goat", false)]
	public class Goat_MetabolismState : AnimalAIState
	{
		private bool hasAnotherRoomEntered;

		public Goat_MetabolismState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (base.ShouldSleepNow || animal.isSleep)
			{
				return GetState<Normal_SleepState>();
			}
			if (hasFailed || !animal.NeedMetabolism)
			{
				return GetState<Goat_FreeTimeState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			base.OnEnter();
			hasAnotherRoomEntered = false;
		}

		public override LinearTask MakeDecision()
		{
			if (TryEscapeRain(out var task))
			{
				return task;
			}
			if (FindLintRollerToProduce(animal, out var task2))
			{
				return task2;
			}
			if (hasAnotherRoomEntered)
			{
				hasFailed = true;
				return WanderEx();
			}
			if (animal.GenTask_JourneyToRoom(AnimalRoomState.AnotherRoom, force: false, out var task3))
			{
				hasAnotherRoomEntered = true;
				return task3;
			}
			hasFailed = true;
			return WanderEx();
		}
	}

	[State("slime", true)]
	public class Slime_FreeTimeState : AnimalAIState
	{
		private readonly Counter metabolismCDCounter = new Counter(100);

		public Slime_FreeTimeState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (base.ShouldSleepNow || animal.isSleep)
			{
				return GetState<Normal_SleepState>();
			}
			if (animal.NeedBreed && animal.CheckBreedInterval())
			{
				return GetState<Normal_BreedState>();
			}
			if (animal.NeedMetabolism && metabolismCDCounter.Tick())
			{
				return GetState<Slime_MetabolismState>();
			}
			if (animal.CheckExcreteInterval() && animal.ShouldExcrete)
			{
				return GetState<Normal_ExcreteState>();
			}
			if (animal.IsHungry)
			{
				return GetState<Normal_HungryState>();
			}
			return null;
		}

		public override LinearTask MakeDecision()
		{
			return MakeDecision_FreeTime();
		}
	}

	[State("slime", false)]
	public class Slime_MetabolismState : AnimalAIState
	{
		public Slime_MetabolismState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (hasFailed || !animal.NeedMetabolism)
			{
				return GetState<Slime_FreeTimeState>();
			}
			return null;
		}

		public override LinearTask MakeDecision()
		{
			if (TryEscapeRain(out var task))
			{
				return task;
			}
			if (FindHoneyCombToProduce(animal, out var task2))
			{
				return task2;
			}
			if (availableRooms.Count > 0)
			{
				Room room = availableRooms.Dequeue();
				if (EscapeRainCheckForRoom(room) && animal.GenTask_JourneyToRoom(room, force: false, out var task3))
				{
					return task3;
				}
			}
			hasFailed = true;
			return Idle();
		}
	}

	[State("marsh_pangolin", true)]
	public class MarshPangolin_FreeTimeState : AnimalAIState
	{
		public MarshPangolin_FreeTimeState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (base.ShouldSleepNow || animal.isSleep)
			{
				return GetState<Normal_SleepState>();
			}
			if (animal.NeedBreed && animal.CheckBreedInterval())
			{
				return GetState<Normal_BreedState>();
			}
			if (animal.NeedMetabolism && animal.CheckMetabolismInterval())
			{
				return GetState<MarshPangolin_MetabolismState>();
			}
			if (animal.CheckExcreteInterval() && animal.ShouldExcrete)
			{
				return GetState<Normal_ExcreteState>();
			}
			if (animal.IsHungry)
			{
				return GetState<Normal_HungryState>();
			}
			return null;
		}

		public override LinearTask MakeDecision()
		{
			return MakeDecision_FreeTime();
		}
	}

	[State("marsh_pangolin", false)]
	public class MarshPangolin_MetabolismState : AnimalAIState
	{
		private bool hasAnotherRoomEntered;

		public MarshPangolin_MetabolismState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (base.ShouldSleepNow || animal.isSleep)
			{
				return GetState<Normal_SleepState>();
			}
			if (hasFailed || !animal.NeedMetabolism)
			{
				return GetState<MarshPangolin_FreeTimeState>();
			}
			return null;
		}

		public override void OnEnter()
		{
			base.OnEnter();
			hasAnotherRoomEntered = false;
		}

		public override LinearTask MakeDecision()
		{
			if (TryEscapeRain(out var task))
			{
				return task;
			}
			if (FindMilkingMachineToProduce(animal, out var task2))
			{
				return task2;
			}
			if (hasAnotherRoomEntered)
			{
				hasFailed = true;
				return WanderEx();
			}
			if (animal.GenTask_JourneyToRoom(AnimalRoomState.AnotherRoom, force: false, out var task3))
			{
				hasAnotherRoomEntered = true;
				return task3;
			}
			hasFailed = true;
			return WanderEx();
		}
	}

	[State("normal", true)]
	public class Normal_FreeTimeState : AnimalAIState
	{
		public Normal_FreeTimeState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (base.ShouldSleepNow || animal.isSleep)
			{
				return GetState<Normal_SleepState>();
			}
			if (animal.NeedBreed && animal.CheckBreedInterval())
			{
				return GetState<Normal_BreedState>();
			}
			if (animal.CheckExcreteInterval() && animal.ShouldExcrete)
			{
				return GetState<Normal_ExcreteState>();
			}
			if (animal.IsHungry)
			{
				return GetState<Normal_HungryState>();
			}
			return null;
		}

		public override LinearTask MakeDecision()
		{
			return MakeDecision_FreeTime();
		}
	}

	[State("goat", false)]
	[State("marsh_pangolin", false)]
	[State("normal", false)]
	[State("slime", false)]
	[State("chicken", false)]
	public class Normal_BreedState : AnimalAIState
	{
		public Normal_BreedState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (!hasFailed && animal.NeedBreed)
			{
				return null;
			}
			return defaultAnyState;
		}

		public override LinearTask MakeDecision()
		{
			return MakeDecision_Breed();
		}
	}

	[State("goat", false)]
	[State("marsh_pangolin", false)]
	[State("normal", false)]
	[State("slime", false)]
	[State("chicken", false)]
	public class Normal_ExcreteState : AnimalAIState
	{
		public Normal_ExcreteState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (hasFailed || !animal.ShouldExcrete)
			{
				return defaultAnyState;
			}
			return null;
		}

		public override LinearTask MakeDecision()
		{
			return MakeDecision_Excrete();
		}
	}

	[State("goat", false)]
	[State("marsh_pangolin", false)]
	[State("normal", false)]
	[State("slime", false)]
	[State("chicken", false)]
	public class Normal_HungryState : AnimalAIState
	{
		public Normal_HungryState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (base.ShouldSleepNow || animal.isSleep)
			{
				return GetState<Normal_SleepState>();
			}
			if (hasFailed && animal.CheckEatInterval())
			{
				OnEnter();
			}
			if (!animal.IsHungry)
			{
				return defaultAnyState;
			}
			return null;
		}

		public override LinearTask MakeDecision()
		{
			return MakeDecision_Hungry();
		}
	}

	[State("normal", false)]
	[State("goat", false)]
	[State("marsh_pangolin", false)]
	[State("slime", false)]
	[State("chicken", false)]
	public class Normal_SleepState : AnimalAIState
	{
		public Normal_SleepState(Animal animal, RedSaw.AI.StateMachine.StateMachine machine)
			: base(animal, machine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (!base.ShouldSleepNow)
			{
				return defaultAnyState;
			}
			return null;
		}

		public override void OnExit()
		{
			if (animal.isSleep)
			{
				animal.WakeUp();
			}
		}

		public override LinearTask MakeDecision()
		{
			return MakeDecision_Sleep();
		}
	}

	private AnimalAIState CurrentAnimalState;

	private static Type GetDefaultAnyState(string animalId)
	{
		return animalId switch
		{
			"chicken" => typeof(Chicken_FreeTimeState), 
			"slime" => typeof(Slime_FreeTimeState), 
			"goat" => typeof(Goat_FreeTimeState), 
			"marsh_pangolin" => typeof(MarshPangolin_FreeTimeState), 
			_ => typeof(Normal_FreeTimeState), 
		};
	}

	public static AnimalAI Create(string name, Animal animal)
	{
		return new AnimalAI(name, new object[1] { animal });
	}

	private AnimalAI(string name, object[] args)
		: base(name, args)
	{
	}

	protected override void OnStateChanged(State nextState)
	{
		CurrentAnimalState = (AnimalAIState)nextState;
	}

	public LinearTask MakeDecision()
	{
		if (CurrentAnimalState == null)
		{
			Debug.LogError("AnimalAI: 当前状态为空，无法进行决策");
			Update(1f);
			return LinearTask.Defalut;
		}
		return CurrentAnimalState.MakeDecision();
	}
}
