using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Room;
using DolocTown.Config.Tile;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[DebugObject]
[JsonObject(MemberSerialization.OptIn)]
public abstract class Room : IDropItemHost, IBaseHost, IMonsterHost, IMissionItemHost, IDungeonResourceHost, IVegetationHost, IEnvObjectHost, IPlatformHost, IBuildingHost, IEquipmentHost, IAnimalHost
{
	private DolocTown.Config.Room.SceneInfo _sceneConfig;

	private RoomInfo _roomInfo;

	private RoomEffectInfo _roomEffectInfo;

	private readonly AutomateSystem _automateSystem;

	private readonly ElectricSystem _electricSystem;

	private readonly AnimalSystem _animalSystem;

	public bool isRenderNow;

	protected bool shouldLightNow;

	protected Dictionary<Vector2Int, TileMaterial> materialMapPt;

	protected Dictionary<Vector2Int, TileMaterial> materialMapBase;

	protected Dictionary<Vector2Int, TileMaterial> materialMapFixedPt;

	private Counter globalTuCounter;

	[JsonProperty]
	private int stopTime;

	public Room CurrentRoom => this;

	public RoomProto baseProto { get; protected set; }

	public abstract RoomType Type { get; }

	public abstract string Title { get; }

	public SceneInfo SceneInfo => baseProto.sceneInfo;

	public DolocTown.Config.Room.SceneInfo SceneConfig
	{
		get
		{
			DolocTown.Config.Room.SceneInfo sceneInfo = _sceneConfig;
			if (sceneInfo == null)
			{
				DolocTown.Config.Room.SceneInfo obj = DolocConfig.Tables.TbScene.GetOrDefault(RoomId) ?? DolocConfig.Tables.TbScene.GetOrDefault(SceneRawName);
				DolocTown.Config.Room.SceneInfo sceneInfo2 = obj;
				_sceneConfig = obj;
				sceneInfo = sceneInfo2;
			}
			return sceneInfo;
		}
	}

	public RoomInfo RoomInfo
	{
		get
		{
			RoomInfo roomInfo = _roomInfo;
			if (roomInfo == null)
			{
				RoomInfo obj = DolocConfig.Tables.TbRoom.GetOrDefault(RoomId) ?? DolocConfig.Tables.TbRoom.GetOrDefault(SceneRawName) ?? DolocConfig.Tables.TbRoom.DataList.First();
				RoomInfo roomInfo2 = obj;
				_roomInfo = obj;
				roomInfo = roomInfo2;
			}
			return roomInfo;
		}
	}

	public RoomSpawnInfo RoomSpawnInfo => RoomInfo.SpawnInfo;

	public RoomConstructInfo RoomConstructInfo => RoomInfo.ConstructInfo;

	public virtual RoomEffectInfo RoomEffectInfo => _roomEffectInfo ?? (_roomEffectInfo = DolocConfig.Tables.TbRoomEffect.DataList.First());

	public abstract string RoomId { get; }

	public RoomGeometry Geometry => baseProto.geometry;

	public bool IsInHouse => baseProto.isInHouse;

	public virtual bool ShouldShowBackground => !baseProto.isInHouse;

	public virtual bool ShouldMaskBackground => false;

	public virtual bool HasActiveLamp
	{
		get
		{
			if (DolocAPI.archiveHandle.ShouldLightUp)
			{
				return ((IEquipmentHost)this).HasActiveLamp();
			}
			return false;
		}
	}

	public bool DisableMotor => RoomInfo.DisableMotor;

	public ISceneHandle SceneHandle { get; private set; }

	[JsonProperty]
	public RoomInteractableObjectManager DM_interactableObject { get; protected set; }

	public int SceneId => SceneInfo.id;

	public string SceneRawName => SceneInfo.name;

	public string SceneShortName => SceneInfo.shortName;

	public string ScenePath => SceneInfo.path;

	public SceneType SceneType => SceneInfo.type;

	public Vector2 ScenePosition => Geometry.scenePosition;

	public Vector2 SceneSize => Geometry.sceneSize;

	public Vector2 CameraPosition => Geometry.scenePosition - new Vector2(Geometry.cameraPadding.x, Geometry.cameraPadding.z);

