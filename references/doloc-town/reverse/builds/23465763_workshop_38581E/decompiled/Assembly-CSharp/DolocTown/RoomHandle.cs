using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Room;
using DolocTown.Config.Tile;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using RedSaw;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace DolocTown;

[DisallowMultipleComponent]
public class RoomHandle : MonoBehaviour, ISceneHandle
{
	[SerializeField]
	public Tilemap resourceLayerTree;

	[SerializeField]
	public Tilemap resourceLayerOther;

	[SerializeField]
	public Tilemap envObjectLayer;

	[FormerlySerializedAs("resourceLayerPlantObstacle")]
	[SerializeField]
	[Tooltip("仅作用于地牢资源，不作用于植被，为空时，将生成空的坐标列表")]
	public Tilemap envObstaclesLayer;

	[SerializeField]
	public Tilemap monsterLayer;

	[SerializeField]
	public Tilemap vegetationLayer;

	[SerializeField]
	public bool autoGenData = true;

	[SerializeField]
	public RoomType roomType;

	[SerializeField]
	public bool isIndoorRoom;

	[SerializeField]
	private TileMaterial tileMaterial;

	[SerializeField]
	[Tooltip("设置为空时使用默认背景")]
	public EnvBackgroundSO background;

	[SerializeField]
	public InteractableObject[] interactableObjects;

	[SerializeField]
	public Vector2 scenePos;

	[SerializeField]
	public Vector2 sceneSize = new Vector2(60f, 33.75f);

	[SerializeField]
	public bool useCameraPadding;

	[SerializeField]
	public float cameraPaddingTop;

	[SerializeField]
	public float cameraPaddingBottom;

	[SerializeField]
	public float cameraPaddingLeft;

	[SerializeField]
	public float cameraPaddingRight;

	[SerializeField]
	private bool lockColliderSize;

	[SerializeField]
	public EdgeCollider2D airWallCollider;

	[SerializeField]
	protected float airWallColliderPadding;

	[SerializeField]
	[Tooltip("地板高度，用于决定Npc处于该场景时的目标高度，处于室内时会被室内房间高度覆盖，处于室外时最终=室外相机活动位置起点+地板高度")]
	public float floorHeight;

	[SerializeField]
	[Tooltip("默认入口点，目前仅用于农场室内房间，相对坐标(起点是房间坐标)")]
	public Vector2 defaultEntryPosition;

	[SerializeField]
	public Vector2 roomPosition = Vector2.zero;

	[SerializeField]
	public Vector2 roomSize = Vector2.zero;

	[SerializeField]
	private Tilemap tilemap;

	[SerializeField]
	private Tilemap obstacleTilemap;

	[SerializeField]
	private Tilemap decoratorTilemap;

	[SerializeField]
	private Tilemap platformTilemap;

	private TimeAffectableManager _timeAffectableManager;

	private WaterHandle[] _waterHandles;

	private RoomHandle[] _terrainExtensionLevels;

	[SerializeField]
	private Material[] postProcessingEffects;

	private static string drawRoomName;

	private static Vector2Int[] obstaclesCache;

	private static Vector2Int[] groundPositionsCache;

	[SerializeField]
	public bool disableGizmos;

	private bool _checkCameraSize
	{
		get
		{
			Vector2 vector = sceneSize;
			if (vector.x >= 60f)
			{
				return vector.y >= 33.75f;
			}
			return false;
		}
	}

	private Sprite MainSprite
	{
		get
		{
			SpriteRenderer component = GetComponent<SpriteRenderer>();
			if (!(component == null))
			{
				return component.sprite;
			}
			return null;
		}
	}

	public GameObject GameObject => base.gameObject;

	public Tilemap Tilemap => tilemap;

	public Tilemap TilemapPt => platformTilemap;

	public Tilemap TilemapDct => decoratorTilemap;

	private Vector2 cellSize
	{
		get
		{
			if (!(tilemap != null))
			{
				return DolocTransform.TILE_WORLD_SIZE;
			}
			return tilemap.cellSize;
		}
	}

