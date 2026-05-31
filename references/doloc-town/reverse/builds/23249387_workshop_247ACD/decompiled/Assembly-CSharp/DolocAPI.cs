using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.Animal;
using DolocTown.Config.Archives;
using DolocTown.Config.Buff;
using DolocTown.Config.Building;
using DolocTown.Config.Dialogue;
using DolocTown.Config.Email;
using DolocTown.Config.EnvOptimizer;
using DolocTown.Config.Equipment;
using DolocTown.Config.Fishing;
using DolocTown.Config.Item;
using DolocTown.Config.Localization;
using DolocTown.Config.Mission;
using DolocTown.Config.Monster;
using DolocTown.Config.NPC;
using DolocTown.Config.Plant;
using DolocTown.Config.Platform;
using DolocTown.Config.Player;
using DolocTown.Config.Recipe;
using DolocTown.Config.Resource;
using DolocTown.Config.Room;
using DolocTown.Config.Settings;
using DolocTown.Config.TechTree;
using DolocTown.Config.Time;
using DolocTown.Config.UI;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using DolocTown.GameServiceLocator;
using DolocTown.NodeCanvas;
using DolocTown.Params;
using DolocTown.UI;
using RedSaw;
using RedSaw.CommandLineInterface;
using RedSaw.Web;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using XLua;
using Yarn.Markup;

public static class DolocAPI
{
	private static MessageSystem<UserSettingType> messageSystem;

	public static DataPersistenceManager dataPersistenceManager;

	public static bool DisableOverlayUi;

	public static readonly UnityEvent<bool> OnAfterLoadArchiveData = new UnityEvent<bool>();

	private static readonly Regex TagRegex = new Regex("<([^>]+)>", RegexOptions.Compiled);

	public static InteractableObject CurrentInteractableObject;

	[CommandProperty("selected_animal")]
	public static Animal CurrentAnimal;

	private static ItemBorder _itemBorder;

	private static HoverBoxGroup _hoverBoxGroup;

	private static readonly Regex ActionTagRegex = new Regex("(?<=(<action=))[.\\s\\S]*?(?=(/>))", RegexOptions.Multiline | RegexOptions.Singleline);

	private static SwingAnimationCache _swingAnimationCache;

	public static GameManager gameManager { get; set; }

	public static bool IsGameInitialized
	{
		get
		{
			if (gameManager == null)
			{
				return false;
			}
			return gameManager.IsGameInitialized;
		}
	}

	public static bool IsNewVersion
	{
		get
		{
			if (archiveHandle == null)
			{
				return false;
			}
			return archiveHandle.baseDataOnLoad.version != Application.version;
		}
	}

	public static bool IsDataLoaded => dataPersistenceManager.IsDataLoaded;

	public static bool UseMods
	{
		get
		{
			if (IsGameInitialized)
			{
				return modManager.HasEnabledMods;
			}
			return false;
		}
	}

	public static DevHelper devHelper { get; private set; }

	public static GameLoadingTip gameLoadingTip { get; private set; }

	public static LuaEnv luaEnv { get; private set; }

	public static RoomGizmos RoomGizmos { get; private set; }

	public static WebOperationHandle WebOperationHandle { get; private set; }

	public static DolocBundleManager assets { get; private set; }

	public static DolocUserInput UserInput { get; private set; }

	public static GameLoop gameLoop { get; private set; }

	public static ScreenManager screenManager { get; private set; }

	public static CursorManager cursorManager { get; private set; }

	public static TimeScaleManager timeScaleManager { get; private set; }

	public static GameStateMachine userInput { get; private set; }

	public static SceneManager sceneManager { get; private set; }

	public static PPManager ppm { get; private set; }

	public static Vector2Int worldResolution => screenManager.worldResolution;

	public static Vector2 screenSize => screenManager.screenSize;

	public static FontManager FontManager { get; private set; } = new FontManager();


	public static GlobalParameterInfo GlobalParameter => DolocConfig.Tables.TbGlobalParameter.Data;

	public static IGameConfigProvider gameConfig { get; private set; }

	public static IEffectsConfigProvider eftConfig { get; private set; }

	public static CameraController cameraController { get; private set; }

	public static Camera mainCamera { get; private set; }

	public static GameProcessSystem GameProcessSystem { get; private set; }

	public static BackgroundRenderer envBackgroundEx { get; private set; }

	public static EnvCovariantController EnvCovariantController { get; private set; }

	public static DolocUiSystem uiSystem { get; private set; }

	public static DolocGameUiStateManager gameUiStates { get; private set; }

	public static BodyController agent { get; private set; }

	public static AgentControllerState AgentController => gameStateManager?.agentController;

	public static MotorController Motor { get; private set; }

	public static AbilitySystem AbilitySystem { get; private set; }

	public static GameStateManager gameStateManager { get; private set; }

	public static GameEntitySystem<GEMGameObjectAttribute> EntitySystem { get; private set; }

	[CommandProperty("achievement")]
	public static AchievementSystem AchievementSystem => archiveHandle.extraData.achievementSystem;

	public static IEffectsProvider effectProvider { get; private set; }

	public static IUIEffectsProvider uiEffectsProvider { get; private set; }

	public static WwiseSoundManager Sound { get; private set; }

	public static CharacterRenderer AgentRenderer => gameStateManager.agentController.CharacterRenderer;

	public static DolocBuilder dolocBuilder { get; private set; }

	public static SceneRendererFarm farmRenderer { get; private set; }

	public static EmotionManager emotionSystem { get; private set; }

	public static RoutineManager routineManager { get; private set; }

	public static DroneRenderer droneRenderer { get; private set; }

	public static BattleSystem battleSystem => gameStateManager.normalGameState.battleSystem;

	[CommandProperty("archive")]
	public static ArchiveDataHandle archiveHandle => dataPersistenceManager.gameData;

	public static UserSettings userSettings => dataPersistenceManager.userSettings;

	public static ModManager modManager => dataPersistenceManager.modManager;

	public static string CurrentL10nId => DolocConfig.Tables.CurrentL10nId;

	public static LocalizationInfo CurrentL10nInfo => DolocConfig.Tables.TbLocalization.GetOrDefault(CurrentL10nId);

	public static Transform CurrentDialogueHostTransform
	{
		get
		{
			if (userInput.CurrentState is DialogueState dialogueState)
			{
				return dialogueState.interactedNpc.EntityTransform;
			}
			return null;
		}
	}

	public static bool IsNormalState => userInput.CheckState<NormalGameState>();

	[CommandProperty("current_room")]
	public static Room CurrentRoom => archiveHandle?.currentRoom;

	public static Item SelectedItem => uiSystem.inventoryQuick.GetSelectedItem();

	public static bool HasBufferItem => archiveHandle.InventorySystem.buffer.IsFull;

	[CommandProperty("selected_equipment")]
	public static Equipment SelectedEquipment => gameStateManager.normalGameState.AgentController.RoomScanner.CurrentEquipment;

	[CommandProperty("selected_building")]
	public static Building SelectedBuilding => gameStateManager.normalGameState.AgentController.BuildingScanner.CurrentBuilding;

	public static int SelectedItemIndex => uiSystem.inventoryQuick.selectedIndex;

	public static DolocObject Agent
	{
		get
		{
			if (!IsAgentRiding)
			{
				return agent;
			}
			return Motor;
		}
	}

	public static Transform AgentTransform
	{
		get
		{
			if (!IsAgentRiding)
			{
				return agent.transform;
			}
			return Motor.transform;
		}
	}

	public static AgentEquipmentAbility AgentEquipmentParams => archiveHandle.farmData.agentData.agentEquipment.EquipmentAbility;

	public static AgentEquipmentManager AgentEquipmentManager => archiveHandle.farmData.agentData.agentEquipment;

	public static Vector3 AgentPosition
	{
		get
		{
			return Agent.transform.position;
		}
		set
		{
			Agent.transform.position = new Vector3(value.x, value.y, agent.DefaultZ);
		}
	}

	public static float AgentZ
	{
		get
		{
			return Agent.transform.position.z;
		}
		set
		{
			Agent.transform.position = new Vector3(AgentPosition.x, AgentPosition.y, value);
		}
	}

	public static Vector2Int AgentRoomCellPosition => archiveHandle?.currentRoom?.Geometry.CalcCellPosition(AgentPosition) ?? Vector2Int.zero;

	public static Vector2Int AgentRealRoomCellPosition
	{
		get
		{
			Vector3 agentPosition = AgentPosition;
			agentPosition.y += 0.1f;
			return archiveHandle.currentRoom.Geometry.CalcMinCellPosition(agentPosition);
		}
	}

	public static Vector2 AgentWorldCellPosition => archiveHandle?.currentRoom?.Geometry.CalcWorldPosition(AgentRoomCellPosition) ?? Vector2.zero;

	public static bool AgentEnabled
	{
		set
		{
			Agent.SetVisible(value);
		}
	}

	public static bool IsAgentRiding
	{
		get
		{
			if (IsDataLoaded)
			{
				return gameStateManager.agentController.IsRidingNow;
			}
			return false;
		}
	}

	public static Drone CurrentDrone => gameStateManager.agentController.droneController.CurrentDrone;

	public static Transform DroneFollowTarget
	{
		get
		{
			if (!IsAgentRiding)
			{
				return agent.DroneFollower;
			}
			return Motor.DroneFollowPoint;
		}
	}

	public static bool AgentFaceRight
	{
		get
		{
			return agent.IsFaceRight;
		}
		set
		{
			agent.IsFaceRight = value;
		}
	}

	public static bool IsPlayerInFarmScene
	{
		get
		{
			if (archiveHandle.currentRoom == null)
			{
				return false;
			}
			return archiveHandle.currentRoom.Type == RoomType.Farm;
		}
	}

	public static bool IsAgentInWater => InteractiveWater.IsInWater;

	public static InteractiveWater CurrentWater => InteractiveWater.CurrentWater;

	public static bool DisableDisposeItem => archiveHandle.farmData.agentData.disableDisposeItem;

	public static BuffManager BuffManager => archiveHandle.farmData.agentData.buffManager;

	public static bool IsCurrentStateSupportCutscenes
	{
		get
		{
			if (IsDataLoaded && IsCurrentStateSupportInteract)
			{
				return userInput.CurrentState.SupportCutscenes;
			}
			return false;
		}
	}

	public static bool IsCurrentStateSupportFestival
	{
		get
		{
			if (IsDataLoaded)
			{
				return userInput.CurrentState.SupportFestival;
			}
			return false;
		}
	}

	public static bool IsCurrentStateSupportInteract => agent.IsCurrentStateSupportInteract;

	private static ItemBorder itemBorder
	{
		get
		{
			if ((object)_itemBorder == null)
			{
				_itemBorder = uiSystem.GetEntity<ItemBorder>();
			}
			return _itemBorder;
		}
	}

	public static HoverBoxGroup HoverBoxGroup
	{
		get
		{
			if ((object)_hoverBoxGroup == null)
			{
				_hoverBoxGroup = uiSystem.GetEntity<HoverBoxGroup>();
			}
			return _hoverBoxGroup;
		}
	}

	[CommandProperty("main_farm_electric_system")]
	private static ElectricSystem MainFarmElectricSystem => archiveHandle.MainFarm.DM_electric;

	[CommandProperty("player")]
	private static BodyController Player => agent;

	public static void InitDevelopmentHelper()
	{
		Transform transform = UnityEngine.Object.FindObjectOfType<DolocUiSystem>().transform;
		GameObject gameObject = Resources.Load<GameObject>("GameConsole/devHelper");
		if (gameObject != null)
		{
			devHelper = UnityEngine.Object.Instantiate(gameObject, transform).GetComponent<DevHelper>();
		}
		else
		{
			devHelper = UnityEngine.Object.FindObjectOfType<DevHelper>();
		}
		devHelper.Init();
		luaEnv = new LuaEnv();
		luaEnv.AddLoader(DolocLuaLoader.LoadLua);
		GameObject gameObject2 = Resources.Load<GameObject>("GameConsole/game_loading_tip");
		if (gameObject2 != null)
		{
			gameLoadingTip = UnityEngine.Object.Instantiate(gameObject2, transform).GetComponent<GameLoadingTip>();
			if (gameLoadingTip != null)
			{
				gameLoadingTip.Init();
			}
		}
		RoomGizmos = gameManager.GetComponentInChildren<RoomGizmos>();
		GameObject gameObject3 = Resources.Load<GameObject>("web_operation_handle");
		if (gameObject3 != null)
		{
			WebOperationHandle = UnityEngine.Object.Instantiate(gameObject3, transform).GetComponent<WebOperationHandle>();
			WebOperationHandle.gameObject.SetActive(value: true);
		}
	}

	public static void __LoadStaticAssets(Action callback)
	{
		Sound.LoadSoundBank(SoundBanks.SFX);
		Sound.LoadSoundBank(SoundBanks.MUSIC);
		Stopwatch sw = new Stopwatch();
		sw.Start();
		UnityEngine.Debug.Log("准备加载游戏静态资源..");
		assets = gameManager.GetComponent<DolocBundleManager>();
		assets.LoadAsync(delegate(bool succeed)
		{
			sw.Stop();
			output($"所有资源加载完毕, 耗时:{sw.Elapsed.TotalMilliseconds}ms", DolocColor.yellow);
			if (succeed)
			{
				outputSuccess("所有资源加载成功");
				if (gameLoadingTip != null)
				{
					UnityEngine.Object.Destroy(gameLoadingTip.gameObject);
				}
				callback();
			}
			else
			{
				outputError("资源加载失败,游戏将不会初始化");
				UnityEngine.Debug.LogError("由于资源加载失败,游戏启动失败!");
			}
		});
	}

	public static void __InitGlobalSystems()
	{
		UnityEngine.Debug.Log("正在初始化多洛可小镇全局游戏系统");
		UserInput = new DolocUserInput();
		dataPersistenceManager = new DataPersistenceManager();
		messageSystem = new MessageSystem<UserSettingType>();
		sceneManager = new SceneManager();
		userInput = new GameStateMachine();
		gameLoop = gameManager.GetComponent<GameLoop>();
		gameLoop.Init(userInput);
		ppm = gameManager.GetComponentInChildren<PPManager>(includeInactive: true);
		ppm.Init();
		if (!ppm.gameObject.activeSelf)
		{
			ppm.gameObject.SetActive(value: true);
		}
		Sound = gameManager.GetComponentInChildren<WwiseSoundManager>(includeInactive: true);
		Sound.Init();
	}

	public static void __InstallStateSwitchPlugins()
	{
		outputSuccess("安装所有的状态转换插件");
		if (gameManager.shouldCheckStateSwitch)
		{
			userInput.InstallPlugin(new GameStatePluginCurrent());
		}
		userInput.InstallPlugin(new GameStatePluginUiBackground());
		userInput.InstallPlugin(new GameStatePluginPauseTip());
		userInput.InstallPlugin(new GameStatePluginStopAgentWhileModalState());
		userInput.InstallPlugin(new GameStatePluginBodyPause());
	}

	public static void __InstallConfigs()
	{
		gameConfig = assets.gameConfig;
		eftConfig = assets.effectsConfig;
	}

	public static void __InitDolocBaseGameSystems(GameManager manager)
	{
		UnityEngine.Debug.Log("初始化全局游戏系统");
		mainCamera = Camera.main;
		__InitWeatherRenderSystem(manager);
		uiSystem = UnityEngine.Object.FindObjectOfType<DolocUiSystem>();
		uiSystem.Init();
		screenManager = new ScreenManager(uiSystem.rectTransform);
		cursorManager = UnityEngine.Object.FindObjectOfType<CursorManager>();
		cursorManager.Init();
		timeScaleManager = new TimeScaleManager();
		cameraController = manager.GetComponentInChildren<CameraController>(includeInactive: true);
		envBackgroundEx = manager.GetComponentInChildren<BackgroundRenderer>(includeInactive: true);
		envBackgroundEx.Init();
		GameProcessSystem = new GameProcessSystem();
		AbilitySystem = new AbilitySystem(assets.motionParam);
		agent = manager.GetComponentInChildren<BodyController>(includeInactive: true);
		agent.Init(UserInput);
		Motor = manager.GetComponentInChildren<MotorController>(includeInactive: true);
		Motor.Init();
		gameUiStates = new DolocGameUiStateManager();
		effectProvider = new EffectsManager(manager.effectsContainer);
		uiEffectsProvider = new UiEffectsManager(uiSystem.transform);
		gameStateManager = new GameStateManager(userInput);
		EntitySystem = new GameEntitySystem<GEMGameObjectAttribute>(manager.globalContainer);
	}

	private static void __InitWeatherRenderSystem(GameManager manager)
	{
		manager.GetComponentInChildren<CameraController>(includeInactive: true);
		GameObject asset = GetAsset<GameObject>(DolocGameAssets.GAME_ENTITY_COVARIANT_CONTROLLER);
		DolocAssert.IsTrue(asset != null);
		Transform transform = Camera.main.transform;
		GameObject gameObject = UnityEngine.Object.Instantiate(asset, transform);
		Sun componentInChildren = gameObject.GetComponentInChildren<Sun>(includeInactive: true);
		Volume componentInChildren2 = transform.GetComponentInChildren<Volume>(includeInactive: true);
		DayNightRendererController dayNightRendererController = new DayNightRendererController(componentInChildren, componentInChildren2);
		WeatherRendererController weatherRendererController = new WeatherRendererController();
		WeatherRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<WeatherRenderer>(includeInactive: true);
		foreach (WeatherRenderer weatherRenderer in componentsInChildren)
		{
			weatherRenderer.Init();
			weatherRendererController.AddWeatherRenderer(weatherRenderer);
		}
		EnvCovariantController = new EnvCovariantController(gameObject, dayNightRendererController, weatherRendererController);
	}

	public static bool __InitFarmSystem(GameManager manager)
	{
		dolocBuilder = manager.GetComponentInChildren<DolocBuilder>(includeInactive: true);
		dolocBuilder.Init();
		farmRenderer = new SceneRendererFarm(manager.farmContaier);
		return true;
	}

	public static void __InitCitySystem(GameManager gameManager)
	{
		emotionSystem = new EmotionManager(gameManager.effectsContainer.Find("emos"));
		routineManager = new RoutineManager();
		routineManager.StartAll(assets.routineGraphs.totalValues);
	}

	public static void __InitDungeonSystem(GameManager manager)
	{
		__InitPlayerDrone();
	}

	private static void __InitPlayerDrone()
	{
		GameObject asset = GetAsset<GameObject>(DolocGameAssets.GAME_ENTITY_DRONE);
		if (asset == null)
		{
			UnityEngine.Debug.LogError("初始化玩家无人机:无法找到无人机的预制体");
			return;
		}
		droneRenderer = UnityEngine.Object.Instantiate(asset).GetComponent<DroneRenderer>();
		droneRenderer.Init(agent.DroneFollower);
		droneRenderer.SetVisible(value: false);
	}

	public static bool IsFirstPlayGame(bool shouldMark)
	{
		return dataPersistenceManager.IsFirstPlayGame(shouldMark);
	}

	public static void NewGame(int index)
	{
		sceneManager.UnloadSceneAsync("sys_home");
		dataPersistenceManager.NewGame(index);
		gameManager.gameInitConfig.InitNewGame(archiveHandle);
		AfterLoadArchiveData(isNewGame: true);
		StartGame();
	}

	private static void StartGame()
	{
		SetPPM_FlowPoints(value: false);
		gameLoop.IsGlobalPaused = true;
		if (gameManager.gameInitConfig.skipTraining || !assets.dungeons.GetDungeonProto(GlobalParameter.TrainingDungeonName, out var dungeon))
		{
			FinishTrainingDungeon();
		}
		else
		{
			EnterDungeon(dungeon.name);
		}
	}

	public static void FinishTrainingDungeon(bool shouldFade = false)
	{
		DisableOverlayUi = true;
		UnityEngine.Debug.Log("结束教学关卡");
		gameLoop.IsGlobalPaused = false;
		archiveHandle.dungeonData.trainFlag = false;
		if (shouldFade)
		{
			agent.Faint(FaintReason.None, shouldMarkFaintState: false);
			ResetPlayerValues();
			userInput.ClearState(gameStateManager.normalGameState);
			SetResidentUiVisible(showBasicTip: false, showQuickInventory: false);
			ClearSceneResidentTips();
			Delay(1.5f, delegate
			{
				ppm.FadeIn(2f, FinishTrainingDungeonCallback);
			});
			DisableOverlayUi = false;
		}
		else
		{
			if (!gameManager.gameInitConfig.skipInitAnim)
			{
				ppm.FadeIn(0f);
			}
			FinishTrainingDungeonCallback();
		}
	}

	private static void FinishTrainingDungeonCallback()
	{
		SetPPM_FlowPoints(value: false);
		Broadcast(GameEventType.GAME_START);
		GameInitConfig gameInitConfig = gameManager.gameInitConfig;
		if (gameInitConfig.skipInitAnim)
		{
			DoTransport(gameInitConfig.initMarkPoint);
			DisableOverlayUi = false;
			return;
		}
		MarkPointInfo orDefault = DolocConfig.Tables.TbMarkPoint.GetOrDefault(gameInitConfig.initMarkPoint);
		QueryRoom(orDefault.RoomId, out var room);
		StartDialogueNode(gameInitConfig.openingAnimationNode);
		gameStateManager.TransitScene(room, orDefault.Position, delegate
		{
			room.OnEnterRoom();
			Delay(3f, delegate
			{
				DisableOverlayUi = false;
			});
		}, shouldFade: false);
	}

	public static bool LoadGame(int index)
	{
		if (!dataPersistenceManager.LoadGame(index))
		{
			return false;
		}
		sceneManager.UnloadSceneAsync("sys_home");
		AfterLoadArchiveData(isNewGame: false);
		return true;
	}

	public static bool DuplicateGame(int index, out int targetIndex)
	{
		return dataPersistenceManager.DuplicateGame(index, out targetIndex);
	}

	private static void ResetArchiveState()
	{
		UnloadDrone();
		uiSystem.basicTip.ResetUIState();
		AbilitySystem.Recalculate();
		ClearSceneOperationTips();
	}

	private static void AfterLoadArchiveData(bool isNewGame)
	{
		ppm.AfterLoadArchiveData();
		cameraController.RefreshResolution();
		userSettings.UpdateCachedData();
		archiveHandle.RenderWeatherAndDayNight();
		OnAfterLoadArchiveData.Invoke(isNewGame);
		uiSystem.basicTip.ResetBasicTipState();
		uiSystem.inventoryQuick.Clear();
		uiSystem.inventoryQuick.BindQuickInventory(archiveHandle.InventorySystem.inventory);
		gameLoop.IsGlobalPaused = false;
		foreach (NpcInfo data in DolocConfig.Tables.TbNpc.DataList)
		{
			if (!archiveHandle.cityData.dialogueManager.CheckDialogueData(data.Id) && data.ShouldPreload && !data.InitialDialogueEntry.IsNullOrEmpty())
			{
				SetDialogueEntrance(data.InitialDialogueEntry, data.Id);
			}
		}
		if (isNewGame)
		{
			gameManager.gameInitConfig.InitCommandScripts();
			EquipHat(GlobalParameter.InitHat, out var _);
			foreach (Npc allNpc in archiveHandle.cityData.npcManager.AllNpcs)
			{
				AddDialogueNode(allNpc.proto.GiftDialogue);
			}
			archiveHandle.extraData.achievementSystem.Initialize();
		}
		else
		{
			VersionPatch[] array = VersionPatcher.LoadAllVersionPatchesBeyond(archiveHandle.baseDataOnLoad.version);
			for (int i = 0; i < array.Length; i++)
			{
				array[i].ApplyPatches(UnityEngine.Debug.Log, UnityEngine.Debug.LogException);
			}
			gameManager.gameInitConfig.RunVersionAppendCommandScripts(archiveHandle.baseDataOnLoad.version);
		}
		gameManager.gameInitConfig.RunPatchCommandScripts();
		gameManager.gameInitConfig.ExecuteDLCScripts();
	}

	public static bool SaveGame(int index)
	{
		ShowMessageBoxInfo(DolocConfig.StaticTexts.GameDataSaving);
		if (!dataPersistenceManager.SaveGame(index))
		{
			ShowMessageBoxErr(DolocConfig.StaticTexts.GameDataSaveFail);
			return false;
		}
		ShowMessageBoxInfo(DolocConfig.StaticTexts.GameDataSaveSuccessful);
		return true;
	}

	public static void DeleteGame(int index)
	{
		dataPersistenceManager.DeleteGame(index);
	}

	public static void ReturnHome(bool quickLoaded = false)
	{
		RevertTimeScale();
		Sound.StopAll();
		Sound.PlayBgm(GlobalParameter.HomepageBgmEvent);
		PlatformTileGeneratorUtils.ClearCache();
		RemoveUiState<CollectionBookUiState>();
		RemoveUiState<TechTreeUiState>();
		AgentEnabled = false;
		EntitySystem.Clear(force: true);
		uiSystem.ClearPool();
		farmRenderer.Clear();
		sceneManager.UnloadAll();
		EnvCovariantController.Clear();
		envBackgroundEx.SetVisible(value: false);
		cameraController.setEnabled(value: false);
		cameraController.position2d = new Vector2(30f, 16.875f);
		ppm.ReturnHome();
		GameProcessSystem.Clear();
		ResetArchiveState();
		dataPersistenceManager.UnloadGame();
		userInput.ClearState(gameStateManager.normalGameState);
		if (quickLoaded)
		{
			if (!gameManager.gameInitConfig.useDefaultArchive || !LoadGame(0))
			{
				NewGame(0);
			}
		}
		else
		{
			sceneManager.LoadSceneAsync("sys_home", delegate
			{
				EnterUI<HomePageUiState>();
			});
		}
	}

	public static BaseArchiveData GetArchiveInfo(int index)
	{
		return dataPersistenceManager.GetArchiveInfo(index);
	}

	public static BaseArchiveData[] GetAllArchiveInfos()
	{
		return dataPersistenceManager.GetAllArchiveInfo();
	}

	public static bool Has087DemoData()
	{
		return dataPersistenceManager.HasDataStartWith("doloc-archive-");
	}

	public static bool LoadUserSettings()
	{
		return dataPersistenceManager.LoadUserSettings();
	}

	public static bool SaveUserSettings(UserSettings settings)
	{
		return dataPersistenceManager.SaveUserSettings(settings);
	}

	public static string GetCurrentArchiveDataAsString()
	{
		return dataPersistenceManager.GetCurrentArchiveDataAsString();
	}

	public static bool SaveUserSettings()
	{
		return SaveUserSettings(dataPersistenceManager.userSettings);
	}

	public static void ResetUserSettings()
	{
		dataPersistenceManager.ResetUserSettings();
	}

	public static void RevertUserSettings(UserSettings settings)
	{
		dataPersistenceManager.RevertUserSettings(settings);
	}

	public static void RevertUserSettingsToDefaultByGroup(string groupId)
	{
		dataPersistenceManager.RevertUserSettingsToDefaultByGroup(groupId);
	}

	public static bool CheckAsset<T>(string address) where T : UnityEngine.Object
	{
		return assets.cache.CheckAsset<T>(address);
	}

	public static T GetAsset<T>(string address, bool useLog = true) where T : UnityEngine.Object
	{
		return assets.cache.GetAsset<T>(address, useLog);
	}

	public static bool TryGetAsset<T>(string address, out T asset) where T : UnityEngine.Object
	{
		asset = assets.cache.GetAsset<T>(address, useLog: false);
		return asset != null;
	}

	public static T GetAsset<T>(DolocGameAssets address) where T : UnityEngine.Object
	{
		return GetAsset<T>(address.ToString().ToLower());
	}

	public static Sprite LoadSprite(string imageName)
	{
		return GetAsset<Sprite>(imageName);
	}

	public static int GetEquipmentLimitation(string name)
	{
		return -1;
	}

	public static bool QueryTemplateRoom(string name, out RoomProto proto)
	{
		return assets.rooms.QueryData(name, out proto);
	}

	public static Reward CreateReward(RewardProto proto)
	{
		return Reward.CreateReward(proto);
	}

	public static IEnumerable<Reward> CreateRewards(IEnumerable<RewardProto> protos)
	{
		foreach (RewardProto proto in protos)
		{
			yield return Reward.CreateReward(proto);
		}
	}

	public static bool CashReward(RewardProto proto)
	{
		return Reward.CreateReward(proto).CashReward();
	}

	public static bool IsInSameDungeon(Room L, Room R)
	{
		if (L == null || R == null)
		{
			return false;
		}
		if (L.Type != RoomType.Dungeon || R.Type != RoomType.Dungeon)
		{
			return false;
		}
		return L.SceneShortName == R.SceneShortName;
	}

	public static bool QueryItemProto(string name, out ItemInfo proto)
	{
		proto = DolocConfig.Tables.TbItem.GetOrDefault(name ?? "");
		return proto != null;
	}

	public static string GetItemTitle(string itemName)
	{
		Item item = GenerateItem(itemName);
		if (item == null)
		{
			return string.Empty;
		}
		return item.title;
	}

	public static float GetItemSortingOrder(string itemName)
	{
		ItemOrderInfo orDefault = DolocConfig.Tables.TbItemOrder.GetOrDefault(itemName ?? "");
		if (orDefault == null)
		{
			return 2.1474836E+09f;
		}
		if (orDefault.CustomOrder == 0f)
		{
			return orDefault.SortingOrder;
		}
		return orDefault.CustomOrder;
	}

	public static string GetItemDescription(string itemName)
	{
		Item item = GenerateItem(itemName);
		if (item == null)
		{
			return string.Empty;
		}
		return item.description;
	}

	public static Sprite GetItemSprite(string itemName)
	{
		if (!QueryItemProto(itemName, out var proto))
		{
			return null;
		}
		return proto.UiSpriteAsset.Asset;
	}

	public static bool IsItemSalable(Item item)
	{
		return item?.salable ?? false;
	}

	public static int GetItemSellingPrice(Item item)
	{
		return Mathf.Max(0, item.sellingPrice);
	}

	public static int GetItemSellingPrice(string itemName)
	{
		if (!QueryItemProto(itemName, out var proto))
		{
			return 0;
		}
		if (proto.SellingPrice >= 0)
		{
			return proto.SellingPrice;
		}
		Item item = GenerateItem(itemName);
		return Mathf.Max(0, item.sellingPrice);
	}

	public static int GetItemBuyingPrice(string itemName)
	{
		if (!QueryItemProto(itemName, out var proto))
		{
			return 0;
		}
		return proto.BuyingPrice;
	}

	public static bool IsItemDisposable(Item item)
	{
		if (DisableDisposeItem)
		{
			return false;
		}
		return item?.disposable ?? false;
	}

	public static bool IsItemCanPutInToContainer(Item item)
	{
		return item == null || item.canPutInToContainer;
	}

	public static bool QueryItemSpawnLut(string id, out ItemSpawnInfo spawnLut)
	{
		spawnLut = DolocConfig.Tables.TbItemSpawn.GetOrDefault(id ?? "");
		return spawnLut != null;
	}

	public static bool QueryEquipment(string name, out EquipmentInfo proto)
	{
		proto = null;
		if (name.IsNullOrEmpty())
		{
			return false;
		}
		return DolocConfig.Tables.TbEquipment.DataMap.TryGetValue(name, out proto);
	}

	public static string GetEquipmentTitle(string name)
	{
		if (!QueryEquipment(name, out var proto))
		{
			return name;
		}
		return proto.Title;
	}

	public static bool QueryBuilding(string name, out BuildingInfo proto)
	{
		proto = DolocConfig.Tables.TbBuilding.GetOrDefault(name ?? "");
		return proto != null;
	}

	public static string GetBuildingTitle(string buildingName)
	{
		if (!QueryBuilding(buildingName, out var proto))
		{
			return buildingName;
		}
		return proto.Title;
	}

	public static bool IsVisitedNpcName(string npcName)
	{
		if (string.IsNullOrEmpty(npcName))
		{
			return false;
		}
		if (QueryNpc(npcName, out var npc))
		{
			return npc.hasKnownName;
		}
		return false;
	}

	public static string GetNpcTitle(string npcName, bool ignoreUnknown = false)
	{
		if (string.IsNullOrEmpty(npcName) || !QueryNpc(npcName, out var npc))
		{
			return string.Empty;
		}
		return npc.GetCurrentTitle(ignoreUnknown);
	}

	public static bool QueryRecipeProto(string recipeName, out RecipeInfo proto)
	{
		proto = DolocConfig.Tables.TbRecipe.GetOrDefault(recipeName);
		return proto != null;
	}

	public static bool QueryRecipe(string name, out Recipe recipe)
	{
		recipe = new Recipe(name);
		if (recipe.isValid)
		{
			return true;
		}
		recipe = null;
		return false;
	}

	public static bool QueryResourceProto(string name, out ResourceInfo proto)
	{
		proto = DolocConfig.Tables.TbResource.GetOrDefault(name ?? "");
		return proto != null;
	}

	public static bool QueryVegetationProto(string name, out VegetationInfo proto)
	{
		proto = DolocConfig.Tables.TbVegetation.GetOrDefault(name ?? "");
		return proto != null;
	}

	public static bool QueryPlatformProto(string name, out PlatformInfo proto)
	{
		proto = null;
		if (!name.IsNullOrEmpty())
		{
			return DolocConfig.Tables.TbPlatform.DataMap.TryGetValue(name, out proto);
		}
		return false;
	}

	public static string GetTechTreeTitle(string treeName)
	{
		return DolocConfig.Tables.TbTechTree.GetById(treeName)?.Title;
	}

	public static bool GetActionKeyIconGroup(string actionName, out ActionIconGroup iconGroup)
	{
		return GetActionKeyIconGroup(UserInput.DeviceType, actionName, out iconGroup);
	}

	public static bool GetActionKeyIconGroup(DolocInputDeviceType deviceType, string actionName, out ActionIconGroup iconGroup)
	{
		iconGroup = default(ActionIconGroup);
		if (!DolocConfig.Tables.TbGameKeyAction.DataMap.TryGetValue(actionName, out var value))
		{
			return false;
		}
		return value.GetIconGroup(deviceType, out iconGroup);
	}