	public Vector2 CameraSize => Geometry.sceneSize + new Vector2(Geometry.cameraPadding.x + Geometry.cameraPadding.y, Geometry.cameraPadding.z + Geometry.cameraPadding.w);

	public Vector2Int RoomGridSize => Geometry.gridSize;

	public Vector2Int RoomGridPos => Geometry.gridPos;

	public Vector2 RoomPosition => Geometry.roomPosition;

	public Vector2 RoomSize => Geometry.roomSize;

	public Vector2Int[] Obstacles => Geometry.obstacles;

	public Vector2Int[] GroundPositions => Geometry.groundPositions;

	public DropItemManager DM_dropitem { get; private set; }

	public MonsterEnv MonsterEnv { get; private set; }

	public MonsterManager DM_monster { get; private set; }

	public MonsterGenInfoProto MonsterGenInfo => baseProto.monsterInfo;

	public MissionItemManager DM_missionItem { get; private set; }

	public DungeonResourceManager DM_dungeonResource { get; private set; }

	public ResourceGenInfoProto ResourceGenInfo => baseProto.resourceInfo;

	public bool HasResource => DM_dungeonResource.ResourceTotalCount > 0;

	public VegetationManager DM_vegetation { get; private set; }

	public ResourceGenInfoProto VegetationGenInfo => baseProto.vegetationInfo;

	public EnvObjectManager DM_envObject { get; } = new EnvObjectManager();


	public ResourceGenInfoProto EnvObjectGenInfo => baseProto.envObjectGenInfo;

	[JsonProperty]
	public bool blockCreate { get; set; }

	public PlatformManager DM_platform { get; private set; }

	public BuildingManager DM_building { get; private set; }

	public EquipmentManager DM_equipment { get; private set; }

	public AutomateSystem DM_automate { get; private set; }

	public ElectricSystem DM_electric { get; private set; }

	public virtual WeatherInfo CurrentWeatherInfo => DolocAPI.archiveHandle.CurrentWeatherInfo;

	public virtual Room RootRoom { get; private set; }

	public bool ListenEquipmentRemoval { get; set; }

	public AnimalSystem animalSystem { get; private set; }

	public AnimalManager DM_animal { get; private set; }

	public IEnumerable<Animal> AllAnimals
	{
		get
		{
			foreach (Animal allAnimal in DM_animal.AllAnimals)
			{
				yield return allAnimal;
			}
			foreach (Building building in DM_building.Buildings)
			{
				foreach (Animal allAnimal2 in building.room.DM_animal.AllAnimals)
				{
					yield return allAnimal2;
				}
			}
		}
	}

	public int AnimalCount => AllAnimals.Count();

	public bool IsAnimalBuilding
	{
		get
		{
			if (!(this is TemplateRoomInHouse templateRoomInHouse))
			{
				return false;
			}
			return templateRoomInHouse.Building.proto.IsAnimalBuilding;
		}
	}

	public bool ContainsWeatherStation => false;

	public bool IsRenderNow => isRenderNow;

	public Terrain DM_terrain { get; private set; }

	public virtual float GrowthAddition => 0f;

	public virtual float GrowthAdditionFungus => 0f;

	public Vector2Int AgentPositionCell { get; private set; }

	public virtual bool AffectedByMalignantWeather => !IsInHouse;

	public virtual int StopTime
	{
		get
		{
			return stopTime;
		}
		set
		{
			stopTime = value;
		}
	}

	public bool AdjustLightIntensityFlag { get; set; }

	public void RefreshMaterialMapPt()
	{
		materialMapPt = DolocUtils.GenPosToMaterialLut(DolocUtils.GetTileToMatLut(), baseProto.geometry.gridPos, baseProto.geometry.gridSize, DolocAPI.farmRenderer.RM_platformTilemap);
	}

	protected Room(RoomProto proto)
	{
		baseProto = proto;
		DM_monster = new MonsterManager();
		DM_dropitem = new DropItemManager();
		DM_missionItem = new MissionItemManager();
		DM_dungeonResource = new DungeonResourceManager();
		DM_vegetation = new VegetationManager();
		DM_envObject = new EnvObjectManager();
		DM_platform = new PlatformManager();
		DM_building = new BuildingManager();
		DM_equipment = new EquipmentManager();
		DM_animal = new AnimalManager();
		_automateSystem = new AutomateSystem(this);
		_electricSystem = new ElectricSystem();
		_animalSystem = new AnimalSystem();
		DM_automate = _automateSystem;
		DM_electric = _electricSystem;
		animalSystem = _animalSystem;
		RootRoom = this;
		globalTuCounter = DolocAPI.GlobalParameter.NewTuCounter;
		ResetTerrain();
		MonsterEnv = new MonsterEnv(this);
	}

