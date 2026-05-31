using System;
using System.Linq;
using DolocTown.GameData;
using RedSaw;
using RedSaw.AI.LinearTask;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[DebugObject]
public abstract class AutomateBotDecisionMaker : DecisionMaker
{
	protected enum IdleMode
	{
		Wander,
		ViewEquipment
	}

	public static readonly Vector2 GateOffset = new Vector2(0f, 1.5f);

	protected int _idleCount;

	protected LinearTask FixedTaskDropAllItems => FixedTaskBackToStation.Do(delegate
	{
		Item[] array = BotInventory.ReadAll();
		foreach (Item item in array)
		{
			BotInventory.Take(item);
			DolocAPI.GenerateDropItem(StationRoom, item, Bot.Position);
		}
	});

	public AutomateStationEnv StationEnv => Bot.Station.Env;

	public LinearTask FixedTaskBackToStationRoom
	{
		get
		{
			if (IsInStationScene)
			{
				return LinearTask.WaitFrames(1);
			}
			return _JourneyToRoom(StationRoom);
		}
	}

	public LinearTask FixedTaskWanderAroundStation
	{
		get
		{
			LinearTask fixedTaskBackToStationRoom = FixedTaskBackToStationRoom;
			int dur = UnityEngine.Random.Range(2, 5);
			LinearTask linearTask = new AutomateTaskMoveInt(Station.GetRandomPosition(), MoveSpeed).WaitFrm(dur);
			if (fixedTaskBackToStationRoom != null)
			{
				return fixedTaskBackToStationRoom.Then(linearTask);
			}
			return linearTask;
		}
	}

	public virtual LinearTask FixedTaskBackToStation
	{
		get
		{
			if (IsInStationScene)
			{
				return new AutomateTaskMove(Bot.ChargePosition);
			}
			return FixedTaskBackToStationRoom.Then(new AutomateTaskMove(Bot.ChargePosition));
		}
	}

	private LinearTask FixedTaskCharge => FixedTaskBackToStation.WaitFrm(1).AutomateDo(delegate(AutomateBot b)
	{
		b.StartToCharge();
	});

	public string controllerName => GetType().Name.Replace("AutomateBotDecisionMaker", string.Empty).ToLower();

	public AutomateBot Bot { get; private set; }

	public AutomateSystemLocker locker { get; private set; }

	public LinearInventory BotInventory => Bot.inventory;

	public int MoveSpeed => Bot.proto.speed;

	public string CurrentRoomGuid => Bot.CurrentRoomGuid;

	public bool IsInMainFarm => string.IsNullOrEmpty(Bot.CurrentRoomGuid);

	[DebugInfo("当前任务")]
	public string CurrentTaskName
	{
		get
		{
			if (!(base.CurrentTaskType == null))
			{
				return base.CurrentTaskType.Name;
			}
			return "无任务";
		}
	}

	[DebugInfo("是否处于工作站点场景", Color = "#fffde3")]
	public bool IsInStationScene => CurrentRoomGuid == StationRoomGuid;

	public Room CurrentRoom => Bot.CurrentRoom;

	public AutomateBotStation Station => Bot.Station;

	public TemplateRoom StationRoom => (TemplateRoom)Bot.Station.CurrentRoom;

	public TemplateRoomOutdoor StationRootRoom => (TemplateRoomOutdoor)Bot.Station.CurrentRootRoom;

	public string StationRoomGuid => Bot.Station.CurrentRoom.Title;

	protected int IdleCount => _idleCount;

	protected bool TryReleaseAnyItems(out LinearTask task, Func<Case, bool> condition = null, Room highPriorityRoom = null)
	{
		task = null;
		if (BotInventory.isEmpty)
		{
			return false;
		}
		if (!((highPriorityRoom == null) ? StationEnv.TryGetEquipment((Func<Case, bool>)IsTargetContainer, out Case target) : StationEnv.TryGetEquipment(highPriorityRoom, (Func<Case, bool>)IsTargetContainer, out target)))
		{
			return false;
		}
		task = BuildTaskOfReleaseItems(target);
		return true;
		bool IsTargetContainer(Case contaienr)
		{
			return BotInventory.ReadAll().Any((Item item) => contaienr.IsAutomateLabelMatch(item) && (condition?.Invoke(contaienr) ?? true));
		}
	}

	private LinearTask BuildTaskOfReleaseItems(Case targetContainer)
	{
		return SmartJourney(targetContainer).ReleaseItemsAsPossible(targetContainer);
	}