	public ResourceGenInfoSO CreateResourceGenInfoSO(Vector2Int[] groundPositions, Vector2Int gridPos)
	{
		RoomInfo orDefault = DolocConfig.Tables.TbRoom.GetOrDefault(GetRoomId());
		bool flag = orDefault != null && !orDefault.SpawnInfo.ResourceSpawnEntry.IsEmpty;
		if (resourceLayerOther == null && resourceLayerTree == null)
		{
			if (flag)
			{
				Debug.LogError("房间<" + GetRoomId() + ">没有指定资源瓦片层");
			}
			return null;
		}
		if (groundPositions.IsNullOrEmpty())
		{
			return null;
		}
		return new ResourceGenInfoSO(RoomGeneratorUtils.GenResourceConstraint(groundPositions, resourceLayerOther, resourceLayerTree, gridPos));
	}

	private void Start()
	{
		DisableEditorLayers();
	}

	private void DisableEditorLayers()
	{
		if (resourceLayerOther != null)
		{
			resourceLayerOther.gameObject.SetActive(value: false);
		}
		if (resourceLayerTree != null)
		{
			resourceLayerTree.gameObject.SetActive(value: false);
		}
		if (vegetationLayer != null)
		{
			vegetationLayer.gameObject.SetActive(value: false);
		}
		if (monsterLayer != null)
		{
			monsterLayer.gameObject.SetActive(value: false);
		}
		if (envObjectLayer != null)
		{
			envObjectLayer.gameObject.SetActive(value: false);
		}
	}

	public ResourceGenInfoSO CreateEnvObjectGenInfoSO()
	{
		RoomInfo orDefault = DolocConfig.Tables.TbRoom.GetOrDefault(GetRoomId());
		bool flag = orDefault != null && !orDefault.SpawnInfo.EnvObjectSpawnEntry.IsEmpty;
		if (envObjectLayer == null)
		{
			if (flag)
			{
				Debug.LogError("房间<" + GetRoomId() + ">没有指定自然对象瓦片层");
			}
			return null;
		}
		Vector2Int vector2Int = (isIndoorRoom ? roomPosition.SnapToGrid(DolocTransform.TILE_WORLD_SIZE) : scenePos.SnapToGrid(DolocTransform.TILE_WORLD_SIZE));
		Vector2Int rt = (isIndoorRoom ? (roomPosition + roomSize).SnapToGrid(DolocTransform.TILE_WORLD_SIZE) : (sceneSize.SnapToGrid(DolocTransform.TILE_WORLD_SIZE) + vector2Int));
		return new ResourceGenInfoSO((tilemap == null) ? envObjectLayer.GenEnvObjectConstraint(vector2Int, rt) : envObjectLayer.GenEnvObjectConstraint(tilemap, vector2Int, rt));
	}

	private TerrainConstraintSO CreateEnvObstacles(Vector2Int[] groundPositions, Vector2Int gridPos)
	{
		return RoomGeneratorUtils.GenEnvObstacles(groundPositions, envObstaclesLayer, gridPos);
	}

	public MonsterGenInfoSO CreateMonsterGenInfoSO(Tilemap baseMap, Vector2Int gridPos, Vector2Int gridSize)
	{
		RoomInfo orDefault = DolocConfig.Tables.TbRoom.GetOrDefault(GetRoomId());
		bool flag = orDefault != null && !orDefault.SpawnInfo.MonsterSpawnEntry.IsEmpty;
		if (monsterLayer == null)
		{
			if (flag)
			{
				Debug.LogError("房间<" + GetRoomId() + ">没有指定怪物瓦片层");
			}
			return null;
		}
		return baseMap.GenMonsterInfo(monsterLayer, gridPos, gridPos + gridSize, flag);
	}

	public ResourceGenInfoSO CreateVegetationGenInfoSO(Vector2Int[] groundPositions, Vector2Int gridPos)
	{
		RoomInfo orDefault = DolocConfig.Tables.TbRoom.GetOrDefault(GetRoomId());
		bool flag = orDefault != null && !orDefault.SpawnInfo.VegetationSpawnEntry.IsEmpty;
		if (vegetationLayer == null)
		{
			if (flag)
			{
				Debug.LogError("房间<" + GetRoomId() + ">没有指定植被瓦片层");
			}
			return null;
		}
		if (groundPositions.IsNullOrEmpty())
		{
			return null;
		}
		return new ResourceGenInfoSO(vegetationLayer.GenVegetationConstraint(groundPositions, gridPos));
	}