	protected Room(DropItemManager DM_dropitem, RoomInteractableObjectManager DM_interactableObject, MissionItemManager DM_missionItem = null, DungeonResourceManager DM_dungeonResource = null, VegetationManager DM_vegetation = null, PlatformManager DM_platform = null, BuildingManager DM_building = null, EquipmentManager DM_equipment = null, AnimalManager DM_animal = null, int stopTime = 0, bool blockCreate = false)
	{
		globalTuCounter = DolocAPI.GlobalParameter.NewTuCounter;
		_automateSystem = new AutomateSystem(this);
		_electricSystem = new ElectricSystem();
		_animalSystem = new AnimalSystem();
		baseProto = null;
		DM_monster = new MonsterManager();
		this.DM_dropitem = DM_dropitem;
		this.DM_interactableObject = DM_interactableObject ?? new RoomInteractableObjectManager();
		this.DM_missionItem = DM_missionItem ?? new MissionItemManager();
		this.DM_dungeonResource = DM_dungeonResource ?? new DungeonResourceManager();
		this.DM_vegetation = DM_vegetation ?? new VegetationManager();
		this.DM_platform = DM_platform ?? new PlatformManager();
		this.DM_building = DM_building ?? new BuildingManager();
		this.DM_equipment = DM_equipment ?? new EquipmentManager();
		this.DM_animal = DM_animal ?? new AnimalManager();
		DM_automate = _automateSystem;
		DM_electric = _electricSystem;
		animalSystem = _animalSystem;
		this.blockCreate = blockCreate;
		this.stopTime = stopTime;
		SetParent(this);
	}

	public void SetParent(Room room)
	{
		if (room == null)
		{
			RootRoom = this;
			DM_automate = _automateSystem;
			DM_electric = _electricSystem;
			animalSystem = _animalSystem;
		}
		else
		{
			RootRoom = room;
			DM_automate = room.DM_automate;
			DM_electric = room.DM_electric;
			animalSystem = room.animalSystem;
		}
	}

	public virtual void __AfterNewGame()
	{
		((IMonsterHost)this).GenerateMonstersData();
		((IDungeonResourceHost)this).AfterNewGame();
		((IVegetationHost)this).AfterNewGame();
		((IBuildingHost)this).AfterNewGame();
		((IEquipmentHost)this).AfterNewGame();
	}

	public virtual void __AfterLoadData()
	{
		AfterLoadData();
		LoadAllDataFinally();
	}

	protected virtual void AfterLoadData()
	{
		baseProto = GetBaseRoomProto();
		MonsterEnv = new MonsterEnv(this);
		ResetTerrain();
		((IMonsterHost)this).GenerateMonstersData();
		((IDropItemHost)this).__AfterLoadDropItems();
		((IMissionItemHost)this).__AfterLoadMissionItems();
		((IDungeonResourceHost)this).__AfterLoadDungeonResources();
		((IVegetationHost)this).__AfterLoadVegetation();
		((IPlatformHost)this).__AfterLoadPlatforms();
		((IBuildingHost)this).AfterLoadBuildings();
		((IEquipmentHost)this).__AfterLoadEquipments();
		((IAnimalHost)this).__AfterLoadAnimals();
		AfterLoadData_LoadWeather();
	}

	private void AfterLoadData_LoadWeather()
	{
		if (baseProto.roomType != RoomType.Farm || !baseProto.isInHouse)
		{
			SetWeatherInfo(DolocAPI.archiveHandle.CurrentWeatherType);
		}
	}

	public void ResetTerrain()
	{
		DM_terrain = Terrain.Create(baseProto.geometry.gridSize, baseProto.geometry.obstacles, baseProto.geometry.extraObstacles);
	}

	protected virtual void LoadAllDataFinally()
	{
		this.__AfterLoadAllTerrain();
	}