	protected Vector2 GetFuturePositionAfterJourney(Room from, Room to)
	{
		if (from == to)
		{
			return Bot.Position;
		}
		if (to is TemplateRoomInHouse templateRoomInHouse)
		{
			if (templateRoomInHouse.Building.IsNotCellar())
			{
				return templateRoomInHouse.CurrentRoom.Geometry.DefaultEntryPosition + GateOffset;
			}
			Vector2 botPosition = ((from is TemplateRoomInHouse templateRoomInHouse2) ? (templateRoomInHouse2.Building.EntryPosition + GateOffset) : Bot.Position);
			return (from tuple in templateRoomInHouse.GetAllQuitPositionsPair()
				orderby Vector2.Distance(tuple.Item2, botPosition)
				select tuple).FirstOrDefault().Item1.EntryPosition + GateOffset;
		}
		if (!(from is TemplateRoomInHouse templateRoomInHouse3))
		{
			return Bot.Position;
		}
		if (templateRoomInHouse3.Building.IsNotCellar())
		{
			return templateRoomInHouse3.Building.EntryPosition + GateOffset;
		}
		return (from tuple in templateRoomInHouse3.GetAllQuitPositionsPair()
			orderby Vector2.Distance(tuple.Item2, Bot.Position)
			select tuple).FirstOrDefault().Item1.EntryPosition + GateOffset;
	}

	protected LinearTask _JourneyToRoom(Room futureRoom, Vector2 futurePosition, Room room)
	{
		if (futureRoom == null)
		{
			return _JourneyToRoom(room);
		}
		if (futureRoom == room)
		{
			return LinearTask.Empty;
		}
		if (room is TemplateRoomInHouse buildingRoom)
		{
			if (!futureRoom.IsInHouse)
			{
				return LinearTask.StartWith.__AutomateBotJourneyIntoBuildingSameScene(Bot.Position, buildingRoom, MoveSpeed);
			}
			Vector2 quitPosition;
			return FixedTaskLeaveFromRoom(futureRoom, futurePosition, out quitPosition).__AutomateBotJourneyIntoBuildingSameScene(quitPosition, buildingRoom, MoveSpeed);
		}
		Vector2 quitPosition2;
		return FixedTaskLeaveFromRoom(futureRoom, futurePosition, out quitPosition2);
	}

	protected LinearTask _JourneyToRoom(Room room)
	{
		if (CurrentRoom == room)
		{
			return LinearTask.Empty;
		}
		if (room is TemplateRoomInHouse buildingRoom)
		{
			if (IsInMainFarm)
			{
				return LinearTask.StartWith.__AutomateBotJourneyIntoBuildingSameScene(Bot.Position, buildingRoom, MoveSpeed);
			}
			Vector2 quitPosition;
			return FixedTaskLeaveCurrentRoom(out quitPosition).__AutomateBotJourneyIntoBuildingSameScene(quitPosition, buildingRoom, MoveSpeed);
		}
		Vector2 quitPosition2;
		return FixedTaskLeaveCurrentRoom(out quitPosition2);
	}

	protected LinearTask SmartJourney(Room room, Vector2 pos, Func<bool> breaker, LinearTask preTask = null, int wait = 2)
	{
		if (preTask == null)
		{
			preTask = LinearTask.Empty;
		}
		if (CurrentRoom != room)
		{
			return preTask.BeginBreaker(breaker).Then(_JourneyToRoom(room)).AutomateMove(pos)
				.WaitFrm(wait)
				.EndBreaker();
		}
		if (Vector2.Distance(Bot.Position, pos) > 2f)
		{
			return preTask.BeginBreaker(breaker).AutomateMove(pos).WaitFrm(wait)
				.EndBreaker();
		}
		return preTask;
	}

	protected LinearTask SmartJourney(Equipment equipment, LinearTask preTask = null, int wait = 2, Func<bool> breaker = null)
	{
		return SmartJourney(equipment.CurrentRoom, equipment.PositionTop, () => equipment.IsRemoved || (breaker?.Invoke() ?? false), preTask, wait);
	}

	protected LinearTask SmartJourney(DropItemBase dropItem, LinearTask preTask = null, int wait = 2, Func<bool> breaker = null)
	{
		return SmartJourney(dropItem.Host.CurrentRoom, dropItem.PositionWS, () => dropItem.IsRemoved || (breaker?.Invoke() ?? false), preTask, wait);
	}

	public LinearTask FixedTaskLeaveFromRoom(Room room, Vector2 futurePosition, out Vector2 quitPosition)
	{
		quitPosition = default(Vector2);
		if (!(room is TemplateRoomInHouse templateRoomInHouse))
		{
			return LinearTask.Empty;
		}
		if (templateRoomInHouse.Building.IsNotCellar())
		{
			quitPosition = templateRoomInHouse.Building.EntryPosition + GateOffset;
			return LinearTask.StartWith.AutomateMove(templateRoomInHouse.Geometry.DefaultEntryPosition + GateOffset, room).Then(new AutomateTaskEnterMainFarm());
		}
		(Building, Vector2) tuple2 = (from tuple in templateRoomInHouse.GetAllQuitPositionsPair()
			orderby Vector2.Distance(tuple.Item2, futurePosition)
			select tuple).FirstOrDefault();
		quitPosition = tuple2.Item1.EntryPosition + GateOffset;
		return LinearTask.Empty.AutomateMove(tuple2.Item2 + GateOffset, room).Then(new AutomateTaskEnterMainFarm(tuple2.Item1));
	}

