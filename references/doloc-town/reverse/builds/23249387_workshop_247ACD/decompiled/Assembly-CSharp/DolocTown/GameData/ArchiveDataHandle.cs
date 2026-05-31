using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Sound;
using DolocTown.Config.Time;
using DolocTown.Config.Weather;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public class ArchiveDataHandle
{
	private readonly DateTime timeOnLoad;

	[JsonProperty]
	public int archiveIndex { get; private set; }

	[JsonProperty]
	public FarmArchiveData farmData { get; private set; }

	[JsonProperty]
	public DungeonArchiveData dungeonData { get; private set; }

	[JsonProperty]
	public CityArchiveData cityData { get; private set; }

	[JsonProperty]
	public TimeArchiveData timeData { get; private set; }

	[JsonProperty]
	public ExtraArchiveData extraData { get; private set; }

	[JsonProperty]
	public DataTracker dataTracker { get; private set; }

	private long CurrentTotalGameSeconds
	{
		get
		{
			if (baseDataOnLoad == null)
			{
				return 0L;
			}
			TimeSpan timeSpan = DateTime.Now - timeOnLoad;
			return baseDataOnLoad.totalGameSeconds + (long)timeSpan.TotalSeconds;
		}
	}

	public Room currentRoom
	{
		get
		{
			return farmData.currentRoom;
		}
		set
		{
			if (value == null)
			{
				_OnExitRoom(farmData.currentRoom);
				farmData.currentRoom = null;
			}
			else
			{
				_OnExitRoom(farmData.currentRoom);
				farmData.currentRoom = value;
				_OnEnterRoom(value);
			}
		}
	}

	public bool IsTraining => dungeonData.trainFlag;

	public RoomType currentRoomType => farmData.currentRoomType;

	public string currentSceneName => farmData.currentSceneName;

	public string currentRoomName => farmData.currentRoomName;

	public int currentSceneId => farmData.currentSceneId;

	public string currentSceneTitle => DolocConfig.Tables.TbScene.GetOrDefault(currentRoom?.RoomId ?? "")?.Title ?? DolocConfig.Tables.TbScene.GetOrDefault(currentRoom?.SceneRawName ?? "")?.Title ?? currentRoom?.SceneShortName ?? "";

	public EnvironmentType currentEnvironmentType => (currentRoom?.SceneConfig?.BgmEnvironmentType).GetValueOrDefault();

	public TemplateRoomOutdoor MainFarm => farmData.MainFarm;

	public int farmLevel => farmData.agentData.farmLevel;

	public Dungeon CurrentDungeon
	{
		get
		{
			return dungeonData.currentDungeon;
		}
		set
		{
			dungeonData.currentDungeon = value;
		}
	}

	public int CurrentMoney
	{
		get
		{
			return farmData.agentData.money;
		}
		set
		{
			SetCurrentMoney(value, out var moneyMade);
			if (moneyMade > 0)
			{
				DolocAPI.Broadcast(GameEventType.MAKE_MONEY, new GameEventArgsInt(moneyMade));
			}
		}
	}

	public float CurrentHealthPercent => (float)farmData.agentData.health / (float)farmData.agentData.MaxHealth;

	public float CurrentOverflowHealthPercent
	{
		get
		{
			float num = (float)farmData.agentData.overflowHealth / (float)farmData.agentData.MaxHealth;
			if (farmData.agentData.agentEquipment.TryGetShieldItem(out var item))
			{
				if (num > 0f)
				{
					return (num + item.ShieldPercent) / 2f;
				}
				return item.ShieldPercent;
			}
			return num;
		}
	}

	public float CurrentEnergyPercent => (float)farmData.agentData.energy / (float)farmData.agentData.MaxEnergy;

	public float CurrentOverflowEnergyPercent => (float)farmData.agentData.overflowEnergy / (float)farmData.agentData.MaxEnergy;

	public float CurrentSpiritPercent => farmData.agentData.CurrentSpiritPercent;

	public InventorySystem InventorySystem => farmData.inventory;

	public int backpackLevel => farmData.agentData.backpackLevel;

	[JsonProperty]
	public BaseArchiveData baseData => new BaseArchiveData(Application.version, archiveIndex, farmData.agentData.customPlayerName, farmData.agentData.money, farmData.currentRoomType, farmData.currentSceneName, DateNow, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), CurrentTotalGameSeconds, DolocAPI.modManager.EnabledMods);

	public BaseArchiveData baseDataOnLoad { get; private set; }

	public WeatherType GlobalWeatherType => timeData.weather.WeatherType;

	public WeatherInfo GlobalWeatherInfo => WeatherSystem.WeatherInfo;

	public WeatherSystem GlobalWeatherSystem => timeData.weather;

	public WeatherType CurrentWeatherType => timeData.weather.WeatherType;

	public WeatherInfo CurrentWeatherInfo => WeatherSystem.WeatherInfo;

	public WeatherSystem WeatherSystem => timeData.weather;

	public float DayProcess => timeData.DayProcess;

	public DateInfo DateNow => timeData.dateNow;

	public WeekDay CurrentWeekDay => timeData.dateNow.WeekDay;

	public DayPeriodType CurrentDayPeriodType
	{
		get
		{
			int hour = DolocAPI.archiveHandle.DateNow.Hour;
			return DolocConfig.Tables.TbDayPeriod.GetOrDefault(hour)?.DayPeriodType ?? DayPeriodType.None;
		}
	}

	public bool ShouldLightUp
	{
		get
		{
			if (DolocAPI.GlobalParameter.DefaultSwitchSchedule == null)
			{
				float dayProcess = timeData.DayProcess;
				return dayProcess < 0.2f || dayProcess > 0.75f;
			}
			SwitchScheduleParams param = new SwitchScheduleParams(timeData.dateNow, timeData.weather.WeatherType);
			return DolocAPI.GlobalParameter.DefaultSwitchSchedule.IsTrue(param);
		}
	}

	public bool ShouldDroneLightUp
	{
		get
		{
			if (DolocAPI.GlobalParameter.DroneSwitchSchedule == null)
			{
				return false;
			}
			Room room = DolocAPI.CurrentRoom;
			if (room != null && room.Type == RoomType.City && room.IsInHouse)
			{
				return false;
			}
			SwitchScheduleParams param = new SwitchScheduleParams(timeData.dateNow, timeData.weather.WeatherType);
			return DolocAPI.GlobalParameter.DroneSwitchSchedule.IsTrue(param);
		}
	}

	public bool ShouldAgentLightUp
	{
		get
		{
			if (ShouldLightUp)
			{
				return !DolocAPI.droneRenderer.isVisible;
			}
			return false;
		}
	}

	public SeasonInfo Season => timeData.SeasonProto;

	public void SetCurrentMoney(int value, out int moneyMade)
	{
		value = Mathf.Clamp(value, 0, DolocAPI.GlobalParameter.MaxMoney);
		moneyMade = value - farmData.agentData.money;
		farmData.agentData.money = value;
		DolocAPI.uiSystem.MoneyTip.SetMoney(value);
	}

	public LinearInventory[] GetAvailableInventories(Vector2Int anchor, Vector2Int area, bool useBox)
	{
		List<LinearInventory> list = new List<LinearInventory> { InventorySystem.inventory };
		List<Case> list2 = new List<Case>();
		List<StorageShelf> list3 = new List<StorageShelf>();
		IEquipmentHost equipmentHost = currentRoom;
		if (equipmentHost != null)
		{
			HashSet<Equipment> hashSet = equipmentHost.DM_terrain.GetContentsFromArea<Equipment>(anchor, area).ToHashSet();
			foreach (Equipment allEquipment in equipmentHost.AllEquipments)
			{
				if (allEquipment is Case @case && (@case.IsShared || hashSet.Contains(@case)))
				{
					list2.Add(@case);
				}
				else if (DolocAPI.userSettings.autoUseBox && allEquipment is StorageShelf storageShelf && (storageShelf.IsShared || hashSet.Contains(storageShelf)))
				{
					list3.Add(storageShelf);
				}
			}
		}
		list2 = list2.OrderBy((Case x) => (x.Position - DolocAPI.AgentPosition).magnitude).ToList();
		list3 = list3.OrderBy((StorageShelf x) => (x.Position - DolocAPI.AgentPosition).magnitude).ToList();
		foreach (Case item in list2)
		{
			list.Add(item.inventory);
		}
		if (useBox)
		{
			Item[] array = InventorySystem.inventory.ReadAll();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] is ItemBox itemBox)
				{
					list.Add(itemBox.inventory);
				}
			}
			foreach (StorageShelf item2 in list3)
			{
				array = item2.inventory.ReadAll();
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] is ItemBox itemBox2)
					{
						list.Add(itemBox2.inventory);
					}
				}
			}
		}
		return list.ToArray();
	}

	public ArchiveDataHandle(int archiveIndex)
	{
		this.archiveIndex = archiveIndex;
		timeData = new TimeArchiveData();
		farmData = new FarmArchiveData();
		cityData = new CityArchiveData();
		dungeonData = new DungeonArchiveData();
		extraData = new ExtraArchiveData();
		dataTracker = new DataTracker();
		timeOnLoad = DateTime.Now;
		baseDataOnLoad = baseData;
	}

	[JsonConstructor]
	private ArchiveDataHandle(int archiveIndex = 0, FarmArchiveData farmData = null, CityArchiveData cityData = null, DungeonArchiveData dungeonData = null, TimeArchiveData timeData = null, DataTracker dataTracker = null, ExtraArchiveData extraData = null, long latestTotalGameSeconds = 0L, BaseArchiveData baseData = null)
	{
		this.archiveIndex = archiveIndex;
		this.farmData = farmData ?? new FarmArchiveData();
		this.cityData = cityData ?? new CityArchiveData();
		this.dungeonData = dungeonData ?? new DungeonArchiveData();
		this.timeData = timeData ?? new TimeArchiveData();
		this.extraData = extraData ?? new ExtraArchiveData();
		this.dataTracker = dataTracker ?? new DataTracker();
		baseDataOnLoad = baseData;
		timeOnLoad = DateTime.Now;
	}

	public void SendMessage(GameMessage message)
	{
		if (message == null || message.IsUsed)
		{
			return;
		}
		farmData.agentData.agentEquipment.SendMessage(message);
		cityData.storeManager.OnMessage(message);
		if (currentRoom != null)
		{
			ISceneHandle sceneHandle = currentRoom.LoadSceneHandle();
			if (sceneHandle != null)
			{
				sceneHandle.SendMessage(message);
				sceneHandle.RefreshCondition();
				if (message.IsUsed)
				{
					return;
				}
			}
			else
			{
				UnityEngine.Debug.LogWarning("房间\"" + currentRoom.RoomId + "\"场景控制柄为空");
			}
		}
		farmData.eventRecorderManager.Record(message.Type, message.Args);
		farmData.missionManager.SendMessage(message.Type, message.Args);
	}

	public void SetArchiveIndex(int index)
	{
		archiveIndex = index;
	}

	public WeatherSystem GetCurrentWeatherSystem(Room room)
	{
		if (room == null || !(room is DungeonRoom dungeonRoom))
		{
			return timeData.weather;
		}
		if (!DolocAPI.archiveHandle.dungeonData.dungeonManager.GetDungeon(dungeonRoom.dungeonProtoName, out var dungeon))
		{
			return timeData.weather;
		}
		_ = dungeon.proto.customWeatherSystem;
		return timeData.weather;
	}

	public void Update()
	{
		_UpdatePerSec();
		if (timeData.TUCounter.Tick())
		{
			_UpdatePerTU();
		}
	}

	public void UpdateNoRender()
	{
		_UpdatePerSecNoRender();
		if (timeData.TUCounter.Tick())
		{
			_UpdatePerTUNoRender();
		}
	}

	public void RenderWeatherAndDayNight(bool shouldTransit = false)
	{
		DolocAPI.EnvCovariantController.RenderWeather(timeData.DayProcess, CurrentWeatherType, currentRoom, shouldTransit);
	}

	public void SetWeather(WeatherType type, bool shouldRender = false)
	{
		if (timeData.weather.SetCurrentWeather(type, timeData.totalSeconds, out var hasWeatherPropertyChanged))
		{
			if (shouldRender)
			{
				_OnWeatherChanged(type, hasWeatherPropertyChanged);
				RenderWeatherAndDayNight();
			}
			else
			{
				_OnWeatherChangedNoRender(type, hasWeatherPropertyChanged);
			}
		}
	}

	public void PatchWeather(WeatherType type)
	{
		Vector2Int currentWeatherKey = DateNow.CurrentWeatherKey;
		timeData.weatherPatch.AddPatch(DateNow.TotalMonth, currentWeatherKey.x, currentWeatherKey.y, type);
	}

	public void PassLongTime(int seconds, Action callback = null, float waitTime = 0f, float fadeInTime = 3f, float fadeOutTime = 1.5f)
	{
		new GameStateUniversalTransition(DolocAPI.userInput, delegate
		{
			try
			{
				Stopwatch stopwatch = new Stopwatch();
				DolocAPI.devHelper.Console.SetReceiveUnityLog(value: false);
				stopwatch.Start();
				PassTimeNoControl(seconds, callback);
				stopwatch.Stop();
				DolocAPI.devHelper.Console.SetReceiveUnityLog(value: true);
				UnityEngine.Debug.Log($"跳过时间耗时: {stopwatch.ElapsedMilliseconds}ms");
			}
			catch (Exception exception)
			{
				UnityEngine.Debug.LogError("捕捉到跳过时间的回调函数异常");
				UnityEngine.Debug.LogException(exception);
			}
		}, waitTime, fadeInTime, fadeOutTime).Startup();
	}

	public void PassTime(int seconds, Action callback = null, float waitTime = 0f, float fadeInTime = 3f, float fadeOutTime = 1.5f)
	{
		new GameStateUniversalTransition(DolocAPI.userInput, delegate
		{
			try
			{
				PassTimeNoControl(seconds, callback);
			}
			catch (Exception exception)
			{
				UnityEngine.Debug.LogError("捕捉到跳过时间的回调函数异常");
				UnityEngine.Debug.LogException(exception);
			}
		}, waitTime, fadeInTime, fadeOutTime).Startup();
	}

	public void PassTimeNoControl(int seconds, Action callback = null, bool shouldRefresh = true)
	{
		if (shouldRefresh)
		{
			_BeforeTimePass();
		}
		for (int i = 0; i < seconds; i++)
		{
			UpdateNoRender();
		}
		if (shouldRefresh)
		{
			_AfterTimePass();
		}
		callback?.InvokeSafe();
	}

	public void TrackBackTime(int seconds, Action callback = null, float waitTime = 0f, float fadeInTime = 0f, float fadeOutTime = 0f)
	{
		new GameStateUniversalTransition(DolocAPI.userInput, delegate
		{
			try
			{
				_BeforeTimePass();
				TraceBackTimeNoControl(seconds);
				_AfterTimePass();
				callback?.InvokeSafe();
			}
			catch (Exception exception)
			{
				UnityEngine.Debug.LogError("捕捉到回溯时间的回调函数异常");
				UnityEngine.Debug.LogException(exception);
			}
		}, waitTime, fadeInTime, fadeOutTime).Startup();
	}

	private void TraceBackTimeNoControl(int seconds)
	{
		seconds = Mathf.Clamp(seconds, 0, timeData.totalSeconds);
		GameEventRecorderInt gameEventRecorderInt = (GameEventRecorderInt)farmData.eventRecorderManager.GetRecorder(GameEventType.HOUR_PASSED);
		GameEventRecorderInt gameEventRecorderInt2 = (GameEventRecorderInt)farmData.eventRecorderManager.GetRecorder(GameEventType.DAY_PASSED);
		GameEventRecorderInt gameEventRecorderInt3 = (GameEventRecorderInt)farmData.eventRecorderManager.GetRecorder(GameEventType.MONTH_PASSED);
		GameEventRecorderInt gameEventRecorderInt4 = (GameEventRecorderInt)farmData.eventRecorderManager.GetRecorder(GameEventType.YEAR_PASSED);
		int num = seconds / DolocAPI.GlobalParameter.TULength;
		for (int i = 0; i < num; i++)
		{
			timeData.TraceBackTime(out var hasHourChanged, out var hasDayChanged, out var hasMonthChanged, out var hasYearChanged);
			if (hasHourChanged)
			{
				gameEventRecorderInt.UnRecord(1);
			}
			if (hasDayChanged)
			{
				gameEventRecorderInt2.UnRecord(1);
			}
			if (hasMonthChanged)
			{
				gameEventRecorderInt3.UnRecord(1);
			}
			if (hasYearChanged)
			{
				gameEventRecorderInt4.UnRecord(1);
			}
		}
		timeData.totalSeconds -= seconds;
		timeData.weather.ClipHistory(timeData.totalSeconds);
	}

	private void UpdateDate()
	{
		timeData.UpdateDate(out var hasHourChanged, out var hasDayChanged, out var hasMonthChanged, out var hasYearChanged);
		if (hasHourChanged)
		{
			_OnHourChanged(timeData.dateNow.Hour);
			TryRefreshEvent(isRender: true);
		}
		if (hasDayChanged)
		{
			_OnDayChanged(timeData.dateNow.Day, timeData.dateNow.TotalDays);
		}
		if (hasMonthChanged)
		{
			_OnMonthChanged(timeData.dateNow.Month);
			_OnSeasonChanged();
		}
		if (hasYearChanged)
		{
			_OnYearChanged(timeData.dateNow.Year);
		}
	}

	private void UpdateDateNoRender()
	{
		timeData.UpdateDate(out var hasHourChanged, out var hasDayChanged, out var hasMonthChanged, out var hasYearChanged);
		if (hasHourChanged)
		{
			_OnHourChangedNoRender(timeData.dateNow.Hour);
			TryRefreshEvent(isRender: false);
		}
		if (hasDayChanged)
		{
			_OnDayChangedNoRender(timeData.dateNow.Day, timeData.dateNow.TotalDays);
		}
		if (hasMonthChanged)
		{
			_OnMonthChangedNoRender(timeData.dateNow.Month);
			_OnSeasonChangedNoRender();
		}
		if (hasYearChanged)
		{
			_OnYearChangedNoRender(timeData.dateNow.Year);
		}
	}

	public void TryRefreshEvent(bool isRender)
	{
		DateInfo dateNow = timeData.dateNow;
		if (dateNow.Hour == DolocAPI.GlobalParameter.EventRefreshClock)
		{
			_OnDailyRefresh(isRender);
			if (dateNow.WeekDay == WeekDay.MONDAY)
			{
				_OnWeeklyRefresh(isRender);
			}
			if (dateNow.Day == 1)
			{
				_OnMonthlyRefresh(isRender);
			}
			if (dateNow.Month == 1 && dateNow.Day == 1)
			{
				_OnYearlyRefresh(isRender);
			}
		}
	}

	public void InvokeDungeonHistory(Dungeon dungeon)
	{
		if (dungeon == null)
		{
			return;
		}
		UnityEngine.Debug.Log("时间系统: 演算地牢\"" + dungeon.ProtoName + "\"历史");
		foreach (DungeonRoom allRoom in dungeon.AllRooms)
		{
			InvokeRoomHistory(allRoom, DolocAPI.GlobalParameter.DungeonMaxHistoryThreshold);
		}
	}

	private void InvokeRoomHistory(Room room, int maxThreshold = -1)
	{
		DolocAPI.output($"演算房间\"{room.baseProto.name}\"历史:从{room.StopTime}到{timeData.totalSeconds},演算长度:{timeData.totalSeconds - room.StopTime}");
		WeatherHistoryRecord[] records = timeData.QueryWeatherHistory(room, maxThreshold);
		Vector2Int vector2Int = InvokeRoomHistory(room, records);
		DolocAPI.output($"实际演算时间:{vector2Int.y},实际演算天气数量:{vector2Int.x}", DolocColorHex.pink);
	}

	private Vector2Int InvokeRoomHistory(Room room, WeatherHistoryRecord[] records)
	{
		if (records.IsNullOrEmpty())
		{
			return Vector2Int.zero;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < records.Length; i++)
		{
			WeatherHistoryRecord weatherHistoryRecord = records[i];
			num++;
			room.SetWeatherInfo((WeatherType)weatherHistoryRecord.weatherType);
			for (int j = 0; j < weatherHistoryRecord.duration; j++)
			{
				num2++;
				room.UpdateNoRender();
			}
		}
		return new Vector2Int(num, num2);
	}

	private Vector2Int __CalcHistoryLength(WeatherHistoryRecord[] historyRecords)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < historyRecords.Length; i++)
		{
			WeatherHistoryRecord weatherHistoryRecord = historyRecords[i];
			num2++;
			num += weatherHistoryRecord.duration;
		}
		return new Vector2Int(num2, num);
	}

	private void _OnHourChanged(int hourNow)
	{
		if ((hourNow == 6 || hourNow == 18) && timeData.WeatherMap.TryGetValue(new Vector2Int(timeData.dateNow.Day, hourNow), out var value))
		{
			timeData.weather.SetCurrentWeather(value, timeData.totalSeconds, out var hasWeatherPropertyChanged);
			_OnWeatherChanged(value, hasWeatherPropertyChanged);
		}
		DolocAPI.Broadcast(GameEventType.HOUR_PASSED, new GameEventArgsInt(hourNow));
		farmData.MainFarm.UpdatePerHour(hourNow);
		if (currentRoom != null && currentRoom != farmData.MainFarm)
		{
			if (currentRoom.Type == RoomType.Dungeon)
			{
				CurrentDungeon.UpdatePerHour(hourNow);
			}
			else
			{
				currentRoom.UpdatePerHour(hourNow);
			}
		}
		cityData.npcManager.InvokeNpcSchedule("Hour Changed..");
		RenderWeatherAndDayNight(shouldTransit: true);
		farmData.missionManager.UpdatePerHour();
	}

	private void _OnHourChangedNoRender(int hourNow)
	{
		if ((hourNow == 6 || hourNow == 18) && timeData.WeatherMap.TryGetValue(new Vector2Int(timeData.dateNow.Day, hourNow), out var value))
		{
			timeData.weather.SetCurrentWeather(value, timeData.totalSeconds, out var hasWeatherPropertyChanged);
			_OnWeatherChangedNoRender(value, hasWeatherPropertyChanged);
		}
		DolocAPI.Broadcast(GameEventType.HOUR_PASSED, new GameEventArgsInt(hourNow));
		farmData.MainFarm.UpdatePerHourNoRender(hourNow);
		if (currentRoom != null && currentRoom != farmData.MainFarm)
		{
			if (currentRoom.Type == RoomType.Dungeon)
			{
				CurrentDungeon.UpdatePerHourNoRender(hourNow);
			}
			else
			{
				currentRoom.UpdatePerHourNoRender(hourNow);
			}
		}
		farmData.missionManager.UpdatePerHourNoRender();
		cityData.npcManager.InvokeNpcSchedule("Hour Changed..");
	}

	private void _OnDayChanged(int dayNow, int globalDay)
	{
		_OnDayChangedNoRender(dayNow, globalDay);
	}

	private void _OnDayChangedNoRender(int dayNow, int globalDay)
	{
		DolocAPI.BroadcastInt(GameEventType.DAY_PASSED, dayNow);
		DolocAPI.BroadcastInt(GameEventType.GLOBAL_DAY_PASSED, globalDay);
		timeData.SetFirstTimeWakeUpOnDayPassed();
		dataTracker.GdtDayMoney.Record();
		DolocAPI.uiSystem.basicTip.UpdateTimeInfo();
		cityData.treatyPortFactionManager.OnDayChanged();
	}

	private void _OnMonthChanged(int monthNow)
	{
		_OnMonthChangedNoRender(monthNow);
	}

	private void _OnMonthChangedNoRender(int monthNow)
	{
		DolocAPI.BroadcastInt(GameEventType.MONTH_PASSED, monthNow);
		DolocAPI.archiveHandle.cityData.boardMissionManager.RefreshBoardMissionPool();
	}

	private void _OnYearChanged(int yearNow)
	{
		_OnYearChangedNoRender(yearNow);
	}

	private void _OnYearChangedNoRender(int yearNow)
	{
		DolocAPI.BroadcastInt(GameEventType.YEAR_PASSED, yearNow);
	}

	private void _OnDailyRefresh(bool isRender)
	{
		DolocAPI.archiveHandle.UnlockSeedItemInStore();
		DolocAPI.archiveHandle.RefreshArchiveDialogue();
		DolocAPI.archiveHandle.RefreshAllStores();
		cityData.likingManager.DailyRefresh();
		farmData.missionManager.DailyRefresh();
		DolocAPI.archiveHandle.cityData.boardMissionManager.MissionTiming();
		DolocAPI.archiveHandle.cityData.treatyPortFactionManager.UpdateMaintainTime();
		cityData.globalInteractableObjectManager.DailyRefresh(isRender);
		farmData.agentData.DailyRefresh();
		cityData.calendarManager.DailyRefresh();
	}

	private void _OnWeeklyRefresh(bool isRender)
	{
		cityData.globalInteractableObjectManager.WeeklyRefresh(isRender);
		farmData.missionManager.WeeklyRefresh();
		cityData.likingManager.WeeklyRefresh();
	}

	private void _OnMonthlyRefresh(bool isRender)
	{
		MainFarm.OnMonthChange(isRender && MainFarm.isRenderNow);
		foreach (CityRoom value in cityData.cityRooms.Values)
		{
			value.OnMonthChange(isRender && value.isRenderNow);
		}
		SeasonInfo seasonProto = timeData.SeasonProto;
		DolocAPI.ShowMessageBoxRollCall(seasonProto.Title, seasonProto.SubTitle);
		float fadeDuration = DolocAPI.GlobalParameter.FadeDefaultDurationOnMonthChange / 2f;
		if (isRender)
		{
			DolocAPI.ppm.FadeIn(fadeDuration, delegate
			{
				DolocAPI.ppm.FadeOut(fadeDuration, null, shouldReset: false, "_OnMonthlyRefresh");
			});
		}
		farmData.missionManager.MonthlyRefresh();
		cityData.globalInteractableObjectManager.MonthlyRefresh(isRender);
		if (DolocAPI.archiveHandle.dungeonData.currentDungeon != null)
		{
			DolocAPI.archiveHandle.dungeonData.currentDungeon.OnMonthlyRefresh();
		}
		else
		{
			DolocAPI.CurrentRoom?.LoadSceneHandle()?.OnMonthlyRefresh();
		}
		cityData.calendarManager.ResetMemo();
	}

	private void _OnYearlyRefresh(bool isRender)
	{
		cityData.globalInteractableObjectManager.YearlyRefresh(isRender);
		farmData.missionManager.YearlyRefresh();
	}

	private void _OnExitRoom(Room room)
	{
		farmData.agentData.motorData.OnExitRoom(room);
	}

	private void _OnEnterRoom(Room room)
	{
		if (room.Type == RoomType.City)
		{
			InvokeRoomHistory(room);
		}
		farmData.agentData.motorData.OnEnterRoom(room);
		farmData.agentData.agentEquipment.AfterEnterRoom(room);
		DolocAPI.EnvCovariantController.OnEnterRoom(room, timeData.weather.WeatherType, DayProcess);
		DolocAPI.Sound.OnRoomEntered(room);
		DolocAPI.uiSystem.basicTip.OnEnterRoom(room);
	}

	private void _UpdatePerSec()
	{
		timeData.totalSeconds++;
		farmData.MainFarm.Update();
		if (currentRoom != null)
		{
			switch (currentRoom.Type)
			{
			case RoomType.Dungeon:
				CurrentDungeon.Update();
				break;
			case RoomType.City:
				currentRoom.Update();
				break;
			case RoomType.Farm:
				if (currentRoom.IsInHouse && currentRoom.RootRoom != farmData.MainFarm)
				{
					currentRoom.RootRoom.Update();
					currentRoom.RootRoom.StopTime = timeData.totalSeconds;
				}
				break;
			}
		}
		cityData.npcManager.UpdatePerSec();
		DolocAPI.uiSystem.basicTip.UpdatePerSecond();
		DolocAPI.uiSystem.sceneBoxGroup.TryUpdateInfo();
		cityData.globalInteractableObjectManager.UpdatePerSecond(isRender: true);
	}

	private void _UpdatePerSecNoRender()
	{
		timeData.totalSeconds++;
		farmData.MainFarm.UpdateNoRender();
		if (currentRoom != null)
		{
			if (currentRoom.Type == RoomType.Farm)
			{
				if (currentRoom.IsInHouse && currentRoom.RootRoom != farmData.MainFarm)
				{
					currentRoom.RootRoom.UpdateNoRender();
					currentRoom.RootRoom.StopTime = timeData.totalSeconds;
				}
			}
			else
			{
				currentRoom.UpdateNoRender();
			}
		}
		cityData.npcManager.UpdatePerSecNoRender();
		cityData.globalInteractableObjectManager.UpdatePerSecond(isRender: false);
	}

	private void _UpdatePerTU()
	{
		UpdateDate();
		timeData.CoolingDownWeatherRegulatorPerTU();
		timeData.weather.UpdatePerTU(shouldRender: true);
		WeatherInfo weatherInfo = currentRoom.GetWeatherInfo();
		farmData.agentData.UpdatePerTU(weatherInfo.Id);
		DolocAPI.uiSystem.basicTip.UpdatePerTU();
	}

	private void _UpdatePerTUNoRender()
	{
		UpdateDateNoRender();
		timeData.CoolingDownWeatherRegulatorPerTU();
		timeData.weather.UpdatePerTU();
		WeatherInfo weatherInfo = currentRoom.GetWeatherInfo();
		farmData.agentData.UpdatePerTUNoRender(weatherInfo.Id);
		DolocAPI.uiSystem.basicTip.UpdatePerTU();
	}

	private void _BeforeTimePass()
	{
		currentRoom?.BeforeTimePass();
	}

	private void _AfterTimePass()
	{
		RenderWeatherAndDayNight();
		currentRoom?.AfterTimePass();
		cityData.npcManager.AfterPassTime();
		DolocAPI.agent.WeakLightEnabled = ShouldAgentLightUp;
		DolocAPI.CurrentDrone?.AfterPassTime();
		DolocAPI.uiSystem.sceneBoxGroup.TryUpdateInfo();
	}

	private void _OnWeatherChanged(WeatherType weatherType, bool hasWeatherPropertyChanged)
	{
		_OnWeatherChangedNoRender(weatherType, hasWeatherPropertyChanged);
		RenderWeatherAndDayNight(shouldTransit: true);
		DolocAPI.uiSystem.basicTip.OnWeatherChanged(weatherType);
		DolocAPI.Sound.OnWeatherChange(weatherType);
	}

	private void _OnWeatherChangedNoRender(WeatherType type, bool hasWeatherPropertyChanged)
	{
		if (type == WeatherType.ACID_RAIN && !farmData.hasAcidRainCamed)
		{
			farmData.hasAcidRainCamed = true;
		}
		farmData.MainFarm.SetWeatherInfo(type);
		if (farmData.currentRoom != null && farmData.currentRoom.Type != RoomType.Farm)
		{
			farmData.currentRoom.SetWeatherInfo(type);
		}
		DolocAPI.BroadcastString(GameEventType.WEATHER_CHANGED, type.ToString().ToLower());
		cityData.npcManager.InvokeNpcSchedule("天气变化");
		if (hasWeatherPropertyChanged)
		{
			DolocAPI.Broadcast(GameEventType.WEATHER_PROPERTY_CHANGED, new GameEventArgsBool(type.IsMalignantWeather()));
		}
	}

	public void RefreshWeatherStatus()
	{
		_OnWeatherChangedNoRender(timeData.weather.WeatherType, hasWeatherPropertyChanged: false);
	}

	private void _OnSeasonChanged()
	{
		_OnSeasonChangedNoRender();
	}

	private void _OnSeasonChangedNoRender()
	{
		DolocAPI.Broadcast(GameEventType.SEASON_CHANGED);
	}
}