	protected abstract RoomProto GetBaseRoomProto();

	public virtual void OnEnterRoom()
	{
		Debug.Log("进入房间\"" + RoomId + "\"");
		SceneHandle = LoadSceneHandle();
		SceneHandle?.OnEnterRoom(this);
		DolocAPI.SetEnvCamera(CameraPosition, CameraSize, ShouldShowBackground, ShouldMaskBackground);
		DolocAPI.cameraController.SetPosition(DolocAPI.AgentPosition);
		DolocAPI.agent.MotionAbility.ClearEnvModerate();
		DolocAPI.archiveHandle.currentRoom = this;
		DolocAPI.uiSystem.basicTip.RefreshPosition();
		((IDropItemHost)this).SetAllShieldCollector(shield: false);
		this.HandleBackground();
		DolocAPI.agent.RefreshSortingLayerInRoom();
		DolocAPI.gameStateManager.normalGameState.SetRoom(this);
		DolocAPI.DelayFrame(DolocAPI.ReQuickSelectCurrentItem);
		if (Type != RoomType.Dungeon)
		{
			Render();
		}
		else
		{
			DolocAPI.archiveHandle.CurrentDungeon._RenderNearRooms(this);
		}
		LoadMaterialMaps(SceneHandle);
		DolocAPI.gameStateManager.agentController.SetRoom(this);
		BroadcastRoomMessage();
		DM_vegetation.UpdateLuminousPlantState();
		if (this is TemplateRoomInHouse templateRoomInHouse && templateRoomInHouse.Building.proto.DisableWeather)
		{
			DolocAPI.EnvCovariantController.SetCurrentWeatherRendererEnabled(value: false, 0f, transit: false);
		}
	}

	public virtual void OnExitRoom(Room nextRoom)
	{
		DolocAPI.QuickDeselectCurrentItem();
		StopTime = DolocAPI.archiveHandle.timeData.totalSeconds;
		DolocAPI.gameStateManager.agentController.SetRoom(null);
		if (Type != RoomType.Dungeon)
		{
			ClearRender();
		}
		else
		{
			DolocAPI.archiveHandle.CurrentDungeon?._ExitCurrentRoom(nextRoom);
		}
		if (this is TemplateRoomInHouse templateRoomInHouse && templateRoomInHouse.Building.proto.DisableWeather)
		{
			DolocAPI.EnvCovariantController.SetCurrentWeatherRendererEnabled(value: true, 0f, transit: false);
		}
	}

	public virtual void Render(bool includeMonster = true)
	{
		if (!isRenderNow)
		{
			isRenderNow = true;
			((IDropItemHost)this).RenderAllDropItems();
			if (includeMonster)
			{
				((IMonsterHost)this).RunMonsters();
			}
			((IMissionItemHost)this).RenderAllMissionItems();
			((IDungeonResourceHost)this).RenderAllResources();
			((IVegetationHost)this).RenderAllVegetation();
			((IEnvObjectHost)this).RenderAllEnvObject();
			((IPlatformHost)this).RenderAllPlatforms();
			((IBuildingHost)this).RenderAllBuildings();
			((IEquipmentHost)this).RenderAllEquipments();
			if (SceneHandle == null)
			{
				ISceneHandle sceneHandle2 = (SceneHandle = LoadSceneHandle());
			}
			if (SceneHandle != null)
			{
				SceneHandle.Render(this);
			}
			else
			{
				Debug.LogError("房间" + _roomInfo.Id + "的场景控制柄为空，无法渲染");
			}
		}
	}

	public virtual void ClearRender()
	{
		Debug.Log("清空房间" + SceneRawName + "." + baseProto.name + "的渲染");
		if (isRenderNow)
		{
			isRenderNow = false;
			((IDropItemHost)this).HideAllDropItems();
			((IMonsterHost)this).HideAllMonsters();
			((IMissionItemHost)this).HideAllMissionItems();
			((IDungeonResourceHost)this).HideAllDungeonResources();
			((IVegetationHost)this).HideAllVegetation();
			((IEnvObjectHost)this).HideAllEnvObject();
			((IBuildingHost)this).HideAllBuildings();
			((IPlatformHost)this).HideAllPlatforms();
			((IEquipmentHost)this).HideAllEquipments();
			SceneHandle?.ClearRender();
			SceneHandle = null;
		}
	}

