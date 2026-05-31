using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Player;
using DolocTown.Config.Room;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public class FarmArchiveData : IDataPersistence
{
	[JsonProperty]
	public AgentArchiveData agentData;

	[JsonProperty]
	public List<string> collectEquipments;

	[JsonProperty]
	public readonly List<string> unlockedBuildings;

	[JsonProperty]
	public readonly RecipeManager recipeManager;

	[JsonProperty]
	public HashSet<string> unlockedTechTree;

	[JsonProperty]
	public TechLevelManager techLevelManager;

	[JsonProperty]
	public HashSet<string> unlockedTechNodes;

	[JsonProperty]
	public List<string> lockedTechNodes;

	[JsonProperty]
	public HashSet<string> unlockedSeedNodes;

	[JsonProperty]
	public HashSet<string> seedsCanUnlock;

	[JsonProperty]
	public List<string> unlockedTutorials;

	[JsonProperty]
	public CollectionManager collectionManager;

	[JsonProperty]
	public bool hasAcidRainCamed;

	[JsonProperty]
	public UnlockDataCollection unlockDataCollection;

	[JsonProperty]
	public List<string> unlockedGeneNames;

	[JsonProperty]
	protected TemplateRoomOutdoor farm;

	public Room currentRoom;

	private string _currentRoomId;

	[JsonProperty]
	public readonly InventorySystem inventory;

	[JsonProperty]
	public readonly GameEventRecorderManager eventRecorderManager;

	[JsonProperty]
	public readonly MissionManager missionManager;

	[JsonProperty]
	public readonly MissionChainManager missionChainManager;

	[JsonProperty]
	public readonly EmailManager emailManager;

	[JsonProperty]
	public readonly MapManager mapManager;

	[JsonProperty]
	public readonly EnvOptimizerSystem envOptimizerSystem;

	private bool backpackFullCache;

	[JsonProperty]
	private string currentRoomId => currentRoom.RoomId;

	public TemplateRoomOutdoor MainFarm
	{
		get
		{
			return farm;
		}
		set
		{
			if (value != null)
			{
				farm = value;
			}
		}
	}

	public string ArchiveRoomId => _currentRoomId;

	public RoomType currentRoomType => currentRoom?.Type ?? RoomType.None;

	public string currentSceneName => currentRoom?.SceneRawName;

	public string currentRoomName => currentRoom?.Title;

	public int currentSceneId => currentRoom?.SceneId ?? (-1);

	public FarmArchiveData()
	{
		agentData = new AgentArchiveData();
		techLevelManager = new TechLevelManager();
		BackpackLevelInfo byLevel = DolocConfig.Tables.TbBackpackLevel.GetByLevel(agentData.backpackLevel);
		inventory = new InventorySystem(byLevel.Capacity, DolocAPI.GlobalParameter.InventoryLineCapacity);
		inventory.buffer.BindReceiver(DolocAPI.uiSystem.inventoryMouse.Render);
		FarmLevelInfo byLevel2 = DolocConfig.Tables.TbFarmLevel.GetByLevel(agentData.farmLevel);
		if (byLevel2 != null && DolocAPI.assets.rooms.QueryData(byLevel2.Id, out var data) && data.roomType == RoomType.Farm)
		{
			farm = new TemplateRoomOutdoor(string.Empty, data);
			unlockedTechTree = new HashSet<string>();
			unlockedTechNodes = new HashSet<string>();
			lockedTechNodes = new List<string>();
			collectEquipments = new List<string>();
			recipeManager = new RecipeManager();
			unlockedSeedNodes = new HashSet<string>();
			seedsCanUnlock = new HashSet<string>();
			unlockedBuildings = new List<string>();
			unlockedTutorials = new List<string>();
			collectionManager = new CollectionManager();
			unlockedGeneNames = new List<string>();
			missionChainManager = new MissionChainManager();
			missionManager = new MissionManager(missionChainManager.__OnMissionComplete);
			eventRecorderManager = new GameEventRecorderManager();
			emailManager = new EmailManager();
			mapManager = new MapManager();
			envOptimizerSystem = new EnvOptimizerSystem();
			unlockDataCollection = new UnlockDataCollection();
			return;
		}
		throw new DolocInitError("未找到农场模板: \"" + (byLevel2?.Id ?? agentData.farmLevel.ToString()) + "\"");
	}

	[JsonConstructor]
	private FarmArchiveData(InventorySystem inventory, GameEventRecorderManager eventRecorderManager, MissionManager missionManager, MissionChainManager missionChainManager, EmailManager emailManager, MapManager mapManager, EnvOptimizerSystem envOptimizerSystem, AgentArchiveData agentData, List<string> unlockedBuildings, RecipeManager recipeManager, HashSet<string> unlockedTechTree, TechLevelManager techLevelManager, HashSet<string> unlockedTechNodes, List<string> lockedTechNodes, HashSet<string> unlockedSeedNodes, HashSet<string> seedsCanUnlock, List<string> collectEquipments, bool isExpressDroneUnlocked, List<string> unlockedTutorials, CollectionManager collectionManager, bool hasAcidRainCamed, AchievementSystem achievementSystem, UnlockDataCollection unlockDataCollection, List<string> unlockedGeneNames, TemplateRoomOutdoor farm, string currentRoomId)
	{
		this.inventory = inventory;
		this.eventRecorderManager = eventRecorderManager;
		this.missionManager = missionManager;
		this.missionChainManager = missionChainManager;
		this.emailManager = emailManager;
		this.mapManager = mapManager ?? new MapManager();
		this.envOptimizerSystem = envOptimizerSystem ?? new EnvOptimizerSystem();
		this.agentData = agentData;
		this.unlockedBuildings = unlockedBuildings;
		this.collectEquipments = collectEquipments;
		this.recipeManager = recipeManager;
		this.unlockedTechTree = unlockedTechTree ?? new HashSet<string>();
		this.techLevelManager = techLevelManager ?? new TechLevelManager();
		this.unlockedTechNodes = unlockedTechNodes;
		this.lockedTechNodes = lockedTechNodes ?? new List<string>();
		this.unlockedSeedNodes = unlockedSeedNodes;
		this.seedsCanUnlock = seedsCanUnlock ?? new HashSet<string>();
		this.unlockedTutorials = unlockedTutorials;
		this.collectionManager = collectionManager ?? new CollectionManager();
		this.hasAcidRainCamed = hasAcidRainCamed;
		this.unlockDataCollection = unlockDataCollection ?? new UnlockDataCollection();
		this.unlockedGeneNames = unlockedGeneNames ?? new List<string>();
		this.farm = farm;
		currentRoom = null;
		_currentRoomId = currentRoomId;
	}

	public void BeforeNewGame()
	{
		DolocAPI.sceneManager.UnloadAll();
	}

	public void AfterNewGame(ref ArchiveDataHandle data)
	{
		farm.__AfterNewGame();
		inventory.inventory.AddReceiver(CheckBackpackFirstFull);
	}

	public void BeforeSaveData(ref ArchiveDataHandle data)
	{
	}

	public void AfterSaveData(ref ArchiveDataHandle data)
	{
	}

	public void BeforeLoadData(ref ArchiveDataHandle data)
	{
		DolocAPI.sceneManager.UnloadAll();
	}

	public void AfterLoadData(ref ArchiveDataHandle data)
	{
		farm.__AfterLoadData();
		collectionManager.AfterLoadData();
		DolocAPI.archiveHandle.ValidateBackpackCapacity();
		inventory.ReleaseBufferBack();
		inventory.inventory.AddReceiver(CheckBackpackFirstFull);
		inventory.buffer.BindReceiver(DolocAPI.uiSystem.inventoryMouse.Render);
		envOptimizerSystem.AfterLoadData();
		missionManager.__SetCallbackOnMissionCompleted(missionChainManager.__OnMissionComplete);
		missionManager.__AfterLoadMission();
		missionChainManager.AfterLoadData();
		currentRoom = null;
		MarkPointInfo value;
		if (DolocAPI.QueryRoom(_currentRoomId, out var room))
		{
			DolocAPI.EnterRoom(room, agentData._agentPosition);
		}
		else if (DolocConfig.Tables.TbMarkPoint.DataMap.TryGetValue(DolocAPI.gameManager.gameInitConfig.initMarkPoint, out value) && DolocAPI.QueryRoom(value.RoomId, out room))
		{
			DolocAPI.EnterRoom(room, value.Position);
		}
		else
		{
			RoomGeometry geometry = MainFarm.Geometry;
			Vector2 position = geometry.CalcWorldPosition(geometry.groundPositions[0], new Vector2(0.5f, 0.1f));
			DolocAPI.EnterRoom(MainFarm, position);
		}
		agentData.AfterLoadData();
		techLevelManager.AfterLoadData();
	}

	private void CheckBackpackFirstFull(int index, Item item, bool isSlotLocked)
	{
		if (backpackFullCache || eventRecorderManager.GetTotalCount(GameEventType.BACKPACK_FIRST_FULL) > 0)
		{
			backpackFullCache = true;
		}
		else if (inventory.inventory.isFull)
		{
			DolocAPI.Broadcast(GameEventType.BACKPACK_FIRST_FULL);
			backpackFullCache = true;
		}
	}
}