	private void OnChangeInDoor(bool value)
	{
		SnapAirWallToRoom();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (roomType == RoomType.Dungeon && DolocAPI.IsNormalState)
		{
			if (other.IsMotor() && DolocAPI.IsAgentRiding)
			{
				DolocAPI.TryEnterDungeonSubRoom(base.name);
			}
			else if (other.IsAgent() && !DolocAPI.IsAgentRiding)
			{
				DolocAPI.TryEnterDungeonSubRoom(base.name);
			}
		}
	}

	public string GetRoomId()
	{
		string text = base.gameObject.scene.name;
		string text2 = base.name;
		return base.gameObject.scene.name.Split("_")[0] switch
		{
			"farm" => text, 
			"city" => text, 
			"dungeon" => text + "." + text2, 
			_ => "无", 
		};
	}

	public void ResetGeometry()
	{
		Vector4 vector = RoomHandleUtils.CalculateRoomGeometry(base.gameObject);
		scenePos = new Vector2(vector.x, vector.y);
		sceneSize = new Vector2(vector.z, vector.w);
		OnScenePosChanged(scenePos);
		OnSceneSizeChanged(sceneSize);
	}

	private void OnScenePosChanged(Vector2 pos)
	{
		scenePos = new Vector2((float)Mathf.RoundToInt(pos.x / 1.5f) * 1.5f, (float)Mathf.RoundToInt(pos.y / 1.5f) * 1.5f);
		SnapAirWallToRoom();
	}

	private void OnSceneSizeChanged(Vector2 size)
	{
		SnapAirWallToRoom();
	}

	private void AutoFillAirWall()
	{
		if (airWallCollider != null)
		{
			Debug.LogWarning("当前碰撞体已经填充");
			return;
		}
		airWallCollider = GetComponent<EdgeCollider2D>();
		if (airWallCollider == null)
		{
			airWallCollider = base.gameObject.AddComponent<EdgeCollider2D>();
		}
	}

	private void SnapAirWallToRoom()
	{
		if (!lockColliderSize)
		{
			if (airWallCollider == null)
			{
				Debug.LogError("当前对象没有空气墙Collider");
			}
			else if (isIndoorRoom)
			{
				RoomHandleUtils.SnapEdgeColliderToRect(airWallCollider, roomPosition, roomSize, Vector2.zero);
			}
			else
			{
				RoomHandleUtils.SnapEdgeColliderToRect(airWallCollider, scenePos, sceneSize, new Vector2(airWallColliderPadding, airWallColliderPadding));
			}
		}
	}

	private void OnColliderPaddingChanged()
	{
		if (isIndoorRoom)
		{
			airWallColliderPadding = 0f;
		}
		SnapAirWallToRoom();
	}

	private float CalcGroundHeight(RoomGeometrySO geometry)
	{
		if (isIndoorRoom)
		{
			return 0f;
		}
		if (!RoomHandleUtils.CalcGroundHeightFromTilemap(geometry, out var groundHeight))
		{
			return 0f;
		}
		return groundHeight;
	}

	private void AutoSetGroundHeight()
	{
		RoomGeometrySO geometry;
		if (isIndoorRoom)
		{
			Debug.LogWarning("当前房间为室内房间，地板高度将被覆盖");
		}
		else if (tilemap == null)
		{
			Debug.LogError("当前对象没有Tilemap组件");
		}
		else if (!CreateGeometryOutdoor(out geometry))
		{
			Debug.LogError("创建地形几何信息失败");
		}
		else
		{
			floorHeight = CalcGroundHeight(geometry);
		}
	}

	private void SetRoomPositionBySprite()
	{
		Sprite mainSprite = MainSprite;
		if (mainSprite == null)
		{
			Debug.LogError("当前对象\"" + base.gameObject.name + "\"没有设置墙体贴图");
			return;
		}
		Vector2 vector = mainSprite.pivot / mainSprite.rect.size * mainSprite.bounds.size;
		roomPosition = (Vector2)base.transform.position - vector;
	}