	public void RefreshRender()
	{
		if (isRenderNow)
		{
			ClearRender();
			Render();
		}
	}

	public void LoadMaterialMaps(ISceneHandle handle)
	{
		if (handle != null)
		{
			Dictionary<string, TileMaterial> tileToMatLut = DolocUtils.GetTileToMatLut();
			Vector2Int gridPos = baseProto.geometry.gridPos;
			Vector2Int gridSize = baseProto.geometry.gridSize;
			if (handle.Tilemap != null && materialMapBase == null)
			{
				materialMapBase = ((handle.TilemapDct != null) ? DolocUtils.GenPosToMaterialLut(tileToMatLut, handle.Tilemap, handle.TilemapDct, gridPos, gridSize) : DolocUtils.GenPosToMaterialLut(tileToMatLut, gridPos, gridSize, handle.Tilemap));
			}
			if (handle.TilemapPt != null && materialMapPt == null)
			{
				materialMapPt = DolocUtils.GenPosToMaterialLut(tileToMatLut, gridPos, gridSize, handle.TilemapPt);
			}
			RefreshMaterialMapPt();
		}
	}

	private void BroadcastRoomMessage()
	{
		switch (baseProto.roomType)
		{
		case RoomType.City:
			DolocAPI.BroadcastString(GameEventType.ARRIVE_ROOM_CITY, baseProto.name);
			break;
		case RoomType.Dungeon:
		{
			Dungeon currentDungeon = DolocAPI.archiveHandle.CurrentDungeon;
			string value = ((currentDungeon == null) ? string.Empty : currentDungeon.ProtoName) + "." + baseProto.name;
			DolocAPI.BroadcastString(GameEventType.ARRIVE_ROOM_DUNGEON, value);
			break;
		}
		case RoomType.Farm:
			break;
		}
	}

	public ISceneHandle LoadSceneHandle()
	{
		if (SceneHandle != null)
		{
			return SceneHandle;
		}
		string roomName = ((baseProto.roomType == RoomType.Dungeon) ? baseProto.name : null);
		ISceneHandle sceneHandle = DolocAPI.sceneManager.GetSceneHandle(SceneRawName, roomName);
		if (sceneHandle != null)
		{
			return sceneHandle;
		}
		Debug.LogError("未找到场景\"" + SceneRawName + "\"的控制柄，请检查场景是否正确配置");
		return null;
	}

	public virtual bool GetTileMaterial(Vector2 position, out TileMaterial material)
	{
		material = TileMaterial.NONE;
		if (baseProto.isInHouse)
		{
			material = baseProto.defaultTileMaterial;
			return true;
		}
		position.y -= 0.75f;
		Vector2Int key = Geometry.CalcFaceCellPosition(position, resetPos: false);
		key += Geometry.gridPos;
		Dictionary<Vector2Int, TileMaterial> dictionary = materialMapBase;
		if (dictionary != null && dictionary.TryGetValue(key, out material))
		{
			return true;
		}
		Dictionary<Vector2Int, TileMaterial> dictionary2 = materialMapFixedPt;
		if (dictionary2 != null && dictionary2.TryGetValue(key, out material))
		{
			return true;
		}
		Dictionary<Vector2Int, TileMaterial> dictionary3 = materialMapPt;
		if (dictionary3 != null && dictionary3.TryGetValue(key, out material))
		{
			return true;
		}
		return false;
	}

	public virtual void SetNonEnvLight(bool value, bool shouldRender = true)
	{
		shouldLightNow = value;
		foreach (Lamp item in DM_equipment.ForEachEquipments<Lamp>())
		{
			item.ToggleLight(value, shouldRender);
		}
	}

	public virtual void SetEnvLight(float process, float intensity, Color color)
	{
	}

	public virtual void InitRoom()
	{
		RoomPresetObjectProto[] presetObjects = baseProto.presetObjects;
		foreach (RoomPresetObjectProto presetObject in presetObjects)
		{
			SetPresetObject(presetObject);
		}
	}

	public virtual void SetPresetObject(RoomPresetObjectProto preset)
	{
	}

	public virtual bool ReGenRoomDatas()
	{
		DM_monster.Clear();
		((IMonsterHost)this).GenerateMonstersData();
		return true;
	}