	public LinearTask FixedTaskLeaveCurrentRoom(out Vector2 quitPosition)
	{
		quitPosition = default(Vector2);
		if (!(CurrentRoom is TemplateRoomInHouse templateRoomInHouse))
		{
			return LinearTask.Empty;
		}
		if (templateRoomInHouse.Building.IsNotCellar())
		{
			quitPosition = templateRoomInHouse.Building.EntryPosition + GateOffset;
			return LinearTask.StartWith.AutomateMove(templateRoomInHouse.Geometry.DefaultEntryPosition + GateOffset).Then(new AutomateTaskEnterMainFarm());
		}
		(Building, Vector2) tuple2 = (from tuple in templateRoomInHouse.GetAllQuitPositionsPair()
			orderby Vector2.Distance(tuple.Item2, Bot.Position)
			select tuple).FirstOrDefault();
		quitPosition = tuple2.Item1.EntryPosition + GateOffset;
		return LinearTask.StartWith.AutomateMove(tuple2.Item2 + GateOffset).Then(new AutomateTaskEnterMainFarm(tuple2.Item1));
	}

	protected bool TryBuildTaskOfViewEquipment<T>(Func<T, bool> predicate, Action<T> viewBehaviour, out LinearTask task) where T : Equipment
	{
		task = null;
		T equipment = StationEnv.GetRandomEquipment(predicate);
		if (equipment == null)
		{
			return false;
		}
		task = SmartJourney(equipment).Do(delegate
		{
			viewBehaviour?.Invoke(equipment);
		}).WaitFrm(3);
		return true;
	}

	protected IdleMode CvtIdleModeToView(int threshold, float probability)
	{
		if (_idleCount < threshold)
		{
			if (!RandomUtils.Dice(probability))
			{
				return IdleMode.Wander;
			}
			return IdleMode.ViewEquipment;
		}
		return IdleMode.ViewEquipment;
	}

	protected bool TryGetTaskOfNormalFarming(out LinearTask task)
	{
		return TryBuildTaskOfViewEquipment((PlantBasin basin) => basin.HasCrop && RandomUtils.Dice(0.3f), ViewBehaviour, out task);
		void ViewBehaviour(PlantBasin basin)
		{
			if (!Bot.IsNotRenderNow)
			{
				if (!basin.HasCrop)
				{
					Bot.RaiseEmotion(EmotionName.CONFUSE, 0.2f);
				}
				else if (basin.IsCropMature)
				{
					EmotionName emotion = new EmotionName[4]
					{
						EmotionName.LOVE,
						EmotionName.LAUGH,
						EmotionName.PROUD,
						EmotionName.NOTE
					}.Choice();
					Bot.RaiseEmotion(emotion, 0.2f);
				}
				else if (basin.Crop.isDead)
				{
					Bot.RaiseEmotion(EmotionName.NOCOMMENT);
				}
			}
		}
	}

	public static AutomateBotDecisionMaker Create(AutomateBot bot)
	{
		Type controllerType = bot.proto.ControllerType;
		AutomateSystemLocker automateSystemLocker = ((TemplateRoomOutdoor)bot.Station.CurrentRootRoom).DM_automate.Locker;
		return (AutomateBotDecisionMaker)Activator.CreateInstance(controllerType, bot, automateSystemLocker);
	}

	public AutomateBotDecisionMaker(AutomateBot bot, AutomateSystemLocker locker)
	{
		Bot = bot;
		this.locker = locker;
	}

	public void SetLocker(AutomateSystemLocker locker)
	{
		this.locker = locker;
	}

	protected sealed override LinearTask MakeDecision()
	{
		locker.ClearLockedThingsOfBot(Bot);
		LinearTask linearTask = (Bot.IsLowPower ? FixedTaskCharge : AutomateBotMakeDecision());
		if (linearTask == null)
		{
			return LinearTask.WaitFrames(3);
		}
		linearTask = linearTask.Root;
		foreach (LinearTask item in linearTask.AllMissionsFollowed)
		{
			if (item is AutomateTask automateTask)
			{
				automateTask.SetBot(Bot);
			}
		}
		return linearTask;
	}

	protected void RaiseEmotionIfIdleCount(int threshold, EmotionName emotion, float probability = -1f)
	{
		if (!(probability >= 0f) || !(probability < 1f) || RandomUtils.Dice(probability))
		{
			if (_idleCount <= threshold)
			{
				_idleCount = 0;
				return;
			}
			_idleCount = 0;
			Bot.RaiseEmotion(emotion);
		}
	}

	protected void CountIdle()
	{
		_idleCount++;
	}

	protected abstract LinearTask AutomateBotMakeDecision();

	public virtual void OnEnterRoom(Room room)
	{
	}

	public virtual void OnRender()
	{
	}

	public virtual void OnUnRender()
	{
	}

	public virtual void ResetTask()
	{
	}

	public virtual void OnUnload()
	{
	}
}