	public void SetRoomSizeBySprite()
	{
		Sprite mainSprite = MainSprite;
		if (mainSprite == null)
		{
			Debug.LogError("当前对象\"" + base.gameObject.name + "\"没有设置墙体贴图");
			return;
		}
		Vector2 vector = mainSprite.pivot / mainSprite.rect.size * mainSprite.bounds.size;
		roomSize = (Vector2)mainSprite.bounds.size - vector;
	}

	private void OnRoomPosChanged(Vector2 pos)
	{
		roomPosition = new Vector2((float)Mathf.RoundToInt(pos.x / 0.125f) * 0.125f, (float)Mathf.RoundToInt(pos.y / 0.125f) * 0.125f);
		SnapAirWallToRoom();
	}

	private void OnRoomSizeChanged(Vector2 size)
	{
		roomSize = new Vector2((float)Mathf.RoundToInt(size.x / 0.125f) * 0.125f, (float)Mathf.RoundToInt(size.y / 0.125f) * 0.125f);
		SnapAirWallToRoom();
	}

	private bool CreateGeometryIndoor(out RoomGeometrySO geometry)
	{
		geometry = default(RoomGeometrySO);
		Vector2 tILE_WORLD_SIZE = DolocTransform.TILE_WORLD_SIZE;
		if (roomSize.x <= 0f || roomSize.y <= 0f)
		{
			Debug.LogError($"房间大小{roomSize}不合法");
			return false;
		}
		Vector2Int gridPos = roomPosition.SnapToGrid(tILE_WORLD_SIZE);
		Vector2Int gridSize = roomSize.SnapToGrid(tILE_WORLD_SIZE);
		Vector2 vector = ((tilemap != null) ? ((Vector2)tilemap.cellSize) : DolocTransform.TILE_WORLD_SIZE);
		Vector2Int[] obstacles = RoomHandleUtils.GetObstacles(tilemap, obstacleTilemap);
		Vector2Int[] extraObstacles = RoomHandleUtils.GetExtraObstacles(obstacleTilemap);
		Vector2Int[] groundPositions = obstacles.SearchGround(roomSize.SnapToGrid(vector), handleFloorLine: true);
		Vector2[] polygon = GetComponent<EdgeCollider2D>().points.Offset(base.transform.position);
		geometry = new RoomGeometrySO(scenePos, sceneSize, useCameraPadding ? new Vector4(cameraPaddingLeft, cameraPaddingRight, cameraPaddingBottom, cameraPaddingTop) : Vector4.zero, roomPosition, roomSize, gridPos, gridSize, defaultEntryPosition, obstacles, groundPositions, extraObstacles, polygon);
		return true;
	}

	private bool CreateGeometryOutdoor(out RoomGeometrySO geometry)
	{
		geometry = default(RoomGeometrySO);
		if (tilemap == null)
		{
			return false;
		}
		Vector2 vector = tilemap.cellSize;
		Vector2Int gridPos = scenePos.SnapToGrid(vector);
		Vector2Int vector2Int = sceneSize.SnapToGridCeil(vector);
		Vector2Int[] obstacles = RoomHandleUtils.GetObstacles(tilemap, obstacleTilemap, scenePos, sceneSize);
		Vector2Int[] extraObstacles = RoomHandleUtils.GetExtraObstacles(obstacleTilemap);
		Vector2Int[] groundPositions = obstacles.SearchGround(vector2Int);
		geometry = new RoomGeometrySO(scenePos, sceneSize, useCameraPadding ? new Vector4(cameraPaddingLeft, cameraPaddingRight, cameraPaddingBottom, cameraPaddingTop) : Vector4.zero, scenePos, sceneSize, gridPos, vector2Int, defaultEntryPosition, obstacles, groundPositions, extraObstacles);
		return true;
	}

	private bool CreateRoomGeometrySO(out RoomGeometrySO geometrySo)
	{
		if (!isIndoorRoom)
		{
			return CreateGeometryOutdoor(out geometrySo);
		}
		return CreateGeometryIndoor(out geometrySo);
	}