	public virtual bool TryGetHost<T>(out T host) where T : class, IBaseHost
	{
		if (this is T val)
		{
			host = val;
			return true;
		}
		host = null;
		return false;
	}

	public int GetRoomValidHeight()
	{
		int num = 0;
		foreach (Vector2Int item in DM_terrain.Size.IterateGrid().Except(Geometry.obstacles))
		{
			if (item.y > num)
			{
				num = item.y;
			}
		}
		return Mathf.Min(num + 1, DM_terrain.Size.y);
	}

	public void RefreshAnimalEnv()
	{
		animalSystem.RefreshEnv(this);
	}

	public void OnPlatformChanged()
	{
		animalSystem.OnPlatformChanged(this);
	}

	public void OnBuildingChanged(Building building, bool isRemoved)
	{
		animalSystem.OnBuildingChanged(this);
		DM_automate.OnBuildingChanged(building, isRemoved);
	}

	public bool TerrainExtend(RoomProto extendProto)
	{
		if (extendProto.isInHouse != baseProto.isInHouse || extendProto.roomType != RoomType.Farm)
		{
			return false;
		}
		if (extendProto.roomType != RoomType.Farm)
		{
			return false;
		}
		if (extendProto.geometry.gridSize.x < Geometry.gridSize.x || extendProto.geometry.gridSize.y < Geometry.gridSize.y)
		{
			Debug.LogWarning("扩展农场的规模必须比现有规模更大");
			return false;
		}
		Vector2Int offset = new Vector2Int(extendProto.geometry.gridSize.x - Geometry.gridSize.x, 0);
		Vector3 positionOffset = (extendProto.isInHouse ? new Vector3((float)offset.x * 1.5f, 0f) : Vector3.zero);
		baseProto = extendProto;
		DM_terrain.RemoveGroundTerrain();
		IEnumerable<TerrainContent> allContents = DM_terrain.AllContents;
		ResetTerrain();
		foreach (TerrainContent item in allContents)
		{
			if (!(item is Equipment equipment) || !equipment.proto.isDecal)
			{
				item.ShiftTerrainContent(offset, positionOffset);
				DM_terrain.FillContent(item);
			}
		}
		OnTerrainExtent(offset);
		return true;
	}

	protected virtual void OnTerrainExtent(Vector2Int offset)
	{
		InitRoom();
		((RoomHandle)SceneHandle)?.DisableAllLevelsExcludeCurrent(this);
		DM_platform.UpdateAllColliderInfos();
		((IBuildingHost)this).GenerateAllBuildingSupport();
		animalSystem.OnTerrainExtent(this, offset);
	}

	private void CalcAgentPositionCell()
	{
		AgentPositionCell = baseProto.geometry.CalcCellPosition(DolocAPI.AgentPosition);
	}

	public virtual void SetWeatherInfo(WeatherType weatherType)
	{
		SceneHandle?.OnWeatherChanged(weatherType);
		foreach (Equipment allEquipment in DM_equipment.AllEquipments)
		{
			allEquipment.decorator.SetWeatherType(weatherType);
		}
		foreach (Building building in DM_building.Buildings)
		{
			if (!building.IsBroken)
			{
				continue;
			}
			foreach (Equipment allEquipment2 in building.room.DM_equipment.AllEquipments)
			{
				allEquipment2.decorator.SetWeatherType(weatherType);
			}
		}
		_animalSystem.OnWeatherChanged(weatherType);
	}