	public static bool GetAllActionKeyIconGroup(DolocInputDeviceType deviceType, string actionName, out List<ActionIconGroup> iconGroups)
	{
		if (!DolocConfig.Tables.TbGameKeyAction.DataMap.TryGetValue(actionName, out var value))
		{
			iconGroups = new List<ActionIconGroup>();
			return false;
		}
		return value.GetAllIconsGroup(deviceType, out iconGroups);
	}

	public static bool QueryMonsterDocument(string monsterId, out MonsterDocumentInfo document)
	{
		document = null;
		if (monsterId.IsNullOrEmpty())
		{
			return false;
		}
		return DolocConfig.Tables.TbMonsterDocument.DataMap.TryGetValue(monsterId, out document);
	}

	public static bool QuerySeedProto(string name, out SeedInfo proto)
	{
		proto = null;
		if (name.IsNullOrEmpty())
		{
			return false;
		}
		return DolocConfig.Tables.TbSeed.DataMap.TryGetValue(name, out proto);
	}

	public static bool QuerySceneInfo(string name, out DolocTown.Config.Room.SceneInfo proto)
	{
		proto = null;
		if (name.IsNullOrEmpty())
		{
			return false;
		}
		return DolocConfig.Tables.TbScene.DataMap.TryGetValue(name, out proto);
	}

	public static int GetNpcLikingLvLimit(string npcName)
	{
		return DolocConfig.Tables.TbNpc.GetOrDefault(npcName)?.LikingLvLimit ?? 0;
	}

	public static bool QueryAnimalDocument(string animalName, out AnimalDocumentInfo document)
	{
		document = null;
		if (animalName.IsNullOrEmpty())
		{
			return false;
		}
		return DolocConfig.Tables.TbAnimalDocument.DataMap.TryGetValue(animalName, out document);
	}

	public static bool QueryFishDocument(string fishName, out FishDocumentInfo document)
	{
		document = null;
		if (fishName.IsNullOrEmpty())
		{
			return false;
		}
		return DolocConfig.Tables.TbFishDocument.DataMap.TryGetValue(fishName, out document);
	}

	public static IEnumerable<string> GetAnimalAllProduce(string animalName)
	{
		if (!DolocConfig.Tables.TbAnimal.DataMap.TryGetValue(animalName, out var value))
		{
			yield break;
		}
		foreach (ItemSpawnData spawnData in value.ProduceSpawnEntry.SpawnLut_Ref.SpawnDatas)
		{
			yield return spawnData.ItemName;
		}
		if (!DolocConfig.Tables.TbHusbandry.DataMap.TryGetValue(animalName, out var value2))
		{
			yield break;
		}
		foreach (AnimalHusbandryData husbandryData in value2.HusbandryDatas)
		{
			if (QueryItemProto(husbandryData.Output, out var proto))
			{
				yield return proto.Id;
			}
		}
	}

	public static bool QueryResourceDocument(string resourceName, out ResourceDocumentInfo document)
	{
		document = null;
		if (resourceName.IsNullOrEmpty())
		{
			return false;
		}
		return DolocConfig.Tables.TbResourceDocument.DataMap.TryGetValue(resourceName, out document);
	}

	public static void output(string cnt)
	{
		devHelper.Console.Output(cnt);
	}

	public static void output(string cnt, Color c)
	{
		devHelper.Console.Output(cnt, c);
	}

	public static void output(string cnt, string color)
	{
		devHelper.Console.Output(cnt, color);
	}

	public static void outputError(string cnt)
	{
		devHelper.Console.Output(cnt, DolocColorHex.red);
	}

	public static void outputSuccess(string cnt)
	{
		devHelper.Console.Output(cnt, DolocColorHex.green);
	}

	public static void outputWarning(string cnt)
	{
		devHelper.Console.Output(cnt, DolocColorHex.yellow);
	}

	public static IEnumerable<MissionLog> GetMissionLogs(string missionId)
	{
		if (!IsGameInitialized)
		{
			return Array.Empty<MissionLog>();
		}
		if (!archiveHandle.farmData.missionManager.QueryMissionLogs(missionId, out var logs))
		{
			return Array.Empty<MissionLog>();
		}
		return logs;
	}

	public static void ShowDebugInfos(object instance, string title = "DEBUG INFO")
	{
		output("- " + title + " START", DolocColorHex.orange);
		(string, string)[] debugInfos = instance.GetDebugInfos(0, 5, "DolocTown");
		for (int i = 0; i < debugInfos.Length; i++)
		{
			var (cnt, text) = debugInfos[i];
			if (text.Length > 0)
			{
				output(cnt, text);
			}
			else
			{
				output(cnt);
			}
		}
		output("- " + title + " END", DolocColorHex.orange);
	}

	public static Delegate GetCommandFunction(string commandName)
	{
		return devHelper.Console.ConsoleSystem.GetFunction(commandName);
	}

	public static IEnumerable<(string, Delegate)> GetAllCommandFunctions()
	{
		return devHelper.Console.ConsoleSystem.GetAllFunctions();
	}

	public static void ExecuteCommand(string input, out object result)
	{
		input = input.Trim();
		if (string.IsNullOrEmpty(input))
		{
			result = null;
			return;
		}
		Exception ex = devHelper.Console.ConsoleSystem.ExecuteCommand(input, out result);
		if (ex != null)
		{
			UnityEngine.Debug.LogError("error occurred while execute \"" + input + "\"");
			if (!(ex is CommandExecuteException))
			{
				UnityEngine.Debug.LogException(ex);
			}
		}
	}

	public static void DoWebRequest(UnityWebRequest req)
	{
		if (!(WebOperationHandle == null))
		{
			WebOperationHandle.DoRequest(req);
		}
	}

	public static bool GetStuckOutPosition(Vector2 currentPositionWS, out Vector2 positionWS)
	{
		positionWS = Vector2Int.zero;
		Room currentRoom = CurrentRoom;
		if (currentRoom == null)
		{
			return false;
		}
		if (!IsStucked(currentRoom, currentPositionWS))
		{
			return false;
		}
		RoomGeometry geometry = currentRoom.Geometry;
		Vector2Int pos = geometry.CalcMinCellPosition(currentPositionWS);
		if (geometry.GetNearestGroundPositionCanPlaceAgent(pos, out var groundPosition))
		{
			positionWS = geometry.roomPosition + DolocTransform.TILE_WORLD_SIZE * new Vector2(groundPosition.x + 1, groundPosition.y);
			positionWS.y += 0.5f;
			return true;
		}
		return false;
	}

	private static bool IsStucked(Room room, Vector2 currentPosition)
	{
		if (!room.Geometry.Contains(currentPosition))
		{
			return true;
		}
		Vector2Int cellPos = room.Geometry.CalcMinCellPosition(currentPosition);
		if (room.Geometry.IsObstacle(cellPos))
		{
			return true;
		}
		return false;
	}

	public static Coroutine Delay(float time, Action callback)
	{
		return gameLoop.Delay(callback, time);
	}

	public static void DelayFrame(Action callback, int frame = 1)
	{
		gameLoop.DelayFrame(callback, frame);
	}

	public static bool TryExecute(Action callback)
	{
		try
		{
			callback?.Invoke();
			return true;
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.LogException(exception);
			return false;
		}
	}

	public static Coroutine StartCoroutine(IEnumerator coroutine)
	{
		if (gameLoop == null)
		{
			return null;
		}
		return gameLoop.StartCoroutine(coroutine);
	}

	public static void StopCoroutine(Coroutine coroutine)
	{
		if (coroutine != null && !(gameLoop == null))
		{
			gameLoop.StopCoroutine(coroutine);
		}
	}

	public static void WaitUntil(Func<bool> predicate, Action callback)
	{
		_WaitUntil(predicate, callback).Forget();
	}

	private static async UniTaskVoid _WaitUntil(Func<bool> predicate, Action callback)
	{
		await UniTask.WaitUntil(predicate);
		callback?.Invoke();
	}

	public static void WaitWhile(Func<bool> predicate, Action callback)
	{
		_WaitWhile(predicate, callback).Forget();
	}

	private static async UniTaskVoid _WaitWhile(Func<bool> predicate, Action callback)
	{
		await UniTask.WaitWhile(predicate);
		callback?.Invoke();
	}

	public static async UniTask WaitWhileInUiSateTask()
	{
		await UniTask.WaitWhile(() => userInput.CurrentState is DolocGameUiState);
	}

	public static Vector2 WorldToScreen(Transform t)
	{
		return mainCamera.WorldToScreenPoint(t.position);
	}

	public static Vector2 WorldToScreen(Vector2 position)
	{
		return mainCamera.WorldToScreenPoint(position);
	}

	public static Vector2 TileSizeToScreenSize(Vector2 tileSize)
	{
		return WorldSizeToScreenSize(tileSize * 1.5f);
	}

	public static Vector2 WorldSizeToScreenSize(Vector2 worldSize)
	{
		float orthographicSize = mainCamera.orthographicSize;
		Vector2 vector = worldSize / new Vector2(orthographicSize * 2f * mainCamera.aspect, orthographicSize * 2f);
		return screenSize * vector;
	}

	public static Vector2 ScreenToWorld(Vector2 position)
	{
		return mainCamera.ScreenToWorldPoint(position);
	}

	public static Vector2 WorldToAroundUiPosBottom(Vector3 posWS, Vector2 uiSize, int dst)
	{
		Vector3 vector = WorldToScreen(posWS);
		vector.x -= uiSize.x / 2f;
		vector.y -= uiSize.y + (float)dst * 4f;
		return vector;
	}

	public static bool IntersectEdgeFromScreenPoint(Vector2 pos, Vector2 dir, Vector2 screenSize, out Vector2 hit, out float dist)
	{
		hit = pos;
		dist = 0f;
		if (dir.sqrMagnitude < 1E-06f)
		{
			return false;
		}
		float x = screenSize.x;
		float y = screenSize.y;
		float num = float.PositiveInfinity;
		Vector2 vector = Vector2.zero;
		if (Mathf.Abs(dir.x) > 1E-06f)
		{
			float num2 = (0f - pos.x) / dir.x;
			if (num2 >= 0f)
			{
				Vector2 vector2 = pos + dir * num2;
				if (vector2.y >= -1E-06f && vector2.y <= y + 1E-06f && num2 < num)
				{
					num = num2;
					vector = vector2;
				}
			}
			num2 = (x - pos.x) / dir.x;
			if (num2 >= 0f)
			{
				Vector2 vector3 = pos + dir * num2;
				if (vector3.y >= -1E-06f && vector3.y <= y + 1E-06f && num2 < num)
				{
					num = num2;
					vector = vector3;
				}
			}
		}
		if (Mathf.Abs(dir.y) > 1E-06f)
		{
			float num3 = (0f - pos.y) / dir.y;
			if (num3 >= 0f)
			{
				Vector2 vector4 = pos + dir * num3;
				if (vector4.x >= -1E-06f && vector4.x <= x + 1E-06f && num3 < num)
				{
					num = num3;
					vector = vector4;
				}
			}
			num3 = (y - pos.y) / dir.y;
			if (num3 >= 0f)
			{
				Vector2 vector5 = pos + dir * num3;
				if (vector5.x >= -1E-06f && vector5.x <= x + 1E-06f && num3 < num)
				{
					num = num3;
					vector = vector5;
				}
			}
		}
		if (float.IsInfinity(num))
		{
			return false;
		}
		hit = vector;
		dist = (vector - pos).magnitude;
		return true;
	}

	public static void SetPPM_FlowPoints(bool value)
	{
		if (value)
		{
			ppm.SetEnabled(PPTypes.FLOWPOINTS, value: true);
			ppm.FlowPointsLightness = eftConfig.fadeInoutLightness;
		}
		else
		{
			ppm.SetEnabled(PPTypes.FLOWPOINTS, value: false);
		}
	}

	public static void SetPPM_CinemaScreen(bool value, float duration = 0.5f, Ease ease = Ease.Linear, TweenCallback callback = null)
	{
		if (duration <= 0f)
		{
			ppm.CinemaScreen.value = (value ? 1 : 0);
			ppm.SetEnabled(PPTypes.CINEMASCREEN, value);
		}
		if (value)
		{
			ppm.SetEnabled(PPTypes.CINEMASCREEN, value: true);
			ppm.CinemaScreen.value = 0f;
			ppm.CinemaScreen.Play(1f, duration, ease, callback);
			return;
		}
		ppm.CinemaScreen.value = 1f;
		ppm.CinemaScreen.Play(0f, duration, ease, delegate
		{
			callback?.Invoke();
			ppm.SetEnabled(PPTypes.CINEMASCREEN, value: false);
		});
	}

	public static void SetPPM_CinemaScreen(bool value)
	{
		if (value)
		{
			SetPPM_CinemaScreen(value: true, eftConfig.cinemaScreenFadeInTime, eftConfig.cinemaScreenFadeInEase);
		}
		else
		{
			SetPPM_CinemaScreen(value: false, eftConfig.cinemaScreenFadeOutTime, eftConfig.cinemaScreenFadeOutEase);
		}
	}

	public static object[] RunLua(string code)
	{
		try
		{
			return luaEnv.DoString(code);
		}
		catch (LuaException exception)
		{
			UnityEngine.Debug.LogError("执行Lua代码时出现异常");
			UnityEngine.Debug.LogException(exception);
			return null;
		}
	}

	public static T RunLua<T>(string code) where T : class
	{
		try
		{
			object[] array = luaEnv.DoString(code);
			if (array == null || array.Length == 0)
			{
				return null;
			}
			return (T)array[0];
		}
		catch (LuaException ex)
		{
			outputError("执行Lua代码时出现异常");
			outputError(ex.Message);
			return null;
		}
	}

	public static void Broadcast(OperationEventType type)
	{
		uiSystem.GuidanceTips.SendMessage(type);
	}

	public static void SwitchLanguage(string l10nId)
	{
		output("切换到语言:" + l10nId, DolocColor.orange);
		DolocConfig.Tables.SwitchLanguage(l10nId);
		FontManager.AdaptToLanguage(l10nId);
		archiveHandle?.cityData.dialogueManager.SwitchLanguage(l10nId);
		uiSystem.MissionTips.RefreshAllTips();
		uiSystem.basicTip.RefreshPosition();
		RefreshSceneResidentTipText();
	}

	public static int GetEventTriggerCount(GameEventType type, string args)
	{
		return archiveHandle.farmData.eventRecorderManager?.GetCount(type, args) ?? 0;
	}

	public static void Broadcast(GameEventType type, GameEventArgs args)
	{
		_ = gameManager.gameInitConfig.showEventMessageLog;
		GameMessage gameMessage = new GameMessage(type, args);
		routineManager.SendMessage(gameMessage);
		GameProcessSystem.SendMessage(gameMessage);
		if (!gameMessage.IsUsed)
		{
			archiveHandle?.SendMessage(gameMessage);
		}
	}

	public static void Broadcast(GameEventType type)
	{
		Broadcast(type, GameEventArgs.None);
	}

	public static void BroadcastString(GameEventType msg, string value)
	{
		Broadcast(msg, new GameEventArgsString(value));
	}

	public static void BroadcastInt(GameEventType msg, int value)
	{
		Broadcast(msg, new GameEventArgsInt(value));
	}

	public static void Broadcast(UserSettingType evtType, GameEventArgs e, object sender = null)
	{
		messageSystem.Broadcast(evtType, e, sender);
	}

	public static void RegisterMsgListener(UserSettingType evtType, Action<object, GameEventArgs> callback)
	{
		messageSystem.Register(evtType, callback);
	}

	public static void UnregisterMsgListener(UserSettingType evtType, Action<object, GameEventArgs> handler)
	{
		messageSystem.Unregister(evtType, handler);
	}

	public static void PerformEnvOptimizerBehaviour(EnvOptimizerBehaviourType evt)
	{
		archiveHandle.farmData.envOptimizerSystem.data.PerformBehaviour(evt);
	}

	public static void PerformEnvOptimizerBehaviour<T>(EnvOptimizerBehaviourType evt, T data = null) where T : class
	{
		archiveHandle.farmData.envOptimizerSystem.data.PerformBehaviour(evt, data);
	}

	public static void SetPPEnabled(PPTypes type, bool value)
	{
		ppm.SetEnabled(type, value);
	}

	public static void SwitchPPEnabled(PPTypes type)
	{
		ppm.SwitchPPEnabled(type);
	}

	public static void CaptureScreen(string filepath, bool withUI = false)
	{
		Vector2Int resolution = new Vector2Int(worldResolution.x, worldResolution.y);
		CaptureScreen(filepath, resolution, withUI);
	}