	public void DisableAllLevelsExcludeCurrent(Room room)
	{
		if (room.Type != RoomType.Farm || (room.IsInHouse && ((TemplateRoomInHouse)room).Building.maxLevel <= 0))
		{
			return;
		}
		_terrainExtensionLevels = base.gameObject.transform.parent.GetComponentsInChildren<RoomHandle>(includeInactive: true);
		if (!_terrainExtensionLevels.IsNullOrEmpty())
		{
			RoomHandle[] terrainExtensionLevels = _terrainExtensionLevels;
			for (int i = 0; i < terrainExtensionLevels.Length; i++)
			{
				GameObject obj = terrainExtensionLevels[i].gameObject;
				obj.SetActive(obj.name == room.baseProto.name);
			}
		}
	}

	private void DisableAllPresets()
	{
		ObjectPreset[] componentsInChildren = GetComponentsInChildren<ObjectPreset>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.SetActive(value: false);
		}
	}

	private void DisableAllControlLayers()
	{
		if (monsterLayer != null)
		{
			monsterLayer.gameObject.SetActive(value: false);
		}
		if (vegetationLayer != null)
		{
			vegetationLayer.gameObject.SetActive(value: false);
		}
		if (resourceLayerOther != null)
		{
			resourceLayerOther.gameObject.SetActive(value: false);
		}
		if (resourceLayerTree != null)
		{
			resourceLayerTree.gameObject.SetActive(value: false);
		}
		if (envObjectLayer != null)
		{
			envObjectLayer.gameObject.SetActive(value: false);
		}
	}

	public void AdjustAirWall(Vector2 scenePos, Vector2 sceneSize)
	{
		if (airWallCollider == null)
		{
			Debug.LogWarning("主农场空气墙未设置!");
			return;
		}
		sceneSize.y += 16.875f;
		Vector2[] points = new Vector2[5]
		{
			scenePos,
			scenePos + new Vector2(sceneSize.x, 0f),
			scenePos + sceneSize,
			scenePos + new Vector2(0f, sceneSize.y),
			scenePos
		};
		airWallCollider.points = points;
	}

	public T[] GetInteractableByType<T>() where T : InteractableObject
	{
		if (interactableObjects == null)
		{
			return null;
		}
		List<T> result = new List<T>();
		interactableObjects.ForEach(delegate(InteractableObject obj)
		{
			if (obj is T item)
			{
				result.Add(item);
			}
		});
		return result.ToArray();
	}

	private void InitWaters(GameObject roomObject)
	{
		try
		{
			_waterHandles = roomObject.GetComponentsInChildren<WaterHandle>(includeInactive: true);
			if (!_waterHandles.IsNullOrEmpty())
			{
				WaterHandle[] waterHandles = _waterHandles;
				for (int i = 0; i < waterHandles.Length; i++)
				{
					waterHandles[i].GenerateWater();
				}
			}
		}
		catch (Exception ex)
		{
			Debug.Log("初始化水体时遇到异常");
			Debug.LogError(ex);
			Debug.LogError(ex.StackTrace);
		}
	}

	private void ForeachInteractableObjects(Action<InteractableObject> action)
	{
		if (interactableObjects == null)
		{
			return;
		}
		InteractableObject[] array = interactableObjects;
		foreach (InteractableObject interactableObject in array)
		{
			if (interactableObject != null)
			{
				action?.Invoke(interactableObject);
			}
		}
	}

	private void UpdateWaters()
	{
		if (_waterHandles == null)
		{
			return;
		}
		WaterHandle[] waterHandles = _waterHandles;
		foreach (WaterHandle waterHandle in waterHandles)
		{
			if (!(waterHandle.Renderer == null))
			{
				waterHandle.Renderer.GenerateBubbles();
			}
		}
	}

	private void ClearWaters()
	{
		if (!_waterHandles.IsNullOrEmpty())
		{
			WaterHandle[] waterHandles = _waterHandles;
			for (int i = 0; i < waterHandles.Length; i++)
			{
				waterHandles[i].ClearWater();
			}
		}
	}

	public virtual void OnLoadScene()
	{
	}

	public virtual void OnEnterRoom(Room room)
	{
		if (_timeAffectableManager == null)
		{
			_timeAffectableManager = new TimeAffectableManager(base.gameObject, room.Type != RoomType.Dungeon);
		}
		DisableAllLevelsExcludeCurrent(room);
		DisableAllControlLayers();
		DisableAllPresets();
		DolocAPI.ppm.SetupRoomPostProcessingEffects(postProcessingEffects);
	}

	public void Render(Room room)
	{
		Debug.Log("场景控制柄\"" + room.baseProto.name + "\"内容渲染");
		if (_timeAffectableManager == null)
		{
			_timeAffectableManager = new TimeAffectableManager(base.gameObject, room.Type != RoomType.Dungeon);
		}
		InitWaters(GameObject);
		ForeachInteractableObjects(delegate(InteractableObject obj)
		{
			obj.Render(room);
		});
		this.RebuildCellarBuildingGates(room);
	}

	public void ClearRender()
	{
		ForeachInteractableObjects(delegate(InteractableObject obj)
		{
			obj.UnRender();
		});
		_timeAffectableManager.Dispose();
		_timeAffectableManager = null;
		ClearWaters();
		DolocAPI.EntitySystem.Clear<BuildingGate>();
	}

	public virtual void BeforeTimePass()
	{
	}

	public virtual void AfterTimePass()
	{
		ForeachInteractableObjects(delegate(InteractableObject obj)
		{
			obj.Render(DolocAPI.CurrentRoom);
		});
		_timeAffectableManager.AfterTimePass();
	}

	public virtual void UpdatePerTu()
	{
		_timeAffectableManager.UpdatePerTu();
		UpdateWaters();
	}

	public virtual void UpdatePerHour(int hour)
	{
		_timeAffectableManager.UpdatePerHour(hour);
	}

	public virtual void OnWeatherChanged(WeatherType weather)
	{
		_timeAffectableManager.OnWeatherChanged(weather);
	}

	public void OnMonthlyRefresh()
	{
		RefreshWaters();
	}

	public void RefreshWaters()
	{
		if (_waterHandles != null)
		{
			WaterHandle[] waterHandles = _waterHandles;
			foreach (WaterHandle obj in waterHandles)
			{
				obj.ClearWater();
				obj.GenerateWater();
			}
		}
	}

	public void ResetWaterResolutions(Vector2 resolution)
	{
		if (!_waterHandles.IsNullOrEmpty())
		{
			WaterHandle[] waterHandles = _waterHandles;
			for (int i = 0; i < waterHandles.Length; i++)
			{
				waterHandles[i].ResetResolution(resolution);
			}
		}
	}

	private void DrawGizmosDebug()
	{
		if (base.gameObject.name != drawRoomName)
		{
			return;
		}
		Vector2 vector = (isIndoorRoom ? roomPosition : scenePos);
		if (obstaclesCache == null)
		{
			return;
		}
		Vector2Int[] array = obstaclesCache;
		for (int i = 0; i < array.Length; i++)
		{
			GizmosHelper.DrawBoxLB(array[i] * cellSize + vector, cellSize, DolocColor.white);
		}
		if (groundPositionsCache != null)
		{
			array = groundPositionsCache;
			for (int i = 0; i < array.Length; i++)
			{
				GizmosHelper.DrawBoxLB(array[i] * cellSize + vector, cellSize, Color.cyan);
			}
		}
	}

	private void LoadObstacles()
	{
		drawRoomName = base.gameObject.name;
		Vector2 v = (isIndoorRoom ? roomSize : sceneSize);
		obstaclesCache = RoomHandleUtils.GetObstacles(tilemap, obstacleTilemap, scenePos, v);
		groundPositionsCache = obstaclesCache.SearchGround(v.SnapToGrid(cellSize), isIndoorRoom);
	}

	protected virtual void OnDrawGizmos()
	{
		if (!disableGizmos)
		{
			GizmosHelper.DrawBoxLB(scenePos, sceneSize, DolocColor.eyecatchUiColor_Cyan);
			if (isIndoorRoom)
			{
				GizmosHelper.DrawBoxLB(roomPosition, roomSize, DolocColor.orange);
			}
			GizmosHelper.DrawBoxMB(defaultEntryPosition + roomPosition, new Vector2(1.8f, 3f), DolocColor.blue);
			DrawGizmosDebug();
		}
	}
}