	public virtual void Update()
	{
		CalcAgentPositionCell();
		if (baseProto.isInHouse)
		{
			if (globalTuCounter.Tick())
			{
				SceneHandle?.UpdatePerTu();
			}
			DM_equipment.Update();
			AdjustLightIntensity();
			return;
		}
		DM_electric.Update();
		DM_automate.Update();
		bool flag = globalTuCounter.Tick();
		if (flag)
		{
			_UpdateLights();
		}
		AdjustLightIntensity();
		if (IsRenderNow)
		{
			if (flag)
			{
				SceneHandle?.UpdatePerTu();
			}
			DM_equipment.Update();
			DM_building.UpdateNoRender();
			if (flag)
			{
				HandleWeather(CurrentWeatherInfo);
				DM_dungeonResource.GrowNatureElements(isRender: true);
				DM_vegetation.UpdatePerTu();
				((IVegetationHost)this).GenNewVegetation(randomGrowthLevel: false);
			}
			DolocAPI.TryExecute(animalSystem.Update);
			return;
		}
		DM_equipment.UpdateNoRender();
		DM_building.UpdateSpec();
		if (flag)
		{
			if (SceneType != SceneType.DUNGEON)
			{
				((IDungeonResourceHost)this).GenNewDungeonResource();
				DM_dungeonResource.GrowNatureElements(isRender: false);
				DM_vegetation.UpdatePerTuNoRender();
				((IVegetationHost)this).GenNewVegetation(randomGrowthLevel: false);
			}
			HandleWeather(CurrentWeatherInfo);
		}
		DolocAPI.TryExecute(animalSystem.Update);
	}

	public virtual void UpdateNoRender()
	{
		CalcAgentPositionCell();
		if (baseProto.isInHouse)
		{
			DM_equipment.UpdateNoRender();
			return;
		}
		DM_electric.Update();
		DM_automate.Update();
		DM_equipment.UpdateNoRender();
		DM_building.UpdateSpec();
		if (globalTuCounter.Tick())
		{
			if (SceneType != SceneType.DUNGEON)
			{
				((IDungeonResourceHost)this).GenNewDungeonResource();
				DM_dungeonResource.GrowNatureElements(isRender: false);
				DM_vegetation.UpdatePerTuNoRender();
				((IVegetationHost)this).GenNewVegetation(randomGrowthLevel: false);
			}
			HandleWeather(CurrentWeatherInfo);
		}
		DolocAPI.TryExecute(animalSystem.UpdateNoRender);
	}

	public virtual void BeforeTimePass()
	{
		SceneHandle?.BeforeTimePass();
		animalSystem.BeforePassTime();
		ClearRender();
	}

	public virtual void AfterTimePass()
	{
		SceneHandle?.AfterTimePass();
		Render();
		if (baseProto.roomType == RoomType.Dungeon)
		{
			((IMonsterHost)this).RunMonsters();
		}
		DM_vegetation.UpdateLuminousPlantState();
		animalSystem.AfterPassTime();
	}

	public virtual void UpdatePerHour(int hourNow)
	{
		SceneHandle?.UpdatePerHour(hourNow);
	}

	public virtual void UpdatePerHourNoRender(int hourNow)
	{
	}

	protected void _UpdateLights()
	{
		if (shouldLightNow == DolocAPI.archiveHandle.ShouldLightUp)
		{
			return;
		}
		shouldLightNow = !shouldLightNow;
		if (IsRenderNow)
		{
			SetNonEnvLight(shouldLightNow);
			{
				foreach (Building building in DM_building.Buildings)
				{
					building.room.SetNonEnvLight(shouldLightNow, shouldRender: false);
				}
				return;
			}
		}
		SetNonEnvLight(shouldLightNow, shouldRender: false);
		foreach (Building building2 in DM_building.Buildings)
		{
			building2.room.SetNonEnvLight(shouldLightNow, building2.room.IsRenderNow);
		}
	}

	protected void AdjustLightIntensity()
	{
		if (!AdjustLightIntensityFlag)
		{
			return;
		}
		foreach (Equipment allEquipment in ((IEquipmentHost)this).AllEquipments)
		{
			if (allEquipment is ILamp lamp)
			{
				lamp.RefreshLightIntensity();
			}
		}
		AdjustLightIntensityFlag = false;
	}

	protected void HandleWeather(WeatherInfo weatherInfo)
	{
		if (!weatherInfo.IsMalignantWeather || weatherInfo.Id != WeatherType.ACID_RAIN || DolocAPI.gameManager.gameInitConfig.buildingInvincible)
		{
			return;
		}
		foreach (Building building in DM_building.Buildings)
		{
			building.Damage(building.proto.DamageSufferRate * DolocAPI.GlobalParameter.AcidRainDamage);
		}
	}

	public void OnMonthChange(bool isRender)
	{
		((IDungeonResourceHost)this).RemoveResourceOnMonthChange(isRender);
		((IVegetationHost)this).ReGenRoomVegetation(isRender);
	}
}