	public static void CaptureScreen(string filepath, Vector2Int resolution, bool withUI = false)
	{
		if (withUI)
		{
			ScreenCapture.CaptureScreenshot(filepath);
			return;
		}
		RenderTexture renderTexture = new RenderTexture(resolution.x, resolution.y, 24);
		mainCamera.targetTexture = renderTexture;
		mainCamera.Render();
		Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, mipChain: false);
		RenderTexture.active = renderTexture;
		texture2D.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
		mainCamera.targetTexture = null;
		RenderTexture.active = null;
		byte[] bytes = texture2D.EncodeToPNG();
		try
		{
			File.WriteAllBytes(filepath, bytes);
			UnityEngine.Debug.Log("截图已存储于: " + filepath);
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogError("截图失败:" + ex.Message);
		}
	}

	public static void SetEnvCamera(Vector2 cameraPosition, Vector2 cameraSize, bool shouldShowBackground, bool shouldMaskBackground, bool shouldFollowPlayer = false)
	{
		cameraController.SetRoomRange(cameraPosition, cameraSize);
		if (shouldFollowPlayer)
		{
			cameraController.SetPosition(AgentPosition);
		}
		envBackgroundEx.SetVisible(shouldShowBackground);
		envBackgroundEx.SetBackgroundMask(shouldMaskBackground);
	}

	public static void LoadBackground(EnvBackgroundSO background)
	{
		if (background == null)
		{
			envBackgroundEx.LoadDefaultPreset();
		}
		else
		{
			envBackgroundEx.LoadPreset(background);
		}
	}

	public static void QuitCurrentRoom(Room nextRoom, bool shouldUnloadScene = true)
	{
		if (archiveHandle.currentRoom != null)
		{
			Room currentRoom = archiveHandle.currentRoom;
			effectProvider.Clear();
			battleSystem.ClearBullets();
			battleSystem.ClearSkills();
			currentRoom.OnExitRoom(nextRoom);
			archiveHandle.currentRoom = null;
			if (currentRoom.Type == RoomType.Dungeon && nextRoom != null && nextRoom.Type != RoomType.Dungeon)
			{
				_TryQuitDungeon();
			}
			if (!sceneManager.IsSceneExists(currentRoom.SceneRawName))
			{
				UnityEngine.Debug.LogError("数据错误:房间<" + currentRoom.Title + ">没有对应场景!");
			}
			if (shouldUnloadScene)
			{
				sceneManager.UnloadSceneAsync(currentRoom.SceneRawName);
			}
		}
	}

	public static bool EnterCity(CityRoom room, Vector2 remotePosition, Action callback = null)
	{
		gameStateManager.TransitScene(room, remotePosition, delegate
		{
			room.OnEnterRoom();
			callback?.Invoke();
		});
		return true;
	}

	public static bool EnterCity(string shortName, Vector2 remotePosition, Action callback = null)
	{
		if (!archiveHandle.QueryCityRoom(shortName, out var room))
		{
			outputError("城镇房间<" + shortName + ">不存在");
			return false;
		}
		if (!sceneManager.IsSceneExists(room.SceneRawName))
		{
			outputError("城镇场景<" + room.SceneRawName + ">不存在");
			return false;
		}
		if (archiveHandle.currentRoom != null && archiveHandle.currentRoom == room)
		{
			outputError("已经处于城镇<" + shortName + ">");
			return false;
		}
		gameStateManager.TransitScene(room, remotePosition, delegate
		{
			room.OnEnterRoom();
			callback?.Invoke();
		});
		BroadcastString(GameEventType.ARRIVE_ROOM_CITY, shortName);
		return true;
	}

	public static bool EnterFarm(TemplateRoom room, Vector2 position, Action callback = null)
	{
		gameStateManager.TransitScene(room, position, delegate
		{
			room.OnEnterRoom();
			callback?.Invoke();
		});
		return true;
	}

	public static bool EnterFarm(string roomGuid, Vector2 position, Action callback = null)
	{
		if (!archiveHandle.QueryTemplateRoom(roomGuid, out var room))
		{
			outputError("房间<" + roomGuid + ">不存在");
			return false;
		}
		if (!sceneManager.IsSceneExists(room.SceneRawName))
		{
			outputError("场景<" + room.SceneRawName + ">不存在");
			return false;
		}
		if (archiveHandle.currentRoom != null && archiveHandle.currentRoom == room)
		{
			outputError("已经处于目标房间");
			return false;
		}
		gameStateManager.TransitScene(room, position, delegate
		{
			room.OnEnterRoom();
			callback?.Invoke();
		});
		return true;
	}

	public static bool EnterDungeon(string name, Action callback = null)
	{
		if (archiveHandle.CurrentDungeon != null && archiveHandle.CurrentDungeon.ProtoName == name)
		{
			outputError("已经处于地牢\"" + name + "\"");
			return false;
		}
		if (!archiveHandle.QueryDungeon(name, out var dungeon))
		{
			outputError("地牢\"" + name + "\"不存在");
			return false;
		}
		if (!sceneManager.IsSceneExists(dungeon.proto.sceneInfo.name))
		{
			outputError("地牢场景\"" + name + "\"不存在");
			return false;
		}
		DungeonRoom room = dungeon.entryRoom;
		gameStateManager.TransitScene(room, room.entryPosition, delegate
		{
			ShowDungeonRoomName(room);
			SetEnvCamera(room.CameraPosition, room.CameraSize, room.ShouldShowBackground, room.ShouldMaskBackground);
			cameraController.SetPosition(AgentPosition);
			_TryEnterDungeon(dungeon);
			room.OnEnterRoom();
			callback?.Invoke();
		});
		return true;
	}

	private static void ShowDungeonRoomName(Room room)
	{
		uiSystem.messageBoxManager.messageBoxRollCall.Show(room.SceneConfig?.Title ?? room.SceneShortName, room.SceneConfig?.SubTitle ?? room.Title);
	}

	public static bool EnterRoom(string roomId, Vector2 position, Action callback = null)
	{
		if (!QueryRoom(roomId, out var room))
		{
			return false;
		}
		return EnterRoom(room, position, callback);
	}

	public static bool EnterRoom(Room room, Vector2 position, Action callback = null)
	{
		DungeonRoom dungeonRoom = room as DungeonRoom;
		bool isDungeon = dungeonRoom != null;
		Dungeon dungeon = null;
		if (isDungeon && !archiveHandle.QueryDungeon(dungeonRoom.dungeonProtoName, out dungeon))
		{
			return false;
		}
		gameStateManager.TransitScene(room, position, delegate
		{
			if (isDungeon)
			{
				if (archiveHandle.CurrentDungeon != dungeon)
				{
					ShowDungeonRoomName(room);
					_TryEnterDungeon(dungeon);
				}
				cameraController.SetPosition(position);
			}
			room.OnEnterRoom();
			callback?.Invoke();
		});
		return true;
	}

	public static bool CheckAvailableInCurrentState(string gateId)
	{
		return gameStateManager.festivalState.CheckGateAvailable(gateId);
	}

	public static bool DoTransport(string markPointId, Action callback = null, bool resetVelocity = true, bool disableFadeIn = false, bool disableFadeOut = false)
	{
		return DoTransport(markPointId, Vector2.zero, callback, resetVelocity, disableFadeIn, disableFadeOut);
	}

	public static bool DoTransport(string markPointId, Vector2 offset, Action callback = null, bool resetVelocity = true, bool disableFadeIn = false, bool disableFadeOut = false, TransportOverrideInfo overrideInfo = null)
	{
		if (string.IsNullOrEmpty(markPointId))
		{
			UnityEngine.Debug.LogWarning("传送点Id不可为空");
			return false;
		}
		MarkPointInfo orDefault = DolocConfig.Tables.TbMarkPoint.GetOrDefault(markPointId);
		if (orDefault == null)
		{
			UnityEngine.Debug.LogWarning("传送点<" + markPointId + ">没有相关配置");
			return false;
		}
		if (disableFadeIn)
		{
			ppm.DisableFadeInOnce = true;
		}
		if (disableFadeOut)
		{
			ppm.DisableFadeOutOnce = true;
		}
		ClearSceneOperationTips();
		if (!resetVelocity)
		{
			Vector2 velocity = agent.Velocity;
			callback = (Action)Delegate.Combine(callback, (Action)delegate
			{
				agent.SetVelocity(velocity);
			});
		}
		string roomId = overrideInfo?.roomId ?? orDefault.RoomId;
		Vector2 vector = overrideInfo?.position ?? orDefault.Position;
		return EnterRoom(roomId, vector + offset, callback);
	}

	public static Room GetRoom(string name)
	{
		QueryRoom(name, out var room);
		return room;
	}

	public static bool QueryRoom(string name, out Room room)
	{
		room = null;
		if (name.IsNullOrEmpty())
		{
			return false;
		}
		DungeonRoom room4;
		if (name.StartsWith("farm_"))
		{
			room = null;
			string guid = "";
			string[] array = name.Replace("farm_", "").Split(".");
			if (array.Length > 1)
			{
				guid = array[^1];
			}
			if (archiveHandle.QueryTemplateRoomFromGlobal(guid, out var room2))
			{
				room = room2;
				return true;
			}
		}
		else if (name.StartsWith("city_"))
		{
			if (archiveHandle.QueryCityRoom(name.Replace("city_", ""), out var room3))
			{
				room = room3;
				return true;
			}
		}
		else if (name.StartsWith("dungeon_") && archiveHandle.QueryDungeonRoom(name.Replace("dungeon_", ""), out room4))
		{
			room = room4;
			return true;
		}
		UnityEngine.Debug.LogError("场景名<" + name + ">未能找到对应房间");
		return false;
	}

	private static void _TryQuitDungeon()
	{
		if (archiveHandle.CurrentDungeon != null)
		{
			archiveHandle.CurrentDungeon = null;
			envBackgroundEx.LoadDefaultPreset();
		}
	}

	private static void _TryEnterDungeon(Dungeon dungeon)
	{
		if (archiveHandle.CurrentDungeon != dungeon)
		{
			archiveHandle.InvokeDungeonHistory(dungeon);
			envBackgroundEx.LoadPreset(dungeon.proto.backgroundSO);
		}
		archiveHandle.CurrentDungeon = dungeon;
	}

	public static void RunDrone(DroneStruct droneStructure, bool shouldRender = true)
	{
		gameStateManager.normalGameState.AgentController.droneController.RunDrone(droneStructure, shouldRender);
		agent.WeakLightEnabled = false;
	}

	private static void UnloadDrone()
	{
		gameStateManager.normalGameState.AgentController.droneController.UnloadDrone();
		if (archiveHandle != null)
		{
			agent.WeakLightEnabled = archiveHandle.ShouldLightUp;
		}
	}

	public static bool ExtendFarm(string farmName, Action<bool> callback = null)
	{
		try
		{
			if (farmName.IsNullOrEmpty() || !assets.rooms.QueryData(farmName, out var proto))
			{
				callback?.Invoke(obj: false);
				return false;
			}
			if (CurrentRoom != archiveHandle.MainFarm)
			{
				bool flag = archiveHandle.SetFarmData(proto);
				callback?.Invoke(flag);
				return flag;
			}
			TransitFadeInout(delegate
			{
				AgentEnabled = false;
				bool obj = archiveHandle.SetFarmData(proto);
				callback?.Invoke(obj);
				bool flag2 = archiveHandle.MainFarm != archiveHandle.currentRoom;
				QuitCurrentRoom(archiveHandle.MainFarm, flag2);
				if (flag2)
				{
					sceneManager.LoadSceneAsync(archiveHandle.MainFarm.SceneRawName, delegate
					{
						OnSceneLoaded(archiveHandle);
					});
				}
				else
				{
					OnSceneLoaded(archiveHandle);
				}
			});
			return true;
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.LogError("农场升级出现异常！");
			UnityEngine.Debug.LogException(exception);
			return false;
		}
		static void OnSceneLoaded(ArchiveDataHandle handle)
		{
			handle.MainFarm.OnEnterRoom();
			AgentEnabled = true;
		}
	}

	public static bool TryEnterDungeonSubRoom(string name)
	{
		if (archiveHandle.CurrentDungeon == null)
		{
			UnityEngine.Debug.Log("当前不处于地牢");
			return false;
		}
		if (!archiveHandle.CurrentDungeon.QueryRoom(name, out var room))
		{
			UnityEngine.Debug.Log("地牢子房间<" + name + ">不存在");
			return false;
		}
		if (archiveHandle.currentRoom == room)
		{
			UnityEngine.Debug.Log("已经处于地牢子房间<" + name + ">");
			return false;
		}
		archiveHandle.currentRoom.OnExitRoom(room);
		gameStateManager.TransitSceneDungeon(room, room.OnEnterRoom);
		return true;
	}

	public static bool HasBuff(string id)
	{
		return BuffManager.HasBuff(id ?? "");
	}

	public static bool AddBuff(string id, float scale = 1f)
	{
		return BuffManager.Add(id, scale);
	}

	public static bool DisableGate(string gateName)
	{
		return archiveHandle.DisableGate(gateName);
	}

	public static bool EnableGate(string gateName)
	{
		return archiveHandle.EnableGate(gateName);
	}

	public static bool RunGameProcess(string name, Action callback = null)
	{
		if (name.IsNullOrEmpty())
		{
			return false;
		}
		if (assets.gameProcessGraphs.QueryData(name, out var data))
		{
			return GameProcessSystem.RunProcess(data, callback);
		}
		UnityEngine.Debug.LogError("流程图\"" + name + "\"不存在");
		return false;
	}

	public static bool RunGameProcess(GameProcessGraph graph, Action callback = null)
	{
		if (graph != null)
		{
			return GameProcessSystem.RunProcess(graph, callback);
		}
		return false;
	}

	public static bool IsGameProcessRunning(string name)
	{
		if (name.IsNullOrEmpty())
		{
			return false;
		}
		if (assets.gameProcessGraphs.QueryData(name, out var data))
		{
			return GameProcessSystem.IsProcessRunning(data);
		}
		return false;
	}

	public static void StopGameProcess(string name)
	{
		if (!name.IsNullOrEmpty())
		{
			if (assets.gameProcessGraphs.QueryData(name, out var data))
			{
				GameProcessSystem.StopProcess(data);
			}
			else
			{
				UnityEngine.Debug.LogError("流程图\"" + name + "\"不存在");
			}
		}
	}

	public static string GetFormatTimeLengthByTU(int timeUnit)
	{
		return GetFormatTimeLengthByMinutes(timeUnit * GlobalParameter.TU2Min);
	}

	public static string GetFormatTimeLengthByHours(int totalHours)
	{
		return GetFormatTimeLengthByMinutes(totalHours * GlobalParameter.Hour2Min);
	}

	public static string GetFormatTimeLengthByMinutes(int totalMinutes)
	{
		if (totalMinutes <= 0)
		{
			return DolocConfig.StaticTexts.UiTextMinutes.Format(0);
		}
		totalMinutes = Mathf.Max(0, totalMinutes);
		int hour2Min = GlobalParameter.Hour2Min;
		int num = hour2Min * GlobalParameter.Day2Hour;
		int num2 = num * GlobalParameter.Month2Day;
		int num3 = num2 * GlobalParameter.Year2Month;
		string text = "";
		bool flag = true;
		int num4 = totalMinutes / num3;
		totalMinutes %= num3;
		if (num4 > 0)
		{
			flag = false;
			text += HandleTimeString(DolocConfig.StaticTexts.UiTextYears, num4);
		}
		int num5 = totalMinutes / num2;
		totalMinutes %= num2;
		if (!flag || num5 > 0)
		{
			flag = false;
			text += HandleTimeString(DolocConfig.StaticTexts.UiTextMonths, num5);
		}
		int num6 = totalMinutes / num;
		totalMinutes %= num;
		if (!flag || num6 > 0)
		{
			flag = false;
			text += HandleTimeString(DolocConfig.StaticTexts.UiTextDays, num6);
		}
		int num7 = totalMinutes / hour2Min;
		totalMinutes %= hour2Min;
		if (!flag || num7 > 0)
		{
			flag = false;
			text += HandleTimeString(DolocConfig.StaticTexts.UiTextHours, num7);
		}
		if (totalMinutes > 0)
		{
			text += HandleTimeString(DolocConfig.StaticTexts.UiTextMinutes, totalMinutes);
		}
		return text.TrimEnd(' ');
	}

	public static string HandleTimeString(string formatStr, int value)
	{
		if (CurrentL10nId == "en")
		{
			if (value == 1)
			{
				return formatStr.Format(value) + " ";
			}
			return formatStr.Format(value) + "s ";
		}
		return formatStr.Format(value);
	}

	public static string HandleJoinString(this IEnumerable<string> str)
	{
		return string.Join(DolocConfig.Tables.CurrentL10nId switch
		{
			"zh-CN" => "、", 
			"zh-TW" => "、", 
			"ja" => "、", 
			_ => ", ", 
		}, str);
	}

	public static void RefreshScanner()
	{
		gameStateManager.agentController.RoomScanner.OnPosChanged(AgentRoomCellPosition);
		gameStateManager.agentController.BuildingScanner.OnPosChanged(AgentRoomCellPosition);
	}

	public static bool IsMotor(this Collider2D other)
	{
		if (other != null)
		{
			return other.GetComponent<MotorInteractable>() != null;
		}
		return false;
	}

	public static bool IsAgent(this Collider2D other)
	{
		if (other != null)
		{
			return other.GetComponent<BodyController>() != null;
		}
		return false;
	}

	public static FishInfo RollFish(string poolName, int toolLv)
	{
		if (!TryGetFishingPoolInfo(out var info2))
		{
			UnityEngine.Debug.LogWarning("池塘配置表\"" + poolName + "\"丢失");
			return null;
		}
		if (DolocPatch.IsNullOrEmpty(info2.Fishes))
		{
			UnityEngine.Debug.LogWarning("池塘配置表\"" + poolName + "\"中没有设置任何鱼原型");
			return null;
		}
		bool isGarbage = RandomUtils.Dice(info2.GarbageProbability);
		if (info2.Fishes_Ref.Count((FishInfo x) => x.IsGarbage) == 0)
		{
			isGarbage = false;
		}
		int fishingLevel = archiveHandle.farmData.techLevelManager.GetLevelData(TechPointType.FISHING).CurrentLevel;
		FishInfo[] source = info2.Fishes_Ref.Where((FishInfo fish) => fish.IsMatch(isGarbage, toolLv, fishingLevel, archiveHandle.CurrentWeatherType, archiveHandle.DateNow)).ToArray();
		source = source.Where((FishInfo fish) => CheckFishUnlocked(fish.Id)).ToArray();
		if (source.Length == 0)
		{
			return null;
		}
		int[] array = source.Select((FishInfo fish) => fish.Rarity).Distinct().ToArray();
		int[] probabilities = array.Select((int rarity) => info2.RarityWeights[rarity]).ToArray();
		int rarity2 = array[RandomUtils._RussianRoulette(probabilities)];
		source = source.Where((FishInfo fish) => fish.Rarity == rarity2).ToArray();
		return source.Choice() ?? null;
		bool TryGetFishingPoolInfo(out FishingPoolInfo info)
		{
			info = null;
			if (poolName.IsNullOrEmpty())
			{
				return false;
			}
			info = DolocConfig.Tables.TbFishingPool.GetOrDefault(poolName);
			return info != null;
		}
	}

	public static FishInfo RollFishByRarity(string poolName, int rarity)
	{
		if (poolName.IsNullOrEmpty())
		{
			return null;
		}
		FishingPoolInfo orDefault = DolocConfig.Tables.TbFishingPool.GetOrDefault(poolName);
		if (orDefault == null)
		{
			UnityEngine.Debug.LogWarning("池塘配置表\"" + poolName + "\"丢失");
			return null;
		}
		if (DolocPatch.IsNullOrEmpty(orDefault.Fishes_Ref))
		{
			UnityEngine.Debug.LogWarning("池塘配置表\"" + poolName + "\"中没有设置任何鱼原型");
			return null;
		}
		bool isGarbage = RandomUtils.Dice(orDefault.GarbageProbability);
		if (orDefault.Fishes_Ref.Count((FishInfo x) => x.IsGarbage) == 0)
		{
			isGarbage = false;
		}
		FishInfo[] source = orDefault.Fishes_Ref.Where((FishInfo fish) => fish.IsMatch(isGarbage, rarity, archiveHandle.CurrentWeatherType, archiveHandle.DateNow)).ToArray();
		source = source.Where((FishInfo fish) => CheckFishUnlocked(fish.Id)).ToArray();
		if (source.Length == 0)
		{
			return null;
		}
		return source.Choice();
	}

	public static void SendCatchFishEvent(FishInfo fish)
	{
		if (fish != null)
		{
			string id = fish.Id;
			BroadcastString(fish.IsFish ? GameEventType.FISHING_CATCH_FISH : GameEventType.FISHING_CATCH_TRASH, id);
			if (fish.IsFish)
			{
				archiveHandle.RecordCollection(CollectionType.Fish, id);
			}
		}
	}

	public static void SpawnResourceDropItems(DungeonResource resource, bool isRender, string overrideSpawnLut)
	{
		archiveHandle.dungeonData.guaranteedManager.SpawnResourceDropItems(resource, isRender, overrideSpawnLut);
	}

	public static void SpawnMonsterDropItems(Monster monster)
	{
		archiveHandle.dungeonData.guaranteedManager.SpawnMonsterDropItems(monster);
	}

	public static void RefreshSunLight(float transitDuration)
	{
		EnvCovariantController.TransitSun(archiveHandle.DayProcess, archiveHandle.CurrentWeatherType, CurrentRoom, transitDuration);
	}

	public static void RaiseEffectsSO(Vector2 position, string effectsConfigName)
	{
		if (assets.effects.QueryData(effectsConfigName, out var data))
		{
			data.Raise(position);
		}
	}

	public static void RaiseEffectsSO(string effectsConfigName)
	{
		if (assets.effects.QueryData(effectsConfigName, out var data))
		{
			data.Raise(default(Vector2));
		}
	}

	public static void RaiseEmotion(Transform entity, EmotionName name)
	{
		if (!(entity == null))
		{
			emotionSystem.Raise(entity, name);
		}
	}

	public static void RaiseEmotionLimited(Transform entity, EmotionName name, float interval = 3f)
	{
		if (!(entity == null))
		{
			emotionSystem.Raise(entity, name, interval);
		}
	}

	public static void ClearEmotions(Transform entity)
	{
		if (!(entity == null))
		{
			emotionSystem.Clear(entity);
		}
	}

	public static void RaiseUiTextFadeUp(string text, Color color, Vector2 screenPosition, float duration = 0.7f, Ease moveEase = Ease.OutExpo, float popDistance = 100f)
	{
		uiEffectsProvider.RaiseFadeUpText(text, color, screenPosition, moveEase, duration, popDistance);
	}

	public static void RaiseUiNumberFadeUp(int value, Color color, Vector2 screenPosition, float duration = 0.5f, Ease moveEase = Ease.OutExpo, float popDistance = 100f, float waitDuration = 1.5f)
	{
		uiEffectsProvider.RaiseFadeUpNumber(value, color, screenPosition, moveEase, duration, popDistance, waitDuration);
	}

	public static void RaiseUiSpriteFadeUp(Vector2 screenPosition, Sprite icon, float duration = 0.7f, Ease moveEase = Ease.OutExpo, float popDistance = 100f)
	{
		uiEffectsProvider.RaiseFadeUpSprite(screenPosition, icon, moveEase, duration, popDistance);
	}

	public static void RaiseSpriteFadeUp(Vector2 worldPosition, Sprite icon, float duration = 0.7f, Ease moveEase = Ease.OutExpo, float popDistance = 2f, bool flipX = false)
	{
		effectProvider.RaiseFadeUpSprite(worldPosition, icon, moveEase, duration, popDistance, flipX);
	}

	public static void RaiseSpriteFadeUp(Vector2 worldPosition, string itemName, float duration = 0.7f, Ease moveEase = Ease.OutExpo, float popDistance = 2f, bool flipX = false)
	{
		Sprite itemSprite = GetItemSprite(itemName);
		if (!(itemSprite == null))
		{
			RaiseSpriteFadeUp(worldPosition, itemSprite, duration, moveEase, popDistance, flipX);
		}
	}

	public static void RaiseUiSpriteFadeDown(Vector2 screenPosition, Sprite icon, float duration = 0.7f, Ease moveEase = Ease.OutQuart, float popDistance = 100f)
	{
		uiEffectsProvider.RaiseFadeDownSprite(screenPosition, icon, moveEase, duration, popDistance);
	}

	public static void RaiseSpriteFadeDown(Vector2 worldPosition, Sprite icon, float duration = 0.7f, Ease moveEase = Ease.OutQuart, float popDistance = 2f)
	{
		effectProvider.RaiseFadeDownSprite(worldPosition, icon, moveEase, duration, popDistance);
	}

	public static void RaiseSpriteArrayFadeUp(Vector2 worldPosition, Sprite[] icons, float duration = 0.7f, Ease moveEase = Ease.OutExpo, float popDistance = 2f, bool flipX = false)
	{
		effectProvider.RaiseFadeUpSpriteArray(worldPosition, icons, moveEase, duration, popDistance, flipX);
	}

	public static void RaiseSpriteArrayFadeDown(Vector2 worldPosition, Sprite[] icons, float duration = 0.7f, Ease moveEase = Ease.OutQuart, float popDistance = 2f)
	{
		effectProvider.RaiseFadeDownSpriteArray(worldPosition, icons, moveEase, duration, popDistance);
	}

	public static void MoveFadeOutSprite(Vector2 from, Vector2 to, Sprite icon, float duration = 0.35f, Ease ease = Ease.Linear)
	{
		effectProvider.MoveFadeOutSprite(from, to, icon, ease, duration);
	}

	public static void RaiseDamageTip(int value, Vector2 ws, bool isHeavy = false, float duration = 0.7f, float popDistance = 50f, float waitTime = 1.5f)
	{
		uiEffectsProvider.RaiseDamageTip(value, ws, isHeavy, duration, popDistance, waitTime);
	}

	public static void RaiseInstantAnimEffects(Vector2 worldPosition, InstAnimEffectType type)
	{
		effectProvider.RaiseInstAnim(worldPosition, type);
	}

	public static void RaiseInstantAnimEffects(Vector2 worldPosition, InstAnimEffectType type, Vector2 dir)
	{
		effectProvider.RaiseInstAnim(worldPosition, type, dir);
	}

	public static void RaiseInstantAnimEffects(Vector2 worldPosition, InstAnimEffectType type, bool flip)
	{
		effectProvider.RaiseInstAnim(worldPosition, type, flip);
	}

	public static void RaiseInstantAnimEffects(Vector2 worldPosition, InstAnimEffectType type, Material mat)
	{
		effectProvider.RaiseInstAnim(worldPosition, type, mat);
	}

	public static void RaiseInstantAnimEffects(Vector2 worldPosition, InstAnimEffectType type, string layerName, int sortingOrder = 0)
	{
		effectProvider.RaiseInstAnim(worldPosition, type, layerName, sortingOrder);
	}

	public static void RaiseInstantAnimEffects(Vector2 worldPosition, InstAnimEffectType type, Vector2 dir, bool flip, Material mat, string layerName = "Default", int orderInLayer = 0)
	{
		effectProvider.RaiseInstAnim(worldPosition, type, dir, flip, mat, layerName, orderInLayer);
	}

	public static void RaiseInstantPSEffects(Vector2 positionWS, InstantParticleEffectsType type)
	{
		effectProvider.RaiseInstPS(positionWS, type);
	}

	public static void RaiseInstantPSEffects(Vector2 positionWS, InstantParticleEffectsType type, string sortingLayer, int sortingOrder)
	{
		effectProvider.RaiseInstPS(positionWS, type, sortingLayer, sortingOrder);
	}

	public static InstantGoEffects GetInstantGoEffects(InstantGoEffectsType type)
	{
		return effectProvider.GetInstantGoEffects(type);
	}

	public static void RaiseInstantGoEffects(Vector2 positionWS, InstantGoEffectsType type)
	{
		effectProvider.RaiseInstGo(positionWS, type);
	}

	public static void RaiseInstantGoEffects(Vector2 positionWS, InstantGoEffectsType type, Vector2 dir)
	{
		effectProvider.RaiseInstGo(positionWS, type, dir);
	}

	public static void RaiseContinuesPS(Vector2 positionWS, ContinuesParticleEffectsType type, float duration)
	{
		effectProvider.RaiseContinuesPS(positionWS, type, duration);
	}

	public static ContinuesParticleEffects RaiseContinuesPS(Vector2 positionWS, ContinuesParticleEffectsType type)
	{
		return effectProvider.RaiseContinuesPS(positionWS, type);
	}

	public static void RaiseWind(Vector2 ws, Vector2 force, float dur)
	{
		effectProvider.RaiseWind(ws, force, dur);
	}

	public static void RaiseScreenTwist(Vector2 ws, float dur)
	{
		effectProvider.RaiseScreenTwist(ws, dur);
	}

	public static void RaiseGhostShadow(Vector3 ws, Vector2 directionScale, Sprite sprite, bool useScale = false, float targetScale = 3f)
	{
		effectProvider.RaiseGhostShadow(ws, directionScale, sprite, useScale, targetScale);
	}

	public static void RaiseUiEffects(int value, Color color)
	{
		Vector3 vector = UnityEngine.Random.insideUnitCircle * 1.5f * 1.5f;
		RaiseUiNumberFadeUp(value, color, WorldToScreen(AgentPosition + vector));
	}

	public static void TransitFadeInout(Action callbackOnFadeIn, float fadeInDuration = 3f, float fadeOutDuration = 1.5f, float waitDuration = 0f)
	{
		new GameStateUniversalTransition(userInput, callbackOnFadeIn, waitDuration, fadeInDuration, fadeOutDuration).Startup();
	}

	public static void TransitAnimation(Action callbackOnAnimation, float waitDuration = 0f)
	{
	}

	public static void QuickSelectCurrentItem()
	{
		if (IsDataLoaded)
		{
			if (SelectedItem == null)
			{
				uiSystem.basicTip.AgentCellTip.Hide();
			}
			SelectedItem?.QuickSelect();
		}
	}

	public static void QuickDeselectCurrentItem()
	{
		if (IsDataLoaded)
		{
			SelectedItem?.QuickDeselect();
		}
	}

	public static void ReQuickSelectCurrentItem()
	{
		QuickDeselectCurrentItem();
		QuickSelectCurrentItem();
	}

	public static void ReEnableAgent()
	{
		AgentEnabled = false;
		AgentEnabled = true;
	}

	public static void GetAgentCellAreaAnchor(Vector2Int offset, Vector2Int cellSize, bool flipWhenFaceLeft, out Vector2Int anchor)
	{
		if (flipWhenFaceLeft && !AgentFaceRight)
		{
			offset *= new Vector2Int(-1, 1);
			offset.x -= cellSize.x;
		}
		anchor = AgentRealRoomCellPosition + offset;
	}

	public static bool GetAgentPosInMap(out Vector2 pos)
	{
		pos = Vector2.zero;
		CityMapPanel cityMapPanel = uiSystem.GetEntity<CollectionBookPanel>()?.mapPanel?.cityMap;
		if (cityMapPanel == null)
		{
			UnityEngine.Debug.LogError("获取城镇地图ui失败");
			return false;
		}
		return cityMapPanel.GetMapPosByWorldPosition(CurrentRoom, agent.PositionCenter, out pos);
	}

	public static bool GetMotorDirToAgentInMap(out Vector2 dir)
	{
		dir = Vector2.down;
		CityMapPanel cityMapPanel = uiSystem.GetEntity<CollectionBookPanel>()?.mapPanel?.cityMap;
		if (cityMapPanel == null)
		{
			UnityEngine.Debug.LogError("获取城镇地图ui失败");
			return false;
		}
		MotorDataManager motorData = archiveHandle.farmData.agentData.motorData;
		if (cityMapPanel.GetMapPosByWorldPosition(motorData.CurrentRoom, motorData.position, out var mapPosition) && cityMapPanel.GetMapPosByWorldPosition(CurrentRoom, agent.PositionCenter, out var mapPosition2))
		{
			dir = mapPosition2 - mapPosition;
			float num = Mathf.Max(0f, Mathf.Abs(dir.y) - 8f * screenManager.screenScale.y);
			dir.y = num * Mathf.Sign(dir.y);
			dir = dir.normalized;
			return true;
		}
		return false;
	}

	public static bool GetMapPosByWorldPosition(Room room, Vector2 worldPosition, out Vector2 mapPosition)
	{
		mapPosition = Vector2.zero;
		if (room == null)
		{
			return false;
		}
		CityMapPanel cityMapPanel = uiSystem.GetEntity<CollectionBookPanel>()?.mapPanel?.cityMap;
		if (cityMapPanel == null)
		{
			UnityEngine.Debug.LogError("获取城镇地图ui失败");
			return false;
		}
		return cityMapPanel.GetMapPosByWorldPosition(room, worldPosition, out mapPosition);
	}

	public static bool GetMotorPosInMap(out Vector2 pos)
	{
		pos = Vector2.zero;
		CityMapPanel cityMapPanel = uiSystem.GetEntity<CollectionBookPanel>()?.mapPanel?.cityMap;
		if (cityMapPanel == null)
		{
			UnityEngine.Debug.LogError("获取城镇地图ui失败");
			return false;
		}
		MotorDataManager motorData = archiveHandle.farmData.agentData.motorData;
		return cityMapPanel.GetMapPosByWorldPosition(motorData.CurrentRoom, motorData.position, out pos);
	}

	public static void ResetPlayerValues()
	{
		archiveHandle.ResetPlayerValues();
		uiSystem.agentStatusBar.UpdateAllInfo();
	}

	public static void ResetPlayerValues(float healthPercent, float energyPercent, float spiritPercent)
	{
		archiveHandle.ResetPlayerValues(healthPercent, energyPercent, spiritPercent);
		uiSystem.agentStatusBar.UpdateAllInfo();
	}

	public static void RecoverPlayerValue(int totalSeconds, bool fullSleepBuff, bool isNap)
	{
		int num = totalSeconds * GlobalParameter.TU2Min / GlobalParameter.TULength;
		float num2 = (float)num * (GlobalParameter.RecoveredPlayerValuesPerHour / (float)GlobalParameter.Hour2Min);
		float num3 = AgentEquipmentParams.recoveryAdditionPercent + 1f;
		num2 *= num3;
		UnityEngine.Debug.Log($"游戏分钟：{num}  现实秒数：{totalSeconds}  回复量{num2}");
		if (fullSleepBuff && num2 >= GlobalParameter.FullSleepBuffThreshold)
		{
			AddBuff(GlobalParameter.FullSleepBuff);
		}
		AddHealthByPercent(num2);
		AddEnergyByPercent(num2);
		AddSpiritByPercent(num2);
	}

	public static void ChangeHealth(int value, HurtReason reason)
	{
		if (value >= 0)
		{
			AddHealth(value);
		}
		else
		{
			CostHealth(Mathf.Abs(value), reason);
		}
	}

	public static bool CostHealth(int value, HurtReason reason)
	{
		if (gameManager.gameInitConfig.agentInvincible)
		{
			return false;
		}
		if (value <= 0)
		{
			return true;
		}
		value = archiveHandle._CostHealth(value);
		uiSystem.agentStatusBar.UpdateHealth();
		if (value == 0)
		{
			if (archiveHandle.IsTraining)
			{
				FinishTrainingDungeon(shouldFade: true);
				return true;
			}
			new FaintGameState(userInput, reason.ToFaintReason()).Startup();
			return true;
		}
		return false;
	}

	public static int AddHealth(int value)
	{
		int health = archiveHandle.farmData.agentData.health;
		value = AbilitySystem.recorveryAbility.GetHealthRecovery(value);
		if (value <= 0)
		{
			return 0;
		}
		int result = archiveHandle._AddHealth(value) - health;
		uiSystem.agentStatusBar.UpdateHealth();
		return result;
	}

	public static int AddHealthByPercent(float percent)
	{
		percent = Mathf.Clamp01(percent);
		return AddHealth(Mathf.CeilToInt((float)archiveHandle.farmData.agentData.MaxHealth * percent));
	}

	public static void ChangeEnergy(int value)
	{
		if (value >= 0)
		{
			AddEnergy(value);
			return;
		}
		archiveHandle._CostEnergyNoProtect(Mathf.Abs(value));
		uiSystem.agentStatusBar.UpdateEnergy();
	}

	public static bool CostEnergy(int value)
	{
		if (!archiveHandle._CostEnergy(value))
		{
			return false;
		}
		uiSystem.agentStatusBar.UpdateEnergy();
		return true;
	}

	public static bool CostToolEnergy()
	{
		if (!archiveHandle._CostEnergy(GlobalParameter.ToolEnergyCost))
		{
			return false;
		}
		uiSystem.agentStatusBar.UpdateEnergy();
		return true;
	}

	public static bool HasEnoughEnergy(int value)
	{
		return archiveHandle.HasEnoughEnergy(value);
	}

	public static bool HasEnoughEnergyForUsingTool()
	{
		return archiveHandle.HasEnoughEnergy(GlobalParameter.ToolEnergyCost);
	}

	public static int AddEnergy(int value)
	{
		int energy = archiveHandle.farmData.agentData.energy;
		value = AbilitySystem.recorveryAbility.GetEnergyRecovery(value);
		if (value <= 0)
		{
			return 0;
		}
		int result = archiveHandle._AddEnergy(value) - energy;
		uiSystem.agentStatusBar.UpdateEnergy();
		return result;
	}

	public static int AddEnergyByPercent(float percent)
	{
		percent = Mathf.Clamp01(percent);
		return AddEnergy(Mathf.CeilToInt((float)archiveHandle.farmData.agentData.MaxEnergy * percent));
	}

	public static void ChangeSpirit(int value)
	{
		AddSpirit(value);
	}

	public static void AddSpiritByPercent(float percent)
	{
		percent = Mathf.Clamp01(percent);
		AddSpirit(Mathf.CeilToInt((float)archiveHandle.farmData.agentData.MaxSpirit * percent));
	}

	public static void AddSpirit(int value)
	{
		value = AbilitySystem.recorveryAbility.GetSpiritRecovery(value);
		archiveHandle.farmData.agentData.AddSpirit(value);
		uiSystem.agentStatusBar.UpdateSpirit();
	}

	public static void SetSpiritEnabled(bool enabled)
	{
		UnityEngine.Debug.Log(enabled ? "启用精力值系统" : "禁用精力值系统");
		archiveHandle.farmData.agentData.spiritEnabled = enabled;
	}

	public static void CostTechPoint(int count, TechPointType type)
	{
		archiveHandle._CostTechPoint(type, count);
	}

	public static void AddTechExp(TechPointType type, int exp)
	{
		if (archiveHandle.AddTechExp(type, exp, out var levelData))
		{
			BroadcastString(GameEventType.TECHPOINT_LV_UP, type.ToString().ToLower());
			TechPointInfo orDefault = DolocConfig.Tables.TbTechPoint.GetOrDefault(type);
			if (orDefault != null && orDefault.UseLevelTip)
			{
				uiSystem.GetEntity<LevelTipRenderer>().Raise(levelData.Icon, levelData.CurrentLevel);
			}
		}
	}

	public static void AddBattleExp(int exp)
	{
		if (archiveHandle.AddTechExp(TechPointType.BATTLE, exp, out var levelData))
		{
			uiSystem.GetEntity<LevelTipRenderer>().Raise(levelData.Icon, levelData.CurrentLevel);
		}
	}

	public static void AddTechPoint(TechPointType type, int pt)
	{
		archiveHandle.AddTechPoint(type, pt);
	}

	public static void UpgradeMaxHealth(int value)
	{
		if (archiveHandle.farmData.agentData.UpgradeHealth(value))
		{
			uiSystem.agentStatusBar.UpdateHealth();
		}
	}

	public static void UpgradeMaxEnergy(int value)
	{
		archiveHandle.farmData.agentData.UpgradeEnergy(value);
		uiSystem.agentStatusBar.UpdateEnergy();
	}

	public static void UpgradeMaxSpirit(int value)
	{
		archiveHandle.farmData.agentData.UpgradeSpirit(value);
		uiSystem.agentStatusBar.UpdateSpirit();
	}

	public static void PrevLine()
	{
		uiSystem.inventoryQuick.PrevLine();
	}

	public static void NextLine()
	{
		uiSystem.inventoryQuick.NextLine();
	}

	public static void ResetQuickInventorySelection()
	{
		uiSystem.inventoryQuick.ResetSelection();
	}

	public static Item GenerateItem(CountItem countItem)
	{
		return GenerateItem(countItem.itemName, countItem.itemCount);
	}

	public static Item GenerateItem(string name, int count = 1)
	{
		ItemFactory.GenerateItem(name, count, out var item);
		return item;
	}

	public static Item GenerateItem(ItemInfo proto, int count = 1)
	{
		return ItemFactory.GenerateItem(proto, count);
	}

	public static Item PlaceItem(Item item, bool checkBox = false, bool useFade = false)
	{
		if (item == null)
		{
			return null;
		}
		int count = item.count;
		Item item2 = archiveHandle.InventorySystem.PlaceItem(item, checkBox, shouldEqualAsItem: false, useFade);
		if (item2 == null || item2.count < count)
		{
			archiveHandle.RecordCollection(CollectionType.Item | CollectionType.Product, item.name);
		}
		return item2;
	}

	public static Item PlaceItem(string name, int count = 1, bool checkBox = false, bool useFade = false)
	{
		if (ItemFactory.GenerateItem(name, count, out var item))
		{
			return PlaceItem(item, checkBox, useFade);
		}
		return null;
	}

	public static bool PlaceItemAllForce(string itemName, int count, out int overflowCount)
	{
		overflowCount = count;
		QueryItemProto(itemName, out var proto);
		if (proto == null)
		{
			return false;
		}
		for (int i = 0; i < count / proto.Overlay; i++)
		{
			Item item = PlaceItem(itemName, proto.Overlay);
			overflowCount -= proto.Overlay;
			if (item != null)
			{
				overflowCount += proto.Overlay;
				return false;
			}
		}
		if (overflowCount == 0)
		{
			return true;
		}
		overflowCount = PlaceItem(itemName, overflowCount)?.count ?? 0;
		return overflowCount == 0;
	}

	public static bool TryPlaceInBackpack(string name, int count, bool sendEmailOnOverflow = false)
	{
		ItemFactory.GenerateItem(name.ToLower(), count, out var item);
		return TryPlaceInBackpack(item, sendEmailOnOverflow);
	}

	public static bool TryPlaceInBackpack(Item item, bool sendEmailOnOverflow = false)
	{
		if (!sendEmailOnOverflow && !CanPlaceItem(item))
		{
			return false;
		}
		int count = item.count;
		Item item2 = PlaceItem(item);
		if (item2 == null)
		{
			RaiseItemObtainTip(item.name, item.uiSprite, item.title, count);
			return true;
		}
		if (sendEmailOnOverflow)
		{
			SendItemAsEmail(item2.name, item2.count);
		}
		return false;
	}

	public static int CountItem(string itemName, bool checkBox = false)
	{
		return archiveHandle.InventorySystem.GetCount(itemName, checkBox);
	}

	public static int CountItem(Item item, bool checkBox, bool shouldEqualAsItem)
	{
		return archiveHandle.InventorySystem.GetCount(item, checkBox, shouldEqualAsItem);
	}

	public static LinearInventory[] GetInventoriesAroundAgent()
	{
		Vector2Int inventoryAroundOffset = GlobalParameter.InventoryAroundOffset;
		Vector2Int inventoryAroundArea = GlobalParameter.InventoryAroundArea;
		GetAgentCellAreaAnchor(inventoryAroundOffset, inventoryAroundArea, flipWhenFaceLeft: true, out var anchor);
		return archiveHandle.GetAvailableInventories(anchor, inventoryAroundArea, userSettings.autoUseBox);
	}

	public static LinearInventory[] GetInventoriesAroundEquipment(Equipment equipment)
	{
		int equipmentBoundaryPadding = GlobalParameter.EquipmentBoundaryPadding;
		Vector2Int anchor = equipment.Anchor - new Vector2Int(equipmentBoundaryPadding, equipmentBoundaryPadding);
		Vector2Int area = equipment.CoveredSize + new Vector2Int(equipmentBoundaryPadding * 2, equipmentBoundaryPadding * 2);
		return archiveHandle.GetAvailableInventories(anchor, area, userSettings.autoUseBox);
	}

	public static LinearInventory[] GetInventoriesAroundAgent(bool useBox)
	{
		Vector2Int inventoryAroundOffset = GlobalParameter.InventoryAroundOffset;
		Vector2Int inventoryAroundArea = GlobalParameter.InventoryAroundArea;
		GetAgentCellAreaAnchor(inventoryAroundOffset, inventoryAroundArea, flipWhenFaceLeft: true, out var anchor);
		return archiveHandle.GetAvailableInventories(anchor, inventoryAroundArea, useBox);
	}

	public static LinearInventory[] GetBackpackWithInsideBoxes(bool useSharedContainer = true)
	{
		if (useSharedContainer)
		{
			return archiveHandle.GetAvailableInventories(Vector2Int.zero, Vector2Int.zero, useBox: true);
		}
		List<LinearInventory> list = new List<LinearInventory> { archiveHandle.InventorySystem.inventory };
		Item[] array = archiveHandle.InventorySystem.inventory.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemBox itemBox)
			{
				list.Add(itemBox.inventory);
			}
		}
		return list.ToArray();
	}

	public static Item GetFirstItemWithName(this LinearInventory[] inventories, string itemName)
	{
		for (int i = 0; i < inventories.Length; i++)
		{
			Item[] array = inventories[i].ReadAll();
			foreach (Item item in array)
			{
				if (item.name == itemName)
				{
					return item;
				}
			}
		}
		return null;
	}

	public static int CountItem(this LinearInventory[] inventories, string itemName)
	{
		int num = 0;
		foreach (LinearInventory linearInventory in inventories)
		{
			num += linearInventory.Count(itemName);
		}
		return num;
	}

	public static int CountItem(this LinearInventory[] inventories, Item item, bool shouldEqualAsItem)
	{
		int num = 0;
		foreach (LinearInventory linearInventory in inventories)
		{
			num += linearInventory.Count(item, shouldEqualAsItem);
		}
		return num;
	}

	public static int MaxCostItem(this LinearInventory[] inventories, string itemName, int count, int startIndex = 0)
	{
		int num = count;
		for (int i = 0; i < inventories.Length; i++)
		{
			num = inventories[i].MaxCost(itemName, num, startIndex);
		}
		return num;
	}

	public static int MaxCostItem(this LinearInventory[] inventories, Item item, int count, bool shouldEqualAsItem, int startIndex = 0)
	{
		int num = count;
		for (int i = 0; i < inventories.Length; i++)
		{
			num = inventories[i].MaxCost(item, num, shouldEqualAsItem, startIndex);
		}
		return num;
	}

	public static bool TryCostItem(this LinearInventory[] inventories, string itemName, int count)
	{
		if (inventories.CountItem(itemName) < count)
		{
			return false;
		}
		inventories.MaxCostItem(itemName, count);
		return true;
	}

	public static bool CostItem(string itemName, int count, bool checkBox = false)
	{
		return archiveHandle.InventorySystem.Cost(itemName, count, checkBox);
	}

	public static bool CostItem(Item item, int count, bool checkBox, bool shouldEqualAsItem)
	{
		return archiveHandle.InventorySystem.CostItem(item, count, checkBox, shouldEqualAsItem);
	}

	public static void CostItemNoCheck(IEnumerable<CountItem> itemList, bool checkBox = false)
	{
		foreach (CountItem item in itemList)
		{
			CostItem(item.itemName, item.itemCount, checkBox);
		}
	}

	public static void CostItemNoCheck(string name, int count, bool checkBox = false)
	{
		archiveHandle.InventorySystem.Cost(name, count, checkBox);
	}

	public static bool CostSelectedItem(int count = 1, bool showFadeUpIcon = false)
	{
		LinearInventory inventory = archiveHandle.InventorySystem.inventory;
		Item item = inventory.Read(SelectedItemIndex);
		if (item == null)
		{
			return false;
		}
		if (!archiveHandle.InventorySystem.CostAt(SelectedItemIndex, count))
		{
			return false;
		}
		if (inventory.Read(SelectedItemIndex) == null)
		{
			item.QuickDeselect();
		}
		if (showFadeUpIcon)
		{
			RaiseSpriteFadeUp(agent.PositionCenter, item.uiSprite);
		}
		return true;
	}

	public static bool CostSelectedItem(int selectedIndex, int count, bool showFadeUpIcon = false)
	{
		Item item = archiveHandle.InventorySystem.inventory.Read(selectedIndex);
		if (!archiveHandle.InventorySystem.CostAt(selectedIndex, count))
		{
			return false;
		}
		if (archiveHandle.InventorySystem.inventory.Read(selectedIndex) == null)
		{
			item?.QuickDeselect();
		}
		if (showFadeUpIcon)
		{
			RaiseSpriteFadeUp(agent.PositionCenter, item?.uiSprite);
		}
		return true;
	}

	public static bool CanPlaceItem(string name, int count)
	{
		if (ItemFactory.GenerateItem(name, count, out var item))
		{
			return archiveHandle.InventorySystem.CanPlaceItem(item);
		}
		return false;
	}

	public static bool CanPlaceItem(string name, int count, out int firstAvailableIndex)
	{
		if (ItemFactory.GenerateItem(name, count, out var item))
		{
			return archiveHandle.InventorySystem.CanPlaceItem(item, out firstAvailableIndex);
		}
		firstAvailableIndex = -1;
		return false;
	}

	public static bool CanPlaceItem(Item item)
	{
		return archiveHandle.InventorySystem.CanPlaceItem(item);
	}

	public static int MaxItemPlaceCount(string itemName)
	{
		return archiveHandle.InventorySystem.MaxItemPlaceCount(itemName);
	}

	public static bool CanPlaceItem(Item item, out int firstAvailableIndex)
	{
		return archiveHandle.InventorySystem.CanPlaceItem(item, out firstAvailableIndex);
	}

	public static bool CanAfford(CountItem[] costList, bool checkBox = false)
	{
		for (int i = 0; i < costList.Length; i++)
		{
			CountItem countItem = costList[i];
			if (CountItem(countItem.itemName, checkBox) < countItem.itemCount)
			{
				return false;
			}
		}
		return true;
	}

	public static bool CanAfford(CountItem[] costList, bool checkBox, int scale)
	{
		for (int i = 0; i < costList.Length; i++)
		{
			CountItem countItem = costList[i];
			if (CountItem(countItem.itemName, checkBox) < countItem.itemCount * scale)
			{
				return false;
			}
		}
		return true;
	}

	public static bool CostItemAt(int position, int count)
	{
		return archiveHandle.InventorySystem.CostAt(position, count);
	}

	public static void SwapItemFromInventory(int index)
	{
		archiveHandle.InventorySystem.Deposit(index);
	}

	public static void SwapOneItemFromInventory(int index)
	{
		HandleSwapFromInventory(archiveHandle.InventorySystem.inventory, index, 1);
	}

	public static void SwapHalfItemFromInventory(int index)
	{
		Item item = archiveHandle.InventorySystem.inventory.Read(index);
		int count = ((item != null) ? Mathf.Max(1, (int)((float)item.count / 2f)) : 0);
		HandleSwapFromInventory(archiveHandle.InventorySystem.inventory, index, count);
	}

	public static void SwapItemFromOutside(LinearInventory outside, int index)
	{
		archiveHandle.InventorySystem.DepositFromOutside(outside, index);
	}

	public static void SwapOneItemFromOutside(LinearInventory outside, int index)
	{
		HandleSwapFromInventory(outside, index, 1);
	}

	public static void SwapHalfItemFromOutside(LinearInventory outside, int index)
	{
		Item item = outside.Read(index);
		int count = ((item != null) ? Mathf.Max(1, (int)((float)item.count / 2f)) : 0);
		HandleSwapFromInventory(outside, index, count);
	}

	private static void HandleSwapFromInventory(LinearInventory outside, int index, int count)
	{
		SingleInventory buffer = archiveHandle.InventorySystem.buffer;
		Item item = outside.Read(index);
		if (item == null)
		{
			outside.SwapItem(index, buffer.CurrentItem);
			buffer.CurrentItem = null;
		}
		else if (buffer.IsEmpty || buffer.CurrentItem.IsSame(item))
		{
			int num = (HasBufferItem ? buffer.CurrentItem.TestCombine(count) : 0);
			int num2 = (HasBufferItem ? buffer.CurrentItem.count : 0);
			int num3 = count - num;
			buffer.CurrentItem = item.Clone(num2 + num3);
			TakeFromInventory(outside, index, num3);
		}
	}

	private static Item TakeFromInventory(LinearInventory inventory, int index, int count)
	{
		Item item = inventory.Read(index).Clone(count);
		inventory.TryCostAtIndex(index, count, shouldEqualAsItem: false, null, out var leftover);
		item.count += leftover;
		if (item.count != 0)
		{
			return item;
		}
		return null;
	}

	public static void PlaceItemFromOutside(int index, LinearInventory source, LinearInventory target)
	{
		if (source != null && target != null)
		{
			Item item = source.Take(index);
			Item item2 = target.PlaceItem(item);
			if (item2 != null)
			{
				source.SwapItem(index, item2);
			}
		}
	}

	public static int GetExistItemCount(string itemName)
	{
		Room currentRoom = archiveHandle.currentRoom;
		if (currentRoom == null)
		{
			return 0;
		}
		int num = CountItem(itemName, checkBox: true);
		IDropItemHost dropItemHost = currentRoom;
		if (dropItemHost != null)
		{
			num += dropItemHost.GetDropItemsCountByName(itemName);
		}
		foreach (Equipment allEquipment in currentRoom.DM_equipment.AllEquipments)
		{
			if (allEquipment is Case { IsShared: false } @case)
			{
				num += @case.inventory.Count(itemName);
			}
			else
			{
				if (!(allEquipment is StorageShelf { IsShared: false } storageShelf))
				{
					continue;
				}
				Item[] array = storageShelf.inventory.ReadAll();
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] is ItemBox itemBox)
					{
						num += itemBox.inventory.Count(itemName);
					}
				}
			}
		}
		return num;
	}

	public static void DisposeBufferItem()
	{
		if (!archiveHandle.InventorySystem.buffer.IsEmpty)
		{
			IDropItemHost currentRoom = archiveHandle.currentRoom;
			if (currentRoom == null)
			{
				ShowMessageBoxSmallErr(DolocConfig.StaticTexts.DisposeFailRoom);
				return;
			}
			Item item = archiveHandle.InventorySystem.buffer.Take();
			BroadcastString(GameEventType.DROP_ITEM, item.name);
			Vector2 start = AgentPosition;
			float x = agent.transform.localScale.x;
			start.y += 3f;
			currentRoom.CreateDropItem(item, start, shouldSendMsg: false, x * 5f).SetShieldCollector();
		}
	}

	public static void DisposeItem(int index)
	{
		IDropItemHost currentRoom = archiveHandle.currentRoom;
		if (currentRoom == null)
		{
			return;
		}
		Item item = archiveHandle.InventorySystem.inventory.Read(index);
		if (item != null)
		{
			if (!IsItemDisposable(item))
			{
				ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrNotDisposeItem);
				return;
			}
			BroadcastString(GameEventType.DROP_ITEM, item.name);
			Vector2 start = AgentPosition;
			float x = agent.transform.localScale.x;
			start.y += 3f;
			currentRoom.CreateDropItem(item, start, shouldSendMsg: false, x * 5f).SetShieldCollector();
			archiveHandle.InventorySystem.Take(index).QuickDeselect();
		}
	}

	public static bool DisposeItem(Item item)
	{
		if (item == null)
		{
			return false;
		}
		IDropItemHost currentRoom = archiveHandle.currentRoom;
		if (currentRoom == null)
		{
			return false;
		}
		if (!IsItemDisposable(item))
		{
			ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrNotDisposeItem);
			return false;
		}
		BroadcastString(GameEventType.DROP_ITEM, item.name);
		Vector2 start = AgentPosition;
		float x = agent.transform.localScale.x;
		start.y += 3f;
		currentRoom.CreateDropItem(item, start, shouldSendMsg: false, x * 5f).SetShieldCollector();
		return true;
	}

	public static void ThrowItem(string itemName, Vector2 position, float offset = 0f, string filterRoomName = null, bool shouldSendMsg = false)
	{
		Room currentRoom = archiveHandle.currentRoom;
		if (string.IsNullOrEmpty(filterRoomName) || !(currentRoom.Title != filterRoomName))
		{
			IDropItemHost dropItemHost = currentRoom;
			if (dropItemHost != null)
			{
				position.y += 1.5f;
				dropItemHost.CreateDropItem(itemName, position, shouldSendMsg, offset);
			}
		}
	}

	public static void ThrowItemWithCount(CountItem countItem, Vector2 position, float offset = 0f, string filterRoomName = null, bool shouldSendMsg = false)
	{
		Room currentRoom = archiveHandle.currentRoom;
		if (string.IsNullOrEmpty(filterRoomName) || !(currentRoom.Title != filterRoomName))
		{
			IDropItemHost dropItemHost = currentRoom;
			if (dropItemHost != null)
			{
				position.y += 1.5f;
				dropItemHost.CreateDropItemWithCount(countItem, position, shouldSendMsg, offset);
			}
		}
	}

	public static void DestroyItem(int index)
	{
		archiveHandle.InventorySystem.Take(index);
	}

	public static bool CanAffordMoney(int money)
	{
		return archiveHandle.CurrentMoney >= money;
	}

	public static bool SetObjectLockState(string lockObjectId, bool value)
	{
		bool result = archiveHandle.SetObjectLockState(lockObjectId, value);
		Room currentRoom = CurrentRoom;
		if (currentRoom != null)
		{
			currentRoom.SceneHandle.SetObjectLockState(lockObjectId, value);
			return result;
		}
		return result;
	}

	public static bool StartMissionChain(string missionId)
	{
		if (assets.missionChains.QueryData(missionId, out var data))
		{
			return archiveHandle.StartMissionChain(data);
		}
		return false;
	}

	public static void StartFactionMission(string missionId)
	{
		archiveHandle.StartFactionMission(missionId);
	}

	public static bool IsFactionMissionComplete(string factionMissionId)
	{
		if (factionMissionId.IsNullOrEmpty())
		{
			return false;
		}
		if (archiveHandle.cityData.factionMissionManager.QueryFactionMission(factionMissionId, out var mission))
		{
			return mission.IsComplete;
		}
		return false;
	}

	public static bool IsMissionComplete(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return false;
		}
		return archiveHandle.farmData.missionManager.IsMissionComplete(id);
	}

	public static bool IsMissionListening(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return false;
		}
		return archiveHandle.farmData.missionManager.IsMissionListening(id);
	}

	public static bool IsMissionInProcess(string missionId)
	{
		if (!IsMissionListening(missionId))
		{
			return archiveHandle.farmData.missionManager.totalMissions.Any((IMission x) => (x as Mission)?.ChainId == missionId);
		}
		return true;
	}

	public static bool AddEventDecorator(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return false;
		}
		archiveHandle.AddEventDecorator(id);
		CurrentRoom?.SceneHandle?.RefreshCondition();
		return true;
	}

	public static bool RemoveEventDecorator(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return true;
		}
		archiveHandle.RemoveEventDecorator(id);
		CurrentRoom?.SceneHandle?.RefreshCondition();
		return true;
	}

	public static bool IsEventDecoratorComplete(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			return archiveHandle.IsEventDecoratorComplete(id);
		}
		return false;
	}

	public static void SetAllNpcAutoFlipState(bool value)
	{
		if (CurrentRoom == null)
		{
			return;
		}
		foreach (Npc item in archiveHandle.cityData.npcManager.GetNpcsInScene(CurrentRoom.SceneId))
		{
			if (item?.Renderer != null)
			{
				item.Renderer.lockFlipStatus = !value;
			}
		}
	}

	public static void SetAllNpcEventState(bool value)
	{
		foreach (Npc allNpc in archiveHandle.cityData.npcManager.AllNpcs)
		{
			allNpc.disableEventFlag = !value;
			allNpc.RefreshNpcEventStatus();
		}
	}

	public static bool QueryNpc(string npcName, out Npc npc)
	{
		if (npcName == null)
		{
			npcName = string.Empty;
		}
		npcName = npcName.ToLower();
		return archiveHandle.cityData.npcManager.QueryNpc(npcName, out npc);
	}

	public static bool QueryNpcRenderer(string npcName, out NpcRenderer npcRenderer)
	{
		archiveHandle.cityData.npcManager.QueryNpc(npcName, out var data);
		npcRenderer = data?.Renderer;
		return npcRenderer != null;
	}

	public static IDialogueEntity GetDialogueTargetViewOrDefault(string targetName, bool force = false)
	{
		DialogueEntityInfo orDefault = DolocConfig.Tables.TbDialogueEntity.GetOrDefault(targetName);
		if (orDefault != null)
		{
			switch (orDefault.EntityType)
			{
			case DialogueEntityType.Player:
				return AgentRenderer;
			case DialogueEntityType.Aside:
				return new DefaultDialogueTarget();
			case DialogueEntityType.NPC:
			{
				if (QueryNpc(targetName, out var npc))
				{
					if (npc.Renderer == null && force && CurrentRoom != null)
					{
						SetNpcToCurrentScene(targetName);
						ForceReRenderNpc(npc);
					}
					if (npc.Renderer != null)
					{
						npc.Renderer.lockFlipStatus = true;
					}
					IDialogueEntity renderer = npc.Renderer;
					if (renderer != null)
					{
						return renderer;
					}
					UnityEngine.Debug.LogWarning("讲话对象不在当前场景: " + targetName);
					return new NoneDialogueTarget();
				}
				break;
			}
			}
		}
		UnityEngine.Debug.LogWarning("获取讲话对象失败: " + targetName);
		return new NoneDialogueTarget();
	}

	public static void SetDialogueTargetForeground(string targetName, bool active)
	{
		if (!targetName.IsNullOrEmpty())
		{
			Npc npc;
			if (targetName == "player")
			{
				agent.SetToForeground(active);
			}
			else if (QueryNpc(targetName, out npc))
			{
				npc.SetToForeground(active);
			}
		}
	}

	public static void RevertAllDialogueEntitiesSortingOrder()
	{
		agent.SetToForeground(active: false);
		foreach (NpcInfo data in DolocConfig.Tables.TbNpc.DataList)
		{
			if (QueryNpc(data.Id, out var npc))
			{
				npc.SetToForeground(active: false);
			}
		}
	}

	private static void ForceReRenderNpc(Npc npc)
	{
		EntitySystem.Recycle(npc.Renderer);
		RenderNpc(EntitySystem.Next<NpcRenderer>(), npc);
	}

	private static void RenderNpc(NpcRenderer renderer, Npc npc)
	{
		renderer.npc = npc;
		npc.Renderer = renderer;
	}

	public static IDialogueLineView GetDialogueLineView(string targetName)
	{
		DialogueEntityInfo orDefault = DolocConfig.Tables.TbDialogueEntity.GetOrDefault(targetName);
		if (orDefault == null)
		{
			return uiSystem.GetEntity<BubbleDialoguePanel>();
		}
		if (orDefault.EntityType == DialogueEntityType.Aside)
		{
			return uiSystem.GetEntity<AsideDialoguePanel>();
		}
		return uiSystem.GetEntity<BubbleDialoguePanel>();
	}

	public static string GetDefaultNpcNameForNode(string nodeName)
	{
		return archiveHandle.cityData.dialogueManager.GetDefaultNpcNameForNode(nodeName);
	}

	public static bool AddDialogueNode(string nodeName, string npcName = null)
	{
		if (nodeName.IsNullOrEmpty())
		{
			return false;
		}
		if (string.IsNullOrEmpty(npcName))
		{
			npcName = GetDefaultNpcNameForNode(nodeName);
		}
		return archiveHandle.AddDialogueNode(nodeName, npcName);
	}

	public static bool StartDialogueNode(string nodeName, string npcName = null)
	{
		if (nodeName.IsNullOrEmpty())
		{
			return false;
		}
		UnityEngine.Debug.Log("播放动画" + nodeName);
		if (gameManager.gameInitConfig.skipStartDialogue)
		{
			UnityEngine.Debug.LogError("跳过本次对话播放<" + nodeName + ">：系统配置中设置了跳过动画播放！");
			return true;
		}
		if (string.IsNullOrEmpty(npcName))
		{
			npcName = GetDefaultNpcNameForNode(nodeName);
		}
		return gameStateManager.dialogState.AppendDialogue(nodeName, npcName);
	}

	public static bool SetDialogueEntrance(string nodeName, string npcName = null)
	{
		if (string.IsNullOrEmpty(npcName))
		{
			npcName = GetDefaultNpcNameForNode(nodeName);
		}
		return archiveHandle.SetDialogueEntrance(nodeName, npcName);
	}

	public static bool CheckDialogueEvent(string npcName)
	{
		return archiveHandle.QueryDialogueHostHasEventNow(npcName);
	}

	public static bool RemoveDialogueNode(string nodeName, string npcName = null)
	{
		if (string.IsNullOrEmpty(npcName))
		{
			npcName = GetDefaultNpcNameForNode(nodeName);
		}
		return archiveHandle.RemoveDialogueNode(nodeName, npcName);
	}

	public static MarkupParseResult ParseMarkup(string text)
	{
		return archiveHandle.cityData.dialogueManager.ParseMarkup(text);
	}

	public static string GetTextWithMarkup(MarkupParseResult markupLine)
	{
		return archiveHandle.cityData.dialogueManager.GetTextWithMarkup(markupLine);
	}

	public static void AppendDialogueHistoryLine(string npcName, string npcTitle, string content)
	{
		archiveHandle.cityData.dialogueManager.historyManager.AppendLine(npcName, npcTitle, content);
		uiSystem.dialogueHistoryTip.TryShow();
	}

	public static void AppendDialogueHistoryOption(string content)
	{
		archiveHandle.cityData.dialogueManager.historyManager.AppendOption(content);
		uiSystem.dialogueHistoryTip.TryShow();
	}

	public static bool WaitToEnterFestivalState(string festivalName)
	{
		if (festivalName.IsNullOrEmpty())
		{
			return false;
		}
		return gameStateManager.festivalState.AppendFestival(festivalName);
	}

	public static void WaitToExitFestivalState()
	{
		userInput.WaitToPopState(gameStateManager.festivalState);
	}

	public static void InitNpcInFestival(string npcName)
	{
		gameStateManager.festivalState.InitNpcInFestival(npcName);
	}

	public static bool EquipHat(ItemHat hatItem, out Item oldHat)
	{
		if (hatItem == null)
		{
			oldHat = archiveHandle.TakeOffHat();
			if (oldHat == null)
			{
				return false;
			}
			agent.SetHatInfo();
			Motor.driverRenderer.SetHatInfo();
			UnityEngine.Debug.Log("帽子<" + oldHat?.name + ">已取下");
			return true;
		}
		if (!archiveHandle.EquipHat(hatItem, out oldHat))
		{
			UnityEngine.Debug.LogWarning("目标道具不是帽子，无法装备");
			return false;
		}
		HatInfo valueOrDefault = DolocConfig.Tables.TbHat.DataMap.GetValueOrDefault(hatItem.name);
		if (valueOrDefault == null)
		{
			UnityEngine.Debug.LogWarning("<color=red>帽子\"" + hatItem.name + "\"渲染信息丢失</color>");
		}
		agent.SetHatInfo(valueOrDefault);
		Motor.driverRenderer.SetHatInfo(valueOrDefault);
		UnityEngine.Debug.Log("帽子<" + hatItem.name + ">装备成功");
		return true;
	}

	public static bool EquipHat(string hatName, out Item oldHat)
	{
		if (hatName.IsNullOrEmpty() || !QueryItemProto(hatName, out var proto))
		{
			oldHat = archiveHandle.TakeOffHat();
			if (oldHat == null)
			{
				return false;
			}
			agent.SetHatInfo();
			Motor.driverRenderer.SetHatInfo();
			UnityEngine.Debug.Log("帽子<" + oldHat?.name + ">已取下");
			return true;
		}
		Item item = GenerateItem(proto);
		if (!archiveHandle.EquipHat(item, out oldHat))
		{
			UnityEngine.Debug.LogWarning("目标道具不是帽子，无法装备");
			return false;
		}
		HatInfo valueOrDefault = DolocConfig.Tables.TbHat.DataMap.GetValueOrDefault(hatName);
		if (valueOrDefault == null)
		{
			UnityEngine.Debug.LogWarning("<color=red>帽子\"" + hatName + "\"渲染信息丢失</color>");
		}
		agent.SetHatInfo(valueOrDefault);
		Motor.driverRenderer.SetHatInfo(valueOrDefault);
		UnityEngine.Debug.Log("帽子<" + proto.Id + ">装备成功");
		return true;
	}

	public static bool EquipDrone(Item item, out Item oldDrone)
	{
		if (item == null)
		{
			oldDrone = archiveHandle.TakeOffDrone();
			if (oldDrone == null)
			{
				return false;
			}
			UnloadDrone();
			uiSystem.inventoryQuick.RefreshDroneItem();
			return true;
		}
		if (archiveHandle.EquipDrone(item, out oldDrone))
		{
			RunDrone(((ItemDroneStructure)item).droneStructure);
			uiSystem.inventoryQuick.RefreshDroneItem();
			Broadcast(GameEventType.EQUIP_DRONE);
			Broadcast(OperationEventType.EQUIP_DRONE);
			return true;
		}
		oldDrone = null;
		return false;
	}

	public static bool EquipActiveItem(Item item, out Item oldItem)
	{
		if (!archiveHandle.EquipActiveItem(item, out oldItem))
		{
			return false;
		}
		uiSystem.inventoryQuick.RefreshPositiveItem();
		Broadcast(GameEventType.EQUIP_ACTIVE_ITEM);
		Broadcast(OperationEventType.EQUIP_ACTIVE_ITEM);
		return true;
	}

	public static bool EquipPassiveItem(Item item, out Item oldItem)
	{
		if (!archiveHandle.EquipPassiveItem(item, out oldItem))
		{
			return false;
		}
		Broadcast(GameEventType.EQUIP_PASSIVE_ITEM);
		Broadcast(OperationEventType.EQUIP_PASSIVE_ITEM);
		return true;
	}

	public static bool EquipPassiveItem1(Item item, out Item oldItem)
	{
		if (!archiveHandle.EquipPassiveItem1(item, out oldItem))
		{
			return false;
		}
		Broadcast(GameEventType.EQUIP_PASSIVE_ITEM);
		Broadcast(OperationEventType.EQUIP_PASSIVE_ITEM);
		return true;
	}

	public static bool EquipPassiveItem2(Item item, out Item oldItem)
	{
		if (!archiveHandle.EquipPassiveItem2(item, out oldItem))
		{
			return false;
		}
		Broadcast(GameEventType.EQUIP_PASSIVE_ITEM);
		Broadcast(OperationEventType.EQUIP_PASSIVE_ITEM);
		return true;
	}

	public static bool IsEquippedDrone(Item item)
	{
		return archiveHandle.farmData.agentData.agentEquipment.IsEquippedDrone(item);
	}

	public static bool IsEquippedActive(Item item)
	{
		return archiveHandle.farmData.agentData.agentEquipment.IsEquippedActive(item);
	}

	public static bool IsEquippedPassive1(Item item)
	{
		return archiveHandle.farmData.agentData.agentEquipment.IsEquippedPassive1(item);
	}

	public static bool IsEquippedPassive2(Item item)
	{
		return archiveHandle.farmData.agentData.agentEquipment.IsEquippedPassive2(item);
	}

	public static bool RefreshStore(string storeName)
	{
		if (archiveHandle.QueryStore(storeName, out var store))
		{
			store.Refresh();
			return true;
		}
		return false;
	}

	public static bool UnlockStoreItem(string storeName, string itemName)
	{
		if (archiveHandle.QueryStore(storeName, out var store))
		{
			store.UnlockStoreItem(itemName);
			return true;
		}
		return false;
	}

	public static void UnlockMotor(float offset = 0f)
	{
		if (!archiveHandle.IsMotorUnlocked())
		{
			archiveHandle.UnlockMotor();
			Motor.SetVisible(value: true);
			Motor.position = AgentPosition + new Vector3(0f, offset, 0f);
		}
	}

	public static void SetMotorPosition(Room room, Vector2 position)
	{
		if (archiveHandle.IsMotorUnlocked())
		{
			if (room == null)
			{
				room = CurrentRoom;
			}
			archiveHandle.UpdateMotorRoom(room);
			Motor.Reset();
			Motor.position = position;
			Motor.SetVisible(room == CurrentRoom);
		}
	}

	public static void ResetMotorStatus()
	{
		if (IsDataLoaded && archiveHandle.IsMotorUnlocked())
		{
			gameStateManager.agentController.GetOffIfRiding();
		}
	}

	public static bool QueryTreatyPortFaction(string factionName, out TreatyPortFaction faction)
	{
		return archiveHandle.cityData.treatyPortFactionManager.QueryTreatyPortFaction(factionName, out faction);
	}

	public static bool QueryFactionJoinState(FactionType factionType)
	{
		return archiveHandle.cityData.treatyPortFactionManager.QueryFactionJoinState(factionType);
	}

	public static bool CreateMissionItem(string missionItemId, string markPointId)
	{
		MarkPointInfo orDefault = DolocConfig.Tables.TbMarkPoint.GetOrDefault(markPointId);
		if (orDefault != null && QueryRoom(orDefault.RoomId, out var room))
		{
			IMissionItemHost missionItemHost = room;
			if (missionItemHost != null)
			{
				MissionItemInfo orDefault2 = DolocConfig.Tables.TbMissionItem.GetOrDefault(missionItemId);
				bool result = missionItemHost.CreateMissionItemNoRender(orDefault2, orDefault);
				if (orDefault.RoomId == archiveHandle.currentRoom.RoomId)
				{
					missionItemHost.ClearAndRenderAllMissionItems();
				}
				return result;
			}
		}
		return false;
	}

	public static void SetRoomEnvObjectStatus(string roomName, bool status)
	{
		if (QueryRoom(roomName, out var room))
		{
			((IEnvObjectHost)room)?.SetEnvObjectStatus(status);
		}
	}

	public static void TryGiftItemToNpc(string targetNpc, Item item, int itemIndex)
	{
		if (item != null && QueryNpc(targetNpc, out var _))
		{
			Delay(0.2f, delegate
			{
				RaiseSpriteFadeUp(AgentPosition + new Vector3(0f, 2f, 0f), item.uiSprite);
			});
			archiveHandle.cityData.likingManager.ReceiveGift(targetNpc, item.name, IsNpcBirthday(targetNpc, archiveHandle.DateNow));
			CostItemAt(itemIndex, 1);
			BroadcastString(GameEventType.GIFT_ITEM, targetNpc + "@" + item.name);
		}
	}

	public static bool QueryNpcLikingInfo(string npcName, out Liking liking)
	{
		return archiveHandle.cityData.likingManager.QueryNpcLikingInfo(npcName, out liking);
	}

	public static int QueryGiftLevel(string npcName, string itemName)
	{
		return archiveHandle.cityData.likingManager.QueryGiftLevel(npcName, itemName);
	}

	public static void AddTargetNpcLikingValue(string npcName, float value)
	{
		archiveHandle.cityData.likingManager.AddTargetNpcLikingValue(npcName, value);
	}

	public static int QueryNpcLikingLv(string npcName)
	{
		return archiveHandle.cityData.likingManager.QueryNpcLikingLv(npcName);
	}

	public static bool QueryNpcBirthdayByName(string npcName, out int month, out int day)
	{
		return archiveHandle.cityData.calendarManager.QueryNpcBirthdayByName(npcName, out month, out day);
	}

	public static string QueryNpcBirthdayByDate(int month, int day)
	{
		return archiveHandle.cityData.calendarManager.QueryNpcBirthdayByDate(month, day);
	}

	public static bool IsNpcBirthday(string npcName, DateInfo dateInfo)
	{
		if (npcName.IsNullOrEmpty())
		{
			return false;
		}
		return QueryNpcBirthdayByDate(dateInfo.Month, dateInfo.Day) == npcName;
	}

	public static string QueryMemoByDay(int day)
	{
		archiveHandle.cityData.calendarManager.QueryMemoByDay(day, out var content);
		return content;
	}

	public static bool IsNpcAtMarkPoint(string npcName, string markPointName, float threshold = 5f)
	{
		if (npcName.IsNullOrEmpty() || markPointName.IsNullOrEmpty())
		{
			return false;
		}
		return archiveHandle.cityData.npcManager.IsNpcAtMarkPoint(npcName, markPointName, threshold);
	}

	public static bool SendEmail(string emailName, bool allowRepeat = true)
	{
		if (!allowRepeat && archiveHandle.farmData.emailManager.ContainsEmailName(emailName))
		{
			return false;
		}
		return archiveHandle.farmData.emailManager.SendEmail(emailName);
	}

	public static bool SendItemAsEmail(string itemName, int count, string emailName = null, string content = null, string sender = null, string templateName = "send_item_template")
	{
		return archiveHandle.farmData.emailManager.SendItemAsEmail(itemName, count, emailName, content, sender, templateName);
	}

	public static IEnumerable<Npc> GetNpcsInScene(int sceneIndex)
	{
		return archiveHandle.GetNpcsInScene(sceneIndex);
	}

	public static bool IsNpcAtScene(string npcName, string sceneName)
	{
		if (npcName == "player")
		{
			return CurrentRoom?.SceneRawName == sceneName;
		}
		if (!QueryNpc(npcName, out var npc))
		{
			return false;
		}
		if (npc.IsAtVoidScene)
		{
			return string.IsNullOrEmpty(sceneName);
		}
		return npc.sceneName == sceneName;
	}

	public static bool SetNpcToMarkPoint(string npcName, string markPointName)
	{
		MarkPointInfo orDefault = DolocConfig.Tables.TbMarkPoint.GetOrDefault(markPointName);
		if (orDefault == null)
		{
			UnityEngine.Debug.LogError("标记点点<" + markPointName + ">不存在，传送npc<" + npcName + ">失败");
			return false;
		}
		if (!SceneUtils.IsSceneExist(orDefault.SceneRawName))
		{
			UnityEngine.Debug.LogError("场景<" + orDefault.SceneRawName + ">不存在，传送npc<" + npcName + ">失败");
			return false;
		}
		if (!QueryNpc(npcName, out var npc))
		{
			return false;
		}
		npc.ManualSetPosition(orDefault.SceneRawName, orDefault.Position);
		return true;
	}

	public static bool MoveNpcToVoid(string npcName)
	{
		return archiveHandle.MoveNpcToScene(npcName, string.Empty);
	}

	public static bool SetNpcToCurrentScene(string npcName)
	{
		return archiveHandle.MoveNpcToScene(npcName, archiveHandle.currentRoom.SceneRawName);
	}

	public static bool SetNpcToScene(string npcName, string sceneName)
	{
		return archiveHandle.MoveNpcToScene(npcName, sceneName);
	}

	public static void EnableNpcSchedule(string npcName)
	{
		if (QueryNpc(npcName ?? "", out var npc))
		{
			npc.EnableSchedule();
		}
	}

	public static void DisableNpcSchedule(string npcName, string markPointId)
	{
		if (QueryNpc(npcName ?? "", out var npc))
		{
			npc.DisableSchedule(markPointId);
		}
	}

	public static bool IsInPlayerScene(string sceneName)
	{
		if (archiveHandle.currentRoom == null)
		{
			return false;
		}
		return archiveHandle.currentRoom.SceneRawName == sceneName;
	}

	public static void OnWakeUp(bool saveData, bool sendEvent, bool clearRecoveryDecayBuffs = false)
	{
		if (sendEvent)
		{
			Broadcast(GameEventType.WAKE_UP);
		}
		if (clearRecoveryDecayBuffs)
		{
			archiveHandle.farmData.agentData.OnSpiritReset();
		}
		BuffManager.RefreshUI();
		uiSystem.GuidanceTips.Clear();
		if (archiveHandle.timeData.IsFirstTimeWakeUpOnDayPassed())
		{
			FirstTimeWakeUpOnDayPassed();
			archiveHandle.MainFarm.animalSystem.OnDayChanged();
		}
		if (saveData)
		{
			SaveGame(archiveHandle.archiveIndex);
		}
		Sound.OnWeatherChange(archiveHandle.WeatherSystem.WeatherType);
		Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_SLEEP);
	}

	private static void FirstTimeWakeUpOnDayPassed()
	{
		if (CurrentRoom == archiveHandle.MainFarm)
		{
			archiveHandle.farmData.agentData.hasComeoutAfterSleep = true;
			Broadcast(GameEventType.FIRST_COMEOUT_AFTER_SLEEP);
			UnityEngine.Debug.Log("玩家睡觉之后第一次出门了..");
		}
		else
		{
			archiveHandle.farmData.agentData.hasComeoutAfterSleep = false;
		}
		archiveHandle.dungeonData.__ReGenDungeonDatas();
		foreach (CityRoom item in archiveHandle.cityData.cityRooms.Values.Where((CityRoom room) => room != CurrentRoom))
		{
			item.ReGenRoomDatas();
		}
	}

	[Command("set_time_scale")]
	public static void SetTimeScale(float timeScale, bool showTipInMiddle = false)
	{
		timeScaleManager.SetTimeScale(timeScale, showTipInMiddle);
	}

	[Command("revert_time_scale")]
	public static void RevertTimeScale()
	{
		timeScaleManager.RevertTimeScale();
	}

	public static bool CheckResourceUnlocked(string resourceId)
	{
		return archiveHandle.dungeonData.resourceManager.CheckResourceUnlocked(resourceId);
	}

	public static bool CheckFishUnlocked(string fishId)
	{
		return archiveHandle.dungeonData.resourceManager.CheckFishUnlocked(fishId);
	}

	public static bool CheckVegetationUnlocked(string vegetationId)
	{
		return archiveHandle.dungeonData.resourceManager.CheckVegetationUnlocked(vegetationId);
	}

	public static void UIRaiseConfirm()
	{
		Sound.PostSoundEvent(SoundEvents.PLAY_UI_CONFIRM);
	}

	public static void UIRaiseCancel()
	{
		Sound.PostSoundEvent(SoundEvents.PLAY_UI_CANCEL);
	}

	public static void UIRaiseError()
	{
		Sound.PostSoundEvent(SoundEvents.PLAY_UI_ERROR);
	}

	public static void UIRaiseRoll()
	{
		Sound.PostSoundEvent(SoundEvents.PLAY_UI_SELECT);
	}

	public static void UIRaisePopUp()
	{
		Sound.PostSoundEvent(SoundEvents.PLAY_UI_POP_UP);
	}

	public static void UIRaisePopDown()
	{
		Sound.PostSoundEvent(SoundEvents.PLAY_UI_POP_DOWN);
	}

	public static void UIRaisePage()
	{
		Sound.PostSoundEvent(SoundEvents.PLAY_UI_PAGE);
	}

	public static void SetResidentUiInteractable(bool value)
	{
		uiSystem.basicTip.EnableInteract(value);
		uiSystem.inventoryQuick.EnableInteract(value);
	}

	public static void SetResidentUiVisible(bool showBasicTip, bool showQuickInventory)
	{
		SetBasicTipVisible(showBasicTip);
		SetQuickInventoryVisible(showQuickInventory);
	}

	public static void SetBasicTipVisible(bool value)
	{
		if (!IsDataLoaded || !value)
		{
			uiSystem.basicTip.Hide();
		}
		else
		{
			uiSystem.basicTip.Show();
		}
	}

	public static void SetQuickInventoryVisible(bool value)
	{
		if (!IsDataLoaded || !value)
		{
			uiSystem.inventoryQuick.Hide();
		}
		else
		{
			uiSystem.inventoryQuick.Show();
		}
	}

	public static void ShowMessageBox(Sprite icon, string msg, float holdTime = 2f)
	{
		if (!msg.IsNullOrEmpty())
		{
			uiSystem.messageBoxManager.messageBox.Show(icon, msg, holdTime);
		}
	}

	public static void ShowMessageBoxLarge(Sprite icon, string msg, float holdTime = 2f)
	{
		if (!msg.IsNullOrEmpty())
		{
			uiSystem.messageBoxManager.messageBoxLarge.Show(icon, msg, holdTime);
		}
	}

	public static void ShowMessageBoxSmall(string msg, float holdTime = 2f)
	{
		if (!msg.IsNullOrEmpty())
		{
			uiSystem.messageBoxManager.messageBoxLittle.ShowMessage("<color=\"#fffde3\">" + msg + "</color>", holdTime);
		}
	}

	public static void ShowMessageBoxSmall(string msg, Color c, float holdTime = 2f)
	{
		if (!msg.IsNullOrEmpty())
		{
			uiSystem.messageBoxManager.messageBoxLittle.ShowMessage("<color=\"" + c.ToHex() + "\">" + msg + "</color>", holdTime);
		}
	}

	public static void ShowMessageBoxSmallErr(string msg, float holdTime = 2f, bool useSound = true)
	{
		if (!msg.IsNullOrEmpty())
		{
			bool flag = uiSystem.messageBoxManager.messageBoxLittle.ShowMessage("<color=\"" + DolocColorHex.drakRed + "\">" + msg + "</color>", holdTime);
			if (useSound && flag)
			{
				UIRaiseError();
			}
		}
	}

	public static void ShowMessageBoxInfo(string msg, float holdTime = 2f)
	{
		if (!msg.IsNullOrEmpty())
		{
			uiSystem.messageBoxManager.messageBox.Show(LocSprites.UI_INFOICON_ATTENTION, msg, holdTime);
		}
	}

	public static void ShowMessageBoxErr(string msg, float holdTime = 2f, bool useSound = true)
	{
		if (!msg.IsNullOrEmpty())
		{
			bool flag = uiSystem.messageBoxManager.messageBox.Show(LocSprites.UI_INFOICON_ERROR, msg, holdTime);
			if (useSound && flag)
			{
				UIRaiseError();
			}
		}
	}

	public static void ShowMessageBoxNode(Sprite icon, string msg)
	{
		if (!msg.IsNullOrEmpty())
		{
			uiSystem.messageBoxManager.messageBoxNode.Show(icon, msg);
		}
	}

	public static void ShowMessageBoxNodeError(string msg, bool useSound = true)
	{
		if (!msg.IsNullOrEmpty())
		{
			uiSystem.messageBoxManager.messageBoxNode.Show(LocSprites.UI_INFOICON_ERROR_24PX, msg);
			if (useSound)
			{
				Sound.PostSoundEvent(SoundEvents.PLAY_UI_ERROR_02);
			}
		}
	}

	public static void ShowMessageBoxNodeComplete(string msg)
	{
		if (!msg.IsNullOrEmpty())
		{
			uiSystem.messageBoxManager.messageBoxNode.Show(LocSprites.UI_INFOICON_COMPLETE_24PX, msg);
		}
	}

	public static void ShowMessageBoxAttention(string msg, float holdTime = 2f)
	{
		if (!msg.IsNullOrEmpty())
		{
			uiSystem.messageBoxManager.messageBoxCool.Show(msg ?? string.Empty, holdTime);
		}
	}

	public static void ShowMessageBoxRollCall(string msg, string subTitle)
	{
		uiSystem.messageBoxManager.messageBoxRollCall.Show(msg ?? string.Empty, subTitle ?? string.Empty);
	}

	public static void ShowMessageBoxInSceneWaiter(string msg, Vector2 pos, float durShow = 0.5f, float durWiat = 1f, Ease ease = Ease.OutBack)
	{
		if (!msg.IsNullOrEmpty())
		{
			uiSystem.messageBoxManager.messageBoxInScene.ShowAsWaiter(msg, pos, durShow, durWiat, ease);
		}
	}

	public static Action ShowMessageBoxInSceneManual(string msg, Vector2 pos, float durShow = 0.5f, Ease ease = Ease.OutBack)
	{
		if (msg.IsNullOrEmpty())
		{
			return delegate
			{
			};
		}
		return uiSystem.messageBoxManager.messageBoxInScene.ShowAsManual(msg, pos, durShow, ease);
	}

	public static void ShowMessageBoxInSceneWithIconWaiter(string msg, Sprite icon, Vector2 pos, float durShow = 0.5f, float durWiat = 1f, Ease ease = Ease.OutBack)
	{
		if (!msg.IsNullOrEmpty())
		{
			uiSystem.messageBoxManager.messageBoxInSceneWithIcon.ShowAsWaiter(msg, icon, pos, durShow, durWiat, ease);
		}
	}

	public static Action ShowMessageBoxInSceneWithIconManual(string msg, Sprite icon, Vector2 pos, float durShow = 0.5f, Ease ease = Ease.OutBack)
	{
		if (msg.IsNullOrEmpty())
		{
			return delegate
			{
			};
		}
		return uiSystem.messageBoxManager.messageBoxInSceneWithIcon.ShowAsManual(msg, icon, pos, durShow, ease);
	}

	public static void ShowTextByConfig(string tipId)
	{
		TextTipInfo orDefault = DolocConfig.Tables.TbTextTip.GetOrDefault(tipId ?? "");
		if (orDefault == null)
		{
			return;
		}
		AlignmentText content = orDefault.TipArgs.Content;
		TipTextArgsBase tipArgs = orDefault.TipArgs;
		if (!(tipArgs is UISmallMessageBoxArgs uISmallMessageBoxArgs))
		{
			if (!(tipArgs is UIBigMessageBoxArgs uIBigMessageBoxArgs))
			{
				if (!(tipArgs is UITextConfirmBoxArgs uITextConfirmBoxArgs))
				{
					if (!(tipArgs is UINodeMessageBoxArgs uINodeMessageBoxArgs))
					{
						if (tipArgs is SceneTipArgs)
						{
							UnityEngine.Debug.LogError("暂不支持显示场景文本提示");
						}
					}
					else
					{
						ShowMessageBoxNode(uINodeMessageBoxArgs.Icon.Asset, content.Text);
					}
				}
				else
				{
					ShowConfirmBox(uITextConfirmBoxArgs.Title, uITextConfirmBoxArgs.Content, uITextConfirmBoxArgs.Signature, null);
				}
			}
			else
			{
				ShowMessageBoxLarge(uIBigMessageBoxArgs.Icon.Asset, content.Text);
			}
		}
		else if (uISmallMessageBoxArgs.ErrorStyle)
		{
			ShowMessageBoxSmallErr(content.Text);
		}
		else
		{
			ShowMessageBoxSmall(content.Text);
		}
	}

	public static void AddResidentMissionTip(string id, bool useSound)
	{
		uiSystem.MissionTips.AddMissionTip(id, useSound);
	}

	public static void RemoveResidentMissionTip(string id)
	{
		uiSystem.MissionTips.FinishMission(id);
	}

	public static void RefreshResidentMissionTip(string id)
	{
		uiSystem.MissionTips.RefreshTip(id);
	}

	public static void ClearAllMissionTip()
	{
		uiSystem.MissionTips.Clear();
	}

	public static void RaiseItemObtainTip(string itemName, Sprite icon, string title, int count = 1, bool useSound = true)
	{
		bool flag = uiSystem.GetEntity<DropItemPickTipManager>().RaiseTip(itemName + "-" + title, icon, title, count);
		if (useSound && flag)
		{
			Sound.PostSoundEvent(SoundEvents.PLAY_ITEM_PICK_UP);
		}
	}

	public static void ShowQuestionBox(string title, Action onConfirm = null, Action onCancel = null, bool firstSelectConfirm = true)
	{
		gameUiStates.EnterUI((QuestionUiState state) => state.HandleStartUpArgs(title, onConfirm, onCancel, firstSelectConfirm));
	}

	public static void ShowConfirmBox(string content, Action onConfirm = null)
	{
		gameUiStates.EnterUI((ConfirmUiState state) => state.HandleStartUpArgs(content, onConfirm));
	}

	public static void ShowConfirmBox(AlignmentText title, AlignmentText content, AlignmentText signature, Action onConfirm)
	{
		gameUiStates.EnterUI((ConfirmUiState state) => state.HandleStartUpArgs(title, content, signature, onConfirm));
	}

	public static void ShowPendingBox(string text)
	{
		gameUiStates.EnterUI((PendingUiState state) => state.HandleStartUpArgs(text));
	}

	public static void HidePendingBox()
	{
		if (gameUiStates.HasState<PendingUiState>())
		{
			PendingUiState state = gameUiStates.GetState<PendingUiState>();
			if (state != null)
			{
				userInput.TryPopState(state);
			}
		}
	}

	public static void ShowSceneTextBox(Vector2 worldPosition, SceneTextBoxArgs sceneTextArgs, Transform followedTrans = null, float duration = 3f)
	{
		uiSystem.sceneBoxGroup.RenderAndShow(worldPosition, sceneTextArgs, followedTrans, duration);
	}

	public static void HideSceneBox()
	{
		uiSystem.GetEntity<SceneBoxGroup>().Hide();
	}

	public static void OpenSubmitSingleItemPanel(Func<Item, bool> itemFilter, Func<bool> submitConditionChecker = null, Func<Item, int> itemSubmitCountGetter = null, Func<Item, int, string> confirmTextGetter = null, Action<int> onFailedSubmit = null, Action<int, int> onSubmit = null)
	{
		EnterUI((GeneralSubmitUiState state) => state.HandleStartUpArgs(itemFilter, submitConditionChecker, itemSubmitCountGetter, confirmTextGetter, onFailedSubmit, onSubmit));
	}

	public static void RefreshQuickInventory()
	{
		archiveHandle.InventorySystem.inventory.ReEmit();
	}

	public static void RefreshQuickInventory(int index)
	{
		archiveHandle.InventorySystem.inventory.ReEmit(index);
	}

	public static void RefreshQuickInventorySelected()
	{
		RefreshQuickInventory(SelectedItemIndex);
	}

	public static T EnterUI<T>() where T : DolocGameUiState
	{
		return gameUiStates.EnterUI<T>();
	}

	public static T EnterUI<T>(Func<T, bool> handleStartUpArgs) where T : DolocGameUiState
	{
		return gameUiStates.EnterUI(handleStartUpArgs);
	}

	public static void RemoveUiState<T>() where T : DolocGameUiState
	{
		gameUiStates.RemoveUI<T>();
	}

	public static bool OpenStore(string storeId)
	{
		if (string.IsNullOrEmpty(storeId))
		{
			return false;
		}
		EnterUI((StoreUiState state) => state.HandleStartUpArgs(storeId));
		return true;
	}

	public static T OpenPanel<T>() where T : DolocUIPanel
	{
		T entity = uiSystem.GetEntity<T>();
		entity.Show();
		return entity;
	}

	public static T HidePanel<T>() where T : DolocUIPanel
	{
		T entity = uiSystem.GetEntity<T>();
		entity.Hide();
		return entity;
	}

	public static void ClearSceneOperationTips()
	{
		uiSystem.sceneOperationTipManager.Clear();
		uiSystem.sceneBoxGroup.Hide();
	}

	public static void SetSceneOperationTipEnabled(bool value)
	{
		uiSystem.SetSceneUIVisible(value);
	}

	public static void ShowSceneDialogueBox(Vector2 worldPosition, string content, Action callback = null)
	{
		SceneDialogueBox dialogueBox = uiSystem.GetFromPoolInScene<SceneDialogueBox>();
		dialogueBox.SetDialogShowPosition(worldPosition);
		dialogueBox.Say(content);
		dialogueBox.callback = delegate
		{
			Delay(2f, delegate
			{
				uiSystem.RecycleToPoolInScene(dialogueBox);
			});
			try
			{
				callback?.Invoke();
			}
			catch (Exception ex)
			{
				outputError("对话框回调函数执行错误<" + ex.Message + ">");
			}
		};
	}

	public static void AddBuffIcon(Buff buff)
	{
		BuffInfo proto = buff.Proto;
		uiSystem.BuffTip.Add(proto, buff.Timer.currentTick);
		if (proto.EffectType == BuffEffectType.Default)
		{
			Vector2 vector = AgentPosition + new Vector3(0f, 1.5f);
			RaiseSpriteFadeUp(vector, proto.IconNormal.Asset);
			RaiseInstantPSEffects(vector, InstantParticleEffectsType.SPARKS);
		}
	}

	public static void RemoveBuffIcon(string id)
	{
		uiSystem.BuffTip.Remove(id);
	}

	public static void UpdateBuffProgress(string id, int duration)
	{
		uiSystem.BuffTip.UpdateProgress(id, duration);
	}

	public static void InvokeSceneResidentTip(string id, string customPrompt = null)
	{
		Room currentRoom = CurrentRoom;
		if (currentRoom == null)
		{
			UnityEngine.Debug.LogWarning("当前房间为空，无法触发场景提示");
			return;
		}
		ISceneHandle sceneHandle = currentRoom.LoadSceneHandle();
		if (sceneHandle == null)
		{
			UnityEngine.Debug.LogWarning("当前房间没有场景处理器，无法触发场景提示");
		}
		else
		{
			sceneHandle.InvokeResidentTip(id, customPrompt);
		}
	}

	public static void InvokeSceneResidentTip(string id, Vector2 position, string customPrompt = null)
	{
		CurrentRoom?.SceneHandle?.InvokeResidentTip(id, position, customPrompt);
	}

	public static void HideSceneResidentTip(string id)
	{
		CurrentRoom?.SceneHandle?.HideResidentTip(id);
	}

	public static void ClearSceneResidentTips()
	{
		CurrentRoom?.SceneHandle?.ClearResidentTip();
	}

	public static void RefreshSceneResidentTipText()
	{
		CurrentRoom?.SceneHandle?.RefreshLocalizationText();
	}

	public static void OpenMap(Room room = null)
	{
		if (room == null)
		{
			room = CurrentRoom;
		}
		DolocTown.Config.Room.SceneInfo sceneConfig = room.SceneConfig;
		if (sceneConfig != null)
		{
			OpenMap(sceneConfig.MapId);
		}
		else
		{
			ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiTipOpenMapFail);
		}
	}

	public static bool CanOpenMap(Room room = null)
	{
		return room?.SceneConfig?.MapId_Ref != null;
	}

	public static void OpenMap(string mapId)
	{
		if (!mapId.IsNullOrEmpty())
		{
			EnterUI((CollectionBookUiState state) => state.HandleMapArgs(mapId));
		}
	}

	public static SmallTextMenuUiState ShowSmallTextMenu(string[] titles, Vector2 screenPosition, Action<string> onConfirm)
	{
		return EnterUI((SmallTextMenuUiState state) => state.HandleStartUpArgs(titles, screenPosition, onConfirm));
	}

	public static void GetItemBorder(this DolocUiObject obj, BorderType borderType = BorderType.ThinBorder)
	{
		itemBorder.HoverTo(obj, useAnimation: true, borderType);
	}

	public static void GetItemBorder(this RectTransform obj, BorderType borderType = BorderType.ThinBorder)
	{
		itemBorder.HoverTo(obj, useAnimation: true, borderType);
	}

	public static void HideItemBorder(this DolocUiObject obj)
	{
		itemBorder.Hide();
	}

	public static void HideItemBorder()
	{
		itemBorder.Hide();
	}

	public static void SetItemBorderVisible(bool value)
	{
		if (value)
		{
			itemBorder.Show();
		}
		else
		{
			itemBorder.Hide();
		}
	}

	public static void HoverItemViewer(this DolocUiObject obj, ItemData itemData, UIAlignmentType targetAnchor = UIAlignmentType.LeftTop, UIAlignmentType hoverPivot = UIAlignmentType.LeftBottom)
	{
		obj.rectTransform.HoverItemViewer(itemData, targetAnchor, hoverPivot);
	}

	public static void HoverItemViewer(this RectTransform rectTransform, ItemData itemData, UIAlignmentType targetAnchor = UIAlignmentType.LeftTop, UIAlignmentType hoverPivot = UIAlignmentType.LeftBottom)
	{
		if (itemData.notEmpty)
		{
			HoverBoxGroup.RenderAndShow(rectTransform, itemData, targetAnchor, hoverPivot);
		}
	}

	public static void HoverTextSmall(this DolocUiObject obj, string text, UIAlignmentType targetAnchor = UIAlignmentType.TopMiddle, UIAlignmentType hoverPivot = UIAlignmentType.BottomMiddle, bool autoFade = false, HoverBoxStyle style = HoverBoxStyle.Default, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
	{
		obj.rectTransform.HoverTextSmall(text, targetAnchor, hoverPivot, autoFade, style, alignment);
	}

	public static void HoverTextSmall(this RectTransform rectTransform, string text, UIAlignmentType targetAnchor = UIAlignmentType.TopMiddle, UIAlignmentType hoverPivot = UIAlignmentType.BottomMiddle, bool autoFade = false, HoverBoxStyle style = HoverBoxStyle.Default, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
	{
		if (!text.IsNullOrEmpty())
		{
			HoverBoxGroup.RenderAndShowText(rectTransform, text, targetAnchor, hoverPivot, autoFade, style, alignment);
		}
	}

	public static void HoverText(this DolocUiObject obj, TextGroup data, UIAlignmentType targetAnchor = UIAlignmentType.TopMiddle, UIAlignmentType hoverPivot = UIAlignmentType.BottomMiddle)
	{
		if (data.notEmpty)
		{
			HoverBoxGroup.RenderAndShowText(obj.rectTransform, data, targetAnchor, hoverPivot);
		}
	}

	public static void HideHoverBox(this DolocUiObject obj)
	{
		HoverBoxGroup.Hide();
	}

	public static void HideHoverBox(this RectTransform obj)
	{
		HoverBoxGroup.Hide();
	}

	public static void HideHoverBox()
	{
		HoverBoxGroup.Hide();
	}

	public static void ShowSleepMenu(Action onEnd)
	{
		EnterUI((SleepUiState state) => state.HandleSleepStartUpArgs(onEnd));
	}

	public static void JumpTechTreeNode(string treeId, string nodeName)
	{
		DelayFrame(delegate
		{
			EnterUI((TechTreeUiState state) => state.HandleStartUpArgs(treeId, nodeName));
		});
	}

	public static string GetParsedKeystrokeText(string text)
	{
		return GetParsedKeystrokeText(UserInput.DeviceType, text);
	}

	public static string GetParsedKeystrokeText(DolocInputDeviceType deviceType, string text)
	{
		if (text.IsNullOrEmpty())
		{
			return text;
		}
		while (!ActionTagRegex.Match(text).Value.IsNullOrEmpty())
		{
			string value = ActionTagRegex.Match(text).Value;
			if (!GetAllActionKeyIconGroup(deviceType, value, out var iconGroups))
			{
				return text;
			}
			string newValue;
			if (DolocConfig.Tables.TbGameKeyAction.GetOrDefault(value).EnableCombined)
			{
				string[] urls = (from x in iconGroups
					select x.largeIconUrl into x
					where !x.IsNullOrEmpty()
					select x).ToArray();
				if (DolocConfig.Tables.TbGameCombinedKeyIcon.TryMatch(urls, out var asset))
				{
					newValue = "<sprite name=\"" + asset.AssetUrl + "_small\"> ";
					text = text.Replace("<action=" + value + "/>", newValue);
					break;
				}
			}
			newValue = "<sprite name=\"" + iconGroups.First().smallIconUrl + "\"> ";
			text = text.Replace("<action=" + value + "/>", newValue);
		}
		return text;
	}

	[Command("cls", Desc = "清空控制台")]
	private static void Command_ConsoleClear()
	{
		devHelper.Console.ClearOutput();
	}

	[Command("lua", Desc = "运行Lua脚本")]
	private static void Command_ComplexCommand(string luaScript)
	{
		if (string.IsNullOrEmpty(luaScript))
		{
			outputError("Lua脚本不能为空");
		}
		else
		{
			RunLua(luaScript);
		}
	}

	[Command("print")]
	private static void Command_Print(string content)
	{
		UnityEngine.Debug.Log(content);
	}

	[Command("querycmd", Desc = "查询控制台中目标命令的委托对象")]
	private static void Command_QueryCommandDelegate(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			outputError("命令名不能为空");
			return;
		}
		Delegate commandFunction = GetCommandFunction(name);
		if ((object)commandFunction == null)
		{
			outputError("未找到命令<" + name + ">");
		}
		else
		{
			output("命令<" + name + ">的委托对象: " + commandFunction.Method.GetMethodInfo());
		}
	}

	[Command("v2i")]
	private static Vector2Int Command_Vector2Int(int x, int y)
	{
		return new Vector2Int(x, y);
	}

	[Command("v2")]
	private static Vector2 Command_Vector2(float x, float y)
	{
		return new Vector2(x, y);
	}

	[Command("list_all_devices", Desc = "列出所有输入设备")]
	private static void Command_ListAllDevices()
	{
		foreach (InputDevice device in InputSystem.devices)
		{
			UnityEngine.Debug.Log($"{device.displayName}, {device.name}, {device.shortDisplayName}, {device.deviceId}");
		}
	}

	[Command("disable_gate", Desc = "禁用指定房间的指定传送门")]
	private static void Command_DisableGate(string gateName)
	{
		if (DisableGate(gateName))
		{
			outputSuccess("传送门<" + gateName + ">已禁用");
		}
		else
		{
			outputError("传送门<" + gateName + ">禁用失败");
		}
	}

	[Command("enable_gate", Desc = "启用指定房间的指定传送门")]
	private static void Command_EnableGate(string gateName)
	{
		if (EnableGate(gateName))
		{
			outputSuccess("传送门<" + gateName + ">已启用");
		}
		else
		{
			outputError("传送门<" + gateName + ">启用失败");
		}
	}

	[Command("view_disabled_gate", Desc = "查看指定房间的禁用传送门列表")]
	private static void Command_ViewDisabledGates()
	{
		if (archiveHandle.cityData.gateManager.DisableGateList.Length == 0)
		{
			outputError("当前没有禁用的传送门");
			return;
		}
		string[] disableGateList = archiveHandle.cityData.gateManager.DisableGateList;
		foreach (string text in disableGateList)
		{
			output("<" + text + ">", DolocColorHex.lightGreen);
		}
	}

	[Command("is_npc_at_mark_point", Desc = "检查Npc是否处于指定的标记点附近")]
	private static bool Command_IsNpcAtMarkPoint(string npcName, string markName, float threshold = 5f)
	{
		return IsNpcAtMarkPoint(npcName, markName, threshold);
	}

	[Command("unlock_interactable", Desc = "根据给定id解锁交互对象")]
	private static void Command_UnlockInteractableObject(string lockObjectId)
	{
		SetObjectLockState(lockObjectId, value: false);
	}

	[Command("lock_interactable", Desc = "根据给定id锁定交互对象")]
	private static void Command_LockInteractableObject(string lockObjectId)
	{
		SetObjectLockState(lockObjectId, value: true);
	}

	[Command("unlock_all_interactables", Desc = "Debug: 解锁全部可交互对象")]
	private static void Command_UnlockAllInteractableObject()
	{
		foreach (string key in DolocConfig.Tables.TbLockableObject.DataMap.Keys)
		{
			SetObjectLockState(key, value: false);
		}
	}

	[Command("do_npc_schedule")]
	private static void Command_GiveNpcTask(string npcName, string scheduleName)
	{
		NpcScheduleGraph data;
		if (string.IsNullOrEmpty(npcName) || !QueryNpc(npcName, out var npc))
		{
			outputError("无法找到npc:\"" + npcName + "\"");
		}
		else if (string.IsNullOrEmpty(scheduleName) || !assets.npcSchedules.QueryData(scheduleName, out data))
		{
			UnityEngine.Debug.LogError("无法找到npc行为图\"{scheduleName}\"");
		}
		else
		{
			npc.DebugSchedule(data);
		}
	}

	[Command("start_process", Desc = "启动指定的游戏流程")]
	private static void Command_StartProcess(string processName)
	{
		if (string.IsNullOrEmpty(processName))
		{
			UnityEngine.Debug.LogError("流程名不得为空");
		}
		else if (!RunGameProcess(processName))
		{
			UnityEngine.Debug.LogError("流程\"" + processName + "\"启动失败");
		}
	}

	[Command("stop_process", Desc = "终止指定的游戏流程")]
	private static void Command_StopProcess(string processName)
	{
		if (string.IsNullOrEmpty(processName))
		{
			UnityEngine.Debug.LogError("流程名不得为空");
		}
		else
		{
			StopGameProcess(processName);
		}
	}

	[Command("hide_all_npcs_at_scene", Desc = "隐藏当前场景的所有npc")]
	private static void Command_HideAllNpcAtScene()
	{
		foreach (Npc allNpc in archiveHandle.cityData.npcManager.AllNpcs)
		{
			if (allNpc.IsRenderNow && allNpc.Renderer != null)
			{
				allNpc.Renderer = null;
			}
		}
	}

	[Command("show_all_npcs_at_scene", Desc = "显示当前场景的所有npc")]
	private static void Command_ShowAllNpcAtScene()
	{
		foreach (Npc allNpc in archiveHandle.cityData.npcManager.AllNpcs)
		{
			if (allNpc.sceneName == CurrentRoom.SceneRawName && allNpc.Renderer == null)
			{
				ForceReRenderNpc(allNpc);
			}
		}
	}

	[Command("set_drone_visible_state", Desc = "设置是否允许显示无人机")]
	private static void Command_SetDroneVisibleState(bool value)
	{
		gameStateManager.agentController.droneController.ForceHide = !value;
	}

	[Command("set_npc_free_acting_state", Desc = "设置npc是否会自由转向/弹出表情")]
	private static void Command_SetNpcFreeActingState(bool value)
	{
		foreach (Npc allNpc in archiveHandle.cityData.npcManager.AllNpcs)
		{
			allNpc.disableFreeActing = !value;
		}
	}

	[Command("enter_dungeon", Desc = "进入地牢")]
	private static void Command_EnterDungeon(string dungeonName)
	{
		if (dungeonName.IsNullOrWhitespace())
		{
			outputError("地牢名不能为空");
		}
		else if (!EnterDungeon(dungeonName))
		{
			outputError("进入失败");
		}
	}

	[Command("gen_monster", Desc = "生成怪物")]
	private static void Command_GenerateMonster(string monsterName, int count = 1)
	{
		IMonsterHost currentRoom = archiveHandle.currentRoom;
		if (currentRoom == null)
		{
			outputError("当前房间不支持生成怪物");
			return;
		}
		if (count <= 0)
		{
			outputError("生成数量必须大于0");
			return;
		}
		if (!assets.monsters.QueryMonster(monsterName, out var proto))
		{
			outputError("怪物\"" + monsterName + "\"不存在");
			return;
		}
		for (int i = 0; i < count; i++)
		{
			currentRoom.GenerateMonster(proto);
		}
	}

	[Command("gen_monster_at", Desc = "在指定位置生成怪物")]
	private static void Command_GenerateMonsterAt(string monsterName, float x, float y)
	{
		IMonsterHost currentRoom = archiveHandle.currentRoom;
		if (currentRoom == null)
		{
			outputError("当前房间不支持生成怪物");
			return;
		}
		if (!assets.monsters.QueryMonster(monsterName, out var proto))
		{
			outputError("怪物\"" + monsterName + "\"不存在");
			return;
		}
		Vector2 roomPosition = CurrentRoom.Geometry.roomPosition;
		currentRoom.GenerateMonster(proto, new Vector2(x, y) + roomPosition);
	}

	[Command("gen_all_monster", Desc = "所有怪物各生成一只")]
	private static void Command_GenMonsterAll()
	{
		IMonsterHost currentRoom = CurrentRoom;
		if (currentRoom == null)
		{
			UnityEngine.Debug.Log("当前房间不支持生成怪物");
			return;
		}
		foreach (MonsterProto totalProto in assets.monsters.TotalProtos)
		{
			currentRoom.GenerateMonster(totalProto);
		}
	}

	[Command("clear_monster", Desc = "移除当前房间的所有怪物")]
	private static void Command_ClearMonsters(string targetRoom = null)
	{
		if (targetRoom != null)
		{
			Room room = GetRoom(targetRoom);
			if (room == null)
			{
				UnityEngine.Debug.LogError("指定房间\"" + targetRoom + "\"不存在");
			}
			else
			{
				((IMonsterHost)room)?.ClearMonsters();
			}
		}
		else if (CurrentRoom != null)
		{
			((IMonsterHost)CurrentRoom).ClearMonsters();
		}
	}

	[Command("refresh_monsters", Desc = "刷新怪物")]
	private static void Command_RefreshMonster(string sceneName = null)
	{
		IMonsterHost monsterHost = (sceneName.IsNullOrEmpty() ? CurrentRoom : GetRoom(sceneName));
		if (monsterHost != null)
		{
			monsterHost.ClearMonsters();
			monsterHost.GenerateMonstersData();
			monsterHost.RunMonsters();
		}
	}

	[Command("refresh_vegetation", Desc = "刷新房间的植被")]
	private static void Command_RefreshVegetation(string sceneName = null)
	{
		IVegetationHost vegetationHost = (sceneName.IsNullOrEmpty() ? CurrentRoom : GetRoom(sceneName));
		if (vegetationHost != null)
		{
			vegetationHost.ClearAllVegetation();
			vegetationHost.GenerateVegetation(randomGrowthLevel: true);
			vegetationHost.RenderAllVegetation();
		}
	}

	[Command("get_resource_count_by_class", Desc = "获取指定房间指定资源类型资源的数量")]
	private static int Command_GetResourceCountByClass(string resourceClass, string roomId = null, bool needRender = false)
	{
		IDungeonResourceHost dungeonResourceHost = (roomId.IsNullOrEmpty() ? CurrentRoom : GetRoom(roomId));
		if (dungeonResourceHost == null)
		{
			return 0;
		}
		DungeonResourceClass dungeonResourceClass = resourceClass.ConvertToEnumOrDefault<DungeonResourceClass>();
		int num = 0;
		foreach (DungeonResource allDungeonResource in dungeonResourceHost.DM_dungeonResource.AllDungeonResources)
		{
			if (allDungeonResource.Proto.ResourceClass == dungeonResourceClass && (!needRender || allDungeonResource.isRender))
			{
				num++;
			}
		}
		return num;
	}

	[Command("refresh_resources", Desc = "刷新房间的资源")]
	private static void Command_RefreshResource(string roomId = null)
	{
		Room room = (roomId.IsNullOrEmpty() ? CurrentRoom : GetRoom(roomId));
		IDungeonResourceHost dungeonResourceHost = room;
		if (dungeonResourceHost != null)
		{
			dungeonResourceHost.ClearAllResources(useEffect: false);
			dungeonResourceHost.GenerateDungeonResource_DungeonMode(refreshPresets: true);
			if (room.isRenderNow)
			{
				dungeonResourceHost.RenderAllResources();
			}
		}
	}

	[Command("refresh_waters", Desc = "刷新房间的水体")]
	private static void Command_RefreshWater(string roomId = null)
	{
		(roomId.IsNullOrEmpty() ? CurrentRoom : GetRoom(roomId)).LoadSceneHandle().RefreshWaters();
	}

	[Command("clear_resource_at", Desc = "清除指定位置的资源")]
	private static void Command_ClearResource(int x, int y, string roomId = null)
	{
		Room room = (roomId.IsNullOrEmpty() ? CurrentRoom : GetRoom(roomId));
		IDungeonResourceHost dungeonResourceHost = room;
		if (dungeonResourceHost == null)
		{
			return;
		}
		Vector2Int anchor = new Vector2Int(x, y);
		DungeonResource[] resourcesAt = dungeonResourceHost.GetResourcesAt(anchor, 1);
		foreach (DungeonResource resource in resourcesAt)
		{
			if (room.isRenderNow)
			{
				dungeonResourceHost.RemoveDungeonResource(resource);
			}
			else
			{
				dungeonResourceHost.RemoveDungeonResourceNoRender(resource);
			}
		}
	}

	[Command("clear_all_resources", Desc = "清除指定房间的所有资源")]
	private static void Command_ClearAllResources(string roomId = null)
	{
		((IDungeonResourceHost)(roomId.IsNullOrEmpty() ? CurrentRoom : GetRoom(roomId)))?.ClearAllResources(useEffect: false);
	}

	[Command("clear_all_vegetation", Desc = "清除指定房间的所有植被")]
	private static void Command_ClearAllVegetation(string roomId = null)
	{
		((IVegetationHost)(roomId.IsNullOrEmpty() ? CurrentRoom : GetRoom(roomId)))?.ClearAllVegetation();
	}

	[Command("clear_all_resources_by_type", Desc = "清除指定房间指定类型的资源")]
	private static void Command_ClearResourceByType(string type, string roomId = null)
	{
		if (!Enum.TryParse<DungeonResourceType>(type, ignoreCase: true, out var resourceType))
		{
			return;
		}
		Room room = (roomId.IsNullOrEmpty() ? CurrentRoom : GetRoom(roomId));
		IDungeonResourceHost dungeonResourceHost = room;
		if (dungeonResourceHost == null)
		{
			return;
		}
		DungeonResource[] array = room.DM_dungeonResource.AllDungeonResources.Where((DungeonResource x) => x.ResourceType == resourceType).ToArray();
		foreach (DungeonResource resource in array)
		{
			if (room.isRenderNow)
			{
				dungeonResourceHost.RemoveDungeonResource(resource);
			}
			else
			{
				dungeonResourceHost.RemoveDungeonResourceNoRender(resource);
			}
		}
	}

	[Command("force_create_resource_at", Desc = "在指定位置强制生成指定资源(慎用)")]
	private static void Command_ReplaceResource(string resourceId, int x, int y, string roomId = null)
	{
		Room room = (roomId.IsNullOrEmpty() ? CurrentRoom : GetRoom(roomId));
		if (room != null)
		{
			ResourceInfo orDefault = DolocConfig.Tables.TbResource.GetOrDefault(resourceId);
			if (orDefault != null)
			{
				ReplaceResource(room, orDefault, new Vector2Int(x, y));
			}
		}
	}

	private static void ReplaceResource(Room room, ResourceInfo proto, Vector2Int pos)
	{
		if (room == null || proto == null)
		{
			return;
		}
		DungeonResource[] resourcesAt = ((IDungeonResourceHost)room).GetResourcesAt(pos, proto.Width);
		foreach (DungeonResource dungeonResource in resourcesAt)
		{
			if (dungeonResource?.LayerMask == proto.TerrainLayer)
			{
				if (room.isRenderNow)
				{
					((IDungeonResourceHost)room).RemoveDungeonResource(dungeonResource);
				}
				else
				{
					((IDungeonResourceHost)room).RemoveDungeonResourceNoRender(dungeonResource);
				}
			}
		}
		DungeonResource dungeonResource2 = ((IDungeonResourceHost)room).CreateDungeonResourceNoRender(pos, proto);
		if (dungeonResource2 != null && room.isRenderNow)
		{
			((IDungeonResourceHost)room).RenderResource((IEnumerable<DungeonResource>)new DungeonResource[1] { dungeonResource2 });
		}
	}

	[Command("refresh_resources_by_type", Desc = "刷新房间内指定类型的资源(数量和位置不变，仅更换实体类型)")]
	public static void Command_RefreshResourceByType(string type, string roomId = null)
	{
		if (Enum.TryParse<DungeonResourceType>(type, ignoreCase: true, out var result))
		{
			RefreshResourceByType(roomId.IsNullOrEmpty() ? CurrentRoom : GetRoom(roomId), result);
		}
	}

	public static void RefreshResourceByType(Room room, DungeonResourceType type)
	{
		if (room == null)
		{
			return;
		}
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>();
		DungeonResource[] array = ((IDungeonResourceHost)room).DM_dungeonResource.AllDungeonResources.ToArray();
		int i;
		for (i = 0; i < array.Length; i++)
		{
			DungeonResource dungeonResource = array[i];
			if (dungeonResource != null && dungeonResource.Proto?.ResourceType == type)
			{
				hashSet.Add(dungeonResource.Anchor);
				if (room.isRenderNow)
				{
					((IDungeonResourceHost)room).RemoveDungeonResource(dungeonResource);
				}
				else
				{
					((IDungeonResourceHost)room).RemoveDungeonResourceNoRender(dungeonResource);
				}
			}
		}
		if (hashSet.Count == 0)
		{
			return;
		}
		Dictionary<ResourceSpawnData, int> dictionary = room.RoomSpawnInfo.ResourceSpawnEntry.SpawnLut_Ref.SpawnResources(hashSet.Count, archiveHandle.DateNow, (ResourceSpawnData x) => x.ResourceId_Ref.ResourceType == type);
		Vector2Int[] array2 = hashSet.ToArray().Shuffle();
		int num = 0;
		foreach (KeyValuePair<ResourceSpawnData, int> item in dictionary)
		{
			item.Deconstruct(out var key, out i);
			ResourceSpawnData resourceSpawnData = key;
			int num2 = i;
			for (int j = 0; j < num2; j++)
			{
				if (num < array2.Length)
				{
					ReplaceResource(room, resourceSpawnData.ResourceId_Ref, array2[num++]);
				}
			}
		}
	}

	[Command("refresh_world_vegetation", Desc = "刷新世界所有植被(农场除外)")]
	private static void RefreshWorldVegetation()
	{
		foreach (CityRoom value in archiveHandle.cityData.cityRooms.Values)
		{
			((IVegetationHost)value)?.ReGenRoomVegetation(value.isRenderNow);
		}
		foreach (Dungeon item in archiveHandle.dungeonData.dungeonManager.totalDungeons.ToList())
		{
			foreach (DungeonRoom allRoom in item.AllRooms)
			{
				((IVegetationHost)allRoom).ReGenRoomVegetation(allRoom.isRenderNow);
			}
		}
	}

	[Command("unlock_guaranteed_item", Desc = "根据给定id解锁资源掉落中的全局锁定/上限/保底道具")]
	private static void Command_UnlockGuaranteedItem(string itemName, string type = "resource")
	{
		Enum.TryParse<GuaranteedType>(type, ignoreCase: true, out var result);
		archiveHandle.dungeonData.guaranteedManager.UnlockGuaranteedItem(result, itemName);
	}

	[Command("unlock_resource", Desc = "根据给定id解锁资源")]
	private static void Command_UnlockResource(string resourceName)
	{
		archiveHandle.dungeonData.resourceManager.UnlockResource(resourceName);
	}

	[Command("unlock_fish", Desc = "根据id解锁指定的鱼")]
	private static void Command_UnlockFish(string fishId)
	{
		archiveHandle.dungeonData.resourceManager.UnlockFish(fishId);
	}

	[Command("lock_fish", Desc = "锁定指定的鱼")]
	private static void Command_LockFish(string fishId)
	{
		archiveHandle.dungeonData.resourceManager.LockFish(fishId);
	}

	[Command("get_obtained_unique_fish_count", Desc = "获取已发现的鱼的种类数")]
	private static int Command_GetObtainedUniqueFishCount()
	{
		return archiveHandle.cityData.documentManager.fishDocMgr.GetObtainedUniqueFishCount();
	}

	[Command("unlock_vegetation", Desc = "根据给定id解锁植被")]
	private static void Command_UnlockVegetation(string vegetationName)
	{
		archiveHandle.dungeonData.resourceManager.UnlockVegetation(vegetationName);
	}

	[Command("lock_vegetation", Desc = "根据给定id锁定植被")]
	private static void Command_LockVegetation(string vegetationName)
	{
		archiveHandle.dungeonData.resourceManager.LockVegetation(vegetationName);
	}

	[Command("unlock_all_fish", Desc = "解锁全部的鱼")]
	private static void Command_UnlockAllFish()
	{
		foreach (FishInfo data in DolocConfig.Tables.TbFish.DataList)
		{
			archiveHandle.dungeonData.resourceManager.UnlockFish(data.Id);
		}
	}

	[Command("unlock_all_vegetation", Desc = "解锁全部植被")]
	private static void Command_UnlockAllVegetation()
	{
		foreach (VegetationInfo data in DolocConfig.Tables.TbVegetation.DataList)
		{
			archiveHandle.dungeonData.resourceManager.UnlockVegetation(data.Id);
		}
	}

	[Command("gen_dungeon_resource", Desc = "在当前位置生成地牢资源")]
	private static void Command_GenerateDungeonResource(string name)
	{
		ResourceInfo value;
		if (name.IsNullOrEmpty())
		{
			outputError("资源名不能为空");
		}
		else if (!DolocConfig.Tables.TbResource.DataMap.TryGetValue(name, out value))
		{
			outputError("资源\"" + name + "\"不存在");
		}
		else
		{
			((IDungeonResourceHost)CurrentRoom)?.CreateDungeonResource(value);
		}
	}

	[Command("gen_vegetation", Desc = "在当前位置生成植被")]
	private static void Command_GenerateVegetation(string name)
	{
		if (name.IsNullOrEmpty())
		{
			outputError("植被名不能为空");
			return;
		}
		if (!DolocConfig.Tables.TbVegetation.DataMap.TryGetValue(name, out var value))
		{
			outputError("植被\"" + name + "\"不存在");
			return;
		}
		IVegetationHost currentRoom = CurrentRoom;
		if (currentRoom != null)
		{
			Vegetation vegetation = currentRoom.CreateVegetationNoRender(value, randomGrowthLevel: true);
			if (vegetation != null)
			{
				currentRoom.RenderVegetation(EntitySystem.Next<VegetationRenderer>(), vegetation);
			}
		}
	}

	[Command("upgrade_farm", Desc = "扩展农场到下一等级(如果可以升级的话)")]
	private static bool Command_UpgradeFarm()
	{
		archiveHandle.GetFarmNextLevelProto(out var levelProto);
		if (levelProto == null)
		{
			return false;
		}
		return ExtendFarm(levelProto.Id);
	}

	[Command("set_farm_by_level", Desc = "扩展农场(指定等级)")]
	private static bool Command_SetFarmByLevel(int level)
	{
		FarmLevelInfo byLevel = DolocConfig.Tables.TbFarmLevel.GetByLevel(level);
		if (byLevel == null)
		{
			return false;
		}
		return ExtendFarm(byLevel.Id);
	}

	[Command("get_current_farm_level", Desc = "获取当前农场等级")]
	private static int Command_GetCurrentFarmLevel()
	{
		return archiveHandle.farmLevel;
	}

	[Command("can_farm_upgrade", Desc = "查询农场是否可以继续升级")]
	private static bool CanFarmUpgrade()
	{
		return archiveHandle.CanFarmUpgrade();
	}

	[Command("get_farm_upgrade_cost", Desc = "查询升级到下一等级农场的花费")]
	private static int GetFarmUpgradeCost()
	{
		archiveHandle.GetFarmNextLevelProto(out var levelProto);
		return levelProto?.UpgradeCost ?? 0;
	}

	[Command("get_farm_init_mark_point", Desc = "查询当前农场初始位置")]
	private static string GetFarmInitMarkPoint()
	{
		return archiveHandle.GetFarmCurrentLevelProto()?.InitMarkPoint ?? "";
	}

	[Command("set_crop_level", Desc = "手动设置当前选中的作物的生长阶段")]
	private static void Command_SetCropLevel(int lv, bool all = false)
	{
		IEquipmentHost currentRoom = archiveHandle.currentRoom;
		if (currentRoom == null)
		{
			outputError("当前房间不是设备宿主");
		}
		else if (all)
		{
			PlantBasin[] equipments = currentRoom.GetEquipments<PlantBasin>();
			for (int i = 0; i < equipments.Length; i++)
			{
				equipments[i].Crop?.DEBUG_SetLevel(shouldRender: true, lv);
			}
			PlantBasinTree[] equipments2 = currentRoom.GetEquipments<PlantBasinTree>();
			for (int i = 0; i < equipments2.Length; i++)
			{
				equipments2[i].Crop?.DEBUG_SetLevel(lv);
			}
			PlantBasinGrass[] equipments3 = currentRoom.GetEquipments<PlantBasinGrass>();
			for (int i = 0; i < equipments3.Length; i++)
			{
				equipments3[i].DEBUG_SetLevel(lv);
			}
		}
		else
		{
			Equipment selectedEquipment = SelectedEquipment;
			if (selectedEquipment == null)
			{
				outputError("当前没有选中的设备");
			}
			else if (selectedEquipment is PlantBasin plantBasin)
			{
				plantBasin.Crop?.DEBUG_SetLevel(shouldRender: true, lv);
			}
			else if (selectedEquipment is PlantBasinTree plantBasinTree)
			{
				plantBasinTree.Crop?.DEBUG_SetLevel(lv);
			}
			else if (selectedEquipment is PlantBasinGrass plantBasinGrass)
			{
				plantBasinGrass.DEBUG_SetLevel(lv);
			}
			else
			{
				outputError("当前选中的设备不是种植盆");
			}
		}
	}

	[Command("plant_crop", Desc = "为当前种植盆种植农作物")]
	private static void Command_PlantCrop(string cropName, bool all = false)
	{
		IEquipmentHost currentRoom = archiveHandle.currentRoom;
		SeedInfo proto;
		if (currentRoom == null)
		{
			outputError("当前房间不是设备宿主");
		}
		else if (!QuerySeedProto(cropName, out proto))
		{
			UnityEngine.Debug.LogError("作物\"" + cropName + "\"不存在");
		}
		else if (!(GenerateItem(cropName) is ItemSeed seed))
		{
			UnityEngine.Debug.LogError("作物\"" + cropName + "\"不是种子道具");
		}
		else if (all)
		{
			PlantBasin[] equipments = currentRoom.GetEquipments<PlantBasin>();
			foreach (PlantBasin plantBasin in equipments)
			{
				if (!plantBasin.IsPlanted)
				{
					plantBasin.Plant(seed, shouldRender: true);
				}
			}
		}
		else
		{
			Equipment selectedEquipment = SelectedEquipment;
			if (selectedEquipment == null)
			{
				outputError("当前没有选中的设备");
			}
			else if (selectedEquipment is PlantBasin plantBasin2)
			{
				plantBasin2.Plant(seed, shouldRender: true);
			}
			else
			{
				outputError("当前选中的设备不是种植盆");
			}
		}
	}

	[Command("remove_crop", Desc = "移除当前或所有种植盆中的作物对象，不分阶段")]
	private static void Command_RemoveCrop(bool all = false)
	{
		if (all)
		{
			PlantBasin[] equipments = CurrentRoom.DM_equipment.GetEquipments<PlantBasin>();
			for (int i = 0; i < equipments.Length; i++)
			{
				equipments[i].ClearCrop();
			}
		}
		else if (!(SelectedEquipment is PlantBasin plantBasin))
		{
			UnityEngine.Debug.LogError("当前没有选中设备或者设备不是种植盆");
		}
		else
		{
			plantBasin.ClearCrop();
		}
	}

	[Command("clear_crop", Desc = "指定当前或者所有种植盆，移除其中的农作物")]
	private static void Command_ClearCrop(bool all = false)
	{
		IEquipmentHost currentRoom = archiveHandle.currentRoom;
		if (currentRoom == null)
		{
			outputError("当前房间不是设备宿主");
			return;
		}
		if (all)
		{
			PlantBasin[] equipments = currentRoom.GetEquipments<PlantBasin>();
			for (int i = 0; i < equipments.Length; i++)
			{
				equipments[i].Harvest(putInBackpack: false, sendMessage: false);
			}
			return;
		}
		Equipment selectedEquipment = SelectedEquipment;
		if (selectedEquipment == null)
		{
			outputError("当前没有选中的设备");
		}
		else if (selectedEquipment is PlantBasin plantBasin)
		{
			plantBasin.Harvest(putInBackpack: false, sendMessage: false);
		}
		else
		{
			outputError("当前选中的设备不是种植盆");
		}
	}

	[Command("show_selected_building_info", Desc = "显示当前选中的建筑的信息")]
	private static void Command_ShowSelectedBuildingInfo()
	{
		if (archiveHandle.currentRoom == null)
		{
			outputError("当前房间不是建筑宿主");
			return;
		}
		Building selectedBuilding = SelectedBuilding;
		if (selectedBuilding == null)
		{
			outputError("当前没有选中的建筑");
		}
		else
		{
			ShowDebugInfos(selectedBuilding);
		}
	}

	[Command("show_current_room_outdoor_weather_info", Desc = "显示当前房间的室外天气信息")]
	private static void Command_ShowCurrentRoomOutdoorWeatherInfo()
	{
		if (archiveHandle.currentRoom is TemplateRoomInHouse templateRoomInHouse)
		{
			output($"当前房间的天气为:{templateRoomInHouse.CurrentWeatherInfo}");
		}
		else
		{
			outputError("当前房间不是室内房间");
		}
	}

	[Command("invoke_affector", Desc = "手动启动当前房间的所有影响器,可选参数为sprinkler, farm_light, sound")]
	private static void Command_InvokeAffector(string key)
	{
		Room currentRoom = archiveHandle.currentRoom;
		if (currentRoom == null)
		{
			outputError("当前房间为空");
			return;
		}
		IEquipmentHost currentRoom2 = archiveHandle.currentRoom;
		if (currentRoom2 == null)
		{
			outputError("当前房间<" + currentRoom.Title + ">不是设备宿主");
			return;
		}
		if (string.IsNullOrEmpty(key))
		{
			outputError("未指定影响器类型");
			return;
		}
		switch (key)
		{
		case "sprinkler":
			currentRoom2.DEBUG_ManualInvokeAffectors<Sprinkler>();
			break;
		case "farm_light":
			currentRoom2.DEBUG_ManualInvokeAffectors<FarmLight>();
			break;
		case "sound":
			UnityEngine.Debug.LogError("暂无音响设备");
			break;
		default:
			outputError("未知影响器类型<" + key + ">");
			break;
		}
	}

	[Command("clear_all_equipment_in_selected_room", Desc = "清除当前选中建筑内的所有设备")]
	private static void Command_ClearAllEquipmentInSelectedRoom()
	{
		Building selectedBuilding = SelectedBuilding;
		if (selectedBuilding == null)
		{
			outputError("当前没有选中的建筑");
		}
		else
		{
			((IEquipmentHost)selectedBuilding.room).RemoveAllEquipment();
		}
	}

	[Command("query_items", Desc = "查询包含指定字符串的道具")]
	private static void Command_QueryItems(string name)
	{
		if (name.IsNullOrEmpty())
		{
			return;
		}
		IEnumerable<ItemInfo> enumerable = DolocConfig.Tables.TbItem.DataList.Where((ItemInfo x) => x.Id.Contains(name));
		if (!enumerable.Any())
		{
			outputError("没有找到包含\"" + name + "\"的道具");
			return;
		}
		foreach (ItemInfo item in enumerable)
		{
			output(item.Id + "(" + item.Description + ")", DolocColor.orange);
		}
	}

	[Command("set_animal_adult", Desc = "改变当前选中的小动物的成年状态")]
	private static void Commnad_SetAnimalAdult(bool isAdult)
	{
		Animal currentAnimal = CurrentAnimal;
		if (currentAnimal == null)
		{
			UnityEngine.Debug.LogError("当前没有选中任何小动物");
		}
		else
		{
			currentAnimal.DEBUG_SetAdult(isAdult);
		}
	}

	[Command("set_animal_husbandry", Desc = "设置当前小动物的目标产出的畜牧阈值")]
	private static void Command_SetAnimalHusbandry(Animal animal, string outputId, int value)
	{
		if (animal == null)
		{
			UnityEngine.Debug.LogError("当前没有选中任何小动物");
		}
		else
		{
			animal.DEBUG_SetHusbandryValue(outputId, value);
		}
	}

	[Command("upgrade_current_building")]
	public static void Command_ExtendBuilding()
	{
		Building selectedBuilding = SelectedBuilding;
		if (selectedBuilding == null)
		{
			UnityEngine.Debug.LogError("没有选择建筑");
		}
		else
		{
			UnityEngine.Debug.Log(selectedBuilding.Upgrade() ? "升级成功" : "升级失败");
		}
	}

	[Command("has_cellar", Desc = "检查是否拥有地窖")]
	public static bool Command_HasCellar()
	{
		return ((IBuildingHost)archiveHandle.MainFarm).FindFirstBuildingById(GlobalParameter.CellarBuildingId) != null;
	}

	[Command("can_upgrade_cellar", Desc = "检查地窖是否可以升级")]
	public static bool Command_CanUpgradeCellar()
	{
		return ((IBuildingHost)archiveHandle.MainFarm).FindFirstBuildingById(GlobalParameter.CellarBuildingId)?.canUpgrade ?? false;
	}

	[Command("upgrade_cellar", Desc = "升级地窖")]
	public static void Command_UpgradeCellar()
	{
		Building building = ((IBuildingHost)archiveHandle.MainFarm).FindFirstBuildingById(GlobalParameter.CellarBuildingId);
		if (building != null && building.canUpgrade)
		{
			building.Upgrade();
		}
	}

	[Command("pass_time", Desc = "跳过现时秒数")]
	private static void Command_PassTime(int length = 1, bool sendWakeUpEvent = true)
	{
		length = Mathf.Max(length, 0);
		archiveHandle.PassTimeNoControl(length, delegate
		{
			OnWakeUp(saveData: false, sendWakeUpEvent);
		});
	}

	[Command("pass_tu", Desc = "跳过指定的单位时间")]
	private static void Command_PassTimeTu(int length = 1, bool sendWakeUpEvent = true)
	{
		length = Mathf.Max(length, 0);
		archiveHandle.PassTimeNoControl(length * GlobalParameter.TULength, delegate
		{
			OnWakeUp(saveData: false, sendWakeUpEvent);
		});
	}

	[Command("pass_minute", Desc = "跳过游戏内分钟数")]
	private static void Command_PassMinutes(int length = 1, bool sendWakeUpEvent = true)
	{
		length = Mathf.Max(length, 0);
		archiveHandle.PassTimeNoControl(GlobalParameter.GameMinutes2Secs(length), delegate
		{
			OnWakeUp(saveData: false, sendWakeUpEvent);
		});
	}

	[Command("pass_hour", Desc = "跳过游戏内小时数")]
	private static void Command_PassHours(int length = 1, bool sendWakeUpEvent = true)
	{
		length = Mathf.Max(length, 0);
		archiveHandle.PassTimeNoControl(GlobalParameter.GameHours2Secs(length), delegate
		{
			OnWakeUp(saveData: false, sendWakeUpEvent);
		});
	}

	[Command("pass_day", Desc = "跳过指定天数")]
	private static void Command_PassDay(int length = 1, bool sendWakeUpEvent = true)
	{
		length = Mathf.Max(length, 0);
		archiveHandle.PassTimeNoControl(GlobalParameter.GameDays2Secs(length), delegate
		{
			OnWakeUp(saveData: false, sendWakeUpEvent);
		});
	}

	[Command("pass_month", Desc = "跳过指定月数")]
	private static void Command_PassMonth(int length = 1, bool sendWakeUpEvent = true)
	{
		length = Mathf.Max(length, 0);
		archiveHandle.PassTimeNoControl(GlobalParameter.GameMonths2Secs(length), delegate
		{
			OnWakeUp(saveData: false, sendWakeUpEvent);
		});
	}

	[Command("pass_year", Desc = "跳过指定年数")]
	private static void Command_PassYear(int length = 1, bool sendWakeUpEvent = true)
	{
		length = Mathf.Max(length, 0);
		archiveHandle.PassTimeNoControl(GlobalParameter.GameYears2Secs(length), delegate
		{
			OnWakeUp(saveData: false, sendWakeUpEvent);
		});
	}

	[Command("pass_time_to_target_hour", Desc = "跳到指定的小时")]
	private static void Command_PassTimeToTargetHour(int hour, bool sendWakeUpEvent = true)
	{
		DateInfo dateNow = archiveHandle.timeData.dateNow;
		int num = ((hour > dateNow.Hour) ? (hour - dateNow.Hour) : (hour + GlobalParameter.Day2Hour - dateNow.Hour)) * GlobalParameter.Hour2Min - dateNow.Minute;
		int seconds = GlobalParameter.GameMinutes2Secs(num);
		archiveHandle.PassTimeNoControl(seconds, delegate
		{
			OnWakeUp(saveData: false, sendWakeUpEvent);
		});
	}

	[Command("track_back_time", Desc = "回溯指定的时间/秒")]
	private static void Command_TrackBackTime(int seconds)
	{
		archiveHandle.TrackBackTime(seconds, delegate
		{
			OnWakeUp(saveData: false, sendEvent: true);
		});
		archiveHandle.TryRefreshEvent(isRender: false);
		uiSystem.basicTip.RefreshAll();
	}

	[Command("set_time_enabled", Desc = "暂停或恢复时间的运行，该行为不影响主角的运动")]
	private static void Command_SetTimeEnabled(bool value)
	{
		gameLoop.IsGlobalPaused = !value;
	}

	[Command("send_message", Desc = "发送一条游戏消息")]
	private static void Command_SendGameMessage(string msg)
	{
		GameEventType result;
		if (string.IsNullOrEmpty(msg))
		{
			outputError("消息不能为空");
		}
		else if (!Enum.TryParse<GameEventType>(msg, ignoreCase: true, out result))
		{
			outputError("无效的消息类型<" + msg + ">");
		}
		else
		{
			Broadcast(result);
		}
	}

	[Command("send_message_string", Desc = "发送一条带string参数的游戏消息")]
	private static void Command_SendGameMessage(string msg, string param)
	{
		GameEventType result;
		if (string.IsNullOrEmpty(msg))
		{
			outputError("消息不能为空");
		}
		else if (!Enum.TryParse<GameEventType>(msg, ignoreCase: true, out result))
		{
			outputError("无效的消息类型<" + msg + ">");
		}
		else
		{
			BroadcastString(result, param);
		}
	}

	[Command("send_message_int", Desc = "发送一条带int参数的游戏消息")]
	private static void Command_SendGameMessage(string msg, int param)
	{
		GameEventType result;
		if (string.IsNullOrEmpty(msg))
		{
			outputError("消息不能为空");
		}
		else if (!Enum.TryParse<GameEventType>(msg, ignoreCase: true, out result))
		{
			outputError("无效的消息类型<" + msg + ">");
		}
		else
		{
			BroadcastInt(result, param);
		}
	}

	[Command("view_game_states", Desc = "查看当前游戏状态")]
	private static void Command_ViewCurrentGameState()
	{
		outputSuccess($"当前游戏状态: {userInput.CurrentState}");
		string[] array = userInput.stateStack.ToArray();
		output($"当前状态栈有{array.Length}个状态");
		if (array.Length != 0)
		{
			output("----- 游戏状态堆栈 -----", DolocColorHex.pink);
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				outputSuccess(array2[i]);
			}
			output("+---------------------+", DolocColorHex.pink);
		}
	}

	[Command("save_game", Desc = "保存游戏, 可指定存档序号(可选)")]
	private static void Command_SaveGame(int index = -1)
	{
		if (index < 0)
		{
			index = archiveHandle.archiveIndex;
		}
		SaveGame(index);
	}

	[Command("load_game", Desc = "加载一份存档，需指定存档索引")]
	private static void Command_LoadGame(int index)
	{
		userInput.ClearState(gameStateManager.normalGameState);
		LoadGame(index);
	}

	[Command("home", Desc = "回到主页")]
	private static void Command_Home()
	{
		QuitCurrentRoom(null);
		ReturnHome();
	}

	[Command("set_backpack_capacity", Desc = "设置背包容量")]
	public static void Command_SetBackpackCapacity(int count)
	{
		BackpackLevelInfo byCapacity = DolocConfig.Tables.TbBackpackLevel.GetByCapacity(count);
		if (byCapacity != null)
		{
			archiveHandle.farmData.agentData.backpackLevel = byCapacity.Level;
			archiveHandle.InventorySystem.SetBackpackCapacity(count);
		}
	}

	[Command("enable_npc_schedule", Desc = "开启Npc的计划表")]
	private static void Command_EnableNpcSchedule(string npcName)
	{
		EnableNpcSchedule(npcName);
	}

	[Command("disable_npc_schedule", Desc = "关闭Npc的计划表, 需指定备用的默认标记点")]
	private static void Command_DisableNpcSchedule(string npcName, string markPoint)
	{
		DisableNpcSchedule(npcName, markPoint);
	}

	[Command("invoke_npc_schedule", Desc = "强制执行一次npc的计划表, 禁用计划表的npc会前往设置的默认点位")]
	private static void Command_InvokeNpcSchedule(string npcName)
	{
		if (QueryNpc(npcName, out var npc))
		{
			npc.InvokeSchedule();
		}
	}

	[Command("set_produce_require_mood", Desc = "设置生产是否依赖心情值")]
	private static void Command_SetProduceRequireMood(bool value)
	{
		gameManager.gameInitConfig.ignoreAnimalMoodWhenProduce = !value;
		outputSuccess($"已设置生产是否依赖心情值为: {value}");
	}

	[Command("gen_debug_weathers", Desc = "根据当前的矩阵配置来生成全年的天气")]
	private static void Command_GenerateDebugWeathers()
	{
		StringBuilder stringBuilder = new StringBuilder("月份,天数,白天天气,晚上天气\n");
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		int num = 0;
		string key;
		int value;
		for (int i = 1; i <= 4; i++)
		{
			SeasonInfo byMonth = DolocConfig.Tables.TbSeason.GetByMonth(i);
			int randomSeed = UnityEngine.Random.Range(0, int.MaxValue);
			foreach (var (vector2Int2, weatherType2) in byMonth.GenWeatherMap(i, randomSeed))
			{
				stringBuilder.AppendLine($"{i},{vector2Int2.x},{weatherType2},{weatherType2}");
				num++;
				if (dictionary.ContainsKey(weatherType2.ToString()))
				{
					key = weatherType2.ToString();
					value = dictionary[key]++;
				}
				else
				{
					dictionary[weatherType2.ToString()] = 1;
				}
			}
		}
		StringBuilder stringBuilder2 = new StringBuilder("天气,出现次数,占比\n");
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			item.Deconstruct(out key, out value);
			string arg = key;
			int num2 = value;
			float num3 = (float)num2 / (float)num;
			stringBuilder2.AppendLine($"{arg},{num2},{num3:P}");
		}
		stringBuilder.AppendLine();
		stringBuilder.Append(stringBuilder2);
		string text = $"debug_weather_map_{DateTime.Now:ddHHmmss}.csv";
		string text2 = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/" + text;
		File.WriteAllText(text2, stringBuilder.ToString(), Encoding.UTF8);
		outputSuccess("生成的天气数据已保存至: " + text2);
	}

	[Command("equip_hat", Desc = "将指定的帽子装备给玩家，为空时表示取下")]
	private static void Command_EquipHat(string hatName = "")
	{
		if (EquipHat(hatName, out var oldHat))
		{
			CostItem(hatName, 1);
			if (oldHat != null)
			{
				PlaceItem(oldHat);
			}
		}
		else
		{
			UnityEngine.Debug.LogError("帽子装备失败");
		}
	}

	[Command("faint", Desc = "进入晕厥状态")]
	private static void Command_Faint()
	{
		new FaintGameState(userInput, FaintReason.None).Startup();
	}

	[Command("compose_energy", Desc = "叠加一个值到体力条,需要指定一个int参数")]
	private static void Command_ComposeEnergy(int value)
	{
		ChangeEnergy(value);
	}

	[Command("compose_health", Desc = "叠加一个值到生命条,需要指定一个int参数")]
	private static void Command_ComposeHealth(int value)
	{
		ChangeHealth(value, HurtReason.None);
	}

	[Command("compose_spirit", Desc = "叠加一个值到精神条,需要指定一个int参数")]
	private static void Command_ComposeSpirit(int value)
	{
		ChangeSpirit(value);
	}

	[Command("view_spirit", Desc = "查看精力数值")]
	private static void Command_ViewSpirit()
	{
		output(archiveHandle.farmData.agentData.spirit.ToString());
	}

	[Command("add_tech_exp", Desc = "添加科技树经验")]
	private static void Command_AddTechTreeExp(string type, int exp)
	{
		TechPointType result;
		if (string.IsNullOrEmpty(type))
		{
			outputError("科技树类型不能为空");
		}
		else if (exp <= 0)
		{
			outputError("经验值必须大于0");
		}
		else if (!Enum.TryParse<TechPointType>(type, ignoreCase: true, out result))
		{
			outputError("未知的科技树类型");
		}
		else
		{
			AddTechExp(result, exp);
		}
	}

	[Command("add_tech_point", Desc = "添加科技树点数")]
	private static void Command_AddTechPoints(string type, int pt)
	{
		TechPointType result;
		if (type.IsNullOrEmpty() || pt <= 0)
		{
			outputError("参数错误");
		}
		else if (!Enum.TryParse<TechPointType>(type, ignoreCase: true, out result))
		{
			outputError("未知的科技树类型");
		}
		else
		{
			AddTechPoint(result, pt);
		}
	}

	[Command("add_buff", Desc = "添加指定名字的buff到玩家")]
	private static void Command_AddBuff(string buffName, int scale = 1)
	{
		if (string.IsNullOrEmpty(buffName))
		{
			UnityEngine.Debug.LogError("buff名不能为空");
		}
		else if (!AddBuff(buffName, scale))
		{
			UnityEngine.Debug.LogError("不存在name为" + buffName + "的buff对象");
		}
	}

	[Command("get_player_name", Desc = "获取玩家名字")]
	private static string Command_GetPlayerName()
	{
		return archiveHandle.farmData.agentData.playerName;
	}

	[Command("is_npc_birthday", Desc = "当前是否为角色生日(可指定日期偏移)")]
	public static bool Command_IsNpcBirthday(string npcName, int dayOffset = 0)
	{
		DateInfo dateInfo = archiveHandle.DateNow.CopyWithDayOffset(-dayOffset);
		if (npcName == "player")
		{
			return archiveHandle.IsPlayerBirthday(dateInfo.Month, dateInfo.Day);
		}
		return IsNpcBirthday(npcName, dateInfo);
	}

	[Command("refresh_birthday_celebration_npc_order", Desc = "刷新npc参与生日的排序")]
	private static void Command_RefreshNpcCelebrationOrder()
	{
		archiveHandle.cityData.likingManager.RefreshNpcCelebrationOrder();
	}

	[Command("get_birthday_celebration_npc", Desc = "获取庆祝玩家生日的npc(序号从0开始, 同组调用前需使用refresh_birthday_celebration_npc_order重新排序)")]
	private static string Command_GetCelebrationNpcByRank(int index, bool isAmbiance, int fixNpcCount)
	{
		int ignoreAmbienceCount = (isAmbiance ? fixNpcCount : (-1));
		return archiveHandle.cityData.likingManager.GetCelebrationNpcByRank(index, ignoreAmbienceCount);
	}

	[Command("debug_view_birthday_celebration_npc_order", Desc = "在控制台查看当前npc参与生日的排序(可指定是否刷新)")]
	private static void Command_ViewNpcCelebrationOrder(bool refresh = false)
	{
		if (refresh)
		{
			archiveHandle.cityData.likingManager.RefreshNpcCelebrationOrder();
		}
		int num = 0;
		string[] celebrationOrder = archiveHandle.cityData.likingManager.CelebrationOrder;
		foreach (string text in celebrationOrder)
		{
			NpcInfo orDefault = DolocConfig.Tables.TbNpc.GetOrDefault(text);
			Color color = ((orDefault != null && orDefault.IsBirthdayAmbience) ? DolocUiColor.EYECATCHCOLOR_CYAN : DolocUiColor.EYECATCHCOLOR_PURPLE);
			output($"{num++}: {text.Colored(color)} [{archiveHandle.cityData.likingManager.QueryNpcLikingValue(text)}]");
		}
	}

	[Command("get_item_selling_price", Desc = "获取道具售出价格")]
	private static int Command_GetItemSellingPrice(string itemName, float scale)
	{
		return Mathf.RoundToInt((float)GetItemSellingPrice(itemName) * scale);
	}

	[Command("get_item_buying_price", Desc = "获取道具买入价格")]
	private static int Command_GetItemBuyingPrice(string itemName, float scale)
	{
		if (!QueryItemProto(itemName, out var proto))
		{
			return 0;
		}
		return Mathf.RoundToInt((float)proto.BuyingPrice * scale);
	}

	[Command("add_agent_equipment_skill", Desc = "[调试命令] 给玩家添加主角装备技能")]
	private static void Command_AddAgentEquipmentSkill(string skillId)
	{
		if (string.IsNullOrEmpty(skillId))
		{
			UnityEngine.Debug.LogError("技能ID不能为空");
		}
		else
		{
			archiveHandle.farmData.agentData.agentEquipment.DEBUG_AddFunction(skillId);
		}
	}

	[Command("remove_agent_equipment_skill", Desc = "[调试命令] 给玩家移除主角装备技能")]
	private static void Command_RemoveAgentEquipmentSkill(string skillId)
	{
		if (string.IsNullOrEmpty(skillId))
		{
			UnityEngine.Debug.LogError("技能ID不能为空");
		}
		else
		{
			archiveHandle.farmData.agentData.agentEquipment.DEBUG_RemoveFunction(skillId);
		}
	}

	[Command("clear_agent_equipment_skills", Desc = "[调试命令] 清除玩家所有用于调试的主角装备技能")]
	private static void Command_ClearAgentEquipmentSkills()
	{
		archiveHandle.farmData.agentData.agentEquipment.DEBUG_ClearDebugFunction();
	}

	private static bool Command_TryGetHerbalPouch(out ItemHerbPackage pouch)
	{
		if (archiveHandle.farmData.agentData.agentEquipment.passiveItem1 is ItemHerbPackage itemHerbPackage)
		{
			pouch = itemHerbPackage;
			return true;
		}
		if (archiveHandle.farmData.agentData.agentEquipment.passiveItem2 is ItemHerbPackage itemHerbPackage2)
		{
			pouch = itemHerbPackage2;
			return true;
		}
		Item[] array = archiveHandle.farmData.inventory.inventory.ReadAll();
		foreach (Item item in array)
		{
			if (item is ItemHerbPackage itemHerbPackage3)
			{
				pouch = itemHerbPackage3;
				return true;
			}
			if (!(item is ItemBox itemBox))
			{
				continue;
			}
			Item[] array2 = itemBox.inventory.ReadAll();
			for (int j = 0; j < array2.Length; j++)
			{
				if (array2[j] is ItemHerbPackage itemHerbPackage4)
				{
					pouch = itemHerbPackage4;
					return true;
				}
			}
		}
		pouch = null;
		return false;
	}

	[Command("has_herb_pouch", Desc = "检查玩家是否拥有或装备草药包(装备栏&背包)")]
	public static bool Command_HasHerbPouch()
	{
		ItemHerbPackage pouch;
		return Command_TryGetHerbalPouch(out pouch);
	}

	[Command("has_full_herb_pouch", Desc = "检查玩家是否拥有或装备已补充的草药包(装备栏&背包)")]
	private static bool Command_HasFullHerbPouch()
	{
		if (Command_TryGetHerbalPouch(out var pouch))
		{
			return pouch.isFilled;
		}
		return false;
	}

	[Command("supply_herb_pouch", Desc = "补充草药包(装备栏&背包)")]
	private static void Command_SupplyHerbPouch()
	{
		if (!Command_TryGetHerbalPouch(out var pouch))
		{
			UnityEngine.Debug.LogError("未找到草药包..");
			return;
		}
		pouch.SetFilled(value: true);
		UnityEngine.Debug.Log("草药包已补充");
	}

	[Command("use_mods", Desc = "当前是否启用模组")]
	private static bool Command_UseMod()
	{
		return UseMods;
	}

	[Command("genitem", Desc = "生成物品,给定物品的名称和数量")]
	private static void Command_GenItem(string name, int count = 1)
	{
		if (string.IsNullOrEmpty(name))
		{
			outputError("物品名称不能为空");
			return;
		}
		count = Mathf.Max(1, count);
		name = name.ToLower();
		if (ItemFactory.GenerateItem(name, count, out var item))
		{
			outputSuccess("生成物品<" + item.title + ">成功");
			PlaceItem(item);
		}
		else
		{
			outputError("生成物品<" + name + ">失败,可能是由于物品不存在");
		}
	}

	[Command("obtain_item", Desc = "生成物品并触发获取事件, 给定物品的名称和数量")]
	private static void Command_ObtainItem(string name, int count = 1)
	{
		if (string.IsNullOrEmpty(name))
		{
			outputError("物品名称不能为空");
			return;
		}
		count = Mathf.Max(1, count);
		name = name.ToLower();
		if (ItemFactory.GenerateItem(name, count, out var item))
		{
			outputSuccess("生成物品<" + item.title + ">成功");
			int num = PlaceItem(item)?.count ?? 0;
			int num2 = count - num;
			for (int i = 0; i < num2; i++)
			{
				BroadcastString(GameEventType.OBTAIN_ITEM, name);
			}
		}
		else
		{
			outputError("生成物品<" + name + ">失败,可能是由于物品不存在");
		}
	}

	[Command("view_email_proto", Desc = "查询邮件的原型,需要指定一个名称")]
	private static void Command_QueryEmailProto(string name)
	{
		EmailInfo value;
		if (string.IsNullOrEmpty(name))
		{
			outputError("邮件名称不能为空");
		}
		else if (!DolocConfig.Tables.TbEmail.DataMap.TryGetValue(name, out value))
		{
			outputError("邮件<" + name + ">不存在");
		}
		else
		{
			outputSuccess("<" + name + ">:" + value.Title);
		}
	}

	[Command("send_email", Desc = "发送目标邮件给玩家,需要指定一个名称")]
	private static void Command_SendEmail(string name, bool allowRepeat = true)
	{
		if (string.IsNullOrEmpty(name))
		{
			outputError("邮件名称不能为空");
		}
		else if (SendEmail(name, allowRepeat))
		{
			outputSuccess("邮件<" + name + ">已发送");
		}
		else
		{
			outputError("邮件<" + name + ">发送失败,可能是由于邮件不存在或已经被发送过");
		}
	}

	[Command("view_email", Desc = "查看当前玩家拥有的所有邮件")]
	private static void Command_ViewEmail()
	{
		Email[] emails = archiveHandle.GetEmails();
		if (emails.Length == 0)
		{
			outputError("当前玩家没有邮件");
			return;
		}
		output("+----- 邮件箱 -----+", DolocColorHex.pink);
		output($"当前玩家拥有{emails.Length}封邮件");
		Email[] array = emails;
		for (int i = 0; i < array.Length; i++)
		{
			outputSuccess(array[i].ToString());
		}
		output("+----- 邮件箱 -----+", DolocColorHex.pink);
	}

	[Command("unlock_platform", Desc = "解锁平台,指定平台的名称")]
	private static void Command_UnlockPlatform(string platformName)
	{
		if (string.IsNullOrEmpty(platformName))
		{
			outputError("平台名称不能为空");
		}
		else if (archiveHandle.UnlockPlatform(platformName))
		{
			outputSuccess("解锁平台<" + platformName + ">成功");
		}
		else
		{
			outputError("解锁平台<" + platformName + ">失败,可能是由于平台不存在或已经被解锁过");
		}
	}

	[Command("unlock_all_equipment", Desc = "解锁全部设备")]
	private static void Command_UnlockAllEquipment()
	{
		foreach (string key in DolocConfig.Tables.TbEquipment.DataMap.Keys)
		{
			archiveHandle.UnlockRecipe(key);
		}
	}

	[Command("is_building_unlocked", Desc = "建筑是否解锁")]
	private static bool Command_IsBuildingUnlocked(string buildingName)
	{
		return archiveHandle.IsBuildingUnlocked(buildingName);
	}

	[Command("unlock_building", Desc = "解锁建筑")]
	private static void Command_UnlockBuilding(string buildingName)
	{
		if (string.IsNullOrEmpty(buildingName))
		{
			outputError("建筑名称不能为空");
		}
		else if (archiveHandle.UnlockBuilding(buildingName))
		{
			outputSuccess("解锁建筑<" + buildingName + ">成功");
		}
		else
		{
			outputError("解锁建筑<" + buildingName + ">失败,可能是由于建筑不存在或已经被解锁过");
		}
	}

	[Command("unlock_recipe", Desc = "全局解锁配方,指定配方名")]
	private static void Command_UnlockRecipeGlobal(string recipeName, bool shouldTip = false)
	{
		if (string.IsNullOrEmpty(recipeName))
		{
			outputError("配方名不能为空");
		}
		else if (!shouldTip)
		{
			archiveHandle.UnlockRecipe(recipeName);
		}
		else
		{
			new RewardRecipe(recipeName).CashReward();
		}
	}

	[Command("unlock_recipe_group", Desc = "解锁配方组中所有配方,指定配方组名")]
	private static void Command_UnlockAllRecipeInGroup(string recipeGroup)
	{
		if (string.IsNullOrEmpty(recipeGroup))
		{
			outputError("配方组名不能为空");
		}
		else if (archiveHandle.UnlockRecipeGroup(recipeGroup))
		{
			outputSuccess("解锁配方组<" + recipeGroup + ">成功");
		}
	}

	[Command("unlock_all_recipes", Desc = "解锁所有配方")]
	private static void Command_UnlockAllRecipes()
	{
		foreach (RecipeInfo data in DolocConfig.Tables.TbRecipe.DataList)
		{
			archiveHandle.UnlockRecipe(data.Id);
		}
	}

	[Command("view_finish_missions", Desc = "查看玩家当前所有已完成任务")]
	private static void Command_ViewFinishMissions(string keyword = null)
	{
		string[] finishMissions = archiveHandle.farmData.missionManager.FinishMissions;
		if (finishMissions.Length == 0)
		{
			outputError("当前玩家没有完成任何任务");
			return;
		}
		string[] array;
		if (keyword.IsNullOrEmpty())
		{
			outputSuccess($"当前玩家已完成{finishMissions.Length}条任务");
			array = finishMissions;
			foreach (string text in array)
			{
				outputSuccess("任务<" + text + ">");
			}
			return;
		}
		string[] array2 = finishMissions.Where((string x) => x.Contains(keyword)).ToArray();
		outputSuccess($"找到{array2.Length}条已完成的任务");
		array = array2.ToArray();
		foreach (string text2 in array)
		{
			outputSuccess("任务<" + text2 + ">");
		}
	}

	[Command("view_missions", Desc = "查看正在执行的任务列表")]
	private static void Command_ViewMissions(string keyword = null)
	{
		IMission[] totalMissions = archiveHandle.farmData.missionManager.totalMissions;
		if (totalMissions.Length == 0)
		{
			outputError("当前玩家没有正在执行的任务");
			return;
		}
		IMission[] array;
		if (keyword.IsNullOrEmpty())
		{
			output($"当前玩家正在执行{totalMissions.Length}条任务", DolocColor.pink);
			array = totalMissions;
			foreach (IMission mission in array)
			{
				string text = mission.Id + "(" + mission.BriefStatus.ExtractContentFromColorTag() + ")";
				if (mission.IsImplicit)
				{
					output(text, Color.grey);
				}
				else
				{
					output(text + mission.Title.Colored(Color.green));
				}
			}
			return;
		}
		IMission[] array2 = totalMissions.Where((IMission x) => x.Id.Contains(keyword)).ToArray();
		outputSuccess($"找到{array2.Length}条符合条件的任务");
		array = array2.ToArray();
		foreach (IMission mission2 in array)
		{
			string cnt = mission2.Title + "\"" + mission2.Id + "\"(" + mission2.BriefStatus.ExtractContentFromColorTag() + ")";
			if (mission2.IsImplicit)
			{
				output(cnt, Color.grey);
			}
			else
			{
				output(cnt, Color.green);
			}
		}
	}

	[Command("clear_mission_chain_record", Desc = "移除所有任务链节点的的完成记录")]
	private static void Command_RemoveMissionChainCompleteRecords(string name)
	{
		if (!assets.missionChains.QueryData(name, out var data))
		{
			UnityEngine.Debug.LogError("无效的任务链名称:\"" + name + "\"");
		}
		else
		{
			archiveHandle.ClearMissionChainRecords(data);
		}
	}

	[Command("start_mission_chain", Desc = "启动一个任务链，需指定一个名称")]
	private static void Command_StartMissionChain(string name, bool force = false)
	{
		archiveHandle.AppendMissionLog("执行命令 start_mission_chain " + name);
		if (force)
		{
			archiveHandle.RemoveMission(name);
			archiveHandle.farmData.missionChainManager.__ForceRemoveMissionChain(name);
		}
		if (StartMissionChain(name))
		{
			outputSuccess("任务链" + name + "启动成功");
			archiveHandle.AppendMissionLog("任务链\"" + name + "\"启动成功");
		}
		else
		{
			archiveHandle.AppendMissionLog("任务链\"" + name + "\"启动失败");
			outputError("任务链" + name + "启动失败");
		}
	}

	[Command("stop_mission", Desc = "删除目标任务")]
	private static void Command_StopMission(string missionId)
	{
		archiveHandle.AppendMissionLog("执行命令 stop_mission " + missionId);
		if (missionId.IsNullOrEmpty())
		{
			archiveHandle.AppendMissionLog("删除任务失败: 任务Id不能为空");
			return;
		}
		bool num = archiveHandle.farmData.missionManager.RemoveMission(missionId);
		string text = (num ? "成功" : "失败");
		string text2 = (num ? "#00ff00" : "red");
		UnityEngine.Debug.Log("<color=" + text2 + ">任务\"" + missionId + "\"删除" + text + "</color>");
		archiveHandle.AppendMissionLog("任务\"" + missionId + "\"删除" + text);
	}

	private static bool IsValidMissionChainAndMissionId(string chainId, string missionId, out MissionGraph chain, out MissionNodeListenerBase node)
	{
		chain = null;
		node = null;
		if (chainId.IsNullOrEmpty() || missionId.IsNullOrEmpty())
		{
			return false;
		}
		if (!assets.missionChains.QueryData(chainId, out chain))
		{
			return false;
		}
		return chain.QueryMissionNode(missionId, out node);
	}

	[Command("restart_mission", Desc = "指定一个任务链和该任务链中的一个节点，重启该任务")]
	public static bool Command_RestartMission(string chainId, string missionId, bool fromParent = false)
	{
		archiveHandle.AppendMissionLog($"执行命令:restart_mission {chainId} {missionId} {fromParent}");
		if (!IsValidMissionChainAndMissionId(chainId, missionId, out var chain, out var node))
		{
			archiveHandle.AppendMissionLog("无效的任务信息\"" + chainId + "." + missionId + "\"");
			UnityEngine.Debug.LogError("无效的任务信息\"" + chainId + "." + missionId + "\"");
			return false;
		}
		string text = chainId + "." + missionId;
		if (archiveHandle.farmData.missionManager.IsMissionListening(missionId))
		{
			UnityEngine.Debug.LogWarning("任务\"" + text + "\"处于监听状态，无法使用restart_mission重启");
			archiveHandle.AppendMissionLog("任务\"" + text + "\"处于监听状态，无法使用restart_mission重启");
			return false;
		}
		if (archiveHandle.farmData.missionManager.IsMissionComplete(missionId))
		{
			UnityEngine.Debug.LogWarning("任务\"" + text + "\"已经完成，无法使用restart_mission重启");
			archiveHandle.AppendMissionLog("任务\"" + text + "\"已经完成，无法使用restart_mission重启");
			return false;
		}
		if (chain.TryGetPrevMissionNode((MissionNodeListener)node, out var prevNode))
		{
			if (!archiveHandle.farmData.missionManager.IsMissionComplete(prevNode.MissionId))
			{
				UnityEngine.Debug.LogError("任务\"" + text + "\"的父节点\"" + prevNode.MissionId + "\"未完成，无法使用restart_mission重启");
				archiveHandle.AppendMissionLog("任务\"" + text + "\"的父节点\"" + prevNode.MissionId + "\"未完成，无法使用restart_mission重启");
				return false;
			}
		}
		else if (!node.IsPrimeChild)
		{
			UnityEngine.Debug.LogError("任务\"" + text + "\"的父节点\"" + prevNode.MissionId + "\"不是入口节点或未完成，无法使用restart_mission重启");
			archiveHandle.AppendMissionLog("任务\"" + text + "\"的父节点\"" + prevNode.MissionId + "\"不是入口节点或未完成，无法使用restart_mission重启");
			return false;
		}
		if (archiveHandle.farmData.missionChainManager.TryGetMissionChainHandle(chainId, out var handle))
		{
			if (!(fromParent ? handle.ReExecuteMissionNodeFromParent(missionId) : handle.ReExecuteMissionNode(missionId)))
			{
				UnityEngine.Debug.LogError("任务\"" + text + "\"重启失败");
				archiveHandle.AppendMissionLog("任务\"" + text + "\"重启失败");
				return false;
			}
		}
		else
		{
			MissionChainHandle missionChainHandle = archiveHandle.farmData.missionChainManager._StartMissionChainVirtual(chain);
			if (missionChainHandle == null)
			{
				UnityEngine.Debug.LogError("任务链\"" + chainId + "\"重启失败");
				archiveHandle.AppendMissionLog("任务链\"" + chainId + "\"重启失败");
				return false;
			}
			if (!(fromParent ? missionChainHandle.ReExecuteMissionNodeFromParent(missionId) : missionChainHandle.ReExecuteMissionNode(missionId)))
			{
				UnityEngine.Debug.LogError("任务\"" + text + "\"重启失败");
				archiveHandle.AppendMissionLog("任务\"" + text + "\"重启失败");
				return false;
			}
		}
		bool flag = archiveHandle.farmData.missionManager.IsMissionListening(missionId);
		string text2 = (flag ? "成功" : "失败");
		string text3 = (flag ? "#00ff00" : "red");
		UnityEngine.Debug.Log("<color=" + text3 + ">任务\"" + text + "\"重启" + text2 + "</color>");
		archiveHandle.AppendMissionLog("任务\"" + text + "\"重启" + text2);
		return flag;
	}

	[Command("resolve_mission", Desc = "指定一个任务链和该任务链中的一个节点，如果该任务正在执行，则重启该任务")]
	private static bool Command_ResolveMission(string chainId, string missionId, bool fromParent = false)
	{
		archiveHandle.AppendMissionLog($"执行命令 resolve_mission {chainId} {missionId} {fromParent}");
		if (!IsValidMissionChainAndMissionId(chainId, missionId, out var _, out var _))
		{
			archiveHandle.AppendMissionLog("任务重置失败：无效的任务信息\"" + chainId + "." + missionId + "\"");
			return false;
		}
		if (!archiveHandle.farmData.missionChainManager.TryGetMissionChainHandle(chainId, out var handle))
		{
			archiveHandle.AppendMissionLog("任务重置失败：任务链\"" + chainId + "\"未启动");
			return false;
		}
		if (!archiveHandle.farmData.missionManager.IsMissionListening(missionId))
		{
			archiveHandle.AppendMissionLog("任务重置失败：任务\"" + chainId + "." + missionId + "\"未在执行中");
			return false;
		}
		archiveHandle.farmData.missionManager.RemoveMission(missionId);
		handle.TryRemoveListenNode(missionId);
		bool flag = (fromParent ? handle.ReExecuteMissionNodeFromParent(missionId) : handle.ReExecuteMissionNode(missionId)) && archiveHandle.farmData.missionManager.IsMissionListening(missionId);
		string text = (flag ? "成功" : "失败");
		string text2 = (flag ? "#00ff00" : "red");
		UnityEngine.Debug.Log("<color=" + text2 + ">任务\"" + chainId + "." + missionId + "\"重置" + text + "</color>");
		archiveHandle.AppendMissionLog("任务\"" + chainId + "." + missionId + "\"重置" + text);
		return flag;
	}

	[Command("reinvoke_mission", Desc = "指定一个任务链和该任务链中的一个节点，如果该任务节点已经完成，则重新执行一次其后续节点中的所有非任务节点")]
	private static bool Command_ReInvokeMission(string chainId, string missionId)
	{
		archiveHandle.AppendMissionLog("执行命令 reinvoke_mission " + chainId + " " + missionId);
		if (!IsValidMissionChainAndMissionId(chainId, missionId, out var chain, out var _))
		{
			archiveHandle.AppendMissionLog("任务后续节点重启失败：无效的任务信息\"" + chainId + "." + missionId + "\"");
			return false;
		}
		if (!archiveHandle.farmData.missionManager.IsMissionComplete(missionId))
		{
			archiveHandle.AppendMissionLog("任务后续节点重启失败：任务\"" + chainId + "." + missionId + "\"未完成");
			return false;
		}
		bool flag = chain.ReInvokeMissionNode(missionId);
		string text = (flag ? "成功" : "失败");
		string text2 = (flag ? "#00ff00" : "red");
		UnityEngine.Debug.Log("<color=" + text2 + ">任务\"" + chainId + "." + missionId + "\"后续节点重启" + text + "</color>");
		archiveHandle.AppendMissionLog("任务\"" + chainId + "." + missionId + "\"后续节点重启" + text);
		return flag;
	}

	[Command("traceback_mission", Desc = "指定一个任务链和一个任务ID，如果该任务正在执行，则移除该任务，并强制重启它的父节点")]
	private static bool Command_TracebackMission(string chainId, string missionId)
	{
		if (!IsValidMissionChainAndMissionId(chainId, missionId, out var chain, out var node))
		{
			return false;
		}
		if (!archiveHandle.farmData.missionManager.IsMissionListening(missionId))
		{
			return false;
		}
		archiveHandle.farmData.missionManager.RemoveMission(missionId);
		if (chain.TryGetPrevMissionNode((MissionNodeListener)node, out var prevNode))
		{
			archiveHandle.farmData.missionManager.RemoveMission(prevNode.MissionId);
			MissionChainHandle missionChainHandle = archiveHandle.farmData.missionChainManager._StartMissionChainVirtual(chain);
			missionChainHandle.TryRemoveListenNode(missionId);
			missionChainHandle.ReExecuteMissionNode(prevNode.MissionId);
			return archiveHandle.farmData.missionManager.IsMissionListening(prevNode.MissionId);
		}
		return false;
	}

	[Command("complete_mission", Desc = "完成一个指定的任务/任务链")]
	private static void Command_CompleteMission(string missionId)
	{
		if (missionId.IsNullOrEmpty())
		{
			UnityEngine.Debug.LogError("任务Id不能为空");
			return;
		}
		if (!archiveHandle.farmData.missionManager.IsMissionListening(missionId))
		{
			UnityEngine.Debug.LogError("任务\"" + missionId + "\"未在执行中");
			return;
		}
		archiveHandle.farmData.missionManager.CompleteMission(missionId);
		UnityEngine.Debug.LogError("任务\"" + missionId + "\"已完成!");
	}

	[Command("stop_mission_chain", Desc = "停止一个任务链，需指定一个名称")]
	private static void Command_StopMissionChain(string name)
	{
		archiveHandle.StopMissionChain(name);
		UnityEngine.Debug.Log("任务链" + name + "已停止");
	}

	[Command("start_all_mission_chains", Desc = "启动所有任务链")]
	private static void Command_StartAllMissionChains()
	{
		foreach (MissionInfo data in DolocConfig.Tables.TbMission.DataList)
		{
			try
			{
				StartMissionChain(data.Id);
			}
			catch (Exception)
			{
			}
		}
	}

	[Command("start_faction_mission", Desc = "开启一个势力任务，需指定一个名称")]
	private static void Command_StartFactionMission(string name)
	{
		if (archiveHandle.StartFactionMission(name))
		{
			outputSuccess("势力任务" + name + "开启成功");
		}
		else
		{
			outputError("势力任务" + name + "开启失败");
		}
	}

	[Command("start_all_faction_missions", Desc = "开启所有势力任务")]
	private static void Command_StartAllFactionMissions()
	{
		archiveHandle.cityData.factionMissionManager.__StartAllFactionMissions();
	}

	[Command("is_faction_mission_complete", Desc = "势力任务是否完成")]
	private static bool Command_IsFactionMissionComplete(string missionId)
	{
		return GetEventTriggerCount(GameEventType.FACTION_MISSION_FINISH, missionId ?? "") > 0;
	}

	[Command("view_faction_mission", Desc = "查看玩家当前正在执行的势力任务")]
	private static void Command_ViewFactionMission()
	{
		FactionMission[] totalFactionMissions = archiveHandle.GetTotalFactionMissions();
		if (totalFactionMissions.Length == 0)
		{
			outputError("当前玩家没有势力任务");
			return;
		}
		output("+----- 势力任务 -----+");
		outputSuccess($"当前玩家正在执行{totalFactionMissions.Length}条势力任务");
		FactionMission[] array = totalFactionMissions;
		foreach (FactionMission factionMission in array)
		{
			outputSuccess("势力任务<" + factionMission.Id + ":" + factionMission.Proto.Title + ">");
			FactionItem[] factionItems = factionMission.FactionItems;
			foreach (FactionItem factionItem in factionItems)
			{
				outputSuccess($"  需求道具<{factionItem.ItemName}>,是否已提交<{factionItem.FinishState}>");
			}
			Reward[] rewards = factionMission.Rewards;
			foreach (Reward reward in rewards)
			{
				outputSuccess($"  奖励<{reward.type};{reward.Count}>");
			}
		}
		output("+----- 势力任务 -----+");
	}

	[Command("reset_achievement", Desc = "重置Steam成就")]
	private static void Command_ResetAchievement(string achievementId)
	{
		if (!archiveHandle.extraData.achievementSystem.ResetAchievement(achievementId))
		{
			UnityEngine.Debug.LogError("重置成就\"" + achievementId + "\"失败");
		}
		else
		{
			outputSuccess("重置成就\"" + achievementId + "\"成功");
		}
	}

	[Command("reset_all_achievements", Desc = "重置所有Steam成就")]
	private static void Command_ResetAllAchievements()
	{
		if (!IsDataLoaded)
		{
			if (!SteamHelper.ResetAllAchievements(out var _))
			{
				UnityEngine.Debug.LogError("重置Steam端成就失败..");
			}
		}
		else if (!archiveHandle.extraData.achievementSystem.ResetAllAchievements())
		{
			UnityEngine.Debug.LogError("重置所有成就失败");
			return;
		}
		outputSuccess("重置所有成就成功");
	}

	[Command("view_city_rooms", Desc = "查看所有已经被创建的城镇房间实体对象")]
	private static void Command_ViewCityRooms()
	{
		CityRoom[] array = archiveHandle.cityData.cityRooms.Values.ToArray();
		if (array.Length == 0)
		{
			outputError("当前城镇没有房间");
			return;
		}
		output($"当前城镇拥有{array.Length}个房间");
		output("+----- 城镇房间 -----+", DolocColorHex.pink);
		CityRoom[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			outputSuccess(array2[i].Title);
		}
		output("+----- 城镇房间 -----+", DolocColorHex.pink);
	}

	[Command("unlock_seed_node", Desc = "解锁某个种子节点,需指定一个名称")]
	private static void Command_UnlockSeedNode(string name)
	{
		if (archiveHandle.UnlockSeedNode(name))
		{
			outputSuccess("解锁种子节点<" + name + ">成功");
		}
		else
		{
			outputError("解锁种子节点<" + name + ">失败,可能是由于种子不存在或已经被解锁过");
		}
	}

	[Command("get_unlocked_seed_count", Desc = "获取解锁的种子数量")]
	private static int Command_GetUnLockSeedCount()
	{
		return archiveHandle.farmData.unlockedSeedNodes.Count;
	}

	[Command("is_seed_latest_unlocked", Desc = "判断指定种子是否在上次打开ui中解锁")]
	private static bool Command_IsSeedLatestUnlocked(string name)
	{
		return SeedUnLockUiState.latestUnlockSeed.Contains(name);
	}

	[Command("is_seed_unlocked", Desc = "判断指定种子是否解锁")]
	private static bool Command_IsSeedUnlocked(string name)
	{
		return archiveHandle.IsSeedNodeUnlocked(name);
	}

	[Command("is_seed_can_unlock", Desc = "判断指定种子当前是否能够解锁(不考虑材料)")]
	private static bool Command_IsSeedCanUnlocked(string name)
	{
		return archiveHandle.IsSeedNodeAvailableToUnlock(name);
	}

	[Command("set_seed_can_unlock", Desc = "设置种子能够解锁(不考虑材料)")]
	private static void Command_SetSeedCanUnlocked(string name)
	{
		archiveHandle.farmData.seedsCanUnlock.Add(name);
	}

	[Command("set_all_seed_can_unlock", Desc = "设置全部种子能够解锁(不考虑材料)")]
	private static void Command_SetAllSeedCanUnlocked()
	{
		foreach (SeedUnlockInfo data in DolocConfig.Tables.TbSeedUnlock.DataList)
		{
			archiveHandle.farmData.seedsCanUnlock.Add(data.Id);
		}
	}

	[Command("unlock_tech_tree", Desc = "解锁指定名称的科技树")]
	private static void Command_LockPartCollectionFunc(string treeName)
	{
		TechTreeInfo byId = DolocConfig.Tables.TbTechTree.GetById(treeName);
		if (byId != null)
		{
			archiveHandle.farmData.unlockedTechTree.Add(byId.Id);
		}
	}

	[Command("unlock_all_documents", Desc = "解锁全部档案")]
	private static void Command_UnlockAllDocuments()
	{
		foreach (PlantDocumentInfo data in DolocConfig.Tables.TbPlantDocument.DataList)
		{
			archiveHandle.cityData.documentManager.plantDocMgr.UnlockPlantDocument(data.Id);
		}
		foreach (ChipDocumentInfo data2 in DolocConfig.Tables.TbChipDocument.DataList)
		{
			archiveHandle.cityData.documentManager.chipDocMgr.UnlockChipDocumentById(data2.Id);
		}
		foreach (CharacterDocumentInfo data3 in DolocConfig.Tables.TbCharacterDocument.DataList)
		{
			archiveHandle.cityData.documentManager.characterDocMgr.UnLockNpcDocument(data3.Id);
		}
	}

	[Command("unlock_plant_document", Desc = "解锁植物档案")]
	private static void Command_UnlockPlantDocument(string docId)
	{
		if (string.IsNullOrEmpty(docId))
		{
			outputError("档案名称不能为空");
		}
		else if (archiveHandle.cityData.documentManager.plantDocMgr.UnlockPlantDocument(docId))
		{
			outputSuccess("解锁芯片档案<" + docId + ">成功");
		}
		else
		{
			outputError("解锁芯片档案<" + docId + ">失败,可能是由于目标配置不存在或已经被解锁过");
		}
	}

	[Command("unlock_chip_document", Desc = "解锁芯片档案")]
	private static void Command_UnlockChipDocument(string docId)
	{
		if (string.IsNullOrEmpty(docId))
		{
			outputError("档案名称不能为空");
		}
		else if (archiveHandle.cityData.documentManager.chipDocMgr.UnlockChipDocumentById(docId))
		{
			outputSuccess("解锁芯片档案<" + docId + ">成功");
		}
		else
		{
			outputError("解锁芯片档案<" + docId + ">失败,可能是由于目标配置不存在或已经被解锁过");
		}
	}

	[Command("unlock_document", Desc = "解锁一条档案")]
	private static UniTask Command_UnlockDocument(string docId)
	{
		if (string.IsNullOrEmpty(docId))
		{
			outputError("档案名称不能为空");
			return UniTask.NextFrame();
		}
		if (archiveHandle.cityData.documentManager.characterDocMgr.UnLockNpcDocument(docId))
		{
			outputSuccess("解锁npc档案<" + docId + ">成功");
			EnterUI((CollectionBookUiState state) => state.OpenNpcArchiveWithId(docId));
			return WaitWhileInUiSateTask();
		}
		outputError("解锁npc档案<" + docId + ">失败,可能是由于目标配置不存在或已经被解锁过");
		return UniTask.NextFrame();
	}

	[Command("show_document_with_handbook", Desc = "显示指定的档案")]
	private static UniTask Command_ShowDocumentWithHandbook(string docId)
	{
		EnterUI((CollectionBookUiState state) => state.OpenNpcArchiveWithId(docId));
		return WaitWhileInUiSateTask();
	}

	[Command("open_permission_view_plant_doc", Desc = "开放可在图鉴查看植物档案的权限")]
	private static void OpenPermissionViewPlantDoc()
	{
		archiveHandle.cityData.documentManager.SynchronizePlantDoc();
	}

	[Command("open_permission_view_chip_doc", Desc = "开放可在图鉴查看芯片档案的权限")]
	private static void OpenPermissionViewChipDoc()
	{
		archiveHandle.cityData.documentManager.SynchronizeChipDoc();
	}

	[Command("unlock_collection_func", Desc = "解锁对应的图鉴功能")]
	private static void Command_UnlockCollectionFunc(CompendiumLabel label)
	{
		archiveHandle.farmData.collectionManager.UnlockCollectionFunc(label);
	}

	[Command("set_global_store_level", Desc = "设置全局商店等级")]
	private static void Command_SetStoreLevel(int level)
	{
		archiveHandle.cityData.storeManager.SetGlobalStoreLevel(level);
	}

	[Command("refresh_store", Desc = "刷新指定商店商品")]
	private static void Command_RefreshStore(string storeName)
	{
		if (string.IsNullOrEmpty(storeName))
		{
			outputError("商店名称不能为空");
		}
		else
		{
			RefreshStore(storeName);
		}
	}

	[Command("unlock_store_item", Desc = "解锁商店售卖的道具")]
	private static void Command_UnlockStoreItem(string storeName, string itemName)
	{
		if (storeName.IsNullOrEmpty() || itemName.IsNullOrEmpty())
		{
			outputError("参数不能为空");
		}
		else
		{
			UnlockStoreItem(storeName, itemName);
		}
	}

	[Command("revert_unlock_store_item", Desc = "撤销解锁商店售卖的道具(仅对初始锁定的商品有效)")]
	private static void Command_RevertUnlockStoreItem(string storeName, string itemName)
	{
		if (archiveHandle.QueryStore(storeName ?? "", out var store))
		{
			store.RevertUnlockStoreItem(itemName ?? "");
		}
	}

	[Command("add_mission_item", Desc = "在指定的位置创建一个任务道具")]
	public static void Command_AddMissionItem(string missionItemId, string markPointId)
	{
		if (CreateMissionItem(missionItemId, markPointId))
		{
			outputSuccess("创建成功");
		}
		else
		{
			outputError("创建失败");
		}
	}

	[Command("start_board_mission", Desc = "启动一个看板任务，需指定一个名称")]
	private static void Command_StartBoardMission(string missionId)
	{
		archiveHandle.StartBoardMission(missionId);
	}

	[Command("issue_board_mission", Desc = "发布一个看板任务，需指定一个任务名")]
	private static void Command_IssueBoardMission(string missionId)
	{
		archiveHandle.IssueBoardMission(missionId);
	}

	[Command("add_fixed_mission_alternative", Desc = "将某个固定任务加入激活池，需指定任务名")]
	private static void Command_AddFixedMissionAlternative(string missionId)
	{
		archiveHandle.cityData.boardMissionManager.AddFixedMission(missionId);
	}

	[Command("refresh_board_mission", Desc = "直接刷新一次看板任务（同第二天刷新）")]
	private static void Command_RefreshBoardMission()
	{
		archiveHandle.cityData.boardMissionManager.MissionTiming(force: true);
	}

	[Command("unlock_map", Desc = "解锁地图全部房间(不包括需要手动解锁的房间)")]
	private static void Command_UnlockMap(string mapId)
	{
		archiveHandle.farmData.mapManager.UnlockMap(mapId);
	}

	[Command("unlock_room_in_map", Desc = "解锁地图中的指定房间")]
	private static void Command_UnlockRoomInMap(string mapId, string roomId)
	{
		archiveHandle.farmData.mapManager.UnlockRoomInMap(mapId, roomId);
	}

	[Command("set_map_offset", Desc = "设置地图图片偏移")]
	private static void Command_SetMapOffset(string mapId, float x, float y)
	{
		archiveHandle.farmData.mapManager.SetMapOffset(mapId, new Vector2(x, y));
	}

	[Command("show_map_indicators", Desc = "显示地图提示")]
	private static void Command_ShowIndicatorsInMap(string mapId)
	{
		archiveHandle.farmData.mapManager.ShowIndicatorsInMap(mapId);
	}

	[Command("show_map_panel", Desc = "显示指定地图")]
	private static UniTask Command_ShowMapPanel(string mapId = null)
	{
		if (mapId.IsNullOrEmpty())
		{
			OpenMap();
		}
		else
		{
			EnterUI((CollectionBookUiState state) => state.HandleMapArgs(mapId));
		}
		return WaitWhileInUiSateTask();
	}

	[Command("go", Desc = "传送到指定的传送点")]
	private static async UniTask Command_GoRoom(string markPointId, bool disableFadeIn = false, bool disableFadeOut = false)
	{
		bool loaded = false;
		IDolocGameState curState = userInput.CurrentState;
		DoTransport(markPointId, delegate
		{
			loaded = true;
		}, resetVelocity: false, disableFadeIn, disableFadeOut);
		await UniTask.WaitUntil(() => loaded);
		await UniTask.WaitUntil(() => userInput.CurrentState == curState);
	}

	[Command("switch_lang", Desc = "更改语言")]
	private static void Command_SwitchLang(string langId)
	{
		SwitchLanguage(langId);
	}

	[Command("refresh_camera_resolution", Desc = "刷新相机分辨率")]
	private static void Command_RefreshCameraResolution()
	{
		cameraController.RefreshResolution();
	}

	[Command("add_money", Desc = "增加金币并播放动画")]
	private static void Command_AddMoney(int money)
	{
		uiSystem.MoneyTip.ForceShow();
		archiveHandle.CurrentMoney += money;
		Delay(0.2f, delegate
		{
			RaiseSpriteFadeDown(AgentPosition + new Vector3(0f, 2f, 0f), LocSprites.UI_ICON_GOLD28X);
		});
		RaiseItemObtainTip("money", LocSprites.UI_ICON_GOLD28X, DolocConfig.StaticTexts.ItemMoneyTitle, money);
	}

	[Command("cost_money", Desc = "消耗金币并播放动画")]
	private static void Command_CostMoney(int money)
	{
		uiSystem.MoneyTip.ForceShow();
		archiveHandle.CurrentMoney -= money;
		Delay(0.2f, delegate
		{
			RaiseSpriteFadeUp(AgentPosition + new Vector3(0f, 2f, 0f), LocSprites.UI_ICON_GOLD28X);
		});
	}

	[Command("drop_money", Desc = "掉落金币并播放动画")]
	private static async UniTask Command_DropMoney(int totalMoney, int dropCount = 1, float interval = 0.1f)
	{
		Room currentRoom = CurrentRoom;
		IDropItemHost host = currentRoom;
		if (host == null || totalMoney <= 0)
		{
			return;
		}
		dropCount = Mathf.Max(1, dropCount);
		int moneyPerDrop = Mathf.CeilToInt((float)totalMoney / (float)dropCount);
		int remainingMoney = totalMoney;
		Vector3 pos = AgentPosition;
		for (int i = 0; i < dropCount; i++)
		{
			int num = Mathf.Min(moneyPerDrop, remainingMoney);
			remainingMoney -= num;
			if (host.CurrentRoom.isRenderNow)
			{
				host.CreateDropItemMoney(num, pos, shouldSendMsg: true);
				await UniTask.Delay((int)(1000f * interval));
			}
			else
			{
				host.CreateDropItemMoneyNoRender(num, pos, shouldSendMsg: true);
			}
		}
	}

	[Command("check_decorator", Desc = "检查任务装饰器Id是否存在")]
	private static void Command_CheckDecorator(string id)
	{
		if (IsEventDecoratorComplete(id))
		{
			outputSuccess("任务装饰器<" + id + ">已存在");
		}
		else
		{
			outputError("任务装饰器<" + id + ">不存在");
		}
	}

	[Command("list_decorators", Desc = "列出所有已经完成的装饰器Id")]
	private static void Command_ListDecorators()
	{
		string[] allCompletedEventDecorator = archiveHandle.GetAllCompletedEventDecorator();
		outputSuccess($"当前已经完成{allCompletedEventDecorator.Length}个装饰器");
		string[] array = allCompletedEventDecorator;
		foreach (string text in array)
		{
			outputSuccess("  " + text);
		}
	}

	[Command("unlock_motor", Desc = "解锁载具系统")]
	private static void Command_UnlockMotor(float yOffset = 0f)
	{
		UnlockMotor(yOffset);
	}

	[Command("set_motor_position", Desc = "设置载具在当前房间的位置")]
	private static void Command_SetMotorPosition(float x, float y)
	{
		SetMotorPosition(null, new Vector2(x, y));
	}

	[Command("query_event_recorder", Desc = "查询指定事件类型成就状态")]
	private static void Command_QueryEventRecorder(string eventType)
	{
		if (!Enum.TryParse<GameEventType>(eventType, ignoreCase: true, out var result))
		{
			outputError("无效的事件类型\"" + eventType + "\"");
		}
		else
		{
			UnityEngine.Debug.Log(archiveHandle.farmData.eventRecorderManager.GetRecorder(result).ToString());
		}
	}

	[Command("get_archive_data_count", Desc = "获取当前存档数")]
	private static int Command_GetArchiveDataCount()
	{
		return GetAllArchiveInfos().Count((BaseArchiveData x) => x != null);
	}

	[Command("has_any_archive_data", Desc = "当前是否有存档")]
	private static bool Command_HasAnyArchiveData()
	{
		return Command_GetArchiveDataCount() > 1;
	}

	[Command("unlock_resonator", Desc = "解锁共鸣器")]
	private static void Command_UnlockResonator()
	{
		archiveHandle.UnlockResonator();
	}

	[Command("unlock_festival", Desc = "根据给定id解锁节日/生日")]
	private static void Command_UnlockFestival(string id)
	{
		archiveHandle.cityData.calendarManager.UnlockFestival(id);
	}

	[Command("unlock_gene", Desc = "解锁基因")]
	private static void Command_UnlockGene(string geneId)
	{
		archiveHandle.UnlockGene(geneId);
	}

	[Command("unlock_all_genes", Desc = "解锁所有基因")]
	private static void Command_UnlockAllGenes()
	{
		archiveHandle.UnlockAllGenes();
	}

	[Command("unlock_better_water", Desc = "解锁后喝水将会获得一定的体力提升")]
	private static void Command_UnlockBetterWater()
	{
		archiveHandle.UnlockBetterWater();
	}

	[Command("enable_ui", Desc = "启用ui")]
	private static void Command_ShowCanvasOverlay()
	{
		uiSystem.SetGroupsVisible(value: true);
	}

	[Command("disable_ui", Desc = "禁用ui(仅用于跑图录屏)")]
	private static void Command_HideCanvasOverlay()
	{
		uiSystem.SetGroupsVisible(value: false);
	}

	[Command("open_exchange_store", Desc = "打开兑换商店")]
	private static UniTask Command_OpenExchangeStore(string storeId)
	{
		archiveHandle.QueryStore(storeId ?? "", out ExchangeStore store);
		EnterUI((RecipePanelUiState state) => state.HandleExchangeStoreStartUpArgs(store, GetInventoriesAroundAgent()));
		return WaitWhileInUiSateTask();
	}

	[Command("open_store", Desc = "指定一个商店名称, 打开该商店界面")]
	private static UniTask Command_OpenStore(string storeName)
	{
		if (string.IsNullOrEmpty(storeName))
		{
			outputError("商店名称不能为空");
			return default(UniTask);
		}
		if (!OpenStore(storeName))
		{
			outputError("商店<" + storeName + ">不存在或无效");
		}
		return WaitWhileInUiSateTask();
	}

	[Command("get_latest_sold_count_in_store", Desc = "获取上次在ui界面购买的道具数量")]
	private static int Command_GetSoldItemCountStore(string itemName)
	{
		return StoreUiState.GetLatestSoldItemCount(itemName);
	}

	[Command("get_latest_earning_in_store", Desc = "获取上次在ui界面出售获得的金币数")]
	private static int Command_GetLatestEarningInStore()
	{
		return StoreUiState.latestEarning;
	}

	[Command("get_total_earning_in_store", Desc = "获取目前为止在指定商店的赚取的金币数")]
	private static int Command_GetTotalEarningsInStore(string storeName)
	{
		if (!archiveHandle.QueryStore(storeName, out Store store))
		{
			return 0;
		}
		return store.TotalSpending;
	}

	[Command("get_latest_bought_count_in_store", Desc = "获取上次在ui界面购买的道具数量")]
	private static int Command_GetBoughtItemCountStore(string itemName)
	{
		return StoreUiState.GetLatestBoughtItemCount(itemName);
	}

	[Command("get_latest_spending_in_store", Desc = "获取上次在ui界面购买消耗的金币数")]
	private static int Command_GetLatestSpendingInStore()
	{
		return StoreUiState.latestSpending;
	}

	[Command("get_total_spending_in_store", Desc = "获取目前为止在指定商店的消费的金币数")]
	private static int Command_GetTotalSpendingInStore(string storeName)
	{
		if (!archiveHandle.QueryStore(storeName, out Store store))
		{
			return 0;
		}
		return store.TotalEarnings;
	}

	[Command("open_seed_unlock_panel", Desc = "打开种子解锁面板")]
	private static UniTask Command_OpenSeedUnLockPanel()
	{
		EnterUI<SeedUnLockUiState>();
		return WaitWhileInUiSateTask();
	}

	[Command("open_weather_report", Desc = "打开天气预报界面")]
	private static UniTask Command_OpenWeatherReportPanel()
	{
		EnterUI<WeatherReportUiState>();
		return WaitWhileInUiSateTask();
	}

	[Command("debug_test_message_boxes", Desc = "测试各种消息框")]
	private static void Command_DebugTestMessageBox()
	{
		string msg = "测试测试测试测试";
		ShowMessageBoxLarge(null, msg);
		ShowMessageBoxNodeComplete(msg);
		ShowMessageBoxSmall(msg);
		ShowMessageBoxAttention(msg);
	}

	[Command("show_message_box_large", Desc = "弹出大消息框")]
	private static void Command_ShowMessageBoxLarge(string l10nkey, string iconUrl = null, float duration = 3f)
	{
		string l10nText = DolocConfig.GetL10nText(l10nkey);
		ShowMessageBoxLarge(LoadSprite(iconUrl), l10nText, duration);
	}

	[Command("show_message_box_mission_complete", Desc = "弹出任务消息框")]
	private static void Command_ShowMessageBoxMissionComplete(string l10nkey)
	{
		ShowMessageBoxNodeComplete(DolocConfig.GetL10nText(l10nkey));
	}

	[Command("show_message_box_mission_error", Desc = "弹出任务消息框")]
	private static void Command_ShowMessageBoxMission(string l10nkey)
	{
		ShowMessageBoxNodeError(DolocConfig.GetL10nText(l10nkey));
	}

	[Command("show_message_small", Desc = "弹出小消息框(白色文字)")]
	private static void Command_ShowSmallMessage(string l10nkey)
	{
		ShowMessageBoxSmall(DolocConfig.GetL10nText(l10nkey));
	}

	[Command("show_message_small_error", Desc = "弹出小消息框(红色文字)")]
	private static void Command_ShowSmallMessageError(string l10nkey)
	{
		ShowMessageBoxSmallErr(DolocConfig.GetL10nText(l10nkey));
	}

	[Command("show_text_tip", Desc = "弹出文本提示")]
	private static void Command_ShowTextConfirmBox(string tipId)
	{
		ShowTextByConfig(tipId);
	}

	[Command("show_message_box_attention", Desc = "弹出醒目消息框")]
	private static void Command_ShowMessageBoxAttention(string l10nkey)
	{
		ShowMessageBoxAttention(DolocConfig.GetL10nText(l10nkey));
	}

	[Command("open_submit_panel", Desc = "打开道具提交界面")]
	private static async UniTask Command_SubmitItemToNpc(string itemName, int itemCount = 1, bool shouldCostItem = true)
	{
		if (string.IsNullOrEmpty(itemName) || !QueryItemProto(itemName, out var _))
		{
			UnityEngine.Debug.LogError("submit_item_to_npc: 不存在的item name: " + itemName);
			return;
		}
		EnterUI((SubmitItemToNpcUiState state) => state.HandleStartUpArgs(new SubmitItemFilter(itemName, itemCount, shouldCostItem)));
		await WaitWhileInUiSateTask();
	}

	[Command("open_submit_panel_by_config", Desc = "打开道具提交界面(使用配置表中的配置)")]
	private static async UniTask Command_SubmitItemToNpcByInfo(string infoId)
	{
		ItemSubmitConditionInfo config = DolocConfig.Tables.TbItemSubmitCondition.GetOrDefault(infoId);
		if (config != null)
		{
			EnterUI((SubmitItemToNpcUiState state) => state.HandleStartUpArgs(new SubmitItemFilter(config)));
			await WaitWhileInUiSateTask();
		}
	}

	[Command("submit_success", Desc = "最近一次提交的结果")]
	private static bool Command_GetLastSubmitStatus()
	{
		return SubmitItemToNpcUiState.GetLastSubmitStatus();
	}

	[Command("get_submitted_item_name", Desc = "最近一次提交的道具id")]
	private static string Command_GetLastSubmittedItemName()
	{
		return SubmitItemToNpcUiState.lastestItem?.name ?? "";
	}

	[Command("get_submitted_item_title", Desc = "最近一次提交的道具标题")]
	private static string Command_GetLastSubmittedItemTitle()
	{
		return SubmitItemToNpcUiState.lastestItem?.title ?? "";
	}

	[Command("get_submitted_item_count", Desc = "最近一次提交的道具数量")]
	private static int Command_GetLastSubmittedItemCount()
	{
		return SubmitItemToNpcUiState.GetLastSubmitItemCount();
	}

	[Command("get_submitted_gene_titles", Desc = "最近一次提交的道具的基因标题")]
	private static string Command_GetLastSubmitItemGeneTitles()
	{
		return SubmitItemToNpcUiState.GetLastSubmitItemGeneTitles();
	}

	[Command("get_submitted_item_type", Desc = "最近一次提交的道具的主类型")]
	private static string Command_GetLastSubmitItemType()
	{
		return SubmitItemToNpcUiState.lastestItem?.type.ToString().ToLower() ?? string.Empty;
	}

	[Command("get_submitted_item_sub_type", Desc = "最近一次提交的道具的子类型")]
	private static string Command_GetLastSubmitItemSubType()
	{
		return SubmitItemToNpcUiState.lastestItem?.subType.ToString().ToLower() ?? string.Empty;
	}

	[Command("get_submitted_seed_type", Desc = "最近一次提交的道具的种子类型")]
	private static string Command_GetLastSubmitItemSeedType()
	{
		return SubmitItemToNpcUiState.GetLastSubmitItemSeedType();
	}

	[Command("submitted_item_match_config", Desc = "最近一次提交的道具是否满足道具提交配置中指定的条件")]
	private static bool Command_CheckSubmittedItemMatchConfig(string configId)
	{
		return DolocConfig.Tables.TbItemSubmitCondition.GetOrDefault(configId)?.CheckCondition(SubmitItemToNpcUiState.lastestItem) ?? false;
	}

	[Command("get_submitted_seed_lifespan", Desc = "最近一次提交的道具的种子最大收获次数")]
	private static int Command_GetLastSubmitItemSeedLifeSpan()
	{
		return SubmitItemToNpcUiState.GetLastSubmitItemSeedLifeSpan();
	}

	[Command("open_gift_panel", Desc = "打开赠礼界面")]
	private static async UniTask Command_GiftItemToNpc()
	{
		EnterUI<GiveGiftToNPCUiState>();
		await WaitWhileInUiSateTask();
	}

	[Command("get_gift_result", Desc = "获取赠礼结果")]
	private static string Command_GetGiftResult()
	{
		return GiveGiftToNPCUiState.GetLastGiftItem();
	}

	[Command("submit_of_board_mission", Desc = "看板任务道具提交")]
	private static async UniTask Command_SubmitBoardMissionItem(string missionName = null)
	{
		if (string.IsNullOrEmpty(missionName))
		{
			missionName = archiveHandle.cityData.dialogueManager.CurrentNode;
		}
		if (DolocConfig.Tables.TbBoardMission.DataMap.TryGetValue(missionName, out var missionProto))
		{
			EnterUI((SubmitItemToNpcUiState state) => state.HandleStartUpArgs(new SubmitItemFilter(missionProto.Content.Args, missionProto.Content.Count, shouldCostItem: true)));
			await WaitWhileInUiSateTask();
		}
	}

	[Command("unlock_tutorial", Desc = "解锁教程页面")]
	private static void Command_UnlockTutorial(string tutorialName)
	{
		archiveHandle.UnlockTutorial(tutorialName);
	}

	[Command("open_tutorial_panel", Desc = "打开教程页面")]
	private static UniTask Command_OpenTutorialPanel(string tutorialName)
	{
		EnterUI((TutorialPanelUiState state) => state.HandleStartUpArgs(tutorialName));
		return WaitWhileInUiSateTask();
	}

	[Command("open_building_panel", Desc = "打开建筑蓝图制造页面")]
	private static UniTask Command_OpenBuildingPanel()
	{
		EnterUI<BuildingPanelUiState>();
		return WaitWhileInUiSateTask();
	}

	[Command("show_operation_guide", Desc = "显示常驻操作提示")]
	private static void Command_ShowResidentOperationTip(string id, string customPrompt = null)
	{
		InvokeSceneResidentTip(id, customPrompt);
	}

	[Command("hide_operation_guide", Desc = "关闭常驻操作提示")]
	private static void Command_HideResidentOperationTip(string id)
	{
		HideSceneResidentTip(id);
	}

	[Command("show_new_operation_guidance", Desc = "显示一个新的操作引导")]
	private static void Command_ShowNewOperationGuidance(string eventType, string textKey, string imgUrl)
	{
		uiSystem.GuidanceTips.RegisterEvent(eventType, textKey, imgUrl);
	}

	[Command("open_faction_panel", Desc = "打开势力任务面板")]
	private static UniTask Command_OpenFactionPanel(string factionName)
	{
		if (string.IsNullOrEmpty(factionName))
		{
			outputError("势力名不能为空");
			return UniTask.NextFrame();
		}
		TreatyPortFactionInfo proto = DolocConfig.Tables.TbTreatyPortFaction.GetById(factionName);
		if (proto == null)
		{
			outputError("无效的势力名<" + factionName + ">");
			return UniTask.NextFrame();
		}
		EnterUI((FactionMissionUiState state) => state.HandleStartUpArgs(proto.FactionType));
		return WaitWhileInUiSateTask();
	}

	[Command("open_board_mission_panel", Desc = "打开看板任务面板")]
	private static UniTask Command_OpenBoardMissionPanel()
	{
		EnterUI<BoardMissionUiState>();
		return WaitWhileInUiSateTask();
	}

	[Command("open_plant_doc", Desc = "打开植物档案")]
	private static UniTask Command_OpenPlantDoc()
	{
		EnterUI<PlantDocumentUiState>();
		return WaitWhileInUiSateTask();
	}

	[Command("open_chip_doc", Desc = "打开芯片档案")]
	private static UniTask Command_OpenChipDoc()
	{
		EnterUI<ChipDocumentUiState>();
		return WaitWhileInUiSateTask();
	}

	[Command("open_all_doc", Desc = "打开综合档案页")]
	private static UniTask Command_OpenAllDoc()
	{
		EnterUI<AllDocumentUiState>();
		return WaitWhileInUiSateTask();
	}

	[Command("show_developer_list", Desc = "显示开发者名单")]
	private static UniTask Command_ShowDeveloperList()
	{
		EnterUI<DeveloperListUiState>();
		return WaitWhileInUiSateTask();
	}

	[Command("open_recruit_panel", Desc = "打开势力招募面板")]
	private static UniTask Command_RecruitPanel()
	{
		EnterUI<RecruitPanelUiState>();
		return WaitWhileInUiSateTask();
	}

	[Command("open_input_player_name_panel", Desc = "打开玩家名输入面板")]
	private static UniTask Command_ShowPlayerNameInputBox()
	{
		EnterUI((InputNameUiState state) => state.HandleStartUpArgs(DolocConfig.StaticTexts.InputTitlePlayerName, archiveHandle.GetPlayerName(), archiveHandle.SetPlayerName, GlobalParameter.InputPlayerNameMaxLength));
		return WaitWhileInUiSateTask();
	}

	[Command("open_input_player_birthday_panel", Desc = "打开玩家生日输入面板")]
	private static UniTask Command_ShowPlayerBirthdayInputBox()
	{
		EnterUI((InputBirthdayUiState state) => state.HandleStartUpArgs(archiveHandle.SetPlayerBirthday));
		return WaitWhileInUiSateTask();
	}

	[Command("open_demo_end_panel", Desc = "打开demo结束提示面板")]
	private static UniTask Command_OpenDemoEndPanel()
	{
		EnterUI<DemoEndUiSate>();
		return WaitWhileInUiSateTask();
	}

	[Command("update_builder_state_exit_time", Desc = "更新建造模式的退出时间")]
	public static void UpdateExitTime()
	{
		GlobalBuilderState.lastExitTime = Time.time;
	}

	[Command("open_animal_panel", Desc = "打开畜牧面板")]
	private static UniTask Command_OpenAnimalPanel()
	{
		EnterUI((AnimalPanelUiState state) => state.HandleStartUpArgs(CurrentRoom));
		return WaitWhileInUiSateTask();
	}

	[Command("open_calendar_panel", Desc = "打开日历面板")]
	private static UniTask Command_OpenCalendarPanel()
	{
		EnterUI<CalendarUiState>();
		return WaitWhileInUiSateTask();
	}

	[Command("set_weather", Desc = "设置当前天气并重新渲染")]
	private static void Command_SetWeather(string weather, bool patch = false)
	{
		if (string.IsNullOrEmpty(weather))
		{
			outputError("天气不能为空");
			return;
		}
		if (!Enum.TryParse<WeatherType>(weather, ignoreCase: true, out var result))
		{
			outputError("无效的天气类型<" + weather + ">");
			return;
		}
		archiveHandle.SetWeather(result, shouldRender: true);
		if (patch)
		{
			archiveHandle.PatchWeather(result);
		}
	}

	[Command("show_weather_history", Desc = "显示天气历史记录")]
	private static void Command_ShowWeatherHistory()
	{
		archiveHandle.WeatherSystem.ShowHistory(output);
	}

	[Command("clear_drop_items", Desc = "清空掉落物品")]
	private static void Command_ClearDropItems()
	{
		IDropItemHost currentRoom = archiveHandle.currentRoom;
		if (currentRoom == null)
		{
			UnityEngine.Debug.LogWarning("当前房间不是掉落物宿主");
		}
		else
		{
			currentRoom.Clear();
		}
	}

	public static void PerfectDodgeEffects()
	{
		cameraController.ShakeScreen(0.2f, 0.15f);
		effectProvider.RaiseScreenTwist(agent.PositionCenter, 0.5f);
		RaiseInstantPSEffects(agent.PositionCenter, InstantParticleEffectsType.BRUST_STARS);
	}

	private static void _LoadSwingAnimationCache()
	{
		if (_swingAnimationCache == null)
		{
			RuntimeAnimatorController asset = GetAsset<RuntimeAnimatorController>(DolocGameAssets.GAME_ANIM_UNIVERSAL_PLANT);
			if (asset == null)
			{
				UnityEngine.Debug.LogError("加载植物动画控制器失败");
				_swingAnimationCache = new SwingAnimationCache();
			}
			else
			{
				_swingAnimationCache = new SwingAnimationCache(asset);
			}
		}
	}

	public static void SwingPlantOnTouch(Animator animator, bool light = false)
	{
		if (!(animator == null) && !(animator.runtimeAnimatorController == null))
		{
			if (_swingAnimationCache == null)
			{
				_LoadSwingAnimationCache();
			}
			string text = (light ? _swingAnimationCache.RandomTouchSwingLightName : _swingAnimationCache.RandomTouchSwingName);
			if (!text.IsNullOrEmpty())
			{
				animator.Play(text, 0, 0f);
			}
		}
	}

	public static void SwingPlantOnBlow(Animator animator)
	{
		if (!(animator == null) && !(animator.runtimeAnimatorController == null))
		{
			if (_swingAnimationCache == null)
			{
				_LoadSwingAnimationCache();
			}
			string randomWindSwingName = _swingAnimationCache.RandomWindSwingName;
			if (!randomWindSwingName.IsNullOrEmpty())
			{
				animator.Play(randomWindSwingName, 0, 0f);
			}
		}
	}

	public static void PlaceInBackpackOrGenerateDropItem(Item item, bool checkBox = false)
	{
		if (item != null && PlaceItem(item, checkBox) != null)
		{
			ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrBackpackIsFull);
			GenerateDropItem(archiveHandle.currentRoom, item, AgentPosition);
		}
	}

	public static bool GenerateDropItem(IDropItemHost host, Item item, Vector2 pos, bool shouldSendMsg = true)
	{
		if (host == null || item == null)
		{
			return false;
		}
		pos.y += eftConfig.dungeonResourceDropItemPopYOffset;
		host.CreateDropItem(item, pos, shouldSendMsg);
		return true;
	}

	public static bool GenerateDropItems(IDropItemHost host, ItemSpawnEntry entry, int count, Vector2 startPos, bool shouldSendMsg = true)
	{
		if (host == null)
		{
			return false;
		}
		if (entry?.SpawnLut_Ref == null)
		{
			return false;
		}
		host.CreateDropItemAnimated(entry.SpawnLut_Ref.SpawnItems(count), startPos, shouldSendMsg);
		return true;
	}

	public static bool GenerateDropItems(IDropItemHost host, ItemSpawnEntry entry, Vector2 pos, bool shouldSendMsg = true)
	{
		if (host == null)
		{
			return false;
		}
		if (entry?.SpawnLut_Ref == null)
		{
			return false;
		}
		CountItem[] countItems = entry.SpawnLut_Ref.SpawnItems(entry.CountRange.MinCount, entry.CountRange.MaxCount);
		host.CreateDropItemAnimated(countItems, pos, shouldSendMsg);
		return true;
	}

	public static bool GenerateDropItems(IDropItemHost host, string itemName, Vector2 pos, int count = 1, bool shouldSendMsg = true)
	{
		if (host == null || count <= 0)
		{
			return false;
		}
		pos.y += eftConfig.dungeonResourceDropItemPopYOffset;
		for (int i = 0; i < count; i++)
		{
			if (host == CurrentRoom)
			{
				host.CreateDropItem(itemName, pos, shouldSendMsg);
			}
			else
			{
				host.CreateDropItemNoRender(itemName, pos, shouldSendMsg);
			}
		}
		return true;
	}

	public static bool GenerateDropItems(IDropItemHost host, CountItem countItem, Vector2 pos, bool shouldSendMsg = true)
	{
		if (!countItem.isValid)
		{
			return false;
		}
		return GenerateDropItems(host, countItem.itemName, pos, countItem.itemCount, shouldSendMsg);
	}

	public static Vector2 CalcPopPosition(Transform transform, float rate = 1f)
	{
		Vector2 result = transform.position;
		SpriteRenderer component = transform.GetComponent<SpriteRenderer>();
		if (component != null && component.sprite != null)
		{
			result.y += component.sprite.bounds.size.y * rate;
			return result;
		}
		Collider2D component2 = transform.GetComponent<Collider2D>();
		if (component2 != null)
		{
			result.y += component2.bounds.size.y * rate;
			return result;
		}
		return result;
	}

	public static Vector2 CalcUiPopPosition(Transform transform, float rate = 1f)
	{
		return WorldToScreen(CalcPopPosition(transform, rate));
	}

	public static void ToggleActive(this GameObject obj)
	{
		obj.gameObject.SetActive(!obj.gameObject.activeSelf);
	}

	public static CountItem[] ConvertToCountItems(this Item[] items)
	{
		return (from x in items
			where x != null
			select new CountItem(x.name, x.count)).ToArray();
	}

	public static CountItem[] SpawnItems(string itemName, Vector2Int countRange)
	{
		CountItem countItem = new CountItem(itemName, countRange.DiceCount());
		if (!countItem.isValid)
		{
			return Array.Empty<CountItem>();
		}
		return new CountItem[1] { countItem };
	}

	public static IEnumerable<int> SpawnData(int total, int unit)
	{
		if (total <= 0 || unit <= 0)
		{
			yield break;
		}
		if (total <= unit)
		{
			yield return total;
			yield break;
		}
		int count = ((total % unit == 0) ? (total / unit) : (total / unit + 1));
		for (int i = 0; i < count; i++)
		{
			int num = ((total > unit) ? unit : total);
			total -= num;
			yield return num;
		}
	}

	public static IEnumerable<int> SpawnMoneyData(int totalMoney)
	{
		if (totalMoney < 1000)
		{
			return SpawnData(totalMoney, 100);
		}
		if (totalMoney < 10000)
		{
			List<int> list = new List<int>(SpawnData(1000, 100));
			list.AddRange(SpawnData(totalMoney - 1000, 1000));
			return list;
		}
		if (totalMoney < 50000)
		{
			List<int> list2 = new List<int>(SpawnData(1000, 100));
			list2.AddRange(SpawnData(9000, 1000));
			list2.AddRange(SpawnData(totalMoney - 10000, 5000));
			return list2;
		}
		List<int> list3 = new List<int>(SpawnData(1000, 100));
		list3.AddRange(SpawnData(9000, 1000));
		list3.AddRange(SpawnData(40000, 5000));
		list3.AddRange(SpawnData(totalMoney - 50000, 50000));
		return list3;
	}

	public static List<string> ValidateItems(List<string> items)
	{
		if (items.Count == 0)
		{
			return items;
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>();
		HashSet<string> hashSet2 = new HashSet<string>();
		foreach (string item in items)
		{
			if (!item.IsNullOrEmpty() && !hashSet.Contains(item))
			{
				if (hashSet2.Contains(item))
				{
					list.Add(item);
					continue;
				}
				if (DolocConfig.Tables.TbItem.GetOrDefault(item) == null)
				{
					hashSet.Add(item);
					continue;
				}
				hashSet2.Add(item);
				list.Add(item);
			}
		}
		return list;
	}

	[FastButton("增加100科技经验")]
	private static void FastButton_AddTechPoints()
	{
		AddTechExp(TechPointType.NATURE, 100);
		AddTechExp(TechPointType.ANIMAL, 100);
		AddTechExp(TechPointType.OPERATE, 100);
		AddTechExp(TechPointType.SCIENCE, 100);
		AddTechExp(TechPointType.BATTLE, 100);
		AddTechExp(TechPointType.FISHING, 100);
	}

	[FastButton("清空状态到玩家状态")]
	private static void FastButton_ClearStateToNormal()
	{
		userInput.ClearState(gameStateManager.normalGameState);
	}

	[FastButton("打开种子商店")]
	private static void FastButton_OpenSeedStore()
	{
		EnterUI((StoreUiState state) => state.HandleStartUpArgs("villain_shop"));
	}

	[FastButton("打开势力任务面板")]
	private static void FastButton_OpenFactionPanel()
	{
		EnterUI((FactionMissionUiState state) => state.HandleStartUpArgs(FactionType.Doloc));
	}

	[FastButton("重新加载配置表")]
	private static void FastButton_ReloadConfigTables()
	{
		DolocConfig.Reload();
		SaveGame(archiveHandle.archiveIndex);
		LoadGame(archiveHandle.archiveIndex);
	}

	[DevMenuItem("传送菜单")]
	private static void DevMenuItem_EnterTeleportPanel()
	{
		gameManager.gameInitConfig.EnterTransitionMenu();
	}

	[DevMenuItem("回到农场")]
	private static void DevMenuItem_EnterFarm()
	{
		DoTransport(gameManager.gameInitConfig.initMarkPoint);
	}

	[DevMenuItem("前往多洛可河谷")]
	private static void DevMenuItem_EnterDolocMountain()
	{
		EnterDungeon("多洛可河谷");
	}

	[DevMenuItem("前往湿地")]
	private static void DevMenuItem_EnterWetland()
	{
		EnterDungeon("湿地");
	}

	[DevMenuItem("前往旧城市废墟")]
	private static void DevMenuItem_EnterOldCity()
	{
		EnterDungeon("旧城市废墟");
	}

	[FastButton("重新加载所有mod")]
	private static void FastButton_ReloadMods()
	{
		modManager.ReloadMods();
		DolocConfig.Reload();
	}
}
